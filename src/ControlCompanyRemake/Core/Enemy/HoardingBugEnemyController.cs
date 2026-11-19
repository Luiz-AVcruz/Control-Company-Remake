using System;
using System.Collections.Generic;
using ControlCompanyRemake.Core.Extentions;
using Unity.Netcode;
using UnityEngine;

namespace ControlCompanyRemake.Core.Enemy;

internal class HoardingBugEnemyController : EnemyAIController
{
	private HoarderBugAI hoarderBugAI;

	private List<Type> allowedGrabbableItemUse = new List<Type>
	{
		typeof(ShotgunItem),
		typeof(NoisemakerProp),
		typeof(FlashlightItem)
	};

	private float grabRange = 1.5f;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		hoarderBugAI = (HoarderBugAI)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
	}

	protected override void EnableAIControl(bool enabled)
	{
		if (enabled)
		{
			hoarderBugAI.Call("ChooseNestPosition");
			hoarderBugAI.SwitchToBehaviourState(0);
		}
		else
		{
			hoarderBugAI.nestPosition = new Vector3(2000f, 2000f, 2000f);
		}
		base.EnableAIControl(enabled);
	}

	protected override void LateUpdate()
	{
		if (isInitialized)
		{
			base.LateUpdate();
			if (!isAIControlled)
			{
				hoarderBugAI.nestPosition = new Vector3(2000f, 2000f, 2000f);
			}
		}
	}

	protected override void UsePrimarySkillAction()
	{
		if (hoarderBugAI.heldItem != null)
		{
			UseHeldItem();
		}
	}

	protected override void UseSecondarySkillAction()
	{
		ControlCompanyRemakePlugin.Instance.logger.LogMessage("Using hoarder bug secondary");
		if (hoarderBugAI.heldItem == null)
		{
			GrabNearbyItem();
		}
		else
		{
			DropCurrentItem();
		}
	}

	public void UseHeldItem()
	{
		GrabbableObject itemGrabbableObject = hoarderBugAI.heldItem.itemGrabbableObject;
		Type type = itemGrabbableObject.GetType();
		if (allowedGrabbableItemUse.Contains(type))
		{
			if (type == typeof(ShotgunItem))
			{
				ShotgunItem shotgunItem = (ShotgunItem)itemGrabbableObject;
				shotgunItem.gunShootAudio.volume = 0.15f;
				shotgunItem.shotgunRayPoint = enemyGameObject.transform;
				shotgunItem.ShootGunAndSync(heldByPlayer: false);
			}
			else
			{
				itemGrabbableObject.UseItemOnClient();
			}
		}
	}

	public void GrabNearbyItem()
	{
		if (hoarderBugAI.heldItem != null)
		{
			return;
		}
		Collider[] array = Physics.OverlapSphere(enemyGameObject.transform.position, grabRange);
		for (int i = 0; i < array.Length; i++)
		{
			GrabbableObject componentInChildren = array[i].GetComponentInChildren<GrabbableObject>();
			if (componentInChildren != null && hoarderBugAI.heldItem == null)
			{
				NetworkObject component = componentInChildren.GetComponent<NetworkObject>();
				if (component != null)
				{
					hoarderBugAI.targetItem = componentInChildren;
					hoarderBugAI.Call("GrabTargetItemIfClose");
					break;
				}
			}
		}
	}

	public void DropCurrentItem()
	{
		if (hoarderBugAI.heldItem != null)
		{
			hoarderBugAI.Call("DropItemAndCallDropRPC", hoarderBugAI.heldItem.itemGrabbableObject.GetComponent<NetworkObject>(), false);
		}
	}

	public override string GetPrimarySkillName()
	{
		return (hoarderBugAI.heldItem != null && allowedGrabbableItemUse.Contains(hoarderBugAI.heldItem.itemGrabbableObject.GetType())) ? "Use item" : "";
	}

	public override string GetSecondarySkillName()
	{
		return (hoarderBugAI.heldItem == null) ? "Grab item" : "Drop item";
	}
}
