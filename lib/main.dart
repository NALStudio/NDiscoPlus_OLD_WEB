import 'package:flutter/material.dart';
import 'package:n_disco_plus/pages/bridge_page.dart';
import 'package:n_disco_plus/pages/home_page.dart';
import 'package:n_disco_plus/pages/lights_page.dart';
import 'package:n_disco_plus/pages/settings_page.dart';

void main() {
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  // This widget is the root of your application.
  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'NDiscoPlus',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.greenAccent),
        useMaterial3: true,
      ),
      home: const MainLayout(),
    );
  }
}

class MainLayout extends StatefulWidget {
  const MainLayout({super.key});

  @override
  State<MainLayout> createState() => _MainLayoutState();
}

class _MainLayoutState extends State<MainLayout>
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
