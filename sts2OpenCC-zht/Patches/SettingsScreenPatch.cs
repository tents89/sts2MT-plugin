using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

using Sts2ModTranslatorOpenCC.Ui;

namespace Sts2ModTranslatorOpenCC.Patches;

/// <summary>
/// 在遊戲的設定畫面（Settings）複製一列既有的設定項目（Modding 那一列），改成「繁體化」，
/// 讓面板永遠有地方可以打開——不管主選單那顆按鈕開著還是關著（見 ModSettings.ShowInMainMenu）。
/// 做法跟同類型模組替自己加設定列的手法一樣：複製既有節點、改名字/文字、掛自己的點擊事件，
/// 不需要依賴任何其他模組。
/// </summary>
[HarmonyPatch(typeof(NSettingsScreen), "_Ready")]
public static class SettingsScreenPatch
{
    public static void Postfix(NSettingsScreen __instance)
    {
        try
        {
            InjectRow(__instance);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] 設定畫面項目新增失敗: {ex.Message}");
        }
    }

    private static void InjectRow(NSettingsScreen settingsScreen)
    {
        var generalSettings = settingsScreen.GetNodeOrNull<Control>("ScrollContainer/Mask/Clipper/GeneralSettings");
        if (generalSettings == null) return;
        if (generalSettings.HasNode("VBoxContainer/Sts2OpenCCConfig")) return; // 已經加過

        var origDivider = generalSettings.GetNodeOrNull<ColorRect>("VBoxContainer/SendFeedbackDivider");
        var feedbackContainer = generalSettings.GetNodeOrNull<MarginContainer>("VBoxContainer/SendFeedback");
        var templateContainer = generalSettings.GetNodeOrNull<MarginContainer>("VBoxContainer/Modding");
        if (origDivider == null || feedbackContainer == null || templateContainer == null)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] 找不到設定畫面的既有節點，可能是遊戲版本更新改了結構。");
            return;
        }

        var divider = origDivider.Duplicate();
        var container = (MarginContainer)templateContainer.Duplicate();
        container.UniqueNameInOwner = false;
        container.Name = "Sts2OpenCCConfig";
        container.Visible = true;

        var button = container.GetNodeOrNull<NButton>("ModdingButton");
        if (button == null) return;
        button.Name = "Sts2OpenCCConfigButton";
        button.UniqueNameInOwner = true;

        feedbackContainer.AddSibling(divider);
        divider.AddSibling(container);
        button.Owner = settingsScreen;

        var rowLabel = container.GetNodeOrNull<RichTextLabel>("Label");
        if (rowLabel != null) rowLabel.Text = "繁體化";

        var buttonLabel = button.GetNodeOrNull<Label>("Label");
        if (buttonLabel != null) buttonLabel.Text = "開啟";

        button.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ =>
        {
            if (settingsScreen._stack is NMainMenuSubmenuStack stack)
                TraditionalizeSubmenuOpener.Open(stack);
            else
                MainFile.Logger.Warn($"[{MainFile.ModId}] 目前的設定畫面不支援開啟繁體化子選單。");
        }));

        var feedbackButton = feedbackContainer.GetNodeOrNull<Control>("FeedbackButton");
        var moddingButton = templateContainer.GetNodeOrNull<Control>("%ModdingButton");
        var creditsButton = generalSettings.GetNodeOrNull<Control>("VBoxContainer/Credits/CreditsButton");

        if (feedbackButton == null || moddingButton == null || creditsButton == null) return;

        creditsButton.FocusNeighborTop = creditsButton.GetPathTo(moddingButton);
        moddingButton.FocusNeighborBottom = moddingButton.GetPathTo(creditsButton);

        button.FocusNeighborTop = button.GetPathTo(feedbackButton);
        button.FocusNeighborBottom = button.GetPathTo(moddingButton);
        feedbackButton.FocusNeighborBottom = feedbackButton.GetPathTo(button);
        moddingButton.FocusNeighborTop = moddingButton.GetPathTo(button);
    }
}
