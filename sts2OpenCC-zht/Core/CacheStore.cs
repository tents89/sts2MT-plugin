using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Sts2ModTranslatorOpenCC.Core;

/// <summary>
/// 所有持久化資料都只放在「這個模組自己的資料夾」底下，不碰其他模組、也不寫進遊戲本體：
///
///   mods/sts2OpenCC-zht/Cache/
///     versions.json              目標id -> 上次轉換時的版本字串
///     {targetId}/{table}.json    該目標、該表格轉換後的繁體文字
///
/// targetId 可能是模組 id，也可能是遊戲本體的固定 id（見 TargetRegistry.BaseGameId）。
/// </summary>
internal static class CacheStore
{
    public static string Root { get; } =
        Path.Combine(Path.GetDirectoryName(typeof(CacheStore).Assembly.Location) ?? AppContext.BaseDirectory, "Cache");

    private static string VersionsPath => Path.Combine(Root, "versions.json");

    private static string TargetDir(string targetId) => Path.Combine(Root, Sanitize(targetId));

    // ── 版本記錄：每個目標只記一個版本字串 ──────────────────────────
    public static string? GetVersion(string targetId)
    {
        var all = LoadVersions();
        return all.TryGetValue(targetId, out var v) ? v : null;
    }

    public static void SetVersion(string targetId, string version)
    {
        var all = LoadVersions();
        all[targetId] = version;
        SaveVersions(all);
    }

    private static Dictionary<string, string> LoadVersions()
    {
        try
        {
            if (!File.Exists(VersionsPath)) return new(StringComparer.Ordinal);
            string text = File.ReadAllText(VersionsPath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(text)
                   ?? new(StringComparer.Ordinal);
        }
        catch
        {
            return new(StringComparer.Ordinal);
        }
    }

    private static void SaveVersions(Dictionary<string, string> all)
    {
        try
        {
            Directory.CreateDirectory(Root);
            File.WriteAllText(VersionsPath, JsonSerializer.Serialize(all, JsonUtil.Options));
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] 寫入版本記錄失敗: {ex.Message}");
        }
    }

    // ── 轉換後文本快取 ───────────────────────────────────────────
    public static void SaveTable(string targetId, string table, Dictionary<string, string> dict)
    {
        try
        {
            string dir = TargetDir(targetId);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, table + ".json"), JsonSerializer.Serialize(dict, JsonUtil.Options));
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] 寫入快取失敗 {targetId}/{table}: {ex.Message}");
        }
    }

    public static Dictionary<string, string>? LoadTable(string targetId, string table)
    {
        try
        {
            string path = Path.Combine(TargetDir(targetId), table + ".json");
            if (!File.Exists(path)) return null;
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>清掉某個目標舊的快取（重新轉換前先清乾淨，避免殘留已刪除的表格）。</summary>
    public static void ClearTarget(string targetId)
    {
        try
        {
            string dir = TargetDir(targetId);
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] 清除快取失敗 {targetId}: {ex.Message}");
        }
    }

    public static List<string> ListCachedTables(string targetId)
    {
        try
        {
            string dir = TargetDir(targetId);
            if (!Directory.Exists(dir)) return new();
            return Directory.GetFiles(dir, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(n => !string.IsNullOrEmpty(n))
                .Select(n => n!)
                .ToList();
        }
        catch
        {
            return new();
        }
    }

    private static string Sanitize(string id)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            id = id.Replace(c, '_');
        return id;
    }
}
