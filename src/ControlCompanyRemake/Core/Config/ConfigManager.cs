using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using ControlCompanyRemake.Core.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ControlCompanyRemake.Core.Config;

[Flags]
internal enum BindingMode
{
	Player = 1,
	Ghost = 2,
	Monster = 4,
	All = Player | Ghost | Monster
}

internal sealed class Shortcut
{
	public ConfigEntry<string> Entry { get; }
	public string Label { get; }
	public BindingMode Modes { get; }
	private readonly Key defaultKey;
	private readonly Action<KeyControl> assign;

	public Shortcut(ConfigEntry<string> entry, string label, BindingMode modes, Key defaultKey, Action<KeyControl> assign)
	{
		Entry = entry;
		Label = label;
		Modes = modes;
		this.defaultKey = defaultKey;
		this.assign = assign;
	}

	public KeyControl Resolve(string value)
	{
		Keyboard keyboard = Keyboard.current;
		if (value.StartsWith("key:", StringComparison.OrdinalIgnoreCase))
		{
			if (Enum.TryParse(value.Substring(4), true, out Key key))
			{
				foreach (KeyControl control in keyboard.allKeys)
				{
					if (control != null && control.keyCode == key)
					{
						return control;
					}
				}
			}
		}
		else if (string.Equals(value, "esc", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(value, "escape", StringComparison.OrdinalIgnoreCase))
		{
			return keyboard.escapeKey;
		}
		else
		{
			foreach (KeyControl control in keyboard.allKeys)
			{
				if (control != null && string.Equals(control.displayName, value, StringComparison.CurrentCultureIgnoreCase))
				{
					return control;
				}
			}
		}
		return keyboard[defaultKey];
	}

	public void Apply() => assign(Resolve(Entry.Value));
}

internal class ConfigManager : MonoBehaviour
{
	public static ConfigManager Instance;
	private readonly List<Shortcut> shortcuts = new List<Shortcut>();
	private ConfigEntry<bool> spawningBehaviour;
	private bool loadedConfig;

	public IReadOnlyList<Shortcut> Shortcuts => shortcuts;
	public bool ForcePushToTalkWhileInMonsterMode { get; private set; } = true;
	public bool DisableModForSelf { get; private set; }
	public bool UseEnemySpawningBehaviour => spawningBehaviour?.Value ?? false;
	public bool DisableWhileUsingTerminal { get; private set; } = true;
	public bool CrossAreaPreview { get; private set; }

	private void Awake()
	{
		Instance = this;
	}

	private void Update()
	{
		if (!loadedConfig && Keyboard.current != null)
		{
			LoadPlayerConfig(reload: false);
		}
	}

	public void LoadPlayerConfig(bool reload)
	{
		ConfigFile config = ControlCompanyRemakePlugin.Instance.Config;
		if (reload)
		{
			config.Reload();
		}
		ReadOptions(config);
		if (Keyboard.current == null)
		{
			return;
		}
		if (shortcuts.Count == 0)
		{
			BindShortcuts(config);
		}
		foreach (Shortcut shortcut in shortcuts)
		{
			shortcut.Apply();
		}
		loadedConfig = true;
	}

	private void ReadOptions(ConfigFile config)
	{
		spawningBehaviour = config.Bind("General", "use_enemy_spawning_behaviour", false);
		DisableWhileUsingTerminal = config.Bind("General", "disable_while_using_terminal", true).Value;
		DisableModForSelf = config.Bind("General", "disable_mod_for_self", false).Value;
		ForcePushToTalkWhileInMonsterMode = config.Bind("General", "force_push_to_talk_in_monster_mode", true).Value;
		CrossAreaPreview = config.Bind("General", "enable_cross_area_preview", false,
			"Enable the Thumper and Eyeless Dog cross-area multiplayer test cases.").Value;
	}

	private void BindShortcuts(ConfigFile config)
	{
		const string common = "Keybinds";
		const string player = "Keybinds.PlayerMode";
		const string ghost = "Keybinds.GhostMode";
		const string monster = "Keybinds.MonsterMode";
		const BindingMode controlled = BindingMode.Ghost | BindingMode.Monster;
		const BindingMode selection = BindingMode.Player | BindingMode.Ghost;

		void Bind(string section, string id, string value, string label, BindingMode modes, Key defaultKey, Action<KeyControl> assign)
		{
			shortcuts.Add(new Shortcut(config.Bind(section, id, value), label, modes, defaultKey, assign));
		}

		Bind(common, "extended_mode_key", "0", "Extended catalog", BindingMode.All, Key.Digit0, key => InputKeys.EXTENDED_MODE_KEY = key);
		Bind(common, "exit_mode_key", "esc", "Exit current mode", controlled, Key.Escape, key => InputKeys.EXIT_KEY = key);
		Bind(common, "open_door", "e", "Open nearby door", controlled, Key.E, key => InputKeys.CONTROLLER_USE_KEY = key);
		Bind(common, "toggle_ui", "u", "Show / hide HUD", BindingMode.All, Key.U, key => InputKeys.TOGGLE_UI_KEY = key);
		Bind(common, "cycle_left_key", "o", "Previous target / page", selection, Key.O, key => InputKeys.CYCLE_KEY_LEFT = key);
		Bind(common, "cycle_right_key", "p", "Next target / page", selection, Key.P, key => InputKeys.CYCLE_KEY_RIGHT = key);
		Bind(common, "listen_from_player", "alt", "Listen from host", controlled, Key.LeftAlt, key => InputKeys.HOST_LISTEN_KEY = key);
		Bind(player, "ghost_mode_key", "f1", "Enter ghost mode", BindingMode.Player, Key.F1, key => InputKeys.HOST_GHOST_MODE_KEY = key);
		Bind(player, "extended_teleport_to_entrance_key", "9", "Teleport to entrance", BindingMode.Player, Key.Digit9, key => InputKeys.HOST_TELEPORT_TO_ENTRANCE_KEY = key);
		Bind(player, "extended_give_credits_key", "8", "Add credits", BindingMode.Player, Key.Digit8, key => InputKeys.HOST_GIVE_CREDITS_KEY = key);
		Bind(ghost, "jump", "space", "Jump", controlled, Key.Space, key => InputKeys.CONTROLLER_JUMP_KEY = key);
		Bind(ghost, "catalog_cycle_key", "key:PageUp", "Change enemy catalog", BindingMode.Ghost, Key.PageUp, key => InputKeys.CATALOG_KEY = key);
		InputKeys.GHOST_SPAWN_ENEMY_KEYS.Clear();
		for (int i = 0; i < 12; i++)
		{
			int slot = i;
			Key defaultKey = (Key)((int)Key.F1 + slot);
			InputKeys.GHOST_SPAWN_ENEMY_KEYS.Add(Keyboard.current[defaultKey]);
			Bind(ghost, $"control_enemy_{slot + 1}", $"f{slot + 1}", $"Enemy slot {slot + 1}", BindingMode.Ghost,
				defaultKey, key => InputKeys.GHOST_SPAWN_ENEMY_KEYS[slot] = key);
		}
		Bind(monster, "toggle_ai_key", "f1", "Swap manual / AI control", BindingMode.Monster, Key.F1, key => InputKeys.ENEMY_TOGGLE_AI_KEY = key);
		Bind(monster, "toggle_light", "l", "Toggle light", BindingMode.Monster, Key.L, key => InputKeys.ENEMY_TOGGLE_LIGHT_KEY = key);
	}

	public string ApplyChanges(IReadOnlyDictionary<Shortcut, string> draft, bool useMapPoints)
	{
		Key[] keys = shortcuts.Select(shortcut => shortcut.Resolve(draft[shortcut]).keyCode).ToArray();
		for (int i = 0; i < shortcuts.Count; i++)
		{
			Shortcut shortcut = shortcuts[i];
			if (draft[shortcut] == shortcut.Entry.Value)
			{
				continue;
			}
			if (keys[i] == Key.RightAlt && (shortcut.Modes & BindingMode.Monster) != 0)
			{
				return $"{shortcut.Label}: Right Alt locks monster rotation.";
			}
			for (int j = 0; j < shortcuts.Count; j++)
			{
				if (i != j && keys[i] == keys[j] && (shortcut.Modes & shortcuts[j].Modes) != 0)
				{
					return $"{shortcut.Label} conflicts with {shortcuts[j].Label}.";
				}
			}
		}

		ConfigFile config = ControlCompanyRemakePlugin.Instance.Config;
		var oldValues = shortcuts.ToDictionary(shortcut => shortcut, shortcut => shortcut.Entry.Value);
		bool oldSpawn = spawningBehaviour.Value;
		bool saveOnSet = config.SaveOnConfigSet;
		config.SaveOnConfigSet = false;
		try
		{
			foreach (Shortcut shortcut in shortcuts)
			{
				shortcut.Entry.Value = draft[shortcut];
			}
			spawningBehaviour.Value = useMapPoints;
			config.Save();
		}
		catch (Exception ex)
		{
			foreach (Shortcut shortcut in shortcuts)
			{
				shortcut.Entry.Value = oldValues[shortcut];
			}
			spawningBehaviour.Value = oldSpawn;
			ControlCompanyRemakePlugin.Instance.logger.LogError(ex);
			return "Couldn't save the configuration. Previous settings are still active.";
		}
		finally
		{
			config.SaveOnConfigSet = saveOnSet;
		}
		foreach (Shortcut shortcut in shortcuts)
		{
			shortcut.Apply();
		}
		return null;
	}
}
