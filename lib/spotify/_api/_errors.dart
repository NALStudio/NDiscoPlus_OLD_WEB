class SpotifyError implements Exception {
  final String message;

  SpotifyError() : message = "<no message>";
  SpotifyError.message(this.message);
  SpotifyError.statusCode(int statusCode) : message = "status code $statusCode";

  @override
  String toString() => "SpotifyError: $message";
}

class SpotifyApiError extends SpotifyError {
  SpotifyApiError(super.message) : super.message();

  @override
  String toString() => "SpotifyApiError: $message";
}
