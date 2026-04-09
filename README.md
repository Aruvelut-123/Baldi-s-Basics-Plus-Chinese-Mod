# Baldi's Basics Plus 中文模组 (BBPC)

[English](README.en.md) | [繁體中文](README.zh-TW.md)

一个为 **Baldi's Basics Plus** 提供中文语言支持的 BepInEx 模组。

## 功能特性

- 简体中文（SChinese）和繁体中文本地化
- 中文 UI 纹理替换（菜单、海报、制作人员名单等）
- 游戏内语言切换器（通过选项菜单）
- 自动更新检查器（从 GitHub 获取最新版本）
- 社区模组扩展支持（BBExtraContent、ChallengeJar、ModManager、PlusLevelStudio）

## 安装要求

- [Baldi's Basics Plus](https://store.steampowered.com/app/1275890/Baldis_Basics_Plus/) v0.14 或 v0.14.1
- [BepInEx 5](https://github.com/BepInEx/BepInEx)（v5.4.21+）
- [MTM101BaldAPI](https://gamebanana.com/mods/383711)（硬依赖）

### 可选模组支持

以下模组通过软依赖提供支持：

- `pixelguy.pixelmodding.baldiplus.bbextracontent`
- `pixelguy.pixelmodding.baldiplus.newdecors`
- `bbplus.challengejar`
- `rost.moment.baldiplus.funsettings`
- `wazkitta.plusmod.microeventsplus`

## 安装步骤

1. 将 [BepInEx 5](https://github.com/BepInEx/BepInEx) 安装到 Baldi's Basics Plus 游戏目录中。
2. 将 [MTM101BaldAPI](https://gamebanana.com/mods/383711) 安装到 `BepInEx/plugins` 文件夹。
3. 从 [Releases](https://github.com/Aruvelut-123/Baldi-s-Basics-Plus-Chinese-Mod/releases) 页面下载最新的 `BBPC.dll`。
4. 将 `BBPC.dll` 及其依赖 DLL（`MonoMod.Backports.dll`、`MonoMod.ILHelpers.dll`）放入 `BepInEx/plugins` 文件夹。
5. 启动游戏，模组将自动加载。

## 配置说明

首次启动后，将在 `BepInEx/config/com.baymaxawa.bbpc.cfg` 生成配置文件：

| 设置 | 默认值 | 说明 |
|------|--------|------|
| `Enable Textures` | `true` | 启用或禁用纹理替换 |
| `Enable Logging` | `false` | 启用或禁用调试日志 |
| `Enable Dev Mode` | `false` | 启用开发模式（扫描和导出新海报） |
| `Currect Language` | `SChinese` | 使用的语言（`SChinese`、`TChinese`、`English` 等） |

也可以在游戏内通过 **选项 > BBPC** 更改语言。

## 从源码构建

### 前置条件

- [.NET SDK](https://dotnet.microsoft.com/download)（支持 .NET Framework 4.6.2）
- Visual Studio 2022（可选，用于 IDE 支持）

### 构建步骤

```bash
git clone https://github.com/Aruvelut-123/Baldi-s-Basics-Plus-Chinese-Mod.git
cd Baldi-s-Basics-Plus-Chinese-Mod
dotnet restore
dotnet build
```

构建产物位于 `BBPC/Build/Debug/BBPC.dll`。

> **注意：** 部分引用 DLL（如 `Assembly-CSharp.dll`、`MTM101BaldAPI.dll`）包含在 `Dlls/` 文件夹中。`System.Net.Http` 引用指向 Windows 特定路径，在非 Windows 系统上可能需要调整。

## 项目结构

```
BBPC/
  API/              # 核心工具类（配置、日志、版本检查、更新检查器等）
  MTMAPIPatches/    # MTM101BaldAPI 组件的 Harmony 补丁
  Patches/          # 游戏 UI 本地化的 Harmony 补丁
  Plugin.cs         # 主插件入口
  Properties/       # 启动设置
Dlls/               # 所需引用 DLL
.github/workflows/  # CI/CD（推送/PR 构建、发布发布）
```

## 链接

- [GameBanana](https://gamebanana.com/mods/610816)
- [itch.io](https://baymaxqwq.itch.io/baldi-chinese)
- [GitHub Issues](https://github.com/Aruvelut-123/Baldi-s-Basics-Plus-Chinese-Mod/issues)

## 许可证

本项目使用 [MIT 许可证](LICENSE) 授权。

部分源自 TWGSRussifier 的代码同样使用 [MIT 许可证](LICENSE.TWGSRussifier)。
