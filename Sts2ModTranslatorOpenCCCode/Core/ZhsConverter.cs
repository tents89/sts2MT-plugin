using System;
using System.Collections.Generic;
using System.Text.Json;
using Sts2ModTranslator.Core;
using Sts2ModTranslator.Ui;

namespace Sts2ModTranslatorOpenCC.Core;

/// <summary>
/// 「簡轉繁」按鈕的實際動作，一鍵做完：
///   Reference(zhs，模組自己內建的簡體原文) → 簡轉繁 → 直接寫進 Translation(zhs) 並存檔套用。
/// 不管 Translation 目前有沒有內容都直接覆蓋（遊戲目前只有 zhs、沒有獨立的 zht 語系槽，
/// 所以是直接把 zhs 覆寫檔內容換成繁體）。
/// </summary>
public static class ZhsConverter
{
    private const string Lang = "zhs";

    public static void ConvertAndSave(SupportedMod mod)
    {
        try
        {
            TranslationStore.EnsureTemplates(mod, Lang); // 確保 overrides/{id}/zhs 骨架存在

            int filesWritten = 0;
            foreach (var table in mod.EngByTable.Keys)
            {
                // Reference(zhs)：模組自己內建的簡體原文，不是現有的 zhs 覆寫檔。
                string refJson = TranslationStore.SourceText(mod.Id, table, Lang);

                Dictionary<string, string>? dict;
                try { dict = JsonSerializer.Deserialize<Dictionary<string, string>>(refJson); }
                catch { continue; } // Reference JSON 壞掉就跳過這個檔案

                if (dict == null || dict.Count == 0) continue; // 這個模組沒有內建 zhs 原文

                var sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);
                foreach (var kv in dict)
                {
                    string val = kv.Value ?? "";
                    sorted[kv.Key] = val.Length == 0 ? "" : OpenCcBridge.Convert(val);
                }

                // 不管 Translation(zhs) 目前有沒有內容，直接覆蓋。
                string newJson = JsonSerializer.Serialize(sorted, JsonUtil.Options);
                var (ok, err) = TranslationStore.SaveOverrideText(mod.Id, Lang, table, newJson);
                if (ok) filesWritten++;
                else MainFile.Logger.Warn($"[{MainFile.ModId}] 儲存失敗 {mod.Id}/{table}: {err}");
            }

            int applied = TranslationSync.ReloadFromDisk();

            string msg = filesWritten == 0
                ? "沒有 zhs 原文可轉換。"
                : $"簡轉繁完成：{filesWritten} 個檔案已更新（{applied} keys active）。";
            TranslatorPanel.SetStatus(msg, true, false);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] 轉換失敗: {ex}");
            TranslatorPanel.SetStatus("簡轉繁失敗：" + ex.Message, true, true);
        }
    }
}
