using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using Sts2ModTranslatorOpenCC.Core;

namespace Sts2ModTranslatorOpenCC.Patches;

/// <summary>
/// 整個模組唯一的自動化入口。每次遊戲呼叫 LocManager.SetLanguage 時：
///   1. 重新掃描目標（本體 + 模組），跟 Sts2ModTranslator 自己一樣，用「已載入模組數
///      有沒有變化」判斷要不要重新掃，處理模組比第一次 SetLanguage 更晚載入完成的情況。
///   2. 順便把主選單按鈕的標籤字串塞回去（語言切換時表格會重建，每次都要塞一次）。
///   3. 如果這次語言是 zhs：對每個「適用」的目標，先比對版本 —— 版本變了(或還沒轉過)
///      就重新轉換一次並更新版本記錄；不管有沒有重新轉換，都把目前的快取內容套用進去
///      （每次語言表都是重建的，所以每次都要重新套用一次，但不需要每次都重新轉換）。
///
/// Priority.Last 確保排在其他模組（包含 Sts2ModTranslator，如果還裝著的話）後面執行。
/// 這就是「進入遊戲後自動轉換、不需要人為介入」的整個機制；面板上的按鈕只是手動觸發同一套流程。
/// </summary>
[HarmonyPatch(typeof(LocManager), nameof(LocManager.SetLanguage))]
public static class LocManagerSetLanguagePatch
{
    public const string MenuLabelKey = "STS2OPENCC-MENU";

    [HarmonyPriority(Priority.Last)]
    public static void Postfix(LocManager __instance, string language)
    {
        try
        {
            EnsureMenuLabel(__instance);

            var targets = TargetRegistry.EnsureScan();
            if (!string.Equals(language, LangConfig.TargetLanguage, StringComparison.OrdinalIgnoreCase)) return;

            foreach (var target in targets.Where(t => t.Applicable))
                ApplyOne(target, __instance);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] 自動套用失敗: {ex.Message}");
        }
    }

    private static void ApplyOne(LocTarget target, LocManager mgr)
    {
        try
        {
            string? cached = CacheStore.GetVersion(target.Id);
            if (cached == null || !string.Equals(cached, target.Version, StringComparison.Ordinal))
            {
                var (t, k) = LocConverter.Regenerate(target);
                MainFile.Logger.Info($"[{MainFile.ModId}] {target.Id} 版本變更({target.Version})，重新轉換 {t} 表格 {k} 筆。");
            }
            LocConverter.ApplyFromCache(target, mgr);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] 套用失敗 {target.Id}: {ex.Message}");
        }
    }

    private static void EnsureMenuLabel(LocManager mgr)
    {
        try
        {
            mgr.GetTable("main_menu_ui").MergeWith(new Dictionary<string, string>
            {
                [MenuLabelKey] = "繁體化",
            });
        }
        catch { /* main_menu_ui 表格不存在等情況，只影響按鈕標籤，忽略即可 */ }
    }
}
