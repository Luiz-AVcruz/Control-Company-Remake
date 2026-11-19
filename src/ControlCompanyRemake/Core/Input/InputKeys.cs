using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ControlCompanyRemake.Core.Input;

public static class InputKeys
{
	public static KeyControl EXIT_KEY = Keyboard.current.escapeKey;

	public static bool CanUseShiftWithExit => EXIT_KEY.keyCode != Key.LeftShift && EXIT_KEY.keyCode != Key.RightShift;

	public static KeyControl EXTENDED_MODE_KEY = Keyboard.current.digit0Key;

	public static KeyControl TOGGLE_UI_KEY = Keyboard.current.uKey;

	public static KeyControl CYCLE_KEY_LEFT = Keyboard.current.oKey;

	public static KeyControl CYCLE_KEY_RIGHT = Keyboard.current.pKey;

	public static KeyControl HOST_LISTEN_KEY = Keyboard.current.leftAltKey;

	public static KeyControl HOST_GHOST_MODE_KEY = Keyboard.current.f1Key;

	public static KeyControl HOST_GIVE_CREDITS_KEY = Keyboard.current.digit8Key;

	public static KeyControl HOST_TELEPORT_TO_ENTRANCE_KEY = Keyboard.current.digit9Key;

	public static KeyControl CATALOG_KEY = Keyboard.current.pageUpKey;

	public static List<KeyControl> GHOST_SPAWN_ENEMY_KEYS = new List<KeyControl>();

	public static KeyControl ENEMY_TOGGLE_AI_KEY = Keyboard.current.f1Key;

	public static ButtonControl ENEMY_PRIMARY_SKILL_BUTTON = Mouse.current.leftButton;

	public static ButtonControl ENEMY_SECONDARY_SKILL_BUTTON = Mouse.current.rightButton;

	public static KeyControl LOCK_ENEMY_ROTATION_KEY = Keyboard.current.rightAltKey;

	public static KeyControl ENEMY_TOGGLE_LIGHT_KEY = Keyboard.current.lKey;

	public static KeyControl CONTROLLER_USE_KEY = Keyboard.current.eKey;

	public static KeyControl CONTROLLER_JUMP_KEY = Keyboard.current.spaceKey;
}
