class SpotifyPlaybackState {}

abstract class SpotifyPlayerBase {
  String get playerDisplayName;

  Stream<SpotifyPlaybackState> listenState();
}
