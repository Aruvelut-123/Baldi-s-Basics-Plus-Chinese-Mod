# BBPC Extension Template

This branch is the standalone base for BBPC extension branches. It builds a
BepInEx plugin that depends on BBPC without copying the main BBPC source tree.

## Create an extension

1. Create the extension branch from `feat/template`.
2. Change `AssemblyName`, `Product`, and `RootNamespace` in `BBPC/BBPC.csproj`.
3. Change the GUID, name, and version in `BBPC/API/ExtensionInfo.cs`.
4. Add the target mod's hard dependency to `Plugin.cs` and its compile-time DLL
   reference to `BBPC.csproj` only when the extension actually uses that API.
5. Replace the disabled example patch with patches for the target mod.
6. Update the workflow branch, artifact name, and output path.

Open `BBPC.sln` in Visual Studio 2022 or build from the command line:

```powershell
dotnet restore BBPC.sln
dotnet build BBPC.sln -c Debug --no-restore
dotnet build BBPC.sln -c Release --no-restore
```

Build output is written to `BBPC/Build/<Configuration>/`.

## BBPC localization API

- `BBPC.Plugin.Instance.GetTranslationKey(key, fallback)` returns the active
  language value, or the supplied fallback when the key is absent.
- Pass `lang` and `custom_lang: true` only when an explicit language override
  is required.
- `Transform.ApplyLocalizations(keys, forceRefresh)` searches descendants by
  GameObject name, adds or reuses `TextLocalizer`, and optionally refreshes an
  existing localizer even when its key is unchanged.
- `BBPC.API.BBPCTemp.is_eng` reports whether BBPC initialized in English.
- `BBPC.API.ConfigManager.currect_lang.Value` is the current language identifier;
  the spelling is retained for compatibility with BBPC's public API.

The example patch compiles against the real APIs but is disabled with
`HarmonyPrepare`, so an unchanged template has no localization side effects.

Runtime installations should provide BBPC, BepInEx, BaldAPI, the game, and the
target mod. Do not redistribute game or BepInEx assemblies with the extension.
