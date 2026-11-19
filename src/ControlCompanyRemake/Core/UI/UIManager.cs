using System;
using System.Collections.Generic;
using ControlCompanyRemake.Core.Enemy;
using ControlCompanyRemake.Core.Input;
using GameNetcodeStuff;
using UnityEngine;

namespace ControlCompanyRemake.Core.UI;

internal class UIManager : MonoBehaviour
{
	internal static readonly Color Accent = new Color(0.99f, 0.75f, 0.42f);
	private static readonly Color Panel = new Color(0.06f, 0.09f, 0.10f, 0.94f);
	private static readonly Color Muted = new Color(0.68f, 0.73f, 0.74f);
	private static readonly Color KeyBackground = new Color(0.17f, 0.20f, 0.22f, 1);
	private GUIStyle title;
	private GUIStyle text;
	private GUIStyle detail;
	private GUIStyle keyStyle;

	public static UIManager Instance { get; private set; }

	private void Awake() => Instance = this;

	public void Render(SettingsMenu settings, Action hud)
	{
		if (title == null)
		{
			title = Label(20, Color.white, FontStyle.Bold);
			text = Label(14, new Color(0.92f, 0.95f, 0.96f));
			detail = Label(13, Muted);
			keyStyle = Label(12, Accent, FontStyle.Bold, TextAnchor.MiddleCenter);
		}
		float scale = Mathf.Clamp(Screen.height / 900f, 0.55f, 1.8f);
		Matrix4x4 previous = GUI.matrix;
		GUI.matrix = previous * Matrix4x4.Scale(new Vector3(scale, scale, 1));
		try
		{
			if (settings.IsOpen)
			{
				settings.Render(Screen.width / scale, Screen.height / scale, title, detail);
			}
			else
			{
				hud();
			}
		}
		finally
		{
			GUI.matrix = previous;
		}
	}

	public void RenderNotHostLabel() => Draw("Player", "Control Company Remake is available to the host", new List<(string, string)>());

	public void RenderHost(PlayerControllerB target, bool extended)
	{
		var rows = new List<(string, string)>
		{
			(InputKeys.HOST_GHOST_MODE_KEY.displayName, "Enter ghost mode"),
			(PageKeys(), "Change target"),
			(InputKeys.EXTENDED_MODE_KEY.displayName, extended ? "Use normal catalog" : "Use extended catalog")
		};
		if (extended)
		{
			rows.Add((InputKeys.HOST_GIVE_CREDITS_KEY.displayName, "Add credits"));
			rows.Add((InputKeys.HOST_TELEPORT_TO_ENTRANCE_KEY.displayName, "Teleport to entrance"));
		}
		rows.Add(("Ctrl+Insert", "Settings"));
		rows.Add((InputKeys.TOGGLE_UI_KEY.displayName, "Hide HUD"));
		Draw("Player", $"Target  ·  {target?.playerUsername ?? "No target"}", rows);
	}

	public void RenderGhost(string catalog, bool indoors, int page, int pages, IReadOnlyList<ControllableEnemy> enemies,
		string feedback, PlayerControllerB host, Func<ControllableEnemy, string> unavailable)
	{
		var rows = new List<(string, string)>();
		for (int i = 0; i < enemies.Count; i++)
		{
			ControllableEnemy enemy = enemies[i];
			string reason = unavailable(enemy);
			rows.Add(reason == null
				? (InputKeys.GHOST_SPAWN_ENEMY_KEYS[i].displayName, enemy.EnemyName)
				: ("—", $"{enemy.EnemyName}  ·  {reason}"));
		}
		if (enemies.Count == 0)
		{
			rows.Add((null, "No enemies in this catalog"));
		}
		if (pages > 1)
		{
			rows.Add((PageKeys(), "Change page"));
		}
		rows.Add((InputKeys.CATALOG_KEY.displayName, "Change catalog"));
		rows.Add((InputKeys.EXIT_KEY.displayName, "Return to player"));
		rows.Add(("Ctrl+Insert", "Settings"));
		rows.Add((InputKeys.TOGGLE_UI_KEY.displayName, "Hide HUD"));
		string pagination = pages == 0 ? "" : $"  ·  {page}/{pages}";
		Draw("Ghost", $"{catalog}  ·  Spawn: {(indoors ? "Interior" : "Exterior")}{pagination}", rows, feedback ?? HostStatus(host));
	}

	public void RenderScrap(IReadOnlyList<Item> scraps, PlayerControllerB host)
	{
		var rows = new List<(string, string)>();
		for (int i = 0; i < scraps.Count && i < InputKeys.GHOST_SPAWN_ENEMY_KEYS.Count; i++)
		{
			rows.Add((InputKeys.GHOST_SPAWN_ENEMY_KEYS[i].displayName, scraps[i].itemName));
		}
		rows.Add((InputKeys.EXIT_KEY.displayName, "Return to player"));
		rows.Add(("Ctrl+Insert", "Settings"));
		Draw("Ghost", "Scrap", rows, HostStatus(host));
	}

	public void RenderMonster(EnemyController controller, string enemyName, bool crossArea, PlayerControllerB host)
	{
		var rows = new List<(string, string)>();
		if (!controller.IsAIControlled)
		{
			AddSkill(rows, InputKeys.ENEMY_PRIMARY_SKILL_BUTTON.shortDisplayName, controller.GetPrimarySkillName());
			AddSkill(rows, InputKeys.ENEMY_SECONDARY_SKILL_BUTTON.shortDisplayName, controller.GetSecondarySkillName());
		}
		if (crossArea)
		{
			rows.Add((null, "AI handoff unavailable in this area"));
		}
		else
		{
			rows.Add((InputKeys.ENEMY_TOGGLE_AI_KEY.displayName, controller.IsAIControlled ? "Take control" : "Give control to AI"));
			if (InputKeys.CanUseShiftWithExit && controller is EnemyAIController { CanLeaveWithAI: true })
			{
				rows.Add(($"Shift+{InputKeys.EXIT_KEY.displayName}", "Leave with AI"));
			}
		}
		rows.Add((InputKeys.ENEMY_TOGGLE_LIGHT_KEY.displayName, "Toggle light"));
		rows.Add((InputKeys.EXIT_KEY.displayName, "Return to ghost"));
		rows.Add(("Ctrl+Insert", "Settings"));
		rows.Add((InputKeys.TOGGLE_UI_KEY.displayName, "Hide HUD"));
		Draw("Monster", $"{enemyName}  ·  {(controller.IsAIControlled ? "AI control" : "Manual control")}", rows, HostStatus(host));
	}

	private void Draw(string heading, string state, List<(string Key, string Action)> rows, string footer = null)
	{
		float scale = GUI.matrix.m11;
		float width = Mathf.Min(412, Screen.width / scale - 48);
		var panel = new Rect(Screen.width / scale - width - 24, 24, width, 63 + rows.Count * 23 + (footer == null ? 5 : 45));
		Fill(panel, Panel);
		Fill(new Rect(panel.x, panel.y, panel.width, 2), Accent);
		GUI.Label(new Rect(panel.x + 14, panel.y + 8, width - 28, 27), heading, title);
		GUI.Label(new Rect(panel.x + 14, panel.y + 36, width - 28, 20), state, detail);
		float y = panel.y + 61;
		foreach (var row in rows)
		{
			float inset = panel.x + 14;
			if (row.Key != null)
			{
				Fill(new Rect(inset, y + 2, 90, 18), KeyBackground);
				GUI.Label(new Rect(inset, y + 2, 90, 18), row.Key, keyStyle);
				inset += 101;
			}
			GUI.Label(new Rect(inset, y, panel.xMax - 14 - inset, 22), row.Action, text);
			y += 23;
		}
		if (footer != null)
		{
			var footerStyle = new GUIStyle(detail) { wordWrap = true };
			GUI.Label(new Rect(panel.x + 14, y + 3, width - 28, 40), footer, footerStyle);
		}
	}

	private static string HostStatus(PlayerControllerB player)
	{
		if (player == null || player.isPlayerDead)
		{
			return "Host dead";
		}
		string proximity = player.NearOtherPlayers(17f) ? "Near other players" : "Alone";
		var voice = StartOfRound.Instance.voiceChatModule;
		string mic = voice == null || !IngamePlayerSettings.Instance.settings.micEnabled
			? "Mic off" : voice.IsMuted ? "Mic muted" : "Mic open";
		return $"{proximity}  ·  {mic}  ·  {InputKeys.HOST_LISTEN_KEY.displayName} to listen";
	}

	private static string PageKeys() => $"{InputKeys.CYCLE_KEY_LEFT.displayName} / {InputKeys.CYCLE_KEY_RIGHT.displayName}";

	private static void AddSkill(List<(string, string)> rows, string key, string name)
	{
		if (!string.IsNullOrWhiteSpace(name))
		{
			rows.Add((key, name));
		}
	}

	private static GUIStyle Label(int size, Color color, FontStyle weight = FontStyle.Normal, TextAnchor align = TextAnchor.MiddleLeft)
		=> new GUIStyle(GUI.skin.label)
		{
			fontSize = size,
			fontStyle = weight,
			alignment = align,
			clipping = TextClipping.Clip,
			normal = { textColor = color }
		};

	internal static void Fill(Rect rect, Color color)
	{
		Color previous = GUI.color;
		GUI.color = color;
		GUI.DrawTexture(rect, Texture2D.whiteTexture);
		GUI.color = previous;
	}
}
