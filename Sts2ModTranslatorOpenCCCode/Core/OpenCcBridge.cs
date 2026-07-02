using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenccNetLib;

namespace Sts2ModTranslatorOpenCC.Core;

/// <summary>
/// 包一層 OpenccNetLib，另外疊加一份你可以自行編輯的自訂字典。
///
/// 自訂字典檔案位置：模組資料夾下的 CustomDict.txt（跟這個 dll 放在一起，第一次執行會自動建立範例檔）。
/// 格式：每行一組 "來源文字=想要輸出的繁體文字"，# 開頭是註解，空行忽略。
/// 自訂字典的優先權「永遠」高於 OpenCC 內建詞庫——作法是轉換前先把符合的片段換成一個
/// 不會被 OpenCC 誤判的暫存記號（Unicode 私用區字元），跑完 OpenCC 轉換後再換回你指定的文字，
/// 這樣就不受 OpenCC 內部詞庫比對優先順序影響。
///
/// 若要改變簡轉繁的標準（例如只要 OpenCC 標準繁體、或港式繁體），改下面的 Config 常數即可：
///   s2t   = 簡體 → OpenCC 標準繁體
///   s2tw  = 簡體 → 台灣正體（只轉字形，不轉用詞）
///   s2twp = 簡體 → 台灣正體 + 常用詞彙（預設，最口語自然）
///   s2hk  = 簡體 → 香港繁體
/// </summary>
public static class OpenCcBridge
{
    private const string Config = "s2twp";

    private static Opencc? _opencc;
    private static List<KeyValuePair<string, string>>? _customDict; // 依 key 長度由長到短排序
    private static readonly object InitLock = new();

    /// <summary>把一段文字從簡體轉為繁體（含自訂字典覆蓋）。空字串／null 原樣傳回。</summary>
    public static string Convert(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        EnsureInit();

        // 1) 用自訂字典的項目先「保護」起來，避免被 OpenCC 內建詞庫蓋掉。
        string working = text;
        List<string>? restore = null;
        if (_customDict!.Count > 0)
        {
            restore = new List<string>();
            foreach (var pair in _customDict)
            {
                if (pair.Key.Length == 0) continue;
                int idx;
                while ((idx = working.IndexOf(pair.Key, StringComparison.Ordinal)) >= 0)
                {
                    string token = "\uE000" + restore.Count + "\uE001";
                    restore.Add(pair.Value);
                    working = string.Concat(working.AsSpan(0, idx), token,
                        working.AsSpan(idx + pair.Key.Length));
                }
            }
        }

        // 2) 交給 OpenCC 做真正的簡轉繁。
        string converted = _opencc!.Convert(working);

        // 3) 把暫存記號換回自訂字典指定的最終文字。
        if (restore is { Count: > 0 })
            for (int i = 0; i < restore.Count; i++)
                converted = converted.Replace("\uE000" + i + "\uE001", restore[i], StringComparison.Ordinal);

        return converted;
    }

    private static void EnsureInit()
    {
        if (_opencc != null) return;
        lock (InitLock)
        {
            if (_opencc != null) return;
            _opencc = new Opencc(Config);
            _customDict = LoadCustomDict();
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
                    "# 自訂簡轉繁字典 — 每行一組，格式：來源文字=想要輸出的繁體文字\n"
                    + "# 開頭是 # 的整行是註解；這個檔案裡的規則永遠比 OpenCC 內建詞庫優先。\n"
                    + "# 改完存檔、重啟遊戲(或重新按一次簡轉繁按鈕重新載入)就會生效。範例（請自行刪改）：\n"
                    + "# 服务器=伺服器\n",
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
        // 長字串優先比對，避免短詞先吃掉、長詞永遠比對不到的問題。
        return result.OrderByDescending(p => p.Key.Length).ToList();
    }

    /// <summary>面板上如果之後想加「重新載入字典」按鈕可以呼叫這個，目前沒有 UI 掛勾。</summary>
    public static void ReloadCustomDict()
    {
        lock (InitLock) { _customDict = LoadCustomDict(); }
    }
}
