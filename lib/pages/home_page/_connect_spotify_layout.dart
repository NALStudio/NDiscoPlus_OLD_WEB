import 'package:flutter/material.dart';
import 'package:n_disco_plus/components/dialogues/_loading_dialogue.dart';
import 'package:n_disco_plus/constants.dart';
import 'package:n_disco_plus/core/config.dart';
import 'package:n_disco_plus/env/env.dart';
import 'package:n_disco_plus/spotify/_widget.dart';
import 'package:n_disco_plus/spotify/spotify.dart';
import 'package:protocol_handler/protocol_handler.dart';
import 'package:url_launcher/url_launcher.dart';

class ConnectSpotifyLayout extends StatefulWidget {
  const ConnectSpotifyLayout({super.key});

  @override
  State<ConnectSpotifyLayout> createState() => _ConnectSpotifyLayoutState();
}

class _ConnectSpotifyLayoutState extends State<ConnectSpotifyLayout>
    with ProtocolListener {
  String? previousAuthorizationState;

  late bool refreshTokenLoginPending;

  @override
  void initState() {
    protocolHandler.addListener(this);
    super.initState();

    refreshTokenLoginPending = true;
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _refreshTokenLogin().then((_) {
        setState(() => refreshTokenLoginPending = false);
      });
    });
  }

  @override
  void dispose() {
    protocolHandler.removeListener(this);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Center(
      child: ElevatedButton(
        onPressed: refreshTokenLoginPending ? null : _loginSpotify,
        child: const Text("Login"),
      ),
    );
  }

  Future<void> _refreshTokenLogin() async {
    final String? refreshToken = Config.of(context).spotifyRefreshToken;
    if (refreshToken == null) {
      return;
    }

    SpotifyApiResult res = await SpotifyApi.fromRefresh(refreshToken);
    if (res.errorCode != null) {
      return;
    }

    if (mounted) {
      SpotifyHandler.of(context).setInstance(res.api!);
    }
  }

  @override
  void onProtocolUrlReceived(String url) {
    final Uri uri = Uri.parse(url);
    final Uri uriWithoutArgs = Uri.parse(url, 0, url.indexOf('?'));
    if (uriWithoutArgs != SpotifyConstants.redirectUri) return;

    String? state = uri.queryParameters["state"];
    if (state == null || state != previousAuthorizationState) return;

    // this authorization state was used in this login
    previousAuthorizationState = null;
    _spotifyLoginCallbackReceived(uri);
  }

  void _loginSpotify() {
    LoadingDialogue.show(context, message: "Logging in...");

    final String state = SpotifyHelpers.generateRandomSecureString(16);
    previousAuthorizationState = state;

    final Uri uri = Uri.https(
      "accounts.spotify.com",
      "/authorize",
      <String, String>{
        "response_type": "code",
        "client_id": Environment.current.spotifyClientId,
        "scope": SpotifyConstants.scope.join(' '),
        "redirect_uri": SpotifyConstants.redirectUri.toString(),
        "state": state,
      },
    );

    launchUrl(uri);
  }

  static void _loginErrored(BuildContext context, String error) {
    Navigator.pop(context);
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text("Login Error"),
        content: Text(error),
      ),
    );
  }

  void _spotifyLoginCallbackReceived(Uri uri) async {
    String? error = uri.queryParameters["error"];
    if (error != null) {
      _loginErrored(context, error);
      return;
    }

    String authorizationCode = uri.queryParameters["code"]!;

    SpotifyApiResult res =
        await SpotifyApi.fromAuthorization(authorizationCode);
    if (res.errorCode != null) {
      if (mounted) {
        _loginErrored(context, "Error code: ${res.errorCode}");
      }
      return;
    }

    if (mounted) {
      SpotifyHandler.of(context).setInstance(res.api!);
      Navigator.pop(context);
    }
  }
}
