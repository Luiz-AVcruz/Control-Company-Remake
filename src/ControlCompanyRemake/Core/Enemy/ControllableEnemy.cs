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
			GameObject prefab = spawnableEnemy?.enemyType?.enemyPrefab;
			if (prefab == null)
			{
				return null;
			}

			EnemyAI enemyAI = prefab.GetComponentInChildren<EnemyAI>();
			return enemyAI == null ? null : Create(enemyAI, prefab, enemyAI.GetEnemyName());
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static ControllableEnemy FromEnemyAI(EnemyAI enemyAI)
	{
		GameObject prefab = enemyAI.enemyType.enemyPrefab;
		string enemyName = enemyAI == null ? enemyAI.enemyType.enemyName : enemyAI.GetEnemyName();
		return Create(enemyAI, prefab, enemyName);
	}

	private static ControllableEnemy Create(EnemyAI enemyAI, GameObject prefab, string enemyName)
	{
		return new ControllableEnemy
		{
			EnemyName = enemyName,
			AIType = enemyAI.GetType(),
			EnemyPrefab = prefab,
			Allowlisted = enemyAI.IsEnemyAllowlisted(),
			CreateAndAttachEnemyControllerFunc = AttachController
		};
	}

	private static EnemyAIController AttachController(GameObject enemyObject, GameObject controllerObject)
	{
		EnemyAI enemyAI = enemyObject.GetComponentInChildren<EnemyAI>();
		return enemyAI.GetAndAttachEnemyController(controllerObject);
	}
}
