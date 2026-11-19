using System.Linq;
using ControlCompanyRemake.Core.Config;
using ControlCompanyRemake.Core.Extentions;
using HarmonyLib;

namespace ControlCompanyRemake.Patches;

internal class LobbyListPatch
{
	public const char SERVER_NAME_FILTER = '\u200b';

	[HarmonyPatch(typeof(MenuManager), "ConfirmHostButton")]
	[HarmonyPrefix]
	private static void AddServerNameChar(MenuManager __instance)
	{
		string text = __instance.lobbyNameInputField.text;
		if (!ConfigManager.Instance.DisableModForSelf && __instance.hostSettings_LobbyPublic)
		{
			if (!Enumerable.Contains(text, '\u200b'))
			{
				text = ((text.Length > 3) ? text.Insert(text.Length - 2, '\u200b'.ToString()) : ("\u200b" + text));
			}
			if (!text.Trim().StartsWith("[MODDED]"))
			{
				text = "[MODDED] " + text;
			}
			__instance.lobbyNameInputField.text = text;
		}
		else
		{
			__instance.lobbyNameInputField.text = text.RemoveChar('\u200b');
		}
	}
}
