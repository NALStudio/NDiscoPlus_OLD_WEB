import 'package:flutter/material.dart';

class LoadingDialogue extends StatelessWidget {
  final String? message;

  const LoadingDialogue({super.key, this.message});

  @override
  Widget build(BuildContext context) {
    return Dialog(
      child: Padding(
        padding: const EdgeInsets.all(8.0),
        child: Row(
          children: [
            const Padding(
              padding: EdgeInsets.all(8.0),
              child: CircularProgressIndicator(),
            ),
            Padding(
              padding: const EdgeInsets.all(8.0),
              child: Text(message ?? "Loading..."),
            ),
          ],
        ),
      ),
    );
  }

  static Future<void> show(BuildContext context, {String? message}) {
    return showDialog(
      context: context,
      barrierDismissible: false,
      builder: (_) => LoadingDialogue(message: message),
    );
  }
}
