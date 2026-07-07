namespace Sts2ModTranslatorOpenCC.Core;

/// <summary>一個可以「繁體化」的目標——可能是遊戲本體，也可能是一個模組。</summary>
internal sealed class LocTarget
{
    public string Id = "";
    public string Name = "";
    public string Version = "";

    /// <summary>zhs 原文所在的 res:// 資料夾（本體：res://localization/zhs；模組：res://{id}/localization/zhs）。</summary>
    public string ZhsRoot = "";

    public bool Applicable;

    /// <summary>不適用時的原因，給面板顯示用。</summary>
    public string Reason = "";
}
