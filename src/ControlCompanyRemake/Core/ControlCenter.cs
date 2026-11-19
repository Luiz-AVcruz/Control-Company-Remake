using System;
using System.Collections.Generic;
using System.Linq;
using ControlCompanyRemake.Core.Config;
using ControlCompanyRemake.Core.Enemy;
using ControlCompanyRemake.Core.Enemy.NonStandardEnemy;
using ControlCompanyRemake.Core.Extentions;
using ControlCompanyRemake.Core.Input;
using ControlCompanyRemake.Core.UI;
using Dissonance;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

namespace ControlCompanyRemake.Core;

internal class ControlCenter : MonoBehaviour
{
	public enum Mode
	{
		HOST,
		GHOST,
		ENEMY
	}

	private enum EnemyCatalog { Auto, Interior, Exterior }

	public static ControlCenter Instance;
	public static bool LocalInputBlocked => Instance != null && Instance.settings.BlocksInput;

	private readonly SettingsMenu settings = new SettingsMenu();
	private EnemyCatalog enemyCatalog;
	private int spawnPageCount;
	private string spawnFeedback;
	private string controlledEnemyName;
	private bool crossAreaControl;

	private CustomPlayerController ghostController;

	private GameObject ghost;

	private EnemyController currentControlledEnemy;

	private int currentTargetIndex = 0;

	private PlayerControllerB currentTarget;

	private Mode currentMode = Mode.HOST;

	private bool allowAllEnemies = false;

	private bool initialMicPushToTalkSetting = false;

	private bool isExtendedModeEnabled = false;

	private bool scrapSpawnMode = false;

	private int currentSpawnScrapPage = 0;

	private const int scrapSpawnPageSize = 10;

	private int currentSpawnEnemiesPage = 0;

	private const int spawnEnemiesPageSize = 12;

	private List<ControllableEnemy> currentSpawnableEnemyList = new List<ControllableEnemy>();

	private bool loadedConfig = false;

	private bool showUI = true;

	private float gameStartedTime = 0f;

	private bool inRoundEndPhase = false;

	private bool isGhostIndoors = false;

	private List<ControllableEnemy> allIndoorEnemies = new List<ControllableEnemy>();

	private List<ControllableEnemy> allOutdoorEnemies = new List<ControllableEnemy>();

	private List<ControllableEnemy> indoorEnemiesOfLevel = new List<ControllableEnemy>();

	private List<ControllableEnemy> outdoorEnemiesOfLevel = new List<ControllableEnemy>();

	public Mode CurrentMode => currentMode;

	public EnemyController CurrentControlledEnemy => currentControlledEnemy;

	public bool IsGhostIndoors
	{
		get => isGhostIndoors;
		set
		{
			isGhostIndoors = value;
			RefreshEnemyPage(0);
		}
	}

	private void Awake()
	{
		Instance = this;
	}

	private void OnEnable()
	{
		if (!loadedConfig)
		{
			loadedConfig = true;
			ConfigManager.Instance.LoadPlayerConfig(reload: true);
		}
	}

	private void OnDisable()
	{
		settings.Reset();
	}

	private void Update()
	{
		bool activeRound = IsGameStarted();
		PlayerControllerB host = activeRound && IsHost() ? GameNetworkManager.Instance.localPlayerController : null;
		bool canOpenSettings = host != null && !inRoundEndPhase && !host.inTerminalMenu && !host.inSpecialMenu
			&& (host.quickMenuManager == null || !host.quickMenuManager.isMenuOpen) && (currentMode != Mode.HOST || !host.isTypingChat);
		settings.Update(host, canOpenSettings);
		if (!activeRound || settings.BlocksInput || Keyboard.current == null)
		{
			return;
		}
		if (InputKeys.TOGGLE_UI_KEY.wasPressedThisFrame)
		{
			showUI = !showUI;
		}
		gameStartedTime += Time.deltaTime;
		if (!IsHost() || inRoundEndPhase)
		{
			return;
		}
		if ((currentTarget == null || !currentTarget.isPlayerControlled || currentTarget.isPlayerDead) && GetAlivePlayers().Count > 0)
		{
			SetNextTargetPlayer(1);
		}
		if (ghost == null)
		{
			GameObject gameObject = new GameObject("ghost");
			ghostController = gameObject.AddComponent<CustomPlayerController>();
			ghostController.Initialize(GetHostPlayer().playerActions, CustomPlayerController.CameraMode.FIRST_PERSON);
			ghost = gameObject;
			ghost.SetActive(value: false);
			ControlCompanyRemakePlugin.Instance.logger.LogInfo("Creating ghost");
		}
		if (currentMode == Mode.ENEMY && currentControlledEnemy == null)
		{
			EnableGhost(enable: true, ghost.transform.position);
			crossAreaControl = false;
			currentMode = Mode.GHOST;
		}

		if (currentMode != Mode.HOST)
		{
			GetHostPlayer().isTypingChat = true;
		}
		if (IsListeningFromHost() && !IsHostDead() && currentMode != Mode.HOST)
		{
			if (ghostController != null)
			{
				ghostController.AudioListener.enabled = false;
			}
			if (currentControlledEnemy != null)
			{
				currentControlledEnemy.AudioListener.enabled = false;
			}
			GetHostPlayer().activeAudioListener.enabled = true;
			ChangeAudioListener(GetHostPlayer().activeAudioListener);
		}
		else if (currentMode == Mode.GHOST)
		{
			if (ghostController != null && ghostController.IsInitialized)
			{
				GetHostPlayer().activeAudioListener.enabled = false;
				ghostController.AudioListener.enabled = true;
				ChangeAudioListener(ghostController.AudioListener);
			}
		}
		else if (currentMode == Mode.ENEMY)
		{
			if (currentControlledEnemy.IsInitialized)
			{
				GetHostPlayer().activeAudioListener.enabled = false;
				currentControlledEnemy.AudioListener.enabled = true;
				ChangeAudioListener(currentControlledEnemy.AudioListener);
			}
		}
		else if (currentMode == Mode.HOST)
		{
			ChangeAudioListener(GetHostPlayer().activeAudioListener);
		}
		InputUpdate();
	}

	private void InputUpdate()
	{
		if (!IsGameStarted() || !IsHost() || inRoundEndPhase || (ConfigManager.Instance.DisableWhileUsingTerminal && GetHostPlayer().inTerminalMenu))
		{
			return;
		}
		if (InputKeys.EXTENDED_MODE_KEY.wasPressedThisFrame)
		{
			isExtendedModeEnabled = !isExtendedModeEnabled;
			allowAllEnemies = isExtendedModeEnabled;
			RefreshEnemyPage(0);
		}
		switch (currentMode)
		{
		case Mode.ENEMY:
			if (!currentControlledEnemy.IsAIControlled && currentControlledEnemy.CanControlEnemy())
			{
				if (InputKeys.ENEMY_PRIMARY_SKILL_BUTTON.wasPressedThisFrame)
				{
					currentControlledEnemy.UsePrimarySkill();
				}
				else if (InputKeys.ENEMY_PRIMARY_SKILL_BUTTON.wasReleasedThisFrame)
				{
					currentControlledEnemy.ReleasePrimarySkill();
				}
				else if (InputKeys.ENEMY_SECONDARY_SKILL_BUTTON.wasPressedThisFrame)
				{
					currentControlledEnemy.UseSecondarySkill();
				}
				else if (InputKeys.ENEMY_SECONDARY_SKILL_BUTTON.wasReleasedThisFrame)
				{
					currentControlledEnemy.ReleaseSecondarySkill();
				}
				currentControlledEnemy.SetLockEnemyRotation(InputKeys.LOCK_ENEMY_ROTATION_KEY.isPressed);
			}
			if (InputKeys.EXIT_KEY.wasPressedThisFrame)
			{
				ExitEnemyMode(leaveEnemyWithAI: InputKeys.CanUseShiftWithExit && Keyboard.current.shiftKey.isPressed);
			}
			else if (!crossAreaControl && InputKeys.ENEMY_TOGGLE_AI_KEY.wasPressedThisFrame)
			{
				currentControlledEnemy.ToggleAIControl();
			}
			else if (InputKeys.ENEMY_TOGGLE_LIGHT_KEY.wasPressedThisFrame)
			{
				currentControlledEnemy.ToggleLight();
			}
			break;
		case Mode.HOST:
			if (InputKeys.HOST_GHOST_MODE_KEY.wasPressedThisFrame)
			{
				isGhostIndoors = IsTargetInside();
				EnableGhost(enable: true, currentTarget.gameObject.transform.position);
				ghostController.EnableLight(isGhostIndoors);
				EnableHostPlayer(enable: false);
				currentMode = Mode.GHOST;
			}
			else if (InputKeys.CYCLE_KEY_LEFT.wasPressedThisFrame)
			{
				SetNextTargetPlayer(-1);
			}
			else if (InputKeys.CYCLE_KEY_RIGHT.wasPressedThisFrame)
			{
				SetNextTargetPlayer(1);
			}
			if (!isExtendedModeEnabled)
			{
				break;
			}
			if (InputKeys.HOST_TELEPORT_TO_ENTRANCE_KEY.wasPressedThisFrame)
			{
				if (!GetHostPlayer().isPlayerDead)
				{
					EntranceTeleport mainEntrance = GetMainEntrance(getMainEntranceThatIsOutside: true);
					if (mainEntrance != null)
					{
						mainEntrance.TeleportPlayer();
					}
				}
			}
			else if (InputKeys.HOST_GIVE_CREDITS_KEY.wasPressedThisFrame)
			{
				UnityEngine.Object.FindObjectOfType<Terminal>().groupCredits += 100;
			}
			break;
		case Mode.GHOST:
			if (InputKeys.EXIT_KEY.wasPressedThisFrame)
			{
				EnableGhost(enable: false);
				EnableHostPlayer(enable: true);
				currentMode = Mode.HOST;
				GetHostPlayer().isTypingChat = false;
				break;
			}
			if (InputKeys.CATALOG_KEY.wasPressedThisFrame)
			{
				enemyCatalog = (EnemyCatalog)(((int)enemyCatalog + 1) % 3);
				RefreshEnemyPage(0);
				break;
			}
			if (InputKeys.CYCLE_KEY_LEFT.wasPressedThisFrame || InputKeys.CYCLE_KEY_RIGHT.wasPressedThisFrame)
			{
				RefreshEnemyPage(currentSpawnEnemiesPage + (InputKeys.CYCLE_KEY_LEFT.wasPressedThisFrame ? -1 : 1));
				break;
			}
			if (!scrapSpawnMode)
			{
				for (int i = 0; i < currentSpawnableEnemyList.Count && i < InputKeys.GHOST_SPAWN_ENEMY_KEYS.Count; i++)
				{
					if (InputKeys.GHOST_SPAWN_ENEMY_KEYS[i].wasPressedThisFrame)
					{
						TrySpawnSelectedEnemy(currentSpawnableEnemyList[i]);
						break;
					}
				}
			}
			else
			{
				List<Item> list = GetSpawnableScraps().Chunk(10).ToList()[currentSpawnScrapPage].ToList();
				for (int j = 0; j < InputKeys.GHOST_SPAWN_ENEMY_KEYS.Count && j < list.Count; j++)
				{
					if (InputKeys.GHOST_SPAWN_ENEMY_KEYS[j].wasPressedThisFrame)
					{
						SpawnScrap(list[j], ghost.transform.position);
						break;
					}
				}
			}
			if (scrapSpawnMode && Keyboard.current.downArrowKey.wasPressedThisFrame)
			{
				int num = Mathf.CeilToInt((float)GetSpawnableScraps().Count / 10f);
				ControlCompanyRemakePlugin.Instance.logger.LogMessage($"Total scraps: {GetSpawnableScraps().Count()}, final page of scrap: {num}");
				currentSpawnScrapPage++;
				if (currentSpawnScrapPage >= num)
				{
					currentSpawnScrapPage = 0;
				}
			}
			break;
		}
	}

	private void ExitEnemyMode(bool leaveEnemyWithAI)
	{
		EnemyController enemyController = currentControlledEnemy;
		if (leaveEnemyWithAI && (crossAreaControl || enemyController is not EnemyAIController { CanLeaveWithAI: true }))
		{
			return;
		}

		EnableGhost(enable: true, enemyController.transform.position);
		ghostController.EnableLight(isGhostIndoors);
		if (leaveEnemyWithAI)
		{
			// DestroyAndCleanUp also despawns the enemy.
			enemyController.gameObject.SetActive(false);
			UnityEngine.Object.Destroy(enemyController.gameObject);
		}
		else
		{
			enemyController.DestroyAndCleanUp();
		}
		currentControlledEnemy = null;
		crossAreaControl = false;
		currentMode = Mode.GHOST;
	}

	private void ChangeAudioListener(AudioListener audioListener)
	{
		StartOfRound.Instance.audioListener = audioListener;
	}

	private EntranceTeleport GetMainEntrance(bool getMainEntranceThatIsOutside)
	{
		EntranceTeleport[] array = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
		foreach (EntranceTeleport entranceTeleport in array)
		{
			if (entranceTeleport.entranceId != 0)
			{
				continue;
			}
			if (getMainEntranceThatIsOutside)
			{
				if (entranceTeleport.isEntranceToBuilding)
				{
					return entranceTeleport;
				}
			}
			else if (!entranceTeleport.isEntranceToBuilding)
			{
				return entranceTeleport;
			}
		}
		return null;
	}

	private bool IsTargetInside()
	{
		return currentTarget != null && currentTarget.isInsideFactory;
	}

	private void OnGUI()
	{
		if (UIManager.Instance == null || Keyboard.current == null || !IsGameStarted() || (!showUI && !settings.IsOpen))
		{
			return;
		}
		UIManager.Instance.Render(settings, RenderHud);
	}

	private void RenderHud()
	{
		UIManager ui = UIManager.Instance;
		if (!IsHost())
		{
			if (gameStartedTime < 30f)
			{
				ui.RenderNotHostLabel();
			}
			return;
		}
		PlayerControllerB host = GetHostPlayer();
		if (inRoundEndPhase || (ConfigManager.Instance.DisableWhileUsingTerminal && host.inTerminalMenu))
		{
			return;
		}
		switch (currentMode)
		{
			case Mode.ENEMY:
				if (currentControlledEnemy != null)
				{
					ui.RenderMonster(currentControlledEnemy, controlledEnemyName, crossAreaControl, host);
				}
				break;
			case Mode.HOST:
				ui.RenderHost(currentTarget, isExtendedModeEnabled);
				break;
			case Mode.GHOST:
				if (scrapSpawnMode)
				{
					ui.RenderScrap(GetSpawnableScraps().Skip(currentSpawnScrapPage * scrapSpawnPageSize).Take(scrapSpawnPageSize).ToList(), host);
					break;
				}
				ui.RenderGhost(enemyCatalog.ToString(), isGhostIndoors, currentSpawnEnemiesPage + 1, spawnPageCount,
					currentSpawnableEnemyList, spawnFeedback, host, GetSpawnRestriction);
				break;
		}
	}

	private bool IsHostDead()
	{
		return GetHostPlayer().isPlayerDead;
	}

	private void EnableGhost(bool enable)
	{
		EnableGhost(enable, Vector3.zero);
	}

	private void EnableGhost(bool enable, Vector3 position)
	{
		if (enable)
		{
			ghost.transform.position = position + new Vector3(0f, 0.5f, 0f);
			ghost.transform.rotation = Quaternion.identity;
			ghost.SetActive(value: true);
			ChangeAudioListener(ghostController.AudioListener);
			RefreshEnemyPage(0);
		}
		else
		{
			ghost.SetActive(value: false);
		}
	}

	private void EnableHostPlayer(bool enable)
	{
		PlayerControllerB hostPlayer = GetHostPlayer();
		if (enable)
		{
			if (ConfigManager.Instance.ForcePushToTalkWhileInMonsterMode)
			{
				ResetMicPushToTalk();
			}
			GetHostPlayer().activeAudioListener.enabled = true;
			hostPlayer.playerActions.Movement.PingScan.Enable();
			ChangeAudioListener(hostPlayer.activeAudioListener);
			StartOfRound.Instance.activeCamera.enabled = true;
		}
		else
		{
			if (ConfigManager.Instance.ForcePushToTalkWhileInMonsterMode)
			{
				ForceMicPushToTalk();
			}
			GetHostPlayer().activeAudioListener.enabled = false;
			hostPlayer.playerActions.Movement.PingScan.Disable();
			StartOfRound.Instance.activeCamera.enabled = false;
		}
	}

	private void EnablePlayer2DMics(bool enable, List<PlayerControllerB> players)
	{
		foreach (PlayerControllerB player in players)
		{
			if (player.isHostPlayerObject)
			{
				continue;
			}
			AudioSource currentVoiceChatAudioSource = player.currentVoiceChatAudioSource;
			if (enable)
			{
				if (currentVoiceChatAudioSource != null)
				{
					currentVoiceChatAudioSource.spatialBlend = 0f;
				}
				player.currentVoiceChatIngameSettings.set2D = true;
				player.voicePlayerState.Volume = 1f;
			}
			else
			{
				if (currentVoiceChatAudioSource != null)
				{
					currentVoiceChatAudioSource.spatialBlend = 1f;
				}
				player.currentVoiceChatIngameSettings.set2D = false;
				player.voicePlayerState.Volume = 0f;
			}
		}
	}

	private void SetNextTargetPlayer(int direction)
	{
		List<PlayerControllerB> alivePlayers = GetAlivePlayers();
		if (alivePlayers.Count == 0)
		{
			SetCurrentTargetToHost();
			ControlCompanyRemakePlugin.Instance.logger.LogMessage("Alive player count is zero");
		}
		else
		{
			currentTargetIndex += direction;
			if (currentTargetIndex >= alivePlayers.Count)
			{
				currentTargetIndex = 0;
			}
			else if (currentTargetIndex < 0)
			{
				currentTargetIndex = alivePlayers.Count - 1;
			}
			currentTargetIndex = Mathf.Clamp(currentTargetIndex, 0, alivePlayers.Count - 1);
			currentTarget = alivePlayers[currentTargetIndex];
		}
		ControlCompanyRemakePlugin.Instance.logger.LogMessage($"Current target index: {currentTargetIndex}");
		ControlCompanyRemakePlugin.Instance.logger.LogMessage("Setting next target to: " + currentTarget?.playerUsername);
	}

	public void SetCurrentTargetToHost()
	{
		currentTargetIndex = 0;
		currentTarget = GetHostPlayer();
		ControlCompanyRemakePlugin.Instance.logger.LogMessage("Setting next target to host");
	}

	private bool SelectedCatalogIsIndoors => enemyCatalog == EnemyCatalog.Auto ? isGhostIndoors : enemyCatalog == EnemyCatalog.Interior;

	private bool IsCrossAreaCatalog => SelectedCatalogIsIndoors != isGhostIndoors;

	private void RefreshEnemyPage(int page)
	{
		List<ControllableEnemy> enemies = GetSpawnableEnemies(SelectedCatalogIsIndoors);
		spawnPageCount = (enemies.Count + spawnEnemiesPageSize - 1) / spawnEnemiesPageSize;
		currentSpawnEnemiesPage = spawnPageCount == 0 ? 0 : (page % spawnPageCount + spawnPageCount) % spawnPageCount;
		currentSpawnableEnemyList = enemies.Skip(currentSpawnEnemiesPage * spawnEnemiesPageSize).Take(spawnEnemiesPageSize).ToList();
		spawnFeedback = null;
	}

	private string GetSpawnRestriction(ControllableEnemy enemy)
	{
		if (!IsCrossAreaCatalog)
		{
			return null;
		}
		Type type = enemy.AIType;
		if (!(isGhostIndoors ? type == typeof(MouthDogAI) : type == typeof(CrawlerAI)))
		{
			return "Native area only";
		}
		return ConfigManager.Instance.CrossAreaPreview ? null : "Cross-area test only";
	}

	public List<SpawnableEnemyWithRarity> GetDaytimeEnemies()
	{
		return new List<SpawnableEnemyWithRarity>(GetCurrentLevel().DaytimeEnemies);
	}

	public void StartOfRoundInitialization()
	{
		initialMicPushToTalkSetting = IngamePlayerSettings.Instance.settings.pushToTalk;
		allIndoorEnemies = GetAllIndoorEnemiesAcrossAllMapsList();
		allOutdoorEnemies = GetAllOutdoorEnemiesAcrossAllMapsList();
		indoorEnemiesOfLevel = new List<ControllableEnemy>(from enemy in GetCurrentLevel().Enemies
			select ControllableEnemy.FromSpawnableEnemy(enemy) into x
			where x != null
			select x);
		outdoorEnemiesOfLevel = new List<ControllableEnemy>(from enemy in GetCurrentLevel().OutsideEnemies.Concat(GetCurrentLevel().DaytimeEnemies)
			select ControllableEnemy.FromSpawnableEnemy(enemy) into x
			where x != null
			select x);
		ControllableEnemy maskedEnemy = GetMaskedEnemy();
		if (maskedEnemy != null)
		{
			indoorEnemiesOfLevel.Add(maskedEnemy);
			outdoorEnemiesOfLevel.Add(maskedEnemy);
			allIndoorEnemies.Add(maskedEnemy);
			allOutdoorEnemies.Add(maskedEnemy);
		}
		ControllableEnemy companyMonsterEnemy = GetCompanyMonsterEnemy();
		if (companyMonsterEnemy != null)
		{
			indoorEnemiesOfLevel.Add(companyMonsterEnemy);
			outdoorEnemiesOfLevel.Add(companyMonsterEnemy);
			allIndoorEnemies.Add(companyMonsterEnemy);
			allOutdoorEnemies.Add(companyMonsterEnemy);
		}
		ControllableEnemy secretEnemy = GetSecretEnemy();
		if (secretEnemy != null)
		{
			indoorEnemiesOfLevel.Insert(0, secretEnemy);
			allIndoorEnemies.Insert(0, secretEnemy);
		}
		indoorEnemiesOfLevel = indoorEnemiesOfLevel.DedupeBy((ControllableEnemy enemy) => enemy.EnemyName).ToList();
		outdoorEnemiesOfLevel = outdoorEnemiesOfLevel.DedupeBy((ControllableEnemy enemy) => enemy.EnemyName).ToList();
		allIndoorEnemies = allIndoorEnemies.DedupeBy((ControllableEnemy enemy) => enemy.EnemyName).ToList();
		allOutdoorEnemies = allOutdoorEnemies.DedupeBy((ControllableEnemy enemy) => enemy.EnemyName).ToList();
		gameStartedTime = 0f;
		SetCurrentTargetToHost();
		ResetToDefault();
		inRoundEndPhase = false;
		ControlCompanyRemakePlugin.Instance.logger.LogMessage("Start of round initialization completed.");
	}

	public void EndOfRoundCleanUp()
	{
		ResetToDefault();
		inRoundEndPhase = true;
		ControlCompanyRemakePlugin.Instance.logger.LogMessage("End of round clean up completed.");
	}

	private void ResetToDefault()
	{
		settings.Reset();
		StartOfRound.Instance.audioListener = GetHostPlayer().activeAudioListener;
		GetHostPlayer().isTypingChat = false;
		currentMode = Mode.HOST;
		if (ghost != null)
		{
			UnityEngine.Object.Destroy(ghost);
			ghost = null;
		}
		if (currentControlledEnemy != null)
		{
			currentControlledEnemy.DestroyAndCleanUp();
		}
		currentControlledEnemy = null;
		crossAreaControl = false;
		enemyCatalog = EnemyCatalog.Auto;
		RefreshEnemyPage(0);
		EnableHostPlayer(enable: true);
		ControlCompanyRemakePlugin.Instance.logger.LogMessage("Reset to default.");
	}

	public bool IsListeningFromHost()
	{
		return InputKeys.HOST_LISTEN_KEY.isPressed;
	}

	public PlayerControllerB GetHostPlayer()
	{
		return RoundManager.Instance.playersManager.allPlayerObjects[0].GetComponentInChildren<PlayerControllerB>();
	}

	public List<PlayerControllerB> GetDeadPlayers()
	{
		return (from player in GetAllPlayers()
			where player.isPlayerDead
			select player).ToList();
	}

	public List<PlayerControllerB> GetAlivePlayers()
	{
		return (from player in GetAllPlayers()
			where !player.isPlayerDead
			select player).ToList();
	}

	public List<PlayerControllerB> GetAllPlayers()
	{
		List<PlayerControllerB> list = new List<PlayerControllerB>();
		GameObject[] allPlayerObjects = RoundManager.Instance.playersManager.allPlayerObjects;
		for (int i = 0; i < allPlayerObjects.Length; i++)
		{
			PlayerControllerB componentInChildren = allPlayerObjects[i].GetComponentInChildren<PlayerControllerB>();
			if (componentInChildren != null && componentInChildren.isPlayerControlled)
			{
				list.Add(componentInChildren);
			}
		}
		return list;
	}

	public bool DoesCurrentLevelExist()
	{
		return GetCurrentLevel() != null;
	}

	public bool IsGameStarted()
	{
		return RoundManager.Instance != null && RoundManager.Instance.currentLevel != null && GameNetworkManager.Instance != null && GameNetworkManager.Instance.gameHasStarted && StartOfRound.Instance != null && !StartOfRound.Instance.inShipPhase;
	}

	public SelectableLevel GetCurrentLevel()
	{
		return RoundManager.Instance.currentLevel;
	}

	public StartOfRound GetPlayersManager()
	{
		return RoundManager.Instance.playersManager;
	}

	private bool IsHost()
	{
		return RoundManager.Instance.IsHost;
	}

	public List<Item> GetSpawnableScraps()
	{
		return StartOfRound.Instance.allItemsList.itemsList;
	}

	public List<ControllableEnemy> GetPagedSpawnableEnemies()
	{
		return currentSpawnableEnemyList;
	}

	public List<ControllableEnemy> GetSpawnableEnemies(bool isIndoor)
	{
		List<ControllableEnemy> list = ((!isIndoor) ? GetSpawnableOutdoorEnemies() : GetSpawnableIndoorEnemies());
		if (list == null || list.Count == 0)
		{
			list = new List<ControllableEnemy>();
		}
		return list;
	}

	public List<ControllableEnemy> GetSpawnableIndoorEnemies()
	{
		return allowAllEnemies ? GetIndoorEnemyList() : GetAllowListedIndoorEnemies();
	}

	public List<ControllableEnemy> GetAllowListedIndoorEnemies()
	{
		return (from enemy in GetIndoorEnemyList()
			where enemy.Allowlisted
			select enemy).ToList();
	}

	public List<ControllableEnemy> GetIndoorEnemyList()
	{
		return allowAllEnemies ? allIndoorEnemies : indoorEnemiesOfLevel;
	}

	public List<ControllableEnemy> GetAllIndoorEnemiesAcrossAllMapsList()
	{
		HashSet<string> hashSet = new HashSet<string>();
		List<ControllableEnemy> list = new List<ControllableEnemy>();
		SelectableLevel[] levels = StartOfRound.Instance.levels;
		foreach (SelectableLevel selectableLevel in levels)
		{
			if (!(selectableLevel != null))
			{
				continue;
			}
			foreach (SpawnableEnemyWithRarity enemy in selectableLevel.Enemies)
			{
				if (enemy.enemyType != null && !hashSet.Contains(enemy.enemyType.enemyName))
				{
					hashSet.Add(enemy.enemyType.enemyName);
					ControllableEnemy controllableEnemy = ControllableEnemy.FromSpawnableEnemy(enemy);
					if (controllableEnemy != null)
					{
						list.Add(controllableEnemy);
					}
				}
			}
		}
		return list;
	}

	public List<ControllableEnemy> GetSpawnableOutdoorEnemies()
	{
		return allowAllEnemies ? GetOutdoorEnemyList() : GetAllowListedOutdoorEnemies();
	}

	public List<ControllableEnemy> GetAllowListedOutdoorEnemies()
	{
		return (from enemy in GetOutdoorEnemyList()
			where enemy.Allowlisted
			select enemy).ToList();
	}

	public List<ControllableEnemy> GetOutdoorEnemyList()
	{
		return allowAllEnemies ? allOutdoorEnemies : outdoorEnemiesOfLevel;
	}

	public List<ControllableEnemy> GetAllOutdoorEnemiesAcrossAllMapsList()
	{
		HashSet<string> hashSet = new HashSet<string>();
		List<ControllableEnemy> list = new List<ControllableEnemy>();
		SelectableLevel[] levels = StartOfRound.Instance.levels;
		foreach (SelectableLevel selectableLevel in levels)
		{
			if (!(selectableLevel != null))
			{
				continue;
			}
			IEnumerable<SpawnableEnemyWithRarity> enumerable = selectableLevel.OutsideEnemies.Concat(selectableLevel.DaytimeEnemies);
			foreach (SpawnableEnemyWithRarity item in enumerable)
			{
				if (item.enemyType != null && !hashSet.Contains(item.enemyType.enemyName))
				{
					hashSet.Add(item.enemyType.enemyName);
					ControllableEnemy controllableEnemy = ControllableEnemy.FromSpawnableEnemy(item);
					if (controllableEnemy != null)
					{
						list.Add(controllableEnemy);
					}
				}
			}
		}
		return list;
	}

	public ControllableEnemy GetMaskedEnemy()
	{
		SelectableLevel[] levels = StartOfRound.Instance.levels;
		foreach (SelectableLevel selectableLevel in levels)
		{
			foreach (SpawnableEnemyWithRarity enemy in selectableLevel.Enemies)
			{
				EnemyAI enemyAI = enemy.enemyType?.enemyPrefab?.GetComponent<EnemyAI>();
				if (enemyAI != null && enemyAI.GetType() == typeof(MaskedPlayerEnemy))
				{
					return ControllableEnemy.FromSpawnableEnemy(enemy);
				}
			}
		}
		return null;
	}

	private ControllableEnemy GetSecretEnemy()
	{
		UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(typeof(EnemyAI));
		UnityEngine.Object[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			EnemyAI enemyAI = (EnemyAI)array2[i];
			if (enemyAI.GetType() == typeof(TestEnemy))
			{
				return ControllableEnemy.FromEnemyAI(enemyAI);
			}
		}
		return null;
	}

	private GameObject SpawnScrap(Item scrap, Vector3 position)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(scrap.spawnPrefab, position, Quaternion.identity);
		GrabbableObject componentInChildren = gameObject.GetComponentInChildren<GrabbableObject>();
		componentInChildren.transform.rotation = Quaternion.Euler(componentInChildren.itemProperties.restingRotation);
		gameObject.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
		return gameObject;
	}

	private void TrySpawnSelectedEnemy(ControllableEnemy enemy)
	{
		string restriction = GetSpawnRestriction(enemy);
		if (restriction != null)
		{
			spawnFeedback = $"{enemy.EnemyName}: {restriction}";
			return;
		}

		bool crossArea = IsCrossAreaCatalog;
		Vector3 position = ghost.transform.position;
		Quaternion rotation = ghost.transform.rotation;
		if (crossArea)
		{
			NavMeshAgent agent = enemy.EnemyPrefab.GetComponentInChildren<NavMeshAgent>();
			if (agent == null || !NavMesh.SamplePosition(position, out NavMeshHit hit, 1.5f,
				new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask }))
			{
				spawnFeedback = $"{enemy.EnemyName}: no navigable ground nearby.";
				return;
			}
			position = hit.position;
		}
		else if (!enemy.OverrideEnemySpawnLocation && ConfigManager.Instance.UseEnemySpawningBehaviour)
		{
			System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 888);
			if (isGhostIndoors)
			{
				RoundManager.Instance.RefreshEnemyVents();
				Transform[] points = (RoundManager.Instance.allEnemyVents ?? Array.Empty<EnemyVent>())
					.Where(vent => vent != null && vent.floorNode != null).Select(vent => vent.floorNode).ToArray();
				if (points.Length > 0)
				{
					Transform point = points[random.Next(points.Length)];
					position = point.position;
					rotation = point.rotation;
				}
			}
			else
			{
				GameObject[] points = GameObject.FindGameObjectsWithTag("OutsideAINode");
				if (points.Length > 0)
				{
					position = points[random.Next(points.Length)].transform.position;
				}
			}
		}

		EnemyController controller = SpawnControllableEnemy(enemy, position, rotation, crossArea);
		if (controller == null)
		{
			spawnFeedback = $"{enemy.EnemyName}: spawn failed. See BepInEx log.";
			return;
		}
		currentControlledEnemy = controller;
		controlledEnemyName = enemy.EnemyName;
		crossAreaControl = crossArea;
		spawnFeedback = null;
		EnableGhost(enable: false);
		currentMode = Mode.ENEMY;
	}

	internal void ApplyCrossAreaAfterEnemyStart(EnemyAI enemy)
	{
		// EnemyAI.Start resets isOutside to the prefab's native value.
		if (crossAreaControl && currentControlledEnemy is EnemyAIController controller && controller.EnemyAI == enemy)
		{
			enemy.SetEnemyOutside(!isGhostIndoors);
		}
	}

	private EnemyController SpawnControllableEnemy(ControllableEnemy enemy, Vector3 position, Quaternion rotation, bool crossArea)
	{
		Vector3 spawnPosition = enemy.OverrideEnemySpawnLocation ? enemy.EnemySpawnLocationOverride
			: crossArea ? position : position + new Vector3(0f, 0.5f, 0f);
		GameObject enemyObject = null;
		GameObject controllerObject = null;
		NetworkObject networkObject = null;
		try
		{
			enemyObject = enemy.OverrideEnemyPrefab ? enemy.EnemyGameObject
				: UnityEngine.Object.Instantiate(enemy.EnemyPrefab, spawnPosition, Quaternion.identity);
			if (!enemy.OverrideEnemyPrefab)
			{
				networkObject = enemyObject.GetComponentInChildren<NetworkObject>();
				if (networkObject == null)
				{
					throw new InvalidOperationException("Missing enemy NetworkObject.");
				}
				if (crossArea)
				{
					EnemyAI ai = enemyObject.GetComponentInChildren<EnemyAI>();
					ai.SetEnemyOutside(!isGhostIndoors);
					if (!ai.agent.isOnNavMesh)
					{
						throw new InvalidOperationException("Enemy does not have navigable ground at the spawn point.");
					}
				}
				networkObject.Spawn(destroyWithScene: true);
			}
			controllerObject = new GameObject("enemy_controller");
			controllerObject.transform.SetPositionAndRotation(spawnPosition, rotation);
			EnemyController controller = enemy.CreateAndAttachEnemyControllerFunc(enemyObject, controllerObject);
			controller.InitializeEnemyController(GetHostPlayer().playerActions, enemyObject);
			controller.EnableLight(isGhostIndoors);
			RegisterSpawnedEnemy(controller);
			ChangeAudioListener(controller.AudioListener);
			return controller;
		}
		catch (Exception ex)
		{
			ControlCompanyRemakePlugin.Instance.logger.LogError($"Could not spawn {enemy.EnemyName}: {ex}");
			UnityEngine.Object.Destroy(controllerObject);
			if (!enemy.OverrideEnemyPrefab)
			{
				if (networkObject != null && networkObject.IsSpawned)
				{
					networkObject.Despawn();
				}
				else
				{
					UnityEngine.Object.Destroy(enemyObject);
				}
			}
			return null;
		}
	}

	private static void RegisterSpawnedEnemy(EnemyController enemyController)
	{
		EnemyAI enemyAI = (enemyController as EnemyAIController)?.EnemyAI;
		if (enemyAI == null)
		{
			return;
		}

		RoundManager roundManager = RoundManager.Instance;
		if (roundManager == null)
		{
			return;
		}

		List<EnemyAI> spawnedEnemies = roundManager.SpawnedEnemies;
		if (spawnedEnemies != null && !spawnedEnemies.Contains(enemyAI))
		{
			spawnedEnemies.Add(enemyAI);
		}
	}

	private GameObject GetTurretPrefab()
	{
		SelectableLevel[] levels = StartOfRound.Instance.levels;
		foreach (SelectableLevel selectableLevel in levels)
		{
			SpawnableMapObject[] spawnableMapObjects = selectableLevel.spawnableMapObjects;
			foreach (SpawnableMapObject spawnableMapObject in spawnableMapObjects)
			{
				if (spawnableMapObject?.prefabToSpawn?.GetComponentInChildren<Turret>() != null)
				{
					return spawnableMapObject.prefabToSpawn;
				}
			}
		}
		return null;
	}

	private EnemyController SpawnControllableTurret(Vector3 position, Quaternion rotation)
	{
		GameObject turretPrefab = GetTurretPrefab();
		if (turretPrefab == null)
		{
			ControlCompanyRemakePlugin.Instance.logger.LogDebug("Cannot find turret prefab in current level");
			return null;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(turretPrefab, position, rotation);
		gameObject.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
		GameObject gameObject2 = new GameObject("enemy_controller");
		gameObject2.transform.position = position + new Vector3(0f, 0.5f, 0f);
		gameObject2.transform.rotation = rotation;
		TurretEnemyController turretEnemyController = gameObject2.AddComponent<TurretEnemyController>();
		turretEnemyController.InitializeEnemyController(GetHostPlayer().playerActions, gameObject);
		turretEnemyController.EnableLight(enable: true);
		return turretEnemyController;
	}

	private ControllableEnemy GetCompanyMonsterEnemy()
	{
		DepositItemsDesk depositItemsDesk = UnityEngine.Object.FindObjectOfType<DepositItemsDesk>();
		if (depositItemsDesk == null)
		{
			return null;
		}
		GameObject gameObject = GameObject.Find("BellDinger");
		return new ControllableEnemy
		{
			EnemyName = "Company Monster",
			OverrideEnemyPrefab = true,
			EnemyGameObject = depositItemsDesk.gameObject,
			Allowlisted = true,
			OverrideEnemySpawnLocation = true,
			EnemySpawnLocationOverride = ((gameObject != null) ? gameObject.transform.position : depositItemsDesk.transform.position),
			CreateAndAttachEnemyControllerFunc = (GameObject enemyGameObject, GameObject enemyController) => enemyController.AddComponent<CompanyMonsterController>()
		};
	}

	private DissonanceComms GetMic()
	{
		return StartOfRound.Instance.voiceChatModule;
	}

	private void ForceMicPushToTalk()
	{
		IngamePlayerSettings.Instance.settings.pushToTalk = true;
	}

	private void ResetMicPushToTalk()
	{
		IngamePlayerSettings.Instance.settings.pushToTalk = initialMicPushToTalkSetting;
	}
}
