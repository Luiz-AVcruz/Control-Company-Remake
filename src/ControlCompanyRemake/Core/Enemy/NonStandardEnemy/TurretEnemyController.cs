using System;
using System.Linq;
using ControlCompanyRemake.Core.Extentions;
using UnityEngine;

namespace ControlCompanyRemake.Core.Enemy.NonStandardEnemy;

internal class TurretEnemyController : EnemyController
{
	private Turret turret;

	private bool holdingDownFire = false;

	private float holdingDownFireTime = 0f;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		if (!isInitialized)
		{
			Initialize(playerActions, CameraMode.THIRD_PERSON);
			turret = enemyGameObject.GetComponentInChildren<Turret>();
			if (turret == null)
			{
				throw new Exception("Turret gameobject does not have Turret script attached.");
			}
			enemyGameObject.transform.position = base.transform.position;
			enemyGameObject.transform.rotation = base.transform.rotation;
			turret.gameObject.GetComponentsInChildren<Collider>().ToList().ForEach(delegate(Collider c)
			{
				Physics.IgnoreCollision(c, characterController);
			});
			base.enemyGameObject = enemyGameObject;
			canMove = false;
			canJump = false;
			turret.mainAudio.volume *= 0.3f;
			turret.berserkAudio.volume *= 0.3f;
			turret.farAudio.volume *= 0.3f;
			turret.bulletCollisionAudio.volume *= 0.3f;
			EnableAIControl(enabled: false);
		}
	}

	protected override void LateUpdate()
	{
		if (!isInitialized)
		{
			return;
		}
		base.LateUpdate();
		if (!isAIControlled)
		{
			if (controlEnemyMovement)
			{
				enemyGameObject.transform.position = base.transform.position;
			}
			if (controlEnemyRotation && !lockEnemyRotationInput)
			{
				turret.turretRod.transform.rotation = base.transform.rotation;
			}
			if (holdingDownFire)
			{
				holdingDownFireTime += Time.deltaTime;
				if (holdingDownFireTime < 1f)
				{
					SwitchToMode(TurretMode.Charging);
				}
				else
				{
					SwitchToMode(TurretMode.Firing);
				}
			}
			else
			{
				SwitchToMode(TurretMode.Detection);
			}
		}
		else
		{
			base.transform.position = enemyGameObject.transform.position;
		}
	}

	private void SwitchToMode(TurretMode mode)
	{
		if (turret.turretMode != mode)
		{
			turret.Call("SwitchTurretMode", (int)mode);
			turret.SetToModeClientRpc((int)mode);
		}
	}

	protected override void UsePrimarySkillAction()
	{
		holdingDownFire = true;
	}

	protected override void ReleasePrimarySkillAction()
	{
		holdingDownFire = false;
		holdingDownFireTime = 0f;
		SwitchToMode(TurretMode.Detection);
	}

	public override string GetPrimarySkillName()
	{
		return "Fire";
	}
}
