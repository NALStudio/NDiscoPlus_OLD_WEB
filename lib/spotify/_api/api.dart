import 'dart:async';
import 'dart:collection';
import 'dart:convert';

import 'package:flutter/foundation.dart';

import 'package:http/http.dart' as http;

import 'dart:developer' as developer;

export '_token_handling.dart';
import '_token_handling.dart' as token_handling;

export '_errors.dart';
import '_errors.dart';

class _SpotifyAccessToken {
  final String token;
  final Set<String> scope;
  final Duration expiresIn;

  _SpotifyAccessToken({
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
  final Map<String, dynamic> data;

  /// Microseconds
  /// Can be used to improve the accuracy of synchronized content
  final int sendTimestamp;

  /// Microseconds
  /// Can be used to improve the accuracy of synchronized content
  final int receiveTimestamp;

  SpotifyResponse({
    required this.statusCode,
    required this.data,
    required this.sendTimestamp,
    required this.receiveTimestamp,
  });
}

class SpotifyApi {
  static const kMaxRetries = 5;

  Future<void>? __accessTokenUpdate;
  _SpotifyAccessToken? __accessToken;

  final Set<String> scope;
  final ValueNotifier<String> refreshToken;

  SpotifyApi({
    required String refreshToken,
    required Set<String> scope,
  })  : refreshToken = ValueNotifier(refreshToken),
        scope = UnmodifiableSetView(Set.from(scope));

  Future<_SpotifyAccessToken> _getAccessToken() async {
    if (__accessToken == null) {
      await _refreshAccessToken();
    }

    return __accessToken!;
  }

  /// Refresh access token
  /// If another refresh is already in progress,
  /// wait until it is complete and return.
  Future<void> _refreshAccessToken() async {
    __accessTokenUpdate ??=
        __unsafeRefreshAccessToken().then((_) => __accessTokenUpdate = null);
    await __accessTokenUpdate;
  }

  /// Refresh access token
  /// NOT THREAD-SAFE!
  Future<void> __unsafeRefreshAccessToken() async {
    final response = await token_handling.SpotifyTokenRequest.refresh(
      refreshToken: refreshToken.value,
    ).send();

    if (response.hasErrored) {
      throw SpotifyError.statusCode(response.statusCode);
    }

    final token_handling.SpotifyToken token = response.token!;
    __accessToken = _SpotifyAccessToken(
      token: token.accessToken,
      scope: UnmodifiableSetView(token.scope.split(' ').toSet()),
      expiresIn: token.expiresIn,
    );

    if (!setEquals(__accessToken!.scope, scope)) {
      throw SpotifyApiError("Invalid access token scope.");
    }
  }

  Future<SpotifyResponse> send(SpotifyRequest request) {
    return _send(
      request,
      depth: 0,
    );
  }

  Future<SpotifyResponse> _send(
    SpotifyRequest request, {
    required int depth,
  }) async {
    if (depth > kMaxRetries) {
      throw SpotifyApiError("Max retries ($depth) exceeded!");
    }
    assert(request.unencodedUriPath.startsWith('/'));

    final _SpotifyAccessToken accessToken = await _getAccessToken();

    final int sendTimestampMicro = DateTime.timestamp().microsecondsSinceEpoch;
    final response = await http.get(
      Uri.https(
        "api.spotify.com",
        "/v1${request.unencodedUriPath}",
        request.queryParameters,
      ),
      headers: {
        "Authorization": "Bearer ${accessToken.token}",
      },
    );
    final int receiveTimestampMicro =
        DateTime.timestamp().microsecondsSinceEpoch;

    bool retry;
    switch (response.statusCode) {
      case 401:
        developer.log(
          "Automatic access token refresh (401)",
          name: "SpotifyApi",
        );
        await _refreshAccessToken();
        retry = true;
      case 429:
        final String? delaySeconds = response.headers["Retry-After"];
        final int delay;
        final String delayMsg;
        if (delaySeconds != null) {
          delay = int.parse(delaySeconds);
          delayMsg = delay.toString();
        } else {
          delay = 5;
          delayMsg = "$delay (default)";
        }
        developer.log(
          "Rate limit exceeded! Waiting for $delayMsg seconds...",
          name: "SpotifyApi",
        );
        await Future.delayed(Duration(seconds: delay));
        retry = true;
      default:
        retry = false;
    }

    if (retry) {
      return await _send(
        request,
        depth: depth + 1,
      );
    } else {
      return SpotifyResponse(
        statusCode: response.statusCode,
        data: json.decode(response.body),
        sendTimestamp: sendTimestampMicro,
        receiveTimestamp: receiveTimestampMicro,
      );
    }
  }
}
