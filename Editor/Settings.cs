using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.SettingsManagement
{
    /// <summary>
    /// Settings manages a collection of <see cref="ISettingsRepository"/>.
    /// </summary>
    public sealed class Settings
    {
        ISettingsRepository[] m_SettingsRepositories;

        /// <value>
        /// An event that is raised prior to an `ISettingsRepository` serializing it's current state.
        /// </value>
        public event Action beforeSettingsSaved;

        /// <value>
        /// An event that is raised after an `ISettingsRepository` has serialized it's current state.
        /// </value>
        public event Action afterSettingsSaved;

        Settings()
        {
        }

        /// <summary>
        /// Create a new Settings instance with a <see cref="UserSettingsRepository"/> and <see cref="PackageSettingsRepository"/>.
        /// </summary>
        /// <param name="package">The package name. Ex, `com.unity.my-package`.</param>
        /// <param name="settingsFileName">The name of the settings file. Defaults to "Settings."</param>
        public Settings(string package, string settingsFileName = "Settings")
        {
            m_SettingsRepositories = new ISettingsRepository[]
            {
                new PackageSettingsRepository(package, settingsFileName),
                new UserSettingsRepository()
            };
        }

        /// <summary>
        /// Create a new Settings instance with a collection of <see cref="ISettingsRepository"/>.
        /// </summary>
        public Settings(IEnumerable<ISettingsRepository> repositories)
        {
            m_SettingsRepositories = repositories.ToArray();
        }

        /// <summary>
        /// Find a settings repository that matches the requested scope.
        /// </summary>
        /// <param name="scope">The scope of the settings repository to match.</param>
        /// <returns>
        /// An ISettingsRepository instance that is implementing the requested scope. May return null if no
        /// matching repository is found.
        /// </returns>
        public ISettingsRepository GetRepository(SettingsScope scope)
        {
            foreach (var repo in m_SettingsRepositories)
                if (repo.scope == scope)
                    return repo;
            return null;
        }

        /// <summary>
        /// Find a settings repository that matches the requested scope and name.
        /// </summary>
        /// <param name="scope">The scope of the settings repository to match.</param>
        /// <param name="name">The name of the <see cref="ISettingsRepository"/> to match.</param>
        /// <returns>
        /// An <see cref="ISettingsRepository"/> instance that is implementing the requested scope, and matches name.
        /// May return null if no matching repository is found.
        /// </returns>
        public ISettingsRepository GetRepository(SettingsScope scope, string name)
        {
            foreach (var repo in m_SettingsRepositories)
                if (repo.scope == scope && string.Equals(repo.name, name))
                    return repo;
            return null;
        }

        /// <summary>
        /// Serialize the state of all settings repositories.
        /// </summary>
        public void Save()
        {
            if (beforeSettingsSaved != null)
                beforeSettingsSaved();

            foreach (var repo in m_SettingsRepositories)
                repo.Save();

            if (afterSettingsSaved != null)
                afterSettingsSaved();
        }

        /// <summary>
        /// Set a value for key of type T.
        /// </summary>
        /// <param name="key">The settings key.</param>
        /// <param name="value">The value to set. Must be serializable.</param>
        /// <param name="scope">Which scope this settings should be saved in.</param>
        /// <typeparam name="T">Type of value.</typeparam>
        public void Set<T>(string key, T value, SettingsScope scope = SettingsScope.Project)
        {
            if (scope == SettingsScope.Project)
                Set<T, PackageSettingsRepository>(key, value);
            Set<T, UserSettingsRepository>(key, value);
        }

        /// <summary>
        /// Set a value for key of type T.
        /// </summary>
        /// <param name="key">The settings key.</param>
        /// <param name="value">The value to set. Must be serializable.</param>
        /// <param name="repositoryName">If provided, only repositories with a matching name will be considered.</param>
        /// <param name="scope">Which scope this settings should be saved in.</param>
        /// <typeparam name="T">Type of value.</typeparam>
        public void Set<T>(string key, T value, string repositoryName, SettingsScope scope = SettingsScope.Project)
        {
            if (scope == SettingsScope.Project)
                Set<T, PackageSettingsRepository>(key, value, repositoryName);
            Set<T, UserSettingsRepository>(key, value, repositoryName);
        }

        /// <summary>
        /// Set a value for key of type T.
        /// </summary>
        /// <param name="key">The settings key.</param>
        /// <param name="value">The value to set. Must be serializable.</param>
        /// <param name="repositoryName">The name of the repository to set this value in.</param>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <typeparam name="K">Type of repository to set this value for.</typeparam>
        public void Set<T, K>(string key, T value, string repositoryName = null) where K : ISettingsRepository
        {
            bool foundScopeRepository = false;

            foreach (var repo in m_SettingsRepositories)
            {
                if (repo is K && (string.IsNullOrEmpty(repositoryName) || repo.name == repositoryName))
                {
                    repo.Set<T>(key, value);
                    foundScopeRepository = true;
                }
            }

            if (!foundScopeRepository)
                Debug.LogWarning($"No repository with type {typeof(K)} found.");
        }

        /// <summary>
        /// Get a value with key of type T, or return the fallback value if no matching key is found.
        /// </summary>
        /// <param name="key">The settings key.</param>
        /// <param name="scope">Which scope this settings should be retrieved from.</param>
        /// <param name="fallback">If no key with a value of type T is found, this value is returned.</param>
        /// <typeparam name="T">Type of value to search for.</typeparam>
        public T Get<T>(string key, SettingsScope scope = SettingsScope.Project, T fallback = default(T))
        {
            if (scope == SettingsScope.Project)
                return Get<T, PackageSettingsRepository>(key, fallback);
            return Get<T, UserSettingsRepository>(key, fallback);
        }

        /// <summary>
        /// Get a value with key of type T, or return the fallback value if no matching key is found.
        /// </summary>
        /// <param name="key">The settings key.</param>
        /// <param name="repositoryName">The repository name to match.</param>
        /// <param name="scope">Which scope this settings should be retrieved from.</param>
        /// <param name="fallback">If no key with a value of type T is found, this value is returned.</param>
        /// <typeparam name="T">Type of value to search for.</typeparam>
        public T Get<T>(string key, string repositoryName, SettingsScope scope = SettingsScope.Project, T fallback = default(T))
        {
            if (scope == SettingsScope.Project)
                return Get<T, PackageSettingsRepository>(key, fallback, repositoryName);
            return Get<T, UserSettingsRepository>(key, fallback, repositoryName);
        }

        /// <summary>
        /// Get a value with key of type T, or return the fallback value if no matching key is found.
        /// </summary>
        /// <param name="key">The settings key.</param>
        /// <param name="fallback">If no key with a value of type T is found, this value is returned.</param>
        /// <param name="repositoryName">If provided, only repositories with a matching name will be searched for the key.</param>
        /// <typeparam name="T">Type of value to search for.</typeparam>
        /// <typeparam name="K">Only repositories of type K will be searched for matching keys.</typeparam>
        public T Get<T, K>(string key, T fallback = default(T), string repositoryName = null) where K : ISettingsRepository
        {
            foreach (var repo in m_SettingsRepositories)
            {
                if (repo is K && (string.IsNullOrEmpty(repositoryName) || repo.name == repositoryName))
                    return repo.Get<T>(key, fallback);
            }

            Debug.LogWarning($"No repository with type {typeof(K)} found.");
            return fallback;
        }

        /// <summary>
        /// Does the repository contain a setting with key and type.
        /// </summary>
        /// <param name="key">The settings key.</param>
        /// <typeparam name="T">The type of value to search for.</typeparam>
        /// <param name="scope">Which scope should be searched for matching key.</param>
        /// <returns>True if a setting matching both key and type is found, false if no entry is found.</returns>
        public bool ContainsKey<T>(string key, SettingsScope scope = SettingsScope.Project)
        {
            if (scope == SettingsScope.Project)
                return ContainsKey<T, PackageSettingsRepository>(key);
            return ContainsKey<T, UserSettingsRepository>(key);
        }

        /// <summary>
        /// Does the repository contain a setting with key and type.
        /// </summary>
        /// <param name="key">The settings key.</param>
        /// <param name="repositoryName">The repository name to match.</param>
        /// <typeparam name="T">The type of value to search for.</typeparam>
        /// <param name="scope">Which scope should be searched for matching key.</param>
        /// <returns>True if a setting matching both key and type is found, false if no entry is found.</returns>
        public bool ContainsKey<T>(string key, string repositoryName, SettingsScope scope = SettingsScope.Project)
        {
            if (scope == SettingsScope.Project)
                return ContainsKey<T, PackageSettingsRepository>(key, repositoryName);
            return ContainsKey<T, UserSettingsRepository>(key, repositoryName);
        }

        /// <summary>
        /// Does the repository contain a setting with key and type.
        /// </summary>
        /// <param name="key">The settings key.</param>
        /// <typeparam name="T">The type of value to search for.</typeparam>
        /// <typeparam name="K">Only repositories of type K will be searched for matching keys.</typeparam>
        /// <param name="repositoryName">If provided, only repositories with a matching name will be searched for the key.</param>
        /// <returns>True if a setting matching both key and type is found, false if no entry is found.</returns>
        public bool ContainsKey<T, K>(string key, string repositoryName = null) where K : ISettingsRepository
        {
            foreach (var repo in m_SettingsRepositories)
            {
                if (repo is K && (string.IsNullOrEmpty(repositoryName) || repositoryName == repo.name))
                    return repo.ContainsKey<T>(key);
            }

            Debug.LogWarning($"No repository with type {typeof(K)} found.");
            return false;
        }

        /// <summary>
        /// Remove a key value pair from a settings repository.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <param name="scope">Which scope should be searched for matching key.</param>
        /// <typeparam name="T">The type that this key is pointing to.</typeparam>
        public void DeleteKey<T>(string key, SettingsScope scope = SettingsScope.Project)
        {
            if (scope == SettingsScope.Project)
                DeleteKey<T, PackageSettingsRepository>(key);
            DeleteKey<T, UserSettingsRepository>(key);
        }

        /// <summary>
        /// Remove a key value pair from a settings repository.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <param name="repositoryName">The repository name to match.</param>
        /// <param name="scope">Which scope should be searched for matching key.</param>
        /// <typeparam name="T">The type that this key is pointing to.</typeparam>
        public void DeleteKey<T>(string key, string repositoryName, SettingsScope scope = SettingsScope.Project)
        {
            if (scope == SettingsScope.Project)
                DeleteKey<T, PackageSettingsRepository>(key, repositoryName);
            DeleteKey<T, UserSettingsRepository>(key, repositoryName);
        }

        /// <summary>
        /// Remove a key value pair from a settings repository.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <param name="repositoryName">If provided, only repositories with a matching name will be searched for the key.</param>
        /// <typeparam name="T">The type that this key is pointing to.</typeparam>
        /// <typeparam name="K">Only repositories of type K will be searched for matching keys.</typeparam>
        public void DeleteKey<T, K>(string key, string repositoryName = null) where K : ISettingsRepository
        {
            bool foundScopeRepository = false;

            foreach (var repo in m_SettingsRepositories)
            {
                if (repo is K && (string.IsNullOrEmpty(repositoryName) || repositoryName == repo.name))
                {
                    foundScopeRepository = true;
                    repo.Remove<T>(key);
                }
            }

            if(!foundScopeRepository)
                Debug.LogWarning($"No repository with type {typeof(K)} found.");
        }
    }
}
