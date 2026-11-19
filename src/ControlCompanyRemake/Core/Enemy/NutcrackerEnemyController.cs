using ControlCompanyRemake.Core.Extentions;
using UnityEngine;

namespace ControlCompanyRemake.Core.Enemy;

internal class NutcrackerEnemyController : EnemyAIController
{
	private NutcrackerEnemyAI nutcrackerEnemyAI;

	private const int NUTCRACKER_DEFAULT_MODE = 0;

	private const int NUTCRACKER_SENTRY_MODE = 1;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		nutcrackerEnemyAI = (NutcrackerEnemyAI)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
	}

	protected override void LateUpdate()
	{
		if (isInitialized)
		{
			base.LateUpdate();
			if (!isAIControlled && IsMoving() && nutcrackerEnemyAI.currentBehaviourStateIndex != 0)
			{
				nutcrackerEnemyAI.SwitchToBehaviourState(0);
			}
		}
	}

	protected override void UsePrimarySkillAction()
	{
		if (!(nutcrackerEnemyAI.gun == null) && !nutcrackerEnemyAI.GetFieldValue<bool>("reloadingGun"))
		{
			if (nutcrackerEnemyAI.gun.shellsLoaded > 0)
			{
				FireGun();
			}
			if (nutcrackerEnemyAI.gun.shellsLoaded <= 0)
			{
				ReloadGun();
			}
		}
	}

	protected override void UseSecondarySkillAction()
	{
		SwitchAIState(1);
	}

	protected override void ReleaseSecondarySkillAction()
	{
		SwitchAIState(0);
	}

	public override void DestroyAndCleanUp()
	{
		if (!nutcrackerEnemyAI.isEnemyDead)
		{
			Object.Destroy(nutcrackerEnemyAI.gun.gameObject);
		}
		base.DestroyAndCleanUp();
	}

	public override string GetPrimarySkillName()
	{
		return "Fire";
	}

	public override string GetSecondarySkillName()
	{
		return "(HOLD) Sentry mode";
	}

	private void FireGun()
	{
		nutcrackerEnemyAI.gun.gunShootAudio.volume = 0.25f;
		nutcrackerEnemyAI.FireGunServerRpc();
	}

	private void ReloadGun()
	{
		nutcrackerEnemyAI.SetFieldValue("reloadingGun", value: true);
		nutcrackerEnemyAI.ReloadGunServerRpc();
	}
}
