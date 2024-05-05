import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_background_service/flutter_background_service.dart';
import 'package:n_disco_plus/background_task/background_task.dart';
import 'package:n_disco_plus/core/config.dart';

class SyncLayout extends StatefulWidget {
  const SyncLayout({super.key});

  @override
  State<SyncLayout> createState() => _SyncLayoutState();
}

class _SyncLayoutState extends State<SyncLayout> {
  bool isTogglingService = false;
  bool serviceRunning = false;
  late final FlutterBackgroundService backgroundService;

  @override
  void initState() {
    super.initState();

    if (Platform.isAndroid) {
      backgroundService = FlutterBackgroundService()
        ..configure(
          iosConfiguration: IosConfiguration(),
          // ^^^^ needs to be implemented when iOS support arrives
          androidConfiguration: AndroidConfiguration(
            autoStart: true,
            autoStartOnBoot: false,
            isForegroundMode: true,
            onStart: BackgroundService.startNew,
          ),
        );
    } else if (Platform.isWindows) {}
  }

  @override
  Widget build(BuildContext context) {
    return Center(
      child: ElevatedButton(
        onPressed: isTogglingService ? null : _toggleService,
        child: Text("${serviceRunning ? 'Stop' : 'Start'} Sync"),
      ),
    );
  }

  Future<void> _toggleService() async {
    setState(() {
      isTogglingService = true;
    });

    final String spotifyRefreshToken = Config.of(context).spotifyRefreshToken!;

    backgroundService.invoke("is_running");
    final callback = await backgroundService.on("is_running_callback").first;
    final bool serviceRunning;
    if (callback!["running"]) {
      // stop service
      backgroundService.invoke("stop");
      // wait for stop callback
      // after this we can be certain that the background service has actually stopped
      // and thus we can ask it to start again
      await backgroundService.on("stop_callback").first;
      serviceRunning = false;
    } else {
      // startup feels too quick
      await Future.delayed(const Duration(milliseconds: 500));

      backgroundService.invoke(
        "start",
        {
          "spotifyRefreshToken": spotifyRefreshToken,
        },
      );
      // start callback isn't really necessary, but I added it for consistency
      await backgroundService.on("start_callback").first;
      serviceRunning = true;
    }

    setState(() {
      isTogglingService = false;
      this.serviceRunning = serviceRunning;
    });
  }
}
