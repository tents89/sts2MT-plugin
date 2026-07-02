using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace Sts2ModTranslatorOpenCC;

/// <summary>
/// STS2 Mod Translator 的附加外掛（不含對方 dll，只依賴它）：
/// 在他的面板「Install as mod」按鈕旁邊插入一顆「簡轉繁」按鈕，
/// 按下後用 OpenCC 把目前選定模組的 zhs 覆寫檔內容轉為繁體並直接存檔套用。
/// </summary>
[ModInitializer(nameof(Initialize))]
public class MainFile
{
    public const string ModId = "Sts2ModTranslatorOpenCC";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; }
        = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        try
        {
            var harmony = new Harmony(ModId);
            harmony.PatchAll(typeof(MainFile).Assembly);
            Logger.Info($"[{ModId}] initialized — 簡轉繁按鈕已掛載到 Mod Translator 面板。");
        }
        catch (Exception ex)
        {
            Logger.Warn($"[{ModId}] init failed: {ex.Message}");
            Logger.Warn(ex.ToString());
        }
    }
}
