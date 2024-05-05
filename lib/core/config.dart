import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';

abstract class Config {
  static final defaultConfig = _DefaultConfig();

  String? get spotifyRefreshToken;
  set spotifyRefreshToken(String? value);

  static Config? maybeOf(BuildContext context) {
    return context
        .dependOnInheritedWidgetOfExactType<_ConfigInherited>()
        ?.parent;
  }

  static Config of(BuildContext context) {
    final Config? result = maybeOf(context);
    assert(result != null, "No config found in context.");
    return result!;
  }
}

class _DefaultConfig implements Config {
  void _throw() => throw StateError("Cannot modify default config.");

  // Should probably be stored more securely but I can't be bothered
  @override
  String? get spotifyRefreshToken => null;
  @override
  set spotifyRefreshToken(String? value) => _throw();
}

class ConfigWidget extends StatefulWidget {
  final Widget child;
  final SharedPreferences _prefs;

  const ConfigWidget({
    super.key,
    required SharedPreferences prefs,
    required this.child,
  }) : _prefs = prefs;

  @override
  State<StatefulWidget> createState() => _SharedPrefsConfig();
}

class _SharedPrefsConfig extends State<ConfigWidget> implements Config {
  SharedPreferences get _prefs => widget._prefs;

  @override
  Widget build(BuildContext context) {
    return _ConfigInherited(
      parent: this,
      child: widget.child,
    );
  }

  @override
  String? get spotifyRefreshToken => _prefs.getString("spotifyRefreshToken");
  @override
  set spotifyRefreshToken(String? value) {
    // has to set state so that home_page knows to reload when the value of
    // this changes between null or some value
    setState(() {
      if (value != null) {
        _prefs.setString("spotifyRefreshToken", value);
      } else {
        _prefs.remove("spotifyRefreshToken");
      }
    });
  }
}

class _ConfigInherited extends InheritedWidget {
  final _SharedPrefsConfig parent;

  const _ConfigInherited({required this.parent, required super.child});

  @override
  bool updateShouldNotify(covariant InheritedWidget oldWidget) {
    return true;
  }
}
