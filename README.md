# Control Company Remake

Take over monsters in Lethal Company. As host, you can leave your character, move around as a ghost, and spawn a monster to control. Supported monsters can stay in the round under their own AI when you leave them.

Only the host needs the mod. Voice chat stays with your character.

## Install

Use a Lethal Company profile with BepInEx 5. Disable the original **ControlCompany** and **SpawnedEnemiesFix** in that profile. The Remake registers spawned enemies itself.

Build the DLL, copy `ControlCompanyRemake.dll` into `BepInEx/plugins/ControlCompanyRemake/`, and select **Start modded** in r2modman. Look for `Completed initialization.` in `BepInEx/LogOutput.log`.

## Controls

Host a game and land on a moon. The HUD shows the shortcuts for your current mode.

| Mode | Key | Action |
| --- | --- | --- |
| Player | F1 | Enter Ghost mode near the selected player. |
| Player | O / P | Select the previous or next player. |
| Ghost | F1–F12 | Spawn and control the enemy listed next to that key. |
| Ghost | O / P | Change enemy page. |
| Ghost | Page Up | Cycle through Auto, Interior, and Exterior. |
| Ghost | Esc | Return to your character. |
| Monster | Left / right click | Use the abilities shown on the HUD. |
| Monster | F1 | Switch between manual control and AI. |
| Monster | Esc | Exit to Ghost mode. |
| Monster | Shift+Esc | Exit to Ghost mode and leave the monster in the round when **Leave with AI** is shown. |
| Ghost or Monster | Hold left Alt | Listen from your character while they're alive. |
| Any | Ctrl+Insert | Open shortcut settings. |
| Any | U | Show or hide the HUD. |

Movement and sprint use your Lethal Company bindings. In Ghost mode, **Auto** follows your current area. **Interior** and **Exterior** choose which enemies to list. The HUD shows the actual spawn area.

## Build

Run `dotnet build ControlCompanyRemake.sln -c Release` with the .NET 8 SDK. The build uses the managed assemblies from your Lethal Company install and the core assemblies from BepInEx 5. By default, it looks in the Linux Steam install and the r2modman `Default` profile.

If your game or profile is elsewhere, pass both directories.

```sh
dotnet build ControlCompanyRemake.sln -c Release \
  -p:GameManagedDir="/path/to/Lethal Company_Data/Managed" \
  -p:BepInExCoreDir="/path/to/profile/BepInEx/core"
```

The DLL is written to `src/ControlCompanyRemake/bin/Release/net48/ControlCompanyRemake.dll`.

## Testing

BepInEx loaded version 0.2.0. The HUD and shortcut editor still need an in-round check. Enemy movement and AI handoff still need to be checked from a guest running the base game. Ghost Girl control is a known issue.

## Credits

Based on ControlCompany 1.3.3, recovered from the original plugin DLL with ILSpy. The enemy registration fix comes from atomicjuno's SpawnedEnemiesFix. [ControlCompany](https://github.com/ControlCompany/ControlCompany) · [SpawnedEnemiesFix](https://thunderstore.io/c/lethal-company/p/atomicjuno/SpawnedEnemiesFix/)
