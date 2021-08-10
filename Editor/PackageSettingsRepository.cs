using System;

namespace UnityEditor.SettingsManagement
{
    /// <inheritdoc />
    /// <summary>
    /// A settings repository that stores data local to a Unity project.
    /// </summary>
    [Serializable]
    public sealed class PackageSettingsRepository : FileSettingsRepository
    {
        /// <summary>
        /// Constructor sets the serialized data path.
        /// </summary>
        /// <param name="package">
        /// The package name.
        /// </param>
        /// <param name="name">
        /// A name for this settings file. Settings are saved in `ProjectSettings/Packages/{package}/{name}.json`.
        /// </param>
        public PackageSettingsRepository(string package, string name) : base(GetSettingsPath(package, name))
        {
        }

        // Cannot call FindFromAssembly from a constructor or field initializer
//        static string CreateSettingsPath(Assembly assembly, string name)
//        {
//            var info = PackageManager.PackageInfo.FindForAssembly(assembly);
//            return string.Format("{0}/{1}/{2}.json", k_PackageSettingsDirectory, info.name, name);
//        }

        /// <summary>
        /// Get a path for a settings file relative to the calling assembly package directory.
        /// </summary>
        /// <param name="packageName">The name of the package requesting this setting.</param>
        /// <param name="name">An optional name for the settings file. Default is "Settings."</param>
        /// <returns>A package-scoped path to the settings file within Project Settings.</returns>
        public static string GetSettingsPath(string packageName, string name = "Settings")
        {
            return string.Format("{0}/{1}/{2}.json", k_PackageSettingsDirectory, packageName, name);
        }
    }
}
