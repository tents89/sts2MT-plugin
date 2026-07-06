using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Sts2ModTranslatorOpenCC.Core;

/// <summary>
/// 讀遊戲自己拿去初始化 Sentry 用的 release_info.json，取得官方版本號字串（例如 "v0.108.0"）。
/// 這個檔案不在 .pck 裡，是跟執行檔放在同一層的一般檔案。路徑判斷邏輯照抄遊戲自己
/// SentryInit.gd 的 _get_possible_release_info_paths()：
///   1. user://release_info.json
///   2. macOS：{exe}/../Resources/release_info.json，再來才是 exe 旁邊
///      其他平台：exe 旁邊
/// 讀不到就回傳 null，呼叫端（TargetRegistry）會退回用內容簽章當替代版本。
/// </summary>
internal static class GameVersion
{
    private const string FileName = "release_info.json";

    public static string? ReadVersion()
    {
        foreach (var path in CandidatePaths())
        {
            try
            {
                if (!File.Exists(path)) continue;
                string text = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(text);
                if (data != null
                    && data.TryGetValue("version", out var v)
                    && v.ValueKind == JsonValueKind.String)
                {
                    return v.GetString();
                }
            }
            catch
            {
                // 這個路徑讀/解析失敗就試下一個候選路徑，不中斷整體流程。
            }
        }
        return null;
    }

    private static IEnumerable<string> CandidatePaths()
    {
        string? userPath = null;
        try { userPath = Godot.ProjectSettings.GlobalizePath($"user://{FileName}"); }
        catch { /* ignore */ }
        if (!string.IsNullOrEmpty(userPath)) yield return userPath!;

        string exeDir;
        try { exeDir = Path.GetDirectoryName(Godot.OS.GetExecutablePath()) ?? ""; }
        catch { exeDir = ""; }
        if (exeDir.Length == 0) yield break;

        if (Godot.OS.GetName() == "macOS")
        {
            yield return Path.GetFullPath(Path.Combine(exeDir, "..", "Resources", FileName));
            yield return Path.Combine(exeDir, FileName);
        }
        else
        {
            yield return Path.Combine(exeDir, FileName);
        }
    }
}
