using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MegaCrit.Sts2.Core.Localization;

namespace Sts2ModTranslatorOpenCC.Core;

/// <summary>
/// 一個目標（模組或遊戲本體）的簡轉繁流程：
///   Regenerate      讀 target.ZhsRoot 底下的 *.json（原文）→ 簡轉繁 → 存進 CacheStore，更新版本記錄。
///   ApplyFromCache  把 CacheStore 裡目前的快取內容 MergeWith 進 LocManager（不重新轉換，純套用）。
/// LocManager.GetTable(表名).MergeWith(dict) 這個注入 API 本來就不限模組，任何表名都吃，
/// 所以遊戲本體跟模組走的是完全同一套邏輯。
/// </summary>
internal static class LocConverter
{
    /// <summary>重新讀原文並轉換，存進快取＋更新版本記錄。回傳 (表格數, 文字筆數)。</summary>
    public static (int tables, int keys) Regenerate(LocTarget target)
    {
        OpenCcBridge.ReloadCustomDict();
        CacheStore.ClearTarget(target.Id);

        int tableCount = 0, keyCount = 0;
        foreach (var file in SafeFiles(target.ZhsRoot).Where(f => f.EndsWith(".json")))
        {
            string table = file[..^".json".Length];
            var src = ReadResJson($"{target.ZhsRoot}/{file}");
            if (src.Count == 0) continue;

            var converted = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in src)
            {
                if (string.IsNullOrEmpty(kv.Value)) continue;
                converted[kv.Key] = OpenCcBridge.Convert(kv.Value);
            }
            if (converted.Count == 0) continue;

            CacheStore.SaveTable(target.Id, table, converted);
            tableCount++;
            keyCount += converted.Count;
        }

        CacheStore.SetVersion(target.Id, target.Version);
        return (tableCount, keyCount);
    }

    /// <summary>把快取內容套進 LocManager（不重新轉換）。回傳 (表格數, 文字筆數)。</summary>
    public static (int tables, int keys) ApplyFromCache(LocTarget target, LocManager mgr)
    {
        int tableCount = 0, keyCount = 0;
        foreach (var table in CacheStore.ListCachedTables(target.Id))
        {
            var dict = CacheStore.LoadTable(target.Id, table);
            if (dict == null || dict.Count == 0) continue;

            try
            {
                mgr.GetTable(table).MergeWith(dict);
                tableCount++;
                keyCount += dict.Count;
            }
            catch (Exception ex)
            {
                MainFile.Logger.Warn($"[{MainFile.ModId}] merge 失敗 {target.Id}/{table}: {ex.Message}");
            }
        }
        return (tableCount, keyCount);
    }

    /// <summary>手動「重新套用」：不管版本有沒有變，強制重新轉換，並在目前語言是 zhs 時立即套用。</summary>
    public static (int tables, int keys) RegenerateAndApply(LocTarget target, LocManager? mgr)
    {
        var result = Regenerate(target);
        if (mgr != null && string.Equals(mgr.Language, LangConfig.TargetLanguage, StringComparison.OrdinalIgnoreCase))
            ApplyFromCache(target, mgr);
        return result;
    }

    private static List<string> SafeFiles(string resDir)
    {
        try { return Godot.DirAccess.GetFilesAt(resDir).Where(s => !string.IsNullOrEmpty(s)).ToList(); }
        catch { return new(); }
    }

    private static Dictionary<string, string> ReadResJson(string resPath)
    {
        try
        {
            using var f = Godot.FileAccess.Open(resPath, Godot.FileAccess.ModeFlags.Read);
            if (f == null) return new();
            string text = f.GetAsText();
            if (string.IsNullOrWhiteSpace(text)) return new();
            return JsonSerializer.Deserialize<Dictionary<string, string>>(text) ?? new();
        }
        catch
        {
            return new();
        }
    }
}
