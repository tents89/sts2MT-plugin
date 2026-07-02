using System;
using System.Linq;
using Godot;
using HarmonyLib;
using Sts2ModTranslator.Ui;
using Sts2ModTranslatorOpenCC.Core;

namespace Sts2ModTranslatorOpenCC.Patches;

/// <summary>
/// 每次 STS2 Mod Translator 重畫「語言列表」頁（BuildLanguages，就是點進某個模組後、
/// 下面有 Author/Version/Install as mod 那頁）之後，在 Install as mod 按鈕旁邊插入一顆
/// 「簡轉繁」按鈕。只操作 UI 節點、不修改原模組的任何檔案或程式碼。
///
/// 之所以能直接用 TranslatorPanel._content / ._mod / .SetStatus / .Confirm，是因為
/// csproj 用 Krafs.Publicizer 把 Sts2ModTranslator.dll 的 private static 成員在編譯期
/// 當 public 處理（跟原模組自己拿 Publicizer 處理 sts2.dll 是同一招）。
/// </summary>
[HarmonyPatch(typeof(TranslatorPanel), "BuildLanguages")]
public static class TranslatorPanelBuildLanguagesPatch
{
    public static void Postfix()
    {
        try
        {
            var content = TranslatorPanel._content;
            var mod = TranslatorPanel._mod;
            if (content == null || !GodotObject.IsInstanceValid(content) || mod == null) return;
            if (content.GetChildCount() == 0) return;

            // BuildLanguages 最後一次 AddChild 加進去的，就是裝著 Author/Version/Install 那個 box。
            var box = content.GetChild(content.GetChildCount() - 1);

            // 在 box 底下找出裝著 "Install as mod" / "Update installed mod" 按鈕的那一排 HBoxContainer。
            HBoxContainer? footer = box.GetChildren()
                .OfType<HBoxContainer>()
                .FirstOrDefault(h => h.GetChildren().OfType<Button>()
                    .Any(b => b.Text.Contains("Install", StringComparison.OrdinalIgnoreCase)
                              || b.Text.Contains("Update installed", StringComparison.OrdinalIgnoreCase)));
            if (footer == null) return;
            if (footer.HasNode("Sts2OpenCCButton")) return; // 保險：避免重複插入

            var btn = new Button
            {
                Name = "Sts2OpenCCButton",
                Text = "簡轉繁",
                CustomMinimumSize = new Vector2(110, 40),
                TooltipText = "用 OpenCC 把這個模組目前的 zhs 覆寫檔從簡體轉換為繁體，並直接存檔套用。",
            };
            btn.AddThemeFontSizeOverride("font_size", 18);
            btn.Pressed += () => OnPressed(mod);
            footer.AddChild(btn);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] 按鈕掛載失敗: {ex.Message}");
        }
    }

    private static void OnPressed(Sts2ModTranslator.Core.SupportedMod mod)
    {
        TranslatorPanel.Confirm(
            "簡轉繁 (OpenCC)",
            $"將「{mod.Name}」目前的 zhs 覆寫檔內容從簡體轉換為繁體中文，並直接覆寫存檔、套用到遊戲中。"
            + "此動作無法復原（建議先按 Open Folder 備份 overrides 資料夾）。要繼續嗎？",
            "轉換並儲存",
            () => ZhsConverter.ConvertAndSave(mod));
    }
}
