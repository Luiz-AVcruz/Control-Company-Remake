using ControlCompanyRemake.Core;
using HarmonyLib;

namespace ControlCompanyRemake.Patches;

[HarmonyPatch(typeof(HUDManager))]
internal class HUDManagerPatch
{
	[HarmonyPatch("CanPlayerScan")]
	[HarmonyPostfix]
	private static void CanPlayerScanPostfix(ref bool __result)
	{
		if (__result && ControlCenter.Instance != null)
		{
			__result = ControlCenter.Instance.CurrentMode == ControlCenter.Mode.HOST;
		}
	}

	[HarmonyPatch("UpdateHealthUI")]
	[HarmonyPrefix]
	private static bool UpdateHealthUIPrefix()
	{
		if (ControlCenter.Instance != null)
		{
			return ControlCenter.Instance.CurrentMode == ControlCenter.Mode.HOST;
		}
		return true;
	}
}
