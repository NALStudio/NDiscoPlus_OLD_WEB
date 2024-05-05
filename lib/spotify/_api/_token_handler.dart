import 'dart:collection';
import 'dart:convert';

import 'api.dart';
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

  SpotifyAccessToken getAccessToken() => SpotifyAccessToken(
        token: accessToken,
        scope: UnmodifiableSetView(scope.split(' ').toSet()),
        expiresIn: expiresIn,
      );
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

Future<SpotifyApiResult> createApi({
  required String grantType,
  required Map<String, String> extraQueryParameters,
  required String? oldRefreshToken,
}) async {
  final SpotifyTokenResponse tokenResponse = await getToken(
    grantType: grantType,
    extraQueryParameters: extraQueryParameters,
    oldRefreshToken: oldRefreshToken,
  );

  if (tokenResponse.hasErrored) {
    return SpotifyApiResult(
      api: null,
      errorCode: tokenResponse.statusCode,
    );
  }

  final SpotifyToken token = tokenResponse.token!;

  return SpotifyApiResult(
    api: SpotifyApi(
      accessToken: token.getAccessToken(),
      refreshToken: token.refreshToken ?? oldRefreshToken!,
    ),
    errorCode: null,
  );
}

Future<SpotifyTokenResponse> getToken({
  required String grantType,
  required Map<String, String> extraQueryParameters,
  required String? oldRefreshToken,
}) async {
  final response = await http.post(
    Uri.https(
      "accounts.spotify.com",
      "/api/token",
      <String, String>{
        "grant_type": grantType,
        ...extraQueryParameters,
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
