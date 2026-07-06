using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using Sts2ModTranslatorOpenCC.Core;
using Sts2ModTranslatorOpenCC.Ui;

namespace Sts2ModTranslatorOpenCC.Patches;

/// <summary>
/// 主選單準備好時，如果設定裡「在主選單顯示」是開的，複製一顆原生按鈕當「繁體化」項目，
/// 點下去打開原生風格的 TraditionalizeSubmenu。關掉這個設定的話這裡就不會加按鈕，但面板仍然能從
/// 遊戲設定畫面的項目打開（見 SettingsScreenPatch.cs）。
/// </summary>
[HarmonyPatch(typeof(NMainMenu), "_Ready")]
public static class NMainMenuReadyPatch
{
    public static void Postfix(NMainMenu __instance)
    {
        if (!ModSettings.ShowInMainMenu) return;
        try
        {
            if (__instance == null || !GodotObject.IsInstanceValid(__instance)) return;
            if (__instance.HasNode("MainMenuTextButtons/Sts2OpenCCButton")) return;

            var settingsBtn = __instance.GetNodeOrNull<NMainMenuTextButton>("MainMenuTextButtons/SettingsButton");
            if (settingsBtn == null)
            {
                MainFile.Logger.Warn($"[{MainFile.ModId}] SettingsButton 找不到，跳過選單項目。");
                return;
            }

            var btn = (NMainMenuTextButton)settingsBtn.Duplicate(14); // 排除 signals
            btn.Name = "Sts2OpenCCButton";
            settingsBtn.AddSibling(btn, false);
            btn.SetLocalization(LocManagerSetLanguagePatch.MenuLabelKey);
            btn.Connect(NClickableControl.SignalName.Released, Callable.From<NMainMenuTextButton>(_ =>
            {
                __instance._lastHitButton = btn;
                TraditionalizeSubmenuOpener.Open(__instance.SubmenuStack);
            }));

            var min = btn.CustomMinimumSize;
            btn.CustomMinimumSize = new Vector2(Math.Max(300f, min.X), min.Y);
            var self = new NodePath(".");
            btn.FocusNeighborLeft = self;
            btn.FocusNeighborRight = self;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] 選單項目新增失敗: {ex.Message}");
        }
    }
}
