using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using Sts2ModTranslator.Core;
using Sts2ModTranslator.Ui;

namespace Sts2ModTranslatorOpenCC.Core;

/// <summary>
/// 「簡轉繁」按鈕的實際動作：把目前選定模組、zhs 語言底下所有 override 檔的內容，
/// 逐一用 OpenCC 轉成繁體後直接寫回同一個 zhs 檔案（遊戲目前只有 zhs、沒有獨立的 zht 語系槽）。
/// </summary>
public static class ZhsConverter
{
    private const string Lang = "zhs";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static void ConvertAndSave(SupportedMod mod)
    {
        try
        {
            TranslationStore.EnsureTemplates(mod, Lang); // 確保 overrides/{id}/zhs 骨架存在

            int filesTotal = 0, filesChanged = 0, entriesChanged = 0;
            foreach (var table in mod.EngByTable.Keys)
            {
                filesTotal++;
                string raw = TranslationStore.OverrideText(mod.Id, Lang, table);

                Dictionary<string, string>? dict;
                try { dict = JsonSerializer.Deserialize<Dictionary<string, string>>(raw); }
                catch
                {
                    // 檔案本身 JSON 壞掉——留給原本的編輯器處理，這裡跳過避免二次破壞。
                    continue;
                }
                if (dict == null || dict.Count == 0) continue;

                var sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);
                int tableChanged = 0;
                foreach (var kv in dict)
                {
                    string val = kv.Value ?? "";
                    if (val.Length == 0) { sorted[kv.Key] = val; continue; }
                    string converted = OpenCcBridge.Convert(val);
                    sorted[kv.Key] = converted;
                    if (!string.Equals(converted, val, StringComparison.Ordinal)) tableChanged++;
                }
                if (tableChanged == 0) continue;

                string newJson = JsonSerializer.Serialize(sorted, JsonOpts);
                var (ok, err) = TranslationStore.SaveOverrideText(mod.Id, Lang, table, newJson);
                if (ok) { filesChanged++; entriesChanged += tableChanged; }
                else MainFile.Logger.Warn($"[{MainFile.ModId}] 儲存失敗 {mod.Id}/{table}: {err}");
            }

            int applied = TranslationSync.ReloadFromDisk();

            string msg = filesChanged == 0
                ? "沒有可轉換的 zhs 內容（可能全部是空值，或本來就已經是繁體）。"
                : $"簡轉繁完成：{filesChanged}/{filesTotal} 個檔案、{entriesChanged} 筆文字已更新並套用（{applied} keys active）。";
            TranslatorPanel.SetStatus(msg, true, false);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] 轉換失敗: {ex}");
            TranslatorPanel.SetStatus("簡轉繁失敗：" + ex.Message, true, true);
        }
    }
}
