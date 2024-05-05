import 'dart:collection';

class SpotifyConstants {
  static final Uri redirectUri =
      Uri.parse("ndiscoplus://spotify_login_callback/");
  static final Set<String> scope = UnmodifiableSetView(const <String>{
    "user-read-playback-state",
    "app-remote-control",
  });
}
