using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace Sts2ModTranslatorOpenCC.Ui;

/// <summary>可展開/收合的區塊，模仿遊戲設定畫面常見的 ▶/▼ 風格（不必完全一致，抓個相近的感覺）。</summary>
internal sealed class FoldableSection
{
    public VBoxContainer Root { get; }
    public VBoxContainer Content { get; }

    private readonly NativeTextButton _header;
    private readonly string _title;
    private bool _expanded;

    public FoldableSection(string title, bool startExpanded = true)
    {
        _title = title;
        _expanded = startExpanded;
        Root = new VBoxContainer();

        _header = new NativeTextButton("", 58f, HorizontalAlignment.Left);
        UpdateHeaderText();
        _header.Connect(NClickableControl.SignalName.Released, Callable.From<NativeTextButton>(_ => Toggle()));
        Root.AddChild(_header);

        Content = new VBoxContainer { Visible = _expanded };
        var indent = new MarginContainer();
        indent.AddThemeConstantOverride("margin_left", 28);
        indent.AddChild(Content);
        Root.AddChild(indent);
    }

    private void Toggle()
    {
        _expanded = !_expanded;
        Content.Visible = _expanded;
        UpdateHeaderText();
    }

    private void UpdateHeaderText()
    {
        _header.ButtonText = (_expanded ? "▼ " : "▶ ") + _title;
        _header.Active = _expanded;
    }
}
