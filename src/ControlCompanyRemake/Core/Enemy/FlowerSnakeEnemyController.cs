using ControlCompanyRemake.Core.Extentions;
using UnityEngine;

namespace ControlCompanyRemake.Core.Enemy;

internal class FlowerSnakeEnemyController : EnemyAIController
{
	private FlowerSnakeEnemy flowerSnakeAI;

	private bool canLeap = true;

	private float leapCooldownTime = 1f;

	private float leapCooldownTimer;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		flowerSnakeAI = (FlowerSnakeEnemy)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
	}

	protected override void LateUpdate()
	{
		if (!isInitialized)
		{
			return;
		}
		base.LateUpdate();
		if (isAIControlled)
		{
			return;
		}
		bool flag = IsClingingToPlayer();
		bool fieldValue = flowerSnakeAI.GetFieldValue<bool>("leaping");
		if (flag || fieldValue)
		{
			controlEnemyMovement = false;
			controlEnemyRotation = false;
			base.transform.position = enemyGameObject.transform.position;
		}
		else
		{
			controlEnemyMovement = true;
			controlEnemyRotation = true;
		}
		if (!flag && !fieldValue && !canLeap)
		{
			leapCooldownTimer += Time.deltaTime;
			if (leapCooldownTimer >= leapCooldownTime)
			{
				leapCooldownTimer = 0f;
				canLeap = true;
			}
		}
	}

	protected override void UsePrimarySkillAction()
	{
		if (IsClingingToPlayer())
		{
			if (flowerSnakeAI.clingingToPlayer != null)
			{
				flowerSnakeAI.StopClingingServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			}
		}
		else if (canLeap && !flowerSnakeAI.GetFieldValue<bool>("leaping"))
		{
			LeapForward();
		}
	}

	public override string GetPrimarySkillName()
	{
		return IsClingingToPlayer() ? "Stop clinging" : "Leap";
	}

	protected override void UseSecondarySkillAction()
	{
		if (IsClingingToPlayer() && !flowerSnakeAI.activatedFlight)
		{
		}
	}

	public override string GetSecondarySkillName()
	{
		return "";
	}

	private bool IsClingingToPlayer()
	{
		return flowerSnakeAI.clingingToPlayer != null;
	}

	private void LeapForward()
	{
		Vector3 vector = base.transform.forward * 2f;
		vector += Random.insideUnitSphere * Random.Range(0.05f, 0.15f);
		vector.y = Mathf.Clamp(vector.y, -16f, 16f);
		vector = Vector3.Normalize(vector * 1000f);
		flowerSnakeAI.StartLeapOnLocalClient(vector);
		flowerSnakeAI.StartLeapClientRpc(vector);
	}
}
