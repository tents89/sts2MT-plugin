using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace Sts2ModTranslatorOpenCC;

/// <summary>
/// 獨立模組（不依賴 Sts2ModTranslator）：把遊戲本體 + 所有已載入模組裡「有 zhs 原文」的部分
/// 自動轉成繁體中文（OpenCC s2tw），並在主選單新增「繁體化」項目供手動管理。
///
/// 運作方式：
///   1. LocManager.SetLanguage 被呼叫時（見 Patches/LocManagerSetLanguagePatch.cs）自動掃描、
///      比對每個目標的版本、有變動才重新轉換，然後把快取內容套用進去——正常情況下不需要
///      任何手動操作。
///   2. 主選單「繁體化」（見 Patches/MainMenuButtonPatch.cs、Ui/TraditionalizeSubmenu.cs）
///      列出偵測到的模組，可以手動強制重新套用單一模組或全部。
///   3. 所有快取（轉換後文本、各目標的版本記錄）都只存在這個模組自己的資料夾底下
///      （Core/CacheStore.cs），不寫進任何其他模組或遊戲本體的路徑。
/// </summary>
[ModInitializer(nameof(Initialize))]
public class MainFile
{
    public const string ModId = "Sts2OpenCC-zht";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; }
        = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        try
        {
            Core.ModSettings.Load();
            var harmony = new Harmony(ModId);
            harmony.PatchAll(typeof(MainFile).Assembly);
            Logger.Info($"[{ModId}] initialized — LocManager.SetLanguage hooked.");
        }
        catch (Exception ex)
        {
            Logger.Warn($"[{ModId}] init failed: {ex.Message}");
            Logger.Warn(ex.ToString());
        }
    }
}
