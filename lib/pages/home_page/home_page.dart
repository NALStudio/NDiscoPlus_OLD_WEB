import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:n_disco_plus/constants.dart';
import 'package:n_disco_plus/spotify/_widget.dart';

import './_connect_spotify_layout.dart';
import './_sync_layout.dart';

class HomePage extends StatelessWidget {
  const HomePage({super.key});

  @override
  Widget build(BuildContext context) {
    final spotify = SpotifyHandler.of(context);

    if (spotify.api == null) {
      return const ConnectSpotifyLayout();
    }

    // detect when our scope should be updated with a new authorization
    // so when we in the future need to expand the applications scope,
    // we have code that handles this already
    bool apiValid = setEquals(
      spotify.api!.accessToken.scope,
      SpotifyConstants.scope,
    );
    if (!apiValid) {
      return const ConnectSpotifyLayout();
    }

    return const SyncLayout();
  }
}
