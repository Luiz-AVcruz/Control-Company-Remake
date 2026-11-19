using System.Linq;
using ControlCompanyRemake.Core.Config;
using ControlCompanyRemake.Core.Extentions;
using HarmonyLib;

namespace ControlCompanyRemake.Patches;

internal class LobbyListPatch
{
	private const char LobbyMarker = '\u200b';

	[HarmonyPatch(typeof(MenuManager), "ConfirmHostButton")]
	[HarmonyPrefix]
	private static void AddServerNameChar(MenuManager __instance)
	{
		string lobbyName = __instance.lobbyNameInputField.text;
		if (!ConfigManager.Instance.DisableModForSelf && __instance.hostSettings_LobbyPublic)
		{
			if (!lobbyName.Contains(LobbyMarker))
			{
				lobbyName = lobbyName.Length > 3
					? lobbyName.Insert(lobbyName.Length - 2, LobbyMarker.ToString())
					: LobbyMarker + lobbyName;
			}
			if (!lobbyName.Trim().StartsWith("[MODDED]"))
			{
				lobbyName = "[MODDED] " + lobbyName;
			}
			__instance.lobbyNameInputField.text = lobbyName;
		}
		else
		{
			__instance.lobbyNameInputField.text = lobbyName.RemoveChar(LobbyMarker);
		}
	}
}
