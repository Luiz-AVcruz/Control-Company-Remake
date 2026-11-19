using BepInEx;
using BepInEx.Logging;
using ControlCompanyRemake.Core.Config;
using ControlCompanyRemake.Patches;
using HarmonyLib;
using UnityEngine;

namespace ControlCompanyRemake;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInIncompatibility("ControlCompany.ControlCompany")]
public class ControlCompanyRemakePlugin : BaseUnityPlugin
{
	internal const string PluginGuid = "LuizAVcruz.ControlCompanyRemake";

	internal const string PluginName = "Control Company Remake";

	internal const string PluginVersion = "0.2.0";

	private readonly Harmony harmony = new Harmony(PluginGuid);

	public static ControlCompanyRemakePlugin Instance;

	internal ManualLogSource logger;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		logger = BepInEx.Logging.Logger.CreateLogSource(PluginGuid);
		logger.LogInfo($"Loaded {PluginName}. Patching.");
		ApplyPatches();
		logger.LogInfo("Completed patching. Initializing.");
		GameObject configObject = new GameObject("control_company_remake_config_manager");
		configObject.AddComponent<ConfigManager>();
		configObject.hideFlags = HideFlags.HideAndDontSave;
		Object.DontDestroyOnLoad(configObject);
		logger.LogInfo("Completed initialization.");
	}

	private void ApplyPatches()
	{
		harmony.PatchAll(typeof(ControlCompanyRemakePlugin));
		harmony.PatchAll(typeof(HUDManagerPatch));
		harmony.PatchAll(typeof(StartOfRoundPatch));
		harmony.PatchAll(typeof(PlayerControllerBPatch));
		harmony.PatchAll(typeof(EnemyAIPatch));
		harmony.PatchAll(typeof(TimeOfDayPatch));
		harmony.PatchAll(typeof(SoundManagerPatch));
		harmony.PatchAll(typeof(LobbyListPatch));
	}
}
