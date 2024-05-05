import 'package:flutter/material.dart';
import 'package:n_disco_plus/core/config.dart';
import 'package:n_disco_plus/pages/bridge_page.dart';
import 'package:n_disco_plus/pages/home_page/home_page.dart';
import 'package:n_disco_plus/pages/lights_page.dart';
import 'package:n_disco_plus/pages/settings_page.dart';
import 'package:protocol_handler/protocol_handler.dart';
import 'package:shared_preferences/shared_preferences.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  await protocolHandler.register("ndiscoplus");

  SharedPreferences prefs = await SharedPreferences.getInstance();

  runApp(
    MainApp(
      prefs: prefs,
    ),
  );
}

class MainApp extends StatelessWidget {
  final SharedPreferences prefs;

  const MainApp({
    super.key,
    required this.prefs,
  });

  // This widget is the root of your application.
  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'NDiscoPlus',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.greenAccent),
        useMaterial3: true,
      ),
      home: _AppServices(
        prefs: prefs,
        child: const _AppLayout(),
      ),
    );
  }
}

class _AppServices extends StatelessWidget {
  final SharedPreferences prefs;
  final Widget child;

  const _AppServices({
    required this.prefs,
    required this.child,
  });

  @override
  Widget build(BuildContext context) {
    return ConfigWidget(
      prefs: prefs,
      child: child,
    );
  }
}

class _AppLayout extends StatefulWidget {
  const _AppLayout();

  @override
  State<_AppLayout> createState() => _AppLayoutState();
}

class _AppLayoutState extends State<_AppLayout>
    with SingleTickerProviderStateMixin {
  int pageIndex = 0;
  static const List<(String, IconData, Widget)> pages = [
    ("Home", Icons.home, HomePage()),
    ("Bridges", Icons.hub, BridgePage()),
    ("Lights", Icons.lightbulb, LightsPage()),
    ("Settings", Icons.settings, SettingsPage()),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: pages[pageIndex].$3,
      bottomNavigationBar: NavigationBar(
        selectedIndex: pageIndex,
        destinations: pages
            .map(
              (e) => NavigationDestination(
                icon: Icon(e.$2),
                label: e.$1,
              ),
            )
            .toList(),
        onDestinationSelected: (i) => setState(() => pageIndex = i),
      ),
    );
  }
}
