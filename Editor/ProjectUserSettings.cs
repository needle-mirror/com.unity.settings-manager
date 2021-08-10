using System;

namespace UnityEditor.SettingsManagement
{
	[Serializable]
	public class ProjectUserSettings : FileSettingsRepository
	{
		public ProjectUserSettings(string package, string name = "Settings") : base(GetUserSettingsPath(package, name))
		{
		}

		public static string GetUserSettingsPath(string package, string name)
		{
			return string.Format("{0}/{1}/{2}.json", k_UserProjectSettingsDirectory, package, name);
		}
	}
}
