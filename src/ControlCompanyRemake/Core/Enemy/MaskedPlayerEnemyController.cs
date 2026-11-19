using ControlCompanyRemake.Core.Extentions;
using UnityEngine;

namespace ControlCompanyRemake.Core.Enemy;

internal class MaskedPlayerEnemyController : EnemyAIController
{
	private MaskedPlayerEnemy maskedEnemy;

	private float entranceDoorOpenRange = 2.5f;

	private float useEntranceDoorCooldown = 2f;

	private float timeSinceLastEntranceDoorUse = 0f;

	private bool canUseEntranceDoorOnCollision = true;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		maskedEnemy = (MaskedPlayerEnemy)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
		canJump = true;
	}

	protected override void LateUpdate()
	{
		base.LateUpdate();
		bool fieldValue = maskedEnemy.GetFieldValue<bool>("inKillAnimation");
		bool fieldValue2 = maskedEnemy.GetFieldValue<bool>("crouching");
		controlEnemyRotation = !fieldValue && !fieldValue2;
		if (!canUseEntranceDoorOnCollision)
		{
			timeSinceLastEntranceDoorUse += Time.deltaTime;
			if (timeSinceLastEntranceDoorUse >= useEntranceDoorCooldown)
			{
				canUseEntranceDoorOnCollision = true;
				timeSinceLastEntranceDoorUse = 0f;
			}
		}
		if (playerActions.Movement.Interact.WasPressedThisFrame())
		{
			ScanNearbyAndOpenMainEntranceDoor();
		}
		else if (playerActions.Movement.Crouch.WasPressedThisFrame())
		{
			Crouch(!fieldValue2);
		}
	}

	protected override void Move()
	{
		bool fieldValue = maskedEnemy.GetFieldValue<bool>("inKillAnimation");
		bool fieldValue2 = maskedEnemy.GetFieldValue<bool>("crouching");
		if (!fieldValue && !fieldValue2)
		{
			base.Move();
		}
	}

	protected override void UseSecondarySkillAction()
	{
	}

	private void Crouch(bool crouching)
	{
		maskedEnemy.SetCrouchingServerRpc(crouching);
	}

	private void ScanNearbyAndOpenMainEntranceDoor()
	{
		Collider[] array = Physics.OverlapSphere(base.transform.position, entranceDoorOpenRange);
		for (int i = 0; i < array.Length; i++)
		{
			EntranceTeleport componentInChildren = array[i].GetComponentInChildren<EntranceTeleport>();
			if (componentInChildren != null && componentInChildren.entranceId == 0)
			{
				UseEntranceDoor();
			}
		}
	}

	private void UseEntranceDoor()
	{
		characterController.enabled = false;
		Vector3 position = RoundManager.FindMainEntrancePosition(getTeleportPosition: true, !maskedEnemy.isOutside);
		base.transform.position = position;
		maskedEnemy.Call("TeleportMaskedEnemyAndSync", RoundManager.FindMainEntrancePosition(getTeleportPosition: true, !maskedEnemy.isOutside), !maskedEnemy.isOutside);
		characterController.enabled = true;
		canUseEntranceDoorOnCollision = false;
		ControlCenter.Instance.IsGhostIndoors = !maskedEnemy.isOutside;
	}

	protected override void OnControllerColliderHit(ControllerColliderHit hit)
	{
		base.OnControllerColliderHit(hit);
		if (canUseEntranceDoorOnCollision)
		{
			EntranceTeleport componentInChildren = hit.gameObject.GetComponentInChildren<EntranceTeleport>();
			if (componentInChildren != null && componentInChildren.entranceId == 0)
			{
				UseEntranceDoor();
			}
		}
	}

	public override string GetSecondarySkillName()
	{
		return "";
	}
}
