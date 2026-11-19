using ControlCompanyRemake.Core;
using HarmonyLib;

namespace ControlCompanyRemake.Patches;

[HarmonyPatch(typeof(TimeOfDay))]
internal class TimeOfDayPatch
{
	[HarmonyPatch("VoteShipToLeaveEarly")]
	[HarmonyPrefix]
	private static bool VoteToLeaveEarly()
	{
		if (ControlCenter.Instance != null)
		{
			return ControlCenter.Instance.CurrentMode == ControlCenter.Mode.HOST;
		}
		return true;
	}
}
