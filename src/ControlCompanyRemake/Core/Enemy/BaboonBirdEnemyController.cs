using ControlCompanyRemake.Core.Extentions;
using Unity.Netcode;
using UnityEngine;

namespace ControlCompanyRemake.Core.Enemy;

internal class BaboonBirdEnemyController : EnemyAIController
{
	private BaboonBirdAI baboonBirdAI;

	private float grabRange = 1.5f;

	private Vector3 originalBaboonCampPosition;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		baboonBirdAI = (BaboonBirdAI)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
	}

	protected override void EnableAIControl(bool enabled)
	{
		if (enabled)
		{
			BaboonBirdAI.baboonCampPosition = originalBaboonCampPosition;
		}
		else
		{
			originalBaboonCampPosition = BaboonBirdAI.baboonCampPosition;
			BaboonBirdAI.baboonCampPosition = new Vector3(1000f, 1000f, 1000f);
		}
		base.EnableAIControl(enabled);
	}

	protected override void UseSecondarySkillAction()
	{
		if (baboonBirdAI.heldScrap == null)
		{
			GrabNearbyItem();
		}
		else
		{
			DropCurrentItem();
		}
	}

	public void GrabNearbyItem()
	{
		if (baboonBirdAI.heldScrap != null)
		{
			return;
		}
		Collider[] array = Physics.OverlapSphere(enemyGameObject.transform.position, grabRange);
		NetworkObject networkObject = null;
		float num = float.PositiveInfinity;
		for (int i = 0; i < array.Length; i++)
		{
			GrabbableObject componentInChildren = array[i].GetComponentInChildren<GrabbableObject>();
			if (!(componentInChildren != null) || !(bool)baboonBirdAI.Call("CanGrabScrap", componentInChildren))
			{
				continue;
			}
			NetworkObject component = componentInChildren.GetComponent<NetworkObject>();
			if (component != null)
			{
				float num2 = Vector3.Distance(componentInChildren.transform.position, base.transform.position);
				if (num2 < num)
				{
					num = num2;
					networkObject = component;
				}
			}
		}
		if (networkObject != null)
		{
			baboonBirdAI.SwitchToBehaviourState(1);
			baboonBirdAI.Call("GrabItemAndSync", networkObject);
		}
	}

	public void DropCurrentItem()
	{
		if (!(baboonBirdAI.heldScrap == null))
		{
			baboonBirdAI.Call("DropHeldItemAndSync", true);
		}
	}

	public override void DestroyAndCleanUp()
	{
		if (!baboonBirdAI.isEnemyDead && baboonBirdAI.heldScrap != null)
		{
			DropCurrentItem();
		}
		base.DestroyAndCleanUp();
	}

	public override string GetSecondarySkillName()
	{
		return (baboonBirdAI.heldScrap == null) ? "Grab item" : "Drop item";
	}
}
