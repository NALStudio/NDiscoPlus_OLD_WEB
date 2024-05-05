import 'package:flutter/material.dart';
import 'package:n_disco_plus/core/config.dart';

import '_api/api.dart';

class SpotifyHandler extends StatefulWidget {
  final Widget child;
  final SpotifyApi? initialInstance;

  const SpotifyHandler({
    super.key,
    this.initialInstance,
    required this.child,
  });

  @override
  State<SpotifyHandler> createState() => SpotifyHandlerState();

  static SpotifyHandlerState? maybeOf(BuildContext context) {
    return context
        .dependOnInheritedWidgetOfExactType<_SpotifyHandlerInherited>()
        ?.parent;
  }

  static SpotifyHandlerState of(BuildContext context) {
    SpotifyHandlerState? value = maybeOf(context);
    assert(value != null, "No Shared Spotify API instances found.");
    return value!;
  }
}

class SpotifyHandlerState extends State<SpotifyHandler> {
  late SpotifyApi? _api;
  SpotifyApi? get api => _api;

  void setInstance(SpotifyApi api) {
    if (api.onTokenRefreshFailed != null) {
      throw ArgumentError("API's onTokenRefreshFailed should be null.");
    }
    api.onTokenRefreshFailed = _handleRefreshFail;

    if (_api != null) {
      _api!.refreshToken.removeListener(_updateRefreshToken);
    }
    api.refreshToken.addListener(_updateRefreshToken);

    setState(() => _api = api);
    _updateRefreshToken();
  }

  void _handleRefreshFail(int errorCode) {
    setState(() {
      _api = null;
    });
  }

  @override
  void dispose() {
    _api?.refreshToken.removeListener(_updateRefreshToken);
    super.dispose();
  }

  void _updateRefreshToken() {
    final config = Config.of(context);

    final String refreshToken = api!.refreshToken.value;
    // If check because we might use the same token after setInstance
    // Possibly not needed, but I'm too lazy to test this
    // and one if check vs. 1000 build calls... the first one seems better
    if (config.spotifyRefreshToken != refreshToken) {
      config.spotifyRefreshToken = refreshToken;
    }
  }

  @override
  void initState() {
    _api = widget.initialInstance;
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return _SpotifyHandlerInherited(parent: this, child: widget.child);
  }
}

class _SpotifyHandlerInherited extends InheritedWidget {
  final SpotifyHandlerState parent;

  const _SpotifyHandlerInherited({required this.parent, required super.child});

  @override
  bool updateShouldNotify(covariant InheritedWidget oldWidget) {
    return true;
  }
}
