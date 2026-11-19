using ControlCompanyRemake.Core.Extentions;
using UnityEngine;

namespace ControlCompanyRemake.Core.Enemy;

internal class FlowermanEnemyController : EnemyAIController
{
	private FlowermanAI flowermanAI;

	private const int FLOWERMAN_DEFAULT_MODE = 0;

	private const int FLOWERMAN_STAND_MODE = 1;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		flowermanAI = (FlowermanAI)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
	}

	protected override void Move()
	{
		if (!flowermanAI.inSpecialAnimation)
		{
			base.Move();
		}
	}

	protected override void LateUpdate()
	{
		base.LateUpdate();
		controlEnemyRotation = !flowermanAI.inSpecialAnimation;
	}

	protected override void UseSecondarySkillAction()
	{
		flowermanAI.SwitchToBehaviourState(1);
	}

	protected override void ReleaseSecondarySkillAction()
	{
		flowermanAI.SwitchToBehaviourState(0);
	}

	protected override void UsePrimarySkillAction()
	{
		if (flowermanAI.carryingPlayerBody)
		{
			flowermanAI.Call("DropPlayerBody");
			flowermanAI.DropPlayerBodyServerRpc();
		}
	}

	public override string GetPrimarySkillName()
	{
		return flowermanAI.carryingPlayerBody ? "Drop body" : "";
	}

	public override string GetSecondarySkillName()
	{
		return "";
	}
}
