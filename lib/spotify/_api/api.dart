import 'dart:async';

import 'package:flutter/material.dart';
import 'package:n_disco_plus/constants.dart';

import '_models.dart';
export '_models.dart';

import '_token_handler.dart' as token_handler;

import 'package:http/http.dart' as http;

import 'dart:developer' as developer;

class SpotifyApi {
  SpotifyAccessToken? _accessToken;
  SpotifyAccessToken get accessToken {
    if (_accessToken == null) throw StateError("No access token!");
    return _accessToken!;
  }

  final ValueNotifier<String> refreshToken;

  void Function(int errorCode)? onTokenRefreshFailed;

  Future<void>? _automaticTokenRefresh;

  SpotifyApi({
    required SpotifyAccessToken accessToken,
    required String refreshToken,
    this.onTokenRefreshFailed,
  })  : refreshToken = ValueNotifier(refreshToken),
        _accessToken = accessToken;

  static Future<SpotifyApiResult> fromAuthorization(String authorizationCode) {
    return token_handler.createApi(
      grantType: "authorization_code",
      extraQueryParameters: {
        "code": authorizationCode,
        "redirect_uri": SpotifyConstants.redirectUri.toString(),
      },
      oldRefreshToken: null,
    );
  }

  static Future<SpotifyApiResult> fromRefresh(String refreshToken) {
    return token_handler.createApi(
      grantType: "refresh_token",
      extraQueryParameters: {
        "refresh_token": refreshToken,
      },
      oldRefreshToken: refreshToken,
    );
  }

  /// Force refresh the API with a new access token.
  /// If token refresh fails, all scheduled requests are errored.
  Future<void> refresh() async {
    final String refreshToken = this.refreshToken.value;

    final token_handler.SpotifyTokenResponse tokenResponse =
        await token_handler.getToken(
      grantType: "refresh_token",
      extraQueryParameters: {"refresh_token": refreshToken},
      oldRefreshToken: refreshToken,
    );

    if (tokenResponse.hasErrored) {
      _accessToken = null;
      // accessToken is null so all requests should now fail
      if (onTokenRefreshFailed != null) {
        onTokenRefreshFailed!(tokenResponse.statusCode);
      }
      return;
    }

    final token_handler.SpotifyToken token = tokenResponse.token!;

    _accessToken = token.getAccessToken();
    if (token.refreshToken != null) {
      this.refreshToken.value = token.refreshToken!;
    }
  }

  Future<SpotifyResponse> send(SpotifyRequest request) async {
    final String? accessToken = _accessToken?.token;
    if (accessToken == null) {
      throw StateError("No access token!");
    }

    final response = await http.get(
      Uri.https(
        "api.spotify.com",
        request.unencodedUriPath,
        request.queryParameters,
      ),
      headers: {
        "Authorization": "Bearer $accessToken",
      },
    );

    bool retry;
    switch (response.statusCode) {
      case 401:
        developer.log(
          "Automatic access token refresh (401)",
          name: "SpotifyApi",
        );
        _automaticTokenRefresh ??= refresh();
        await _automaticTokenRefresh;
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
      return await send(request);
    } else {
      return SpotifyResponse(
        statusCode: response.statusCode,
        body: response.body,
      );
    }
  }
}
