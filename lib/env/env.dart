import 'package:flutter/foundation.dart';

import '_debug.dart' as debug;
import '_release.dart' as release;

@immutable
abstract class Environment {
  const Environment();

  String get spotifyClientId;
  String get spotifyClientSecret;

  static Environment get current {
    if (kReleaseMode) {
      return const release.ReleaseEnvironment();
    }
    return const debug.DebugEnvironment();
  }
}
