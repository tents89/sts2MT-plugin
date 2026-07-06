using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Modding;

namespace Sts2ModTranslatorOpenCC.Core;

/// <summary>
/// 掃描「遊戲本體 + 所有已載入模組」，找出誰有 zhs 原文可以繁體化。
/// 走遊戲自己的慣例，不用猜路徑：
///   本體： res://localization/zhs
///   模組： res://{modId}/localization/zhs
///
/// 跟 Sts2ModTranslator 的做法一樣：模組可能比第一次 SetLanguage 更晚才載入完成，
/// 所以用「已載入模組數有沒有變化」來判斷要不要重新掃描，而不是只掃一次就永久快取。
/// </summary>
internal static class TargetRegistry
{
    public const string BaseGameId = "_BaseGame";

    private static List<LocTarget>? _cache;
    private static int _lastLoadedCount = -1;

    /// <summary>回傳目前的掃描結果；已載入模組數有變化就重新掃描，否則用快取。</summary>
    public static List<LocTarget> EnsureScan()
    {
        int loaded;
        try { loaded = ModManager.GetLoadedMods().Count(); }
        catch { loaded = -1; }

        if (_cache != null && loaded == _lastLoadedCount) return _cache;

        _cache = ScanAll();
        _lastLoadedCount = loaded;
        return _cache;
    }

    /// <summary>面板顯示用：有快取就直接用，沒有就掃一次（不管模組數變化判斷）。</summary>
    public static List<LocTarget> CurrentOrScan() => _cache ?? EnsureScan();

    private static List<LocTarget> ScanAll()
    {
        var list = new List<LocTarget> { ScanBaseGame() };

        try
        {
            foreach (var mod in ModManager.GetLoadedMods())
            {
                string id = mod.manifest?.id ?? "";
                if (string.IsNullOrEmpty(id)) continue;
                if (id == MainFile.ModId) continue; // 自己不算

                string name = mod.manifest?.name ?? id;
                string version = mod.manifest?.version ?? "";
                string zhsRoot = $"res://{id}/localization/{LangConfig.SourceLanguage}";
                bool applicable = Godot.DirAccess.DirExistsAbsolute(zhsRoot);

                list.Add(new LocTarget
                {
                    Id = id,
                    Name = name,
                    Version = version,
                    ZhsRoot = zhsRoot,
                    Applicable = applicable,
                    Reason = applicable ? "" : "────沒有內建 zhs 原文",
                });
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] 掃描模組失敗: {ex.Message}");
        }

        return list;
    }

    private static LocTarget ScanBaseGame()
    {
        string root = $"res://localization/{LangConfig.SourceLanguage}";
        bool applicable = Godot.DirAccess.DirExistsAbsolute(root);
        string version = applicable ? (GameVersion.ReadVersion() ?? ContentSignature.Compute(root)) : "";
        return new LocTarget
        {
            Id = BaseGameId,
            Name = "遊戲本體 (Slay the Spire 2)",
            Version = version,
            ZhsRoot = root,
            Applicable = applicable,
            Reason = applicable ? "" : $"找不到 {root}",
        };
    }
}
