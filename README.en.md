# Baldi's Basics Plus Chinese Mod (BBPC)

[简体中文](README.md) | [繁體中文](README.zh-TW.md)

A BepInEx mod based on [TWGSRussifier](https://github.com/BaldiTomorrowGames/TWGSRussifier) that makes the Chinese language pack work in **Baldi's Basics Plus**. This mod does not contain translations itself — it enables the Chinese language pack to function properly in the game.

## Requirements

- [Baldi's Basics Plus](https://store.steampowered.com/app/1275890/Baldis_Basics_Plus/) v0.14 or v0.14.1
- [BepInEx 5](https://github.com/BepInEx/BepInEx) (v5.4.21+)
- [MTM101BaldAPI](https://gamebanana.com/mods/383711) (hard dependency)

### Optional Mod Support

The following mods are supported with soft dependencies:

- `pixelguy.pixelmodding.baldiplus.bbextracontent`
- `pixelguy.pixelmodding.baldiplus.newdecors`
- `bbplus.challengejar`
- `rost.moment.baldiplus.funsettings`
- `wazkitta.plusmod.microeventsplus`

## Installation

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx) into your Baldi's Basics Plus game directory.
2. Install [MTM101BaldAPI](https://gamebanana.com/mods/383711) into `BepInEx/plugins`.
3. Download the latest `BBPC.dll` from the [Releases](https://github.com/Aruvelut-123/Baldi-s-Basics-Plus-Chinese-Mod/releases) page.
4. Place `BBPC.dll` along with its dependency DLLs (`MonoMod.Backports.dll`, `MonoMod.ILHelpers.dll`) into `BepInEx/plugins`.
5. Launch the game. The mod will load automatically.

## Configuration

After first launch, a config file is generated at `BepInEx/config/com.baymaxawa.bbpc.cfg`:

| Setting | Default | Description |
|---------|---------|-------------|
| `Enable Textures` | `true` | Enable or disable texture replacement |
| `Enable Logging` | `false` | Enable or disable debug logging |
| `Enable Dev Mode` | `false` | Enable development mode (scans and exports new posters) |
| `Currect Language` | `SChinese` | The language to apply (`SChinese`, `TChinese`, `English`, etc.) |

You can also change the language in-game via **Options > BBPC**.

## Building from Source

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (supports .NET Framework 4.6.2)
- Visual Studio 2022 (optional, for IDE support)

### Steps

```bash
git clone https://github.com/Aruvelut-123/Baldi-s-Basics-Plus-Chinese-Mod.git
cd Baldi-s-Basics-Plus-Chinese-Mod
dotnet restore
dotnet build
```

The built DLL will be at `BBPC/Build/Debug/BBPC.dll`.

> **Note:** Some reference DLLs (e.g., `Assembly-CSharp.dll`, `MTM101BaldAPI.dll`) are included in the `Dlls/` folder. The `System.Net.Http` reference points to a Windows-specific path and may need adjustment on non-Windows systems.

## Project Structure

```
BBPC/
  API/              # Core utilities (config, logging, version check, update checker, etc.)
  MTMAPIPatches/    # Harmony patches for MTM101BaldAPI components
  Patches/          # Harmony patches for game UI localization
  Plugin.cs         # Main plugin entry point
  Properties/       # Launch settings
Dlls/               # Required reference DLLs
.github/workflows/  # CI/CD (build on push/PR, release publishing)
```

## Links

- [GameBanana](https://gamebanana.com/mods/610816)
- [itch.io](https://baymaxqwq.itch.io/baldi-chinese)
- [GitHub Issues](https://github.com/Aruvelut-123/Baldi-s-Basics-Plus-Chinese-Mod/issues)

## License

This project is licensed under the [MIT License](LICENSE).

Parts derived from TWGSRussifier are also under the [MIT License](LICENSE.TWGSRussifier).
