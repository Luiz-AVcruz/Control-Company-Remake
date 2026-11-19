using System;
using ControlCompanyRemake.Core.Config;
using UnityEngine;

namespace ControlCompanyRemake.Core.Enemy;

internal class ControllableEnemy
{
	public GameObject EnemyPrefab { get; set; }

	public Type AIType { get; set; }

	public string EnemyName { get; set; }

	public bool OverrideEnemyPrefab { get; set; }

	public GameObject EnemyGameObject { get; set; }

	public bool OverrideEnemySpawnLocation { get; set; }

	public bool Allowlisted { get; set; }

	public Vector3 EnemySpawnLocationOverride { get; set; }

	public Func<GameObject, GameObject, EnemyController> CreateAndAttachEnemyControllerFunc { get; set; }

	public static ControllableEnemy FromSpawnableEnemy(SpawnableEnemyWithRarity spawnableEnemy)
	{
		try
		{
			GameObject gameObject = spawnableEnemy?.enemyType?.enemyPrefab ?? null;
			if (gameObject == null)
			{
				return null;
			}
			EnemyAI componentInChildren = gameObject.GetComponentInChildren<EnemyAI>();
			string enemyName = ((componentInChildren == null) ? spawnableEnemy.enemyType.enemyName : componentInChildren.GetEnemyName());
			return new ControllableEnemy
			{
				EnemyName = enemyName,
				AIType = componentInChildren.GetType(),
				EnemyPrefab = gameObject,
				OverrideEnemyPrefab = false,
				OverrideEnemySpawnLocation = false,
				Allowlisted = componentInChildren.IsEnemyAllowlisted(),
				CreateAndAttachEnemyControllerFunc = delegate(GameObject enemyGameObject, GameObject enemyController)
				{
					EnemyAI componentInChildren2 = enemyGameObject.GetComponentInChildren<EnemyAI>();
					return componentInChildren2.GetAndAttachEnemyController(enemyController);
				}
			};
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static ControllableEnemy FromEnemyAI(EnemyAI enemyAI)
	{
		GameObject enemyPrefab = enemyAI.enemyType.enemyPrefab;
		string enemyName = ((enemyAI == null) ? enemyAI.enemyType.enemyName : enemyAI.GetEnemyName());
		return new ControllableEnemy
		{
			EnemyName = enemyName,
			AIType = enemyAI.GetType(),
			EnemyPrefab = enemyPrefab,
			OverrideEnemyPrefab = false,
			OverrideEnemySpawnLocation = false,
			Allowlisted = enemyAI.IsEnemyAllowlisted(),
			CreateAndAttachEnemyControllerFunc = delegate(GameObject enemyGameObject, GameObject enemyController)
			{
				EnemyAI componentInChildren = enemyGameObject.GetComponentInChildren<EnemyAI>();
				return componentInChildren.GetAndAttachEnemyController(enemyController);
			}
		};
	}
}
