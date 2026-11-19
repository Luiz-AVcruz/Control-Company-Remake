using ControlCompanyRemake.Core;
using HarmonyLib;

namespace ControlCompanyRemake.Patches;

[HarmonyPatch(typeof(EnemyAI))]
internal class EnemyAIPatch
{
	[HarmonyPatch(nameof(EnemyAI.Start))]
	[HarmonyPostfix]
	private static void StartPostfix(EnemyAI __instance)
	{
		if (ControlCenter.Instance != null && __instance.IsHost)
		{
			ControlCenter.Instance.ApplyCrossAreaAfterEnemyStart(__instance);
		}
	}

	[HarmonyPatch("ChangeOwnershipOfEnemy")]
	[HarmonyPrefix]
	private static bool ChangeOwnershipOfEnemyPrefix(EnemyAI __instance, ref ulong newOwnerClientId)
	{
		return CanChangeOwnership(__instance, newOwnerClientId);
	}

	[HarmonyPatch("ChangeEnemyOwnerServerRpc")]
	[HarmonyPrefix]
	private static bool ChangeEnemyOwnerServerRpcPrefix(EnemyAI __instance, ref ulong clientId)
	{
		return CanChangeOwnership(__instance, clientId);
	}

	private static bool CanChangeOwnership(EnemyAI enemy, ulong clientId)
	{
		ControlCenter controlCenter = ControlCenter.Instance;
		if (controlCenter == null || controlCenter.CurrentMode != ControlCenter.Mode.ENEMY)
		{
			return true;
		}

		EnemyAIController controlledEnemy = controlCenter.CurrentControlledEnemy as EnemyAIController;
		if (controlledEnemy == null || controlledEnemy.IsAIControlled || controlledEnemy.EnemyAI != enemy)
		{
			return true;
		}

		return clientId == controlCenter.GetHostPlayer().actualClientId;
	}
}
