using System.Collections;
using ControlCompanyRemake.Core.Extentions;
using Unity.Netcode;
using UnityEngine;

namespace ControlCompanyRemake.Core.Enemy;

internal class RadMechEnemyController : EnemyAIController
{
	private RadMechAI radMechAI;

	private float originalRunSpeed = 0f;

	private float originalWalkSpeed = 0f;

	private float scanTargetRange = 200f;

	private float fireRate;

	private float shootTimer = 0f;

	private float locateNewTargetTime = 2f;

	private float locateNewTargetTimer = 0f;

	private bool isFiring = false;

	private int visibleThreatsMask;

	// Old Bird audio levels are never restored after control.
	public override bool CanLeaveWithAI => false;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		radMechAI = (RadMechAI)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
		originalRunSpeed = base.RunSpeed;
		originalWalkSpeed = base.WalkSpeed;
		visibleThreatsMask = radMechAI.GetFieldValue<int>("visibleThreatsMask");
		fireRate = radMechAI.fireRate;
		AdjustAudio();
	}

	protected override void LateUpdate()
	{
		if (!isInitialized)
		{
			return;
		}
		base.LateUpdate();
		if (isAIControlled || !hasControl)
		{
			return;
		}
		radMechAI.SetFieldValue("shootTimer", 0f);
		if (MoveInput().sqrMagnitude <= 0.1f)
		{
			radMechAI.SetFieldValue("walkStepTimer", 0.1f);
			radMechAI.SetFieldValue("takingStep", value: false);
		}
		else
		{
			if (radMechAI.aimingGun && !isFiring)
			{
				radMechAI.SetAimingGun(setAiming: false);
			}
			bool fieldValue = radMechAI.GetFieldValue<bool>("takingStep");
			float fieldValue2 = radMechAI.GetFieldValue<float>("walkStepTimer");
			radMechAI.SetFieldValue("walkStepTimer", Mathf.Min(fieldValue2, 0.7f));
			if (!fieldValue)
			{
				base.RunSpeed = 0f;
				base.WalkSpeed = 0f;
				if (IsSprinting())
				{
					radMechAI.SetFieldValue("walkStepTimer", Mathf.Min(fieldValue2, 0.2f));
				}
			}
			else
			{
				base.RunSpeed = originalRunSpeed;
				base.WalkSpeed = originalWalkSpeed;
			}
		}
		if (isFiring)
		{
			shootTimer += Time.deltaTime;
			locateNewTargetTimer += Time.deltaTime;
			if (locateNewTargetTimer >= locateNewTargetTime)
			{
				locateNewTargetTimer = 0f;
				ScanAndSetTarget();
			}
			if (!radMechAI.aimingGun)
			{
				radMechAI.SetAimingGun(setAiming: true);
			}
			if (shootTimer >= fireRate)
			{
				shootTimer = 0f;
				radMechAI.StartShootGun();
			}
		}
	}

	protected override void UsePrimarySkillAction()
	{
		if (radMechAI.focusedThreatTransform == null)
		{
			ScanAndSetTarget();
		}
		if (radMechAI.focusedThreatTransform != null)
		{
			radMechAI.SwitchToBehaviourState(1);
			isFiring = true;
		}
	}

	protected override void ReleasePrimarySkillAction()
	{
		isFiring = false;
		shootTimer = 0f;
		locateNewTargetTimer = 0f;
		radMechAI.SetAimingGun(setAiming: false);
		radMechAI.SwitchToBehaviourState(0);
	}

	protected override void UseSecondarySkillAction()
	{
	}

	protected override void ReleaseSecondarySkillAction()
	{
	}

	public override void DestroyAndCleanUp()
	{
		StartCoroutine(DelayedDestroyAndCleanUp());
	}

	private IEnumerator DelayedDestroyAndCleanUp()
	{
		EnableControl(enable: false);
		EnableCamera(enable: false);
		yield return new WaitForSeconds(3f);
		base.DestroyAndCleanUp();
	}

	public override string GetPrimarySkillName()
	{
		return "Fire";
	}

	public override string GetSecondarySkillName()
	{
		return "";
	}

	private void AdjustAudio()
	{
		radMechAI.creatureSFX.volume *= 0.25f;
		radMechAI.LocalLRADAudio.volume *= 0.25f;
		radMechAI.LocalLRADAudio2.volume *= 0.25f;
		radMechAI.explosionAudio.volume *= 0.25f;
		radMechAI.blowtorchAudio.volume *= 0.25f;
		radMechAI.flyingDistantAudio.volume *= 0.25f;
		radMechAI.spotlightOnAudio.volume *= 0.25f;
		radMechAI.chargeForwardAudio.volume *= 0.25f;
		radMechAI.engineSFX.volume *= 0.25f;
		radMechAI.chargeForwardAudio.volume *= 0.25f;
		radMechAI.chargeForwardAudio.volume *= 0.25f;
	}

	private void FireGun()
	{
	}

	private void ReloadGun()
	{
	}

	private void ScanAndSetTarget()
	{
		float num = float.MaxValue;
		Transform eye = radMechAI.eye;
		IVisibleThreat visibleThreat = null;
		Transform transform = null;
		Collider[] array = Physics.OverlapSphere(eye.position + eye.forward * 58f + -eye.up * 10f, scanTargetRange, visibleThreatsMask, QueryTriggerInteraction.Collide);
		Collider[] array2 = array;
		foreach (Collider collider in array2)
		{
			if (collider == radMechAI.ownCollider)
			{
				continue;
			}
			IVisibleThreat componentInChildren = collider.GetComponentInChildren<IVisibleThreat>();
			if (componentInChildren != null && base.transform.IsObjectWithinAngle(collider.transform, base.transform.forward, 180f))
			{
				float magnitude = (collider.transform.position - eye.position).magnitude;
				if (magnitude < num)
				{
					num = magnitude;
					visibleThreat = componentInChildren;
					transform = collider.transform;
				}
			}
		}
		if (visibleThreat != null)
		{
			float dist = Vector3.Distance(eye.position, transform.position);
			radMechAI.SetTargetedThreat(visibleThreat, transform.position + Vector3.up * 0.5f, dist);
			radMechAI.focusedThreatTransform = visibleThreat.GetThreatTransform();
			NetworkObject component = radMechAI.focusedThreatTransform.gameObject.GetComponent<NetworkObject>();
			radMechAI.SwitchToBehaviourStateOnLocalClient(1);
			radMechAI.SetTargetToThreatClientRpc(component, radMechAI.targetedThreat.lastSeenPosition);
		}
		else
		{
			radMechAI.EnableSpotlight();
		}
	}
}
