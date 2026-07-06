using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sts2ModTranslatorOpenCC.Core;

/// <summary>
/// 面向外部的簡轉繁入口。流程：先用 OpenCcEngine 做標準簡轉繁（s2tw），
/// 轉完之後再套用你的自訂字典（在轉換結果上直接找字換字），所以自訂字典其實就是
/// 「OpenCC 轉出來的東西不合意，就把它換成你想要的」，改起來很直覺。
///
/// 自訂字典檔案位置：模組資料夾下的 CustomDict.txt（跟這個 dll 放在一起，第一次執行會自動建立範例檔）。
/// 格式：每行一組 "OpenCC轉換後看到的文字=想換成的文字"，# 開頭是註解，空行忽略。
/// </summary>
public static class OpenCcBridge
{
    private static List<KeyValuePair<string, string>>? _customDict; // 依 key 長度由長到短排序
    private static readonly object InitLock = new();

    /// <summary>把一段文字從簡體轉為繁體，再套用自訂字典。空字串／null 原樣傳回。</summary>
    public static string Convert(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        EnsureInit();

        string converted = OpenCcEngine.Convert(text);
        return ApplyCustomDict(converted);
    }

    private static string ApplyCustomDict(string text)
    {
        if (_customDict!.Count == 0) return text;
        string result = text;
        // 長字串優先替換，避免短詞先換掉、長詞永遠比對不到。
        foreach (var pair in _customDict)
        {
            if (pair.Key.Length == 0) continue;
            result = result.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
        }
        return result;
    }

    private static void EnsureInit()
    {
        if (_customDict != null) return;
        lock (InitLock)
        {
            _customDict ??= LoadCustomDict();
        }
    }

    private static string CustomDictPath()
    {
        string dir = Path.GetDirectoryName(typeof(OpenCcBridge).Assembly.Location) ?? AppContext.BaseDirectory;
        return Path.Combine(dir, "CustomDict.txt");
    }

    private static List<KeyValuePair<string, string>> LoadCustomDict()
    {
        var result = new List<KeyValuePair<string, string>>();
        try
        {
            string path = CustomDictPath();
            if (!File.Exists(path))
            {
                File.WriteAllText(path,
                    "# 自訂簡轉繁字典 — 每行一組，格式：OpenCC轉換後看到的文字=想換成的文字\n"
                    + "# 這是在 OpenCC 轉換「完成之後」才套用的找字換字，改完存檔、重啟遊戲(或\n"
                    + "# 在面板按「重新套用」重新載入)就會生效。範例（請自行刪改）：\n"
                    + "# 軟件=軟體\n",
                    new UTF8Encoding(false));
                return result;
            }
            foreach (var lineRaw in File.ReadAllLines(path, Encoding.UTF8))
            {
                string line = lineRaw.Trim();
                if (line.Length == 0 || line.StartsWith('#')) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0 || eq == line.Length - 1) continue;
                string k = line[..eq].Trim();
                string v = line[(eq + 1)..].Trim();
                if (k.Length == 0) continue;
                result.Add(new KeyValuePair<string, string>(k, v));
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] 讀取自訂字典失敗: {ex.Message}");
        }
        return result.OrderByDescending(p => p.Key.Length).ToList();
    }

    /// <summary>手動重新載入字典檔（面板「重新套用」時一併呼叫，讓改過的 CustomDict.txt 立刻生效）。</summary>
    public static void ReloadCustomDict()
    {
        lock (InitLock) { _customDict = LoadCustomDict(); }
    }
}
