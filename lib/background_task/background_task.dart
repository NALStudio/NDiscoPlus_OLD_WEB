import 'dart:async';
import 'dart:convert';

import 'package:flutter_background_service/flutter_background_service.dart';
import 'package:n_disco_plus/constants.dart';
import 'package:n_disco_plus/spotify/spotify.dart';

class _StartArgs {
  final String spotifyRefreshToken;

  _StartArgs({required this.spotifyRefreshToken});
}

class BackgroundService {
  bool running = false;
  bool isStopped = false;

  static BackgroundService startNew(ServiceInstance service) =>
      BackgroundService().._onStart(service);

  void _onStart(ServiceInstance service) {
    service.on("start").listen((event) {
      if (running) return;

      running = true;
      _run(
        service,
        _StartArgs(
          spotifyRefreshToken: event!["spotifyRefreshToken"],
        ),
      );
    });

    service.on("stop").listen((event) {
      if (!running) return;

      running = false;
    });

    // poll running status
    service.on("is_running").listen((event) {
      service.invoke("is_running_callback", {"running": running});
    });
  }

  void _run(ServiceInstance service, _StartArgs args) async {
    SpotifyApi spotify = SpotifyApi(
      refreshToken: args.spotifyRefreshToken,
      scope: SpotifyConstants.scope,
    );

    // startup finished
    service.invoke("start_callback");

    while (running) {
      final response = await spotify.send(
        SpotifyRequest(
          unencodedUriPath: "/me/player",
          queryParameters: const {},
        ),
      );
      print(json.encode(response.data));
      await Future.delayed(const Duration(seconds: 5));
    }

    // stop finished
    service.invoke("stop_callback");
  }
}
