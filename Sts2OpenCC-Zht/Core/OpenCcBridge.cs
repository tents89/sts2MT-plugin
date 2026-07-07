using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sts2ModTranslatorOpenCC.Core;

/// <summary>
/// Conversion entry point:
/// OpenCC embedded dictionaries -> bundled Dict.txt -> runtime CustomDict.txt.
/// </summary>
public static class OpenCcBridge
{
    private const string BundledDictResourceName = "Sts2ModTranslatorOpenCC.OpenCcDict.Dict.txt";

    private static List<KeyValuePair<string, string>>? _bundledDict;
    private static List<KeyValuePair<string, string>>? _customDict;
    private static readonly object InitLock = new();

    public static string Convert(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        EnsureInit();

        string converted = OpenCcEngine.Convert(text);
        converted = ApplyDict(converted, _bundledDict!);
        return ApplyDict(converted, _customDict!);
    }

    private static string ApplyDict(string text, List<KeyValuePair<string, string>> dict)
    {
        if (dict.Count == 0) return text;

        string result = text;
        foreach (var pair in dict)
        {
            if (pair.Key.Length == 0) continue;
            result = result.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
        }

        return result;
    }

    private static void EnsureInit()
    {
        if (_bundledDict != null && _customDict != null) return;
        lock (InitLock)
        {
            _bundledDict ??= LoadBundledDict();
            _customDict ??= LoadCustomDict();
        }
    }

    private static string CustomDictPath()
    {
        string dir = Path.GetDirectoryName(typeof(OpenCcBridge).Assembly.Location) ?? AppContext.BaseDirectory;
        return Path.Combine(dir, "CustomDict.txt");
    }

    private static List<KeyValuePair<string, string>> LoadBundledDict()
    {
        try
        {
            using var stream = typeof(OpenCcBridge).Assembly.GetManifestResourceStream(BundledDictResourceName);
            if (stream == null) return new();

            using var reader = new StreamReader(stream, Encoding.UTF8);
            return ParseDictLines(ReadLines(reader));
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] failed to read Dict.txt: {ex.Message}");
            return new();
        }
    }

    private static List<KeyValuePair<string, string>> LoadCustomDict()
    {
        try
        {
            string path = CustomDictPath();
            if (!File.Exists(path))
            {
                File.WriteAllText(path,
                    "# Custom conversion dictionary. One entry per line: source=target\n"
                    + "# Empty lines and lines starting with # are ignored.\n"
                    + "# Example=Example\n",
                    new UTF8Encoding(false));
                return new();
            }

            return ParseDictLines(File.ReadLines(path, Encoding.UTF8));
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] failed to read CustomDict.txt: {ex.Message}");
            return new();
        }
    }

    private static IEnumerable<string> ReadLines(TextReader reader)
    {
        string? line;
        while ((line = reader.ReadLine()) != null)
            yield return line;
    }

    private static List<KeyValuePair<string, string>> ParseDictLines(IEnumerable<string> lines)
    {
        var result = new List<KeyValuePair<string, string>>();
        foreach (var lineRaw in lines)
        {
            string line = lineRaw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            int eq = line.IndexOf('=');
            if (eq <= 0 || eq == line.Length - 1) continue;

            string key = line[..eq].Trim();
            string value = line[(eq + 1)..].Trim();
            if (key.Length == 0) continue;

            result.Add(new KeyValuePair<string, string>(key, value));
        }

        return result.OrderByDescending(p => p.Key.Length).ToList();
    }

    public static void ReloadCustomDict()
    {
        lock (InitLock)
        {
            _bundledDict = LoadBundledDict();
            _customDict = LoadCustomDict();
        }
    }
}
