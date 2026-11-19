using System;
using System.Collections.Generic;
using System.Linq;
using ControlCompanyRemake.Core.Config;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ControlCompanyRemake.Core.UI;

internal sealed class SettingsMenu
{
	private Dictionary<Shortcut, string> draft;
	private Shortcut capturing;
	private bool useMapPoints;
	private string error;
	private Vector2 scroll;
	private readonly List<InputAction> pausedActions = new List<InputAction>();
	private bool restorePending;
	private CursorLockMode previousCursorLock;
	private bool previousCursorVisible;

	public bool IsOpen => draft != null;
	public bool BlocksInput => IsOpen || restorePending;

	public void Update(PlayerControllerB host, bool canOpen)
	{
		if (!canOpen)
		{
			Reset();
			return;
		}

		Keyboard keyboard = Keyboard.current;
		if (keyboard == null)
		{
			Reset();
			return;
		}
		Mouse mouse = Mouse.current;
		if (restorePending)
		{
			if (!keyboard.anyKey.isPressed && (mouse == null || !mouse.leftButton.isPressed && !mouse.rightButton.isPressed))
			{
				RestoreActions();
			}
			return;
		}

		if (IsOpen && capturing != null)
		{
			if (keyboard.escapeKey.wasPressedThisFrame)
			{
				capturing = null;
				return;
			}
			foreach (KeyControl key in keyboard.allKeys)
			{
				if (key != null && key.wasPressedThisFrame)
				{
					draft[capturing] = $"key:{key.keyCode}";
					capturing = null;
					error = null;
					break;
				}
			}
			return;
		}

		bool toggle = keyboard.ctrlKey.isPressed && keyboard.insertKey.wasPressedThisFrame;
		if (IsOpen)
		{
			if (toggle || keyboard.escapeKey.wasPressedThisFrame)
			{
				Close();
			}
		}
		else if (toggle && (mouse == null || !mouse.leftButton.isPressed && !mouse.rightButton.isPressed
			&& !mouse.leftButton.wasReleasedThisFrame && !mouse.rightButton.wasReleasedThisFrame))
		{
			Open(host);
		}
	}

	private void Open(PlayerControllerB host)
	{
		ConfigManager config = ConfigManager.Instance;
		if (config == null || config.Shortcuts.Count == 0)
		{
			return;
		}

		var assets = new[] { InputSystem.actions, IngamePlayerSettings.Instance.playerInput.actions, host.playerActions.asset };
		foreach (InputActionAsset asset in assets.Where(asset => asset != null).Distinct())
		{
			foreach (InputAction action in asset)
			{
				if (action.enabled)
				{
					pausedActions.Add(action);
				}
			}
		}
		draft = config.Shortcuts.ToDictionary(shortcut => shortcut, shortcut => shortcut.Entry.Value);
		useMapPoints = config.UseEnemySpawningBehaviour;
		previousCursorLock = Cursor.lockState;
		previousCursorVisible = Cursor.visible;
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		scroll = Vector2.zero;
		error = null;
		foreach (InputAction action in pausedActions)
		{
			action.Disable();
		}
	}

	private void Close()
	{
		draft = null;
		capturing = null;
		Cursor.lockState = previousCursorLock;
		Cursor.visible = previousCursorVisible;
		restorePending = true;
	}

	public void Reset()
	{
		if (IsOpen)
		{
			Close();
		}
		if (restorePending)
		{
			RestoreActions();
		}
	}

	private void RestoreActions()
	{
		restorePending = false;
		foreach (InputAction action in pausedActions)
		{
			if (action.actionMap?.asset != null)
			{
				action.Enable();
			}
		}
		pausedActions.Clear();
	}

	public void Render(float width, float height, GUIStyle title, GUIStyle detail)
	{
		float panelWidth = Mathf.Min(width - 32f, 610f);
		float panelHeight = Mathf.Min(height - 36f, 670f);
		var panel = new Rect((width - panelWidth) * 0.5f, (height - panelHeight) * 0.5f, panelWidth, panelHeight);
		UIManager.Fill(new Rect(0, 0, width, height), new Color(0, 0, 0, 0.57f));
		UIManager.Fill(panel, new Color(0.07f, 0.09f, 0.10f, 0.99f));
		UIManager.Fill(new Rect(panel.x, panel.y, panel.width, 3), UIManager.Accent);

		GUILayout.BeginArea(new Rect(panel.x + 18, panel.y + 15, panel.width - 36, panel.height - 30));
		GUILayout.Label("Control Company Remake", title);
		GUILayout.Label(capturing == null ? "Shortcuts  ·  Ctrl+Insert / Esc to close" : $"Press a key for {capturing.Label}  ·  Esc to cancel", detail);
		GUILayout.Space(7);
		scroll = GUILayout.BeginScrollView(scroll);
		bool previousEnabled = GUI.enabled;
		GUI.enabled = previousEnabled && capturing == null;
		DrawGroup("Common", "Keybinds", detail);
		DrawGroup("Player", "Keybinds.PlayerMode", detail);
		DrawGroup("Ghost", "Keybinds.GhostMode", detail);
		DrawGroup("Monster", "Keybinds.MonsterMode", detail);
		GUILayout.Space(12);
		useMapPoints = GUILayout.Toggle(useMapPoints, "Use map spawn points in the native area");
		GUILayout.EndScrollView();
		GUILayout.Space(10);
		if (error != null)
		{
			GUILayout.Label(error, detail);
		}
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Restore defaults"))
		{
			foreach (Shortcut shortcut in ConfigManager.Instance.Shortcuts)
			{
				draft[shortcut] = (string)shortcut.Entry.DefaultValue;
			}
			useMapPoints = false;
			error = null;
		}
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Cancel", GUILayout.Width(90)))
		{
			Close();
		}
		else if (GUILayout.Button("Apply", GUILayout.Width(90)))
		{
			error = ConfigManager.Instance.ApplyChanges(draft, useMapPoints);
			if (error == null)
			{
				Close();
			}
		}
		GUILayout.EndHorizontal();
		GUI.enabled = previousEnabled;
		GUILayout.EndArea();
	}

	private void DrawGroup(string title, string section, GUIStyle detail)
	{
		GUILayout.Space(7);
		GUILayout.Label(title, detail);
		foreach (Shortcut shortcut in ConfigManager.Instance.Shortcuts)
		{
			if (shortcut.Entry.Definition.Section != section)
			{
				continue;
			}
			GUILayout.BeginHorizontal();
			GUILayout.Label(shortcut.Label, GUILayout.Height(25));
			string key = shortcut.Resolve(draft[shortcut]).displayName;
			if (GUILayout.Button(capturing == shortcut ? "Press a key…" : key, GUILayout.Width(120), GUILayout.Height(25)))
			{
				capturing = shortcut;
			}
			GUILayout.EndHorizontal();
		}
	}
}
