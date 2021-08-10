using System;
using System.IO;
using UnityEngine;

namespace UnityEditor.SettingsManagement
{
    /// <inheritdoc />
    /// <summary>
    /// A settings repository that stores data serialized to a JSON file.
    /// </summary>
    [Serializable]
    public class FileSettingsRepository : ISettingsRepository
    {
        /// <value>
        /// Package settings that are saved in the ProjectSettings directory.
        /// </value>
        protected const string k_PackageSettingsDirectory = "ProjectSettings/Packages";

        /// <value>
        /// Per-project user settings directory. Resolves to `Project/UserSettings/Packages/com.your-package-name`.
        /// </value>
        protected const string k_UserProjectSettingsDirectory = "UserSettings/Packages";

        const bool k_PrettyPrintJson = true;

        bool m_Initialized;
        string m_Path;
        [SerializeField]
        SettingsDictionary m_Dictionary = new SettingsDictionary();
        Hash128 m_JsonHash;

        /// <summary>
        /// Constructor sets the serialized data path.
        /// </summary>
        /// <param name="path">
        /// The project-relative path to save settings to.
        /// </param>
        public FileSettingsRepository(string path)
        {
            m_Path = path;
            m_Initialized = false;
            AssemblyReloadEvents.beforeAssemblyReload += Save;
            EditorApplication.quitting += Save;
        }

        void Init()
        {
            if (m_Initialized)
                return;

            m_Initialized = true;

            if (TryLoadSavedJson(out string json))
            {
                m_Dictionary = null;
                m_JsonHash = Hash128.Compute(json);
                EditorJsonUtility.FromJsonOverwrite(json, this);
            }

            if (m_Dictionary == null)
                m_Dictionary = new SettingsDictionary();
        }

        /// <value>
        /// This repository implementation is relevant to the Project scope by default, but overriding implementations
        /// may store this serialized data at a user scope if desired.
        /// </value>
        /// <inheritdoc cref="ISettingsRepository.scope"/>
        public virtual SettingsScope scope => SettingsScope.Project;

        /// <value>
        /// The full path to the settings file.
        /// </value>
        /// <inheritdoc cref="ISettingsRepository.path"/>
        public string path
        {
            get { return m_Path; }
        }

        /// <summary>
        /// The name of this settings file.
        /// </summary>
        public string name => Path.GetFileNameWithoutExtension(path);

        public bool TryLoadSavedJson(out string json)
        {
            json = string.Empty;
            if (!File.Exists(path))
                return false;
            json = File.ReadAllText(path);
            return true;
        }

        /// <summary>
        /// Save all settings to their serialized state.
        /// </summary>
        /// <inheritdoc cref="ISettingsRepository.Save"/>
        public void Save()
        {
            Init();

            if (!File.Exists(path))
            {
                var directory = Path.GetDirectoryName(path);

                if (string.IsNullOrEmpty(directory))
                {
                    Debug.LogError(
                        $"Settings file {name} is saved to an invalid path: {path}. Settings will not be saved.");
                    return;
                }

                Directory.CreateDirectory(directory);
            }

            string json = EditorJsonUtility.ToJson(this, k_PrettyPrintJson);

            // While unlikely, a hash collision is possible. Always test the actual saved contents before early exit.
            if (m_JsonHash == Hash128.Compute(json)
                && TryLoadSavedJson(out string existing)
                && existing.Equals(json))
                return;

#if UNITY_2019_3_OR_NEWER 
            // AssetDatabase.IsOpenForEdit can be a very slow synchronous blocking call when Unity is connected to
            // Perforce Version Control. Especially if it's called repeatedly with every EditorGUI redraw.
            if (File.Exists(path) && !AssetDatabase.IsOpenForEdit(path))
            {
                if (!AssetDatabase.MakeEditable(path))
                {
                    Debug.LogWarning($"Could not save package settings to {path}");
                    return;
                }
            }
#endif

            try
            {
                m_JsonHash = Hash128.Compute(json);
                File.WriteAllText(path, json);
            }
            catch (UnauthorizedAccessException)
            {
                Debug.LogWarning($"Could not save package settings to {path}");
            }
        }

        /// <summary>
        /// Set a value for key of type T.
        /// </summary>
        /// <param name="key">The settings key.</param>
        /// <param name="value">The value to set. Must be serializable.</param>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <inheritdoc cref="ISettingsRepository.Set{T}"/>
        public void Set<T>(string key, T value)
        {
            Init();
            m_Dictionary.Set<T>(key, value);
        }

        /// <summary>
        /// Get a value with key of type T, or return the fallback value if no matching key is found.
        /// </summary>
        /// <param name="key">The settings key.</param>
        /// <param name="fallback">If no key with a value of type T is found, this value is returned.</param>
        /// <typeparam name="T">Type of value to search for.</typeparam>
        /// <inheritdoc cref="ISettingsRepository.Get{T}"/>
        public T Get<T>(string key, T fallback = default(T))
        {
            Init();
            return m_Dictionary.Get<T>(key, fallback);
        }

        /// <summary>
        /// Does the repository contain a setting with key and type.
        /// </summary>
        /// <param name="key">The settings key.</param>
        /// <typeparam name="T">The type of value to search for.</typeparam>
        /// <returns>True if a setting matching both key and type is found, false if no entry is found.</returns>
        /// <inheritdoc cref="ISettingsRepository.ContainsKey{T}"/>
        public bool ContainsKey<T>(string key)
        {
            Init();
            return m_Dictionary.ContainsKey<T>(key);
        }

        /// <summary>
        /// Remove a key value pair from the settings repository.
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="T"></typeparam>
        /// <inheritdoc cref="ISettingsRepository.Remove{T}"/>
        public void Remove<T>(string key)
        {
            Init();
            m_Dictionary.Remove<T>(key);
        }
    }
}
