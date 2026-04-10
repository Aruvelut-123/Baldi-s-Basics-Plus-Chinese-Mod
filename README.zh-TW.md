# Baldi's Basics Plus 中文模組 (BBPC)

[简体中文](README.md) | [English](README.en.md)

一個基於 [TWGSRussifier](https://github.com/BaldiTomorrowGames/TWGSRussifier) 的 BepInEx 模組，用於使 **Baldi's Basics Plus** 的中文語言包正常運作。本模組本身不包含翻譯內容，而是讓中文語言包在遊戲中可用。

## 安裝要求

- [Baldi's Basics Plus](https://store.steampowered.com/app/1275890/Baldis_Basics_Plus/) v0.14 或 v0.14.1
- [BepInEx 5](https://github.com/BepInEx/BepInEx)（v5.4.21+）
- [MTM101BaldAPI](https://gamebanana.com/mods/383711)（硬依賴）

### 可選模組支持

以下模組透過軟依賴提供支持：

- `pixelguy.pixelmodding.baldiplus.bbextracontent`
- `pixelguy.pixelmodding.baldiplus.newdecors`
- `bbplus.challengejar`
- `rost.moment.baldiplus.funsettings`
- `wazkitta.plusmod.microeventsplus`

## 安裝步驟

1. 將 [BepInEx 5](https://github.com/BepInEx/BepInEx) 安裝到 Baldi's Basics Plus 遊戲目錄中。
2. 將 [MTM101BaldAPI](https://gamebanana.com/mods/383711) 安裝到 `BepInEx/plugins` 資料夾。
3. 從 [Releases](https://github.com/Aruvelut-123/Baldi-s-Basics-Plus-Chinese-Mod/releases) 頁面下載最新的 `BBPC.dll`。
4. 將 `BBPC.dll` 及其依賴 DLL（`MonoMod.Backports.dll`、`MonoMod.ILHelpers.dll`）放入 `BepInEx/plugins` 資料夾。
5. 啟動遊戲，模組將自動載入。

## 配置說明

首次啟動後，將在 `BepInEx/config/com.baymaxawa.bbpc.cfg` 生成配置檔案：

| 設置 | 預設值 | 說明 |
|------|--------|------|
| `Enable Textures` | `true` | 啟用或停用紋理替換 |
| `Enable Logging` | `false` | 啟用或停用偵錯日誌 |
| `Enable Dev Mode` | `false` | 啟用開發模式（掃描和匯出新海報） |
| `Currect Language` | `SChinese` | 使用的語言（`SChinese`、`TChinese`、`English` 等） |

也可以在遊戲內透過 **選項 > BBPC** 更改語言。

## 從原始碼建置

### 前置條件

- [.NET SDK](https://dotnet.microsoft.com/download)（支持 .NET Framework 4.6.2）
- Visual Studio 2022（可選，用於 IDE 支持）

### 建置步驟

```bash
git clone https://github.com/Aruvelut-123/Baldi-s-Basics-Plus-Chinese-Mod.git
cd Baldi-s-Basics-Plus-Chinese-Mod
dotnet restore
dotnet build
```

建置產物位於 `BBPC/Build/Debug/BBPC.dll`。

> **注意：** 部分參考 DLL（如 `Assembly-CSharp.dll`、`MTM101BaldAPI.dll`）包含在 `Dlls/` 資料夾中。`System.Net.Http` 參考指向 Windows 特定路徑，在非 Windows 系統上可能需要調整。

## 專案結構

```
BBPC/
  API/              # 核心工具類（配置、日誌、版本檢查、更新檢查器等）
  MTMAPIPatches/    # MTM101BaldAPI 元件的 Harmony 補丁
  Patches/          # 遊戲 UI 本地化的 Harmony 補丁
  Plugin.cs         # 主外掛入口
  Properties/       # 啟動設置
Dlls/               # 所需參考 DLL
.github/workflows/  # CI/CD（推送/PR 建置、發布發佈）
```

## 連結

- [GameBanana](https://gamebanana.com/mods/610816)
- [itch.io](https://baymaxqwq.itch.io/baldi-chinese)
- [GitHub Issues](https://github.com/Aruvelut-123/Baldi-s-Basics-Plus-Chinese-Mod/issues)

## 授權條款

本專案使用 [MIT 授權條款](LICENSE) 授權。

部分源自 TWGSRussifier 的程式碼同樣使用 [MIT 授權條款](LICENSE.TWGSRussifier)。
