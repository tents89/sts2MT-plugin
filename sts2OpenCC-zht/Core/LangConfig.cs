namespace Sts2ModTranslatorOpenCC.Core;

/// <summary>
/// 語言設定集中在這一個檔案。目前遊戲只有 zhs、沒有獨立的 zht，所以 SourceLanguage
/// 跟 TargetLanguage 都是 "zhs"（讀 zhs 原文、轉繁體、原地覆蓋回 zhs 這個 slot）。
///
/// 未來遊戲如果支援 zht，只要把 TargetLanguage 改成 "zht"：
///   - LocManagerSetLanguagePatch 會在語言設定為 zht 時（而不是 zhs 時）套用快取內容，
///     也就是玩家選 zht 才看得到繁體字，選 zhs 還是原本的簡體。
///   - 讀取原文（SourceLanguage）、轉換、快取的邏輯完全不用改，因為原文永遠是簡體的 zhs。
/// 其他所有程式碼都只透過這兩個常數存取語言字串，不要再散落寫死 "zhs" 到別的地方。
/// </summary>
internal static class LangConfig
{
    /// <summary>zhs 原文的來源語言，固定不變。</summary>
    public const string SourceLanguage = "zhs";

    /// <summary>
    /// 轉換結果套用到哪個語言 slot。現在等於 SourceLanguage（原地把 zhs 換成繁體）。
    /// 未來遊戲支援 zht 時改成 "zht" 即可，其他地方不用動。
    /// </summary>
    public const string TargetLanguage = SourceLanguage; // TODO: 遊戲支援 zht 後改成 "zht"
}
