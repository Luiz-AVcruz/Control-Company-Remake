using ControlCompanyRemake.Core;
using HarmonyLib;

namespace ControlCompanyRemake.Patches;

[HarmonyPatch(typeof(SoundManager))]
internal class SoundManagerPatch
{
	[HarmonyPatch("SetEarsRinging")]
	[HarmonyPrefix]
	private static bool SetEarsRingingPrefix()
	{
		if (ControlCenter.Instance != null)
		{
			return ControlCenter.Instance.CurrentMode == ControlCenter.Mode.HOST;
		}
		return true;
	}
}
