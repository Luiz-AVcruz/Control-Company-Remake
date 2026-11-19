using ControlCompanyRemake.Core.Extentions;
using UnityEngine;

namespace ControlCompanyRemake.Core;

internal class DoorOpener : MonoBehaviour
{
	public void DetectAndOpenDoors(float range)
	{
		Collider[] array = Physics.OverlapSphere(base.transform.position, range);
		for (int i = 0; i < array.Length; i++)
		{
			DoorLock componentInChildren = array[i].GetComponentInChildren<DoorLock>();
			if (componentInChildren != null)
			{
				OpenDoor(componentInChildren);
			}
		}
	}

	public void OpenDoor(DoorLock doorLock)
	{
		if (doorLock != null && !doorLock.GetFieldValue<bool>("isDoorOpened"))
		{
			doorLock.OpenDoorAsEnemyServerRpc();
			doorLock.gameObject.GetComponent<AnimatedObjectTrigger>().TriggerAnimationNonPlayer(playSecondaryAudios: false, overrideBool: true);
		}
	}
}
