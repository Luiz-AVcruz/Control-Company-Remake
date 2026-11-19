using ControlCompanyRemake.Core;
using GameNetcodeStuff;
using HarmonyLib;

namespace ControlCompanyRemake.Patches;

[HarmonyPatch(typeof(PlayerControllerB))]
internal class PlayerControllerBPatch
{
	[HarmonyPatch("PlayerLookInput")]
	[HarmonyPrefix]
	private static bool PlayerLookInputPrefix()
	{
		if (ControlCenter.Instance != null)
		{
			return ControlCenter.Instance.CurrentMode == ControlCenter.Mode.HOST;
		}
		return true;
	}
}
