using System;
using UnityEngine;

namespace ControlCompanyRemake.Core;

internal class EnemyController : CustomPlayerController
{
	protected GameObject enemyGameObject;

	protected bool isAIControlled = false;

	protected bool controlEnemyRotation = true;

	protected bool controlEnemyMovement = true;

	protected bool lockEnemyRotationInput = false;

	public bool IsAIControlled => isAIControlled;

	public virtual void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
	}

	public virtual bool CanControlEnemy()
	{
		return true;
	}

	public void SetLockEnemyRotation(bool lockEnemyRotationInput)
	{
		this.lockEnemyRotationInput = lockEnemyRotationInput;
	}

	public void UsePrimarySkill()
	{
		CheckInitialized();
		if (!isAIControlled)
		{
			UsePrimarySkillAction();
		}
	}

	protected virtual void UsePrimarySkillAction()
	{
	}

	public void ReleasePrimarySkill()
	{
		CheckInitialized();
		if (!isAIControlled)
		{
			ReleasePrimarySkillAction();
		}
	}

	protected virtual void ReleasePrimarySkillAction()
	{
	}

	public void UseSecondarySkill()
	{
		CheckInitialized();
		if (!isAIControlled)
		{
			UseSecondarySkillAction();
		}
	}

	protected virtual void UseSecondarySkillAction()
	{
	}

	public void ReleaseSecondarySkill()
	{
		CheckInitialized();
		if (!isAIControlled)
		{
			ReleaseSecondarySkillAction();
		}
	}

	protected virtual void ReleaseSecondarySkillAction()
	{
	}

	protected virtual void EnableAIControl(bool enabled)
	{
		CheckInitialized();
		isAIControlled = enabled;
		ControlCompanyRemakePlugin.Instance.logger.LogMessage($"Enable AI control: {enabled}");
		if (IsAIControlled)
		{
			ToggleAIControlTrue();
		}
		else
		{
			ToggleAIControlFalse();
		}
	}

	protected virtual void ToggleAIControlTrue()
	{
	}

	protected virtual void ToggleAIControlFalse()
	{
	}

	public virtual void ToggleAIControl()
	{
		CheckInitialized();
		isAIControlled = !isAIControlled;
		EnableAIControl(isAIControlled);
	}

	public virtual void DestroyAndCleanUp()
	{
		if (ShouldDestroyEnemyGameObjectOnCleanUp())
		{
			UnityEngine.Object.Destroy(enemyGameObject);
		}
		UnityEngine.Object.Destroy(base.gameObject);
	}

	protected virtual bool ShouldDestroyEnemyGameObjectOnCleanUp()
	{
		return true;
	}

	public virtual string GetPrimarySkillName()
	{
		return "";
	}

	public virtual string GetSecondarySkillName()
	{
		return "";
	}

	protected void CheckInitialized()
	{
		if (!isInitialized)
		{
			throw new Exception("Not initialized yet.");
		}
	}

	protected virtual void OnControllerColliderHit(ControllerColliderHit hit)
	{
		DoorLock component = hit.gameObject.GetComponent<DoorLock>();
		if (component != null)
		{
			doorOpener.OpenDoor(component);
		}
	}
}
