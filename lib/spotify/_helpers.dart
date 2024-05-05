import 'dart:convert';
import 'dart:math';
import 'dart:typed_data';

import 'package:n_disco_plus/env/env.dart';

class SpotifyHelpers {
  static final Random _secureRandom = Random.secure();

  static String generateRandomSecureString(int length) {
    const String possible =
        'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
    return Iterable<String>.generate(
      length,
      (_) => possible[_secureRandom.nextInt(possible.length)],
    ).join();
  }

  static String getClientAuthorizationHeader() {
    String clientId = Environment.current.spotifyClientId;
    String clientSecret = Environment.current.spotifyClientSecret;
    Uint8List bytes = utf8.encode("$clientId:$clientSecret");
    return "Basic ${base64.encode(bytes)}";
  }
}
