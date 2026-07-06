using System;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Sts2ModTranslatorOpenCC.Ui;

/// <summary>用遊戲原生的 vertical_popup 場景做 Yes/No 確認框，完全不依賴其他模組。</summary>
internal static class ConfirmDialog
{
    public static void Show(string title, string body, string yesLabel, Action onYes)
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] no SceneTree — 無法顯示確認框。");
            return;
        }
        try
        {
            var packed = ResourceLoader.Load<PackedScene>(SceneHelper.GetScenePath("ui/vertical_popup"));
            if (packed == null)
            {
                MainFile.Logger.Warn($"[{MainFile.ModId}] 找不到 vertical_popup 場景。");
                return;
            }

            var popup = packed.Instantiate<NVerticalPopup>();
            tree.Root.AddChild(popup);
            popup.SetText(title, body);

            popup.YesButton.SetText(yesLabel);
            popup.YesButton.IsYes = true;
            popup.YesButton.Released += _ =>
            {
                try { onYes(); }
                catch (Exception ex) { MainFile.Logger.Warn($"[{MainFile.ModId}] 確認動作失敗: {ex.Message}"); }
                finally { if (GodotObject.IsInstanceValid(popup)) popup.QueueFree(); }
            };

            popup.NoButton.SetText("Cancel");
            popup.NoButton.IsYes = false;
            popup.NoButton.Visible = true;
            popup.NoButton.Released += _ => { if (GodotObject.IsInstanceValid(popup)) popup.QueueFree(); };
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] 顯示確認框失敗: {ex.Message}");
        }
    }
}
