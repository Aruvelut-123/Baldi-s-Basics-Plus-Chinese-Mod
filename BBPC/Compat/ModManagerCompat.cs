using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.OptionsAPI;
using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

using BBPC.API;

namespace BBPC.Compat
{
    /// <summary>
    /// ModManager (mtm101.rulerp.baldiplus.modmanager) compatibility layer.
    /// Localizes the category, manager UI, ModInfo toggle, and tooltips.
    /// </summary>
    public static class ModManagerCompat
    {
        public const string ModGuid = "rost.moment.baldiplus.modmanager";
        private const string AssemblyName = "ModManager";
        private const string ModManagerTypeName = "ModManager.ModManager";
        private const string ModInfoTypeName = "ModManager.ModInfo";

        private static bool _applied;
        private static Type? _modManagerType;
        private static Type? _modInfoType;
        private static FieldInfo? _currentField;
        private static MethodInfo? _addTooltipToggleMethod;
        private static MethodInfo? _addTooltipButtonMethod;

        public static void Apply(Harmony harmony)
        {
            if (_applied || harmony == null) return;
            if (!Chainloader.PluginInfos.ContainsKey(ModGuid)) return;

            _modManagerType = CompatReflection.FindType(AssemblyName, ModManagerTypeName);
            _modInfoType = CompatReflection.FindType(AssemblyName, ModInfoTypeName);

            // 1) BasePlugin.OnMenu: inject the Mod Manager category.
            Type? basePluginType = CompatReflection.FindTypeBySimpleName(AssemblyName, "BasePlugin");
            MethodInfo? onMenu = basePluginType == null ? null : AccessTools.Method(basePluginType, "OnMenu");
            if (onMenu != null)
                harmony.Patch(onMenu, prefix: new HarmonyMethod(typeof(ModManagerCompat), nameof(BasePluginOnMenuPrefix)));

            // 2) ModManager.Build 后缀：本地化 modInfo/modName/Reload/Apply
            if (_modManagerType != null)
            {
                MethodInfo? build = AccessTools.Method(_modManagerType, "Build");
                if (build != null)
                    harmony.Patch(build, postfix: new HarmonyMethod(typeof(ModManagerCompat), nameof(ModManagerBuildPostfix)));

                MethodInfo? update = AccessTools.Method(_modManagerType, "Update");
                if (update != null)
                {
                    harmony.Patch(update, postfix: new HarmonyMethod(typeof(ModManagerCompat), nameof(ModManagerUpdatePostfix)));
                    _currentField = _modManagerType.GetField("current",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

                _addTooltipToggleMethod = _modManagerType.GetMethod("AddTooltip",
                    BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new Type[] { typeof(MenuToggle), typeof(string) }, null);
                _addTooltipButtonMethod = _modManagerType.GetMethod("AddTooltip",
                    BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new Type[] { typeof(StandardMenuButton), typeof(string) }, null);
            }

            // 3) ModInfo 构造后缀：本地化启用开关
            if (_modInfoType != null)
            {
                ConstructorInfo? ctor = _modInfoType.GetConstructor(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, new Type[] { typeof(string) }, null);
                if (ctor != null)
                    harmony.Patch(ctor, postfix: new HarmonyMethod(typeof(ModManagerCompat), nameof(ModInfoCtorPostfix)));

                MethodInfo? setActive = AccessTools.Method(_modInfoType, "SetActive");
                if (setActive != null)
                    harmony.Patch(setActive, postfix: new HarmonyMethod(typeof(ModManagerCompat), nameof(ModInfoSetActivePostfix)));
            }

            _applied = true;
            API.Logger.Info($"ModManager extension compat applied (type={_modManagerType != null}, modInfo={_modInfoType != null}).");
        }

        // ---------- BasePlugin.OnMenu ----------

        private static bool BasePluginOnMenuPrefix(OptionsMenu __0, CustomOptionsHandler handler)
        {
            if (BBPCTemp.is_eng || _modManagerType == null || handler == null) return true;

            MethodInfo? addCategory = handler.GetType().GetMethods()
                .FirstOrDefault(m => m.Name == "AddCategory" && m.IsGenericMethodDefinition
                    && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string));
            if (addCategory != null)
            {
                try
                {
                    addCategory.MakeGenericMethod(_modManagerType).Invoke(handler,
                        new object[] { Plugin.Instance.GetTranslationKey("MMg_ModManager", "Mods\nManager") });
                }
                catch (Exception ex)
                {
                    API.Logger.Error($"ModManagerCompat: AddCategory invoke failed - {ex.Message}");
                }
            }
            return false;
        }

        // ---------- ModManager.Build ----------

        private static void ModManagerBuildPostfix(object __instance)
        {
            if (BBPCTemp.is_eng || __instance == null) return;

            TextMeshProUGUI? modInfo = CompatReflection.GetInstance(__instance, "modInfo") as TextMeshProUGUI;
            TextMeshProUGUI? modName = CompatReflection.GetInstance(__instance, "modName") as TextMeshProUGUI;
            modInfo?.ApplyLocalization("MMg_If_See", true);
            modName?.ApplyLocalization("MMg_If_See", true);

            if (__instance is Component comp)
            {
                Transform reloadT = comp.transform.Find("Reload");
                Transform applyT = comp.transform.Find("ApplyButton");
                if (reloadT != null && applyT != null)
                {
                    StandardMenuButton? reloadBtn = reloadT.GetComponent<StandardMenuButton>();
                    StandardMenuButton? applyBtn = applyT.GetComponent<StandardMenuButton>();
                    if (reloadBtn != null && applyBtn != null && _addTooltipButtonMethod != null)
                    {
                        try
                        {
                            _addTooltipButtonMethod.Invoke(__instance, new object[]
                            {
                                reloadBtn,
                                Plugin.Instance.GetTranslationKey("MMg_Reload_Introduction",
                                    "Load all .dll files from the plugins folder that haven't been loaded yet\n(helps enable mods without restarting the game)\n<color=red>Very broken thing, can broke game</color>")
                            });
                            _addTooltipButtonMethod.Invoke(__instance, new object[]
                            {
                                applyBtn,
                                Plugin.Instance.GetTranslationKey("MMg_Apply_Introduction", "Apply changes")
                            });
                        }
                        catch (Exception ex)
                        {
                            API.Logger.Error($"ModManagerCompat: AddTooltip(button) failed - {ex.Message}");
                        }
                    }
                }
            }
        }

        // ---------- ModManager.Update ----------

        private static void ModManagerUpdatePostfix(object __instance)
        {
            if (BBPCTemp.is_eng || __instance == null || _currentField == null) return;

            bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                               Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
            bool mPressed = Input.GetKeyDown(KeyCode.M);
            if (!(ctrlPressed && mPressed)) return;

            object? current = null;
            try { current = _currentField.GetValue(__instance); }
            catch { /* ignore */ }
            if (current == null) return;

            BepInEx.PluginInfo? plugin = CompatReflection.GetInstance(current, "PluginInfo") as BepInEx.PluginInfo;
            if (plugin == null) return;

            string customBuffer = "GUID: " + plugin.Metadata.GUID + "\n" +
                                  Plugin.Instance.GetTranslationKey("MMg_Version", "Version: ") + plugin.Metadata.Version + "\n" +
                                  Plugin.Instance.GetTranslationKey("MMg_Name", "Name: ") + plugin.Metadata.Name;
            GUIUtility.systemCopyBuffer = customBuffer;
        }

        // ---------- ModInfo ctor ----------

        private static void ModInfoCtorPostfix(object __instance, ref MenuToggle ___toggle)
        {
            if (BBPCTemp.is_eng || __instance == null || ___toggle == null) return;

            object? isException = CompatReflection.GetInstance(__instance, "IsException");
            if (isException is true) return;

            TextMeshProUGUI? toggleText = ___toggle.gameObject.GetComponentInChildren<TextMeshProUGUI>();
            if (toggleText == null) return;

            toggleText.ApplyLocalization("MMg_Active", true);

            if (_modInfoType != null && _addTooltipToggleMethod != null)
            {
                object? modManager = CompatReflection.GetStatic(_modInfoType, "modManager");
                if (modManager != null)
                {
                    try
                    {
                        _addTooltipToggleMethod.Invoke(modManager, new object[]
                        {
                            ___toggle,
                            Plugin.Instance.GetTranslationKey("MMg_Active_Introduction", "Is mod active")
                        });
                    }
                    catch (Exception ex)
                    {
                        API.Logger.Error($"ModManagerCompat: AddTooltip(toggle) failed - {ex.Message}");
                    }
                }
            }
        }

        // ---------- ModInfo.SetActive ----------

        private static void ModInfoSetActivePostfix(object __instance, bool active)
        {
            if (BBPCTemp.is_eng || !active || __instance == null || _modInfoType == null) return;

            object? modManager = CompatReflection.GetStatic(_modInfoType, "modManager");
            if (modManager == null) return;

            TextMeshProUGUI? modInfo = CompatReflection.GetInstance(modManager, "modInfo") as TextMeshProUGUI;
            if (modInfo == null) return;

            BepInEx.PluginInfo? plugin = CompatReflection.GetInstance(__instance, "PluginInfo") as BepInEx.PluginInfo;
            if (plugin == null) return;

            modInfo.text = Plugin.Instance.GetTranslationKey("MMg_Name", "Name: ") + plugin.Metadata.Name + "\nGUID: " +
                           plugin.Metadata.GUID + "\n" +
                           Plugin.Instance.GetTranslationKey("MMg_Version", "Version: ") + plugin.Metadata.Version;
        }
    }
}

