import 'api.dart';

class SpotifyApiResult {
  final SpotifyApi? api;
  final int? errorCode;

  SpotifyApiResult({required this.api, required this.errorCode});
}

class SpotifyAccessToken {
  final String token;
  final Set<String> scope;
  final Duration expiresIn;

  SpotifyAccessToken({
    required this.token,
    required this.scope,
    required this.expiresIn,
  });
}

class SpotifyRequest {
  final String unencodedUriPath;
  final Map<String, String> queryParameters;

  SpotifyRequest({
    required this.unencodedUriPath,
    required this.queryParameters,
  });
}

class SpotifyResponse {
  final int statusCode;
  final String body;

  SpotifyResponse({
    required this.statusCode,
    required this.body,
  });
}
