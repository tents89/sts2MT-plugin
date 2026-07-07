using System;
using System.Linq;

namespace Sts2ModTranslatorOpenCC.Core;

/// <summary>
/// 備援用：如果讀不到遊戲自己的 release_info.json（見 GameVersion.cs）版本號，
/// 用資料夾內容的簡單簽章（檔案數 + 總位元組數）當替代「版本」——只要
/// res://localization/zhs 裡的內容變了，這個簽章就會變，觸發重新轉換。
/// 不是密碼學等級的雜湊，但夠用來判斷「有沒有變」。
/// </summary>
internal static class ContentSignature
{
    public static string Compute(string resDir)
    {
        try
        {
            var files = Godot.DirAccess.GetFilesAt(resDir)
                .Where(f => f.EndsWith(".json"))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToList();

            long total = 0;
            foreach (var f in files)
            {
                using var fa = Godot.FileAccess.Open($"{resDir}/{f}", Godot.FileAccess.ModeFlags.Read);
                if (fa != null) total += (long)fa.GetLength();
            }
            return $"{files.Count}f-{total}b";
        }
        catch
        {
            return "unknown";
        }
    }
}
