import 'dart:convert';

import '../_helpers.dart';
import 'package:http/http.dart' as http;

class SpotifyToken {
  final String accessToken;
  final String scope;
  final Duration expiresIn;
  final String? refreshToken;

  SpotifyToken({
    required this.accessToken,
    required this.scope,
    required this.expiresIn,
    required this.refreshToken,
  });
}

class SpotifyTokenResponse {
  final int statusCode;
  bool get hasErrored => statusCode != 200;

  final SpotifyToken? token;

  SpotifyTokenResponse({
    required this.statusCode,
    required this.token,
  });
}

class SpotifyTokenRequest {
  final String _grantType;
  final Map<String, String> _extraQueryParameters;

  SpotifyTokenRequest.authorize({
    required String authorizationCode,
    required String redirectUri,
  })  : _grantType = "authorization_code",
        _extraQueryParameters = {
          "code": authorizationCode,
          "redirect_uri": redirectUri,
        };

  SpotifyTokenRequest.refresh({required String refreshToken})
      : _grantType = "refresh_token",
        _extraQueryParameters = {
          "refresh_token": refreshToken,
        };

  Future<SpotifyTokenResponse> send() async {
    final response = await http.post(
      Uri.https(
        "accounts.spotify.com",
        "/api/token",
        <String, String>{
          "grant_type": _grantType,
          ..._extraQueryParameters,
        },
      ),
      headers: {
        "Authorization": SpotifyHelpers.getClientAuthorizationHeader(),
        "Content-Type": "application/x-www-form-urlencoded",
      },
    );

    if (response.statusCode != 200) {
      return SpotifyTokenResponse(
        statusCode: response.statusCode,
        token: null,
      );
    }

    final Map<String, dynamic> data = json.decode(response.body);
    final String accessToken = data["access_token"];

    final String tokenType = data["token_type"];
    assert(tokenType == "Bearer");
    final String scope = data["scope"];

    final int expiresIn = data["expires_in"];
    final String? refreshToken = data["refresh_token"];

    return SpotifyTokenResponse(
      statusCode: response.statusCode,
      token: SpotifyToken(
        accessToken: accessToken,
        scope: scope,
        expiresIn: Duration(seconds: expiresIn),
        refreshToken: refreshToken,
      ),
    );
  }
}