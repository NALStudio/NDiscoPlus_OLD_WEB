import 'package:flutter/material.dart';
import 'package:n_disco_plus/core/config.dart';

import './_connect_spotify_layout.dart';
import './_sync_layout.dart';

class HomePage extends StatelessWidget {
  const HomePage({super.key});

  @override
  Widget build(BuildContext context) {
    final config = Config.of(context);

    if (config.spotifyRefreshToken == null) {
      return const ConnectSpotifyLayout();
    } else {
      return const SyncLayout();
    }
  }
}
