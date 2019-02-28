# Settings Manager

A framework for making any serializable field a setting, complete with a pre-built settings interface.

![in action](Documentation~/images/settings.gif)

## Quick Start

Settings are stored and managed by a `Settings` instance. This class is responsible for setting and retrieving serialized values from the appropriate repository.

Settings repositories are used to save and load settingsfor a settings scope. Two are provided with this package: one for saving User preferences (`UserSettingsRepository`, backed by the `EditorPrefs` class) and one for Project settings (`ProjecSettingsRepository`, which saves a JSON file to the `ProjectSettings` directory).

Usually you will want to create and manage a singleton `Settings` instance. Ex:

```
using UnityEditor.SettingsManagement;

namespace UnityEditor.SettingsManagement.Examples
{
    static class MySettingsManager
    {
        internal const string k_ProjectSettingsPath = "ProjectSettings/MySettingsExample.json";

        static Settings s_Instance;

        public static Settings instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new Settings(new ISettingsRepository[]
                    {
                        new ProjectSettingsRepository(k_ProjectSettingsPath),
                        new UserSettingsRepository()
                    });

                return s_Instance;
            }
        }
    }
 }
```

Values are set and retrieved using generic methods on on your `Settings` instance:

```
MySettingsManager.instance.Get<float>("myFloatValue", SettingsScopes.Project);
```

There are two arguments: key, and scope. The `Settings` class will handle finding an appropriate `ISettingsRepository` for the scope, while `key` and `T` are used to find the value. Setting keys are unique among types, meaning you may re-use keys as long as the setting type is different.

Alternatively, you can use the `Setting<T>` class to manage settings. This is a wrapper class around the `Settings` get/set properties, and makes it very easy to make any field a saved setting.

```
// Setting<T>(Settings instance, string key, T defaultValue, SettingsScopes scope = SettingsScopes.Project)
Setting<int> myIntValue = new Setting<int>(MySettingsManager.instance, "int.key", 42, SettingsScopes.User);
```

`Setting<T>` caches the current value, and keeps a copy of the default value so that it may be reset. `Setting<T>` fields are also eligible for use with the `[SettingAttribute]` attribute, which lets the `SettingsManagerProvider` automatically add it to a settings inspector.

## Settings Provider

To register your settings in the `Settings Window` you can either write your own `SettingsProvider` implementation, or use the provided `SettingsManagerProvider` and let it automatically create your interface.

Making use of `SettingsManagerProvider` comes with many benefits, including a uniform look for your settings UI, support for search, and per-field or mass reset support.

```
using UnityEngine;

namespace UnityEditor.SettingsManagement.Examples
{
	static class MySettingsProvider
	{
		const string k_PreferencesPath = "Preferences/My Settings";

		[SettingsProvider]
		static SettingsProvider CreateSettingsProvider()
		{
			// The last parameter tells the provider where to search for settings.
			var provider = new SettingsManagerProvider(k_PreferencesPath,
				MySettingsManager.instance,
				new [] { typeof(MySettingsProvider).Assembly });

			return provider;
		}
	}
}
```

To register a field with the `SettingsManagerProvider`, simply decorate it with `[SettingAttribute(string displayCategory, string key)]`. `[SettingAttribute]` is only valid for static fields.

For more complex settings that require additional UI (or simply don't have a built-in editor), you can use `SettingBlockAttribute`. This provides access to the settings provider GUI. See `SettingsExamples.cs` for more on this.

