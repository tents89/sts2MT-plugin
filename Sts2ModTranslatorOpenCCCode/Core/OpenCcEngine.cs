using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sts2ModTranslatorOpenCC.Core;

/// <summary>
/// 純 C# 的簡轉繁引擎，不依賴任何額外的 dll——資料來源是內嵌在本組件裡的 OpenCC 官方
/// 字典文字檔（Apache-2.0 授權，見 THIRD_PARTY_NOTICES.md）。走的是跟 OpenCC 官方
/// "s2tw"（簡體 → 台灣正體，標準轉換，不做詞彙在地化）設定檔一樣的兩段式轉換鏈：
///
///   第一段（簡體 → 繁體標準字）： STPhrases（詞語，優先） → STCharacters（單字，備援）
///   第二段（繁體標準字 → 台灣正體字形）： TWVariantsPhrases → TWVariants
///
/// 每一段都是「由左到右掃描、每個位置在該段的字典群組裡由長到短找最長匹配、
/// 找不到就換下一個字典試、全部都找不到就照原字元輸出」——這跟 OpenCC 設定檔裡
/// match_policy: short_circuit 的行為一致。多候選值（同一個 key 對應多個繁體字，
/// 用空白分隔）一律取第一個，即 OpenCC 未做上下文消歧時的預設候選。
///
/// 想換成 s2twp（多一段 TWPhrases 詞彙在地化，例如 软件→軟體、鼠标→滑鼠）的話，
/// 要另外從 OpenCC repo 抓 TWPhrases.txt 內嵌進來，見 README「想換轉換標準？」。
/// </summary>
internal static class OpenCcEngine
{
    private static readonly object InitLock = new();
    private static bool _initialized;

    private static PhraseDict _stPhrases = null!;
    private static PhraseDict _stCharacters = null!;
    private static PhraseDict _twVariantsPhrases = null!;
    private static PhraseDict _twVariants = null!;

    /// <summary>把一段文字從簡體轉換為繁體（台灣正體標準字形，s2tw）。</summary>
    public static string Convert(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        EnsureInit();

        string pass1 = ApplyGroup(text, _stPhrases, _stCharacters);
        string pass2 = ApplyGroup(pass1, _twVariantsPhrases, _twVariants);
        return pass2;
    }

    private static void EnsureInit()
    {
        if (_initialized) return;
        lock (InitLock)
        {
            if (_initialized) return;
            _stPhrases = PhraseDict.LoadEmbedded("STPhrases.txt");
            _stCharacters = PhraseDict.LoadEmbedded("STCharacters.txt");
            _twVariantsPhrases = PhraseDict.LoadEmbedded("TWVariantsPhrases.txt");
            _twVariants = PhraseDict.LoadEmbedded("TWVariants.txt");
            _initialized = true;
        }
    }

    /// <summary>"short_circuit" 群組比對：依序試每個字典的最長匹配，比對到就直接用該值。</summary>
    private static string ApplyGroup(string text, params PhraseDict[] dicts)
    {
        int n = text.Length;
        var sb = new StringBuilder(n);
        int i = 0;
        while (i < n)
        {
            bool matched = false;
            foreach (var dict in dicts)
            {
                int maxLen = Math.Min(dict.MaxKeyLength, n - i);
                for (int len = maxLen; len >= 1; len--)
                {
                    string candidate = text.Substring(i, len);
                    if (dict.TryGetValue(candidate, out string? value))
                    {
                        sb.Append(value);
                        i += len;
                        matched = true;
                        break;
                    }
                }
                if (matched) break;
            }
            if (!matched)
            {
                sb.Append(text[i]);
                i += 1;
            }
        }
        return sb.ToString();
    }

    private sealed class PhraseDict
    {
        private readonly Dictionary<string, string> _map;
        public int MaxKeyLength { get; }

        private PhraseDict(Dictionary<string, string> map, int maxKeyLength)
        {
            _map = map;
            MaxKeyLength = maxKeyLength;
        }

        public bool TryGetValue(string key, out string? value) => _map.TryGetValue(key, out value);

        public static PhraseDict LoadEmbedded(string fileName)
        {
            string resourceName = $"Sts2ModTranslatorOpenCC.OpenCcDict.{fileName}";
            var asm = typeof(OpenCcEngine).Assembly;
            using var stream = asm.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"找不到內嵌字典資源: {resourceName}");
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            int maxLen = 1;
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length == 0 || line[0] == '#') continue;
                int tab = line.IndexOf('\t');
                if (tab <= 0) continue;
                string key = line[..tab];
                string rest = line[(tab + 1)..];
                int sp = rest.IndexOf(' ');
                string value = sp >= 0 ? rest[..sp] : rest; // 多個候選值只取第一個
                if (key.Length == 0 || value.Length == 0) continue;
                map[key] = value;
                if (key.Length > maxLen) maxLen = key.Length;
            }
            return new PhraseDict(map, maxLen);
        }
    }
}
