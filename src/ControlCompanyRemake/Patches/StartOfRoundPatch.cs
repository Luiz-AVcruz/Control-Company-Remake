using ControlCompanyRemake.Core;
using ControlCompanyRemake.Core.Config;
using ControlCompanyRemake.Core.UI;
using HarmonyLib;
using UnityEngine;

namespace ControlCompanyRemake.Patches;

internal class StartOfRoundPatch
{
	[HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
	[HarmonyPrefix]
	private static void ShipHasLeftPrefix()
	{
		if (ControlCenter.Instance != null)
		{
			ControlCenter.Instance.EndOfRoundCleanUp();
		}
	}

	[HarmonyPatch(typeof(RoundManager), "RefreshEnemiesList")]
	[HarmonyPostfix]
	private static void StartGamePostfix()
	{
		if (ControlCenter.Instance != null)
		{
			ControlCenter.Instance.StartOfRoundInitialization();
		}
	}

	[HarmonyPatch(typeof(StartOfRound), "Awake")]
	[HarmonyPostfix]
	private static void CreateControlCenter()
	{
		if (!ConfigManager.Instance.DisableModForSelf)
		{
			GameObject gameObject = new GameObject("control_company_remake_control_center");
			gameObject.AddComponent<ControlCenter>();
			gameObject.AddComponent<UIManager>();
		}
	}
}
