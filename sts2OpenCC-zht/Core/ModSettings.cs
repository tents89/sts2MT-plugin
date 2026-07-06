using System;
using System.IO;
using System.Text.Json;

namespace Sts2ModTranslatorOpenCC.Core;

/// <summary>
/// 本模組自己的設定，存在自己資料夾底下的 Settings.json（跟 Cache/ 同一層）。
/// 目前只有一個選項：主選單要不要顯示「繁體化」項目。關掉之後還是可以從
/// 遊戲設定畫面（Settings）進來，不會整個功能都找不到。
/// </summary>
internal static class ModSettings
{
    public static bool ShowInMainMenu = true;

    private static string Path =>
        System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(typeof(ModSettings).Assembly.Location) ?? AppContext.BaseDirectory,
            "Settings.json");

    public static void Load()
    {
        try
        {
            if (!File.Exists(Path)) { Save(); return; }
            var data = JsonSerializer.Deserialize<Data>(File.ReadAllText(Path));
            if (data != null) ShowInMainMenu = data.ShowInMainMenu;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] 讀取設定失敗，使用預設值: {ex.Message}");
        }
    }

    public static void Save()
    {
        try
        {
            File.WriteAllText(Path, JsonSerializer.Serialize(new Data { ShowInMainMenu = ShowInMainMenu }, JsonUtil.Options));
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] 寫入設定失敗: {ex.Message}");
        }
    }

    private sealed class Data
    {
        public bool ShowInMainMenu { get; set; } = true;
    }
}
