using System;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using Sts2ModTranslatorOpenCC.Core;

namespace Sts2ModTranslatorOpenCC.Ui;

internal partial class TraditionalizeSubmenu : NSubmenu
{
    private const float TitleHeight = 90f;
    private const float TopOffset = TitleHeight + 30f;
    private const float BottomOffset = 54f;
    private const float MaxContentWidth = 1120f;
    private const float ActionColumnWidth = 230f;

    private readonly NativeScrollContainer _scrollArea;
    private readonly Control _contentPanel;
    private readonly VBoxContainer _content;
    private readonly Label _title;
    private Label? _status;
    private string _statusText = "";
    private bool _statusIsError;
    private bool _contentBuilt;
    private Tween? _fadeInTween;

    protected override Control? InitialFocusedControl => FindFirstFocusable(_content);

    public TraditionalizeSubmenu()
    {
        Name = "Sts2OpenCCSubmenu";
        SetAnchorsPreset(LayoutPreset.FullRect);
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;

        _scrollArea = new NativeScrollContainer(TopOffset, BottomOffset);
        AddChild(_scrollArea);

        _contentPanel = new Control
        {
            Name = "Sts2OpenCCContentPanel",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _contentPanel.SetAnchorsPreset(LayoutPreset.TopLeft);

        _content = new VBoxContainer
        {
            Name = "VBoxContainer",
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _content.AddThemeConstantOverride("separation", 8);
        _content.MinimumSizeChanged += RefreshSize;
        _contentPanel.AddChild(_content);

        _title = new Label
        {
            Name = "Title",
            Text = "繁體化",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            LabelSettings = new LabelSettings
            {
                Font = NativeFonts.Bold,
                FontSize = 42,
                FontColor = StsColors.gold,
                OutlineSize = 10,
                OutlineColor = new Color(0.18f, 0.08f, 0.08f),
            },
        };
        _title.SetAnchorsPreset(LayoutPreset.TopLeft);
        AddChild(_title);
    }

    public override void _Ready()
    {
        _scrollArea.AttachContent(_contentPanel);
        EnsureBuilt();

        var backButton = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("ui/back_button")).Instantiate<NBackButton>();
        backButton.Name = "BackButton";
        AddChild(backButton);

        GetViewport().Connect(Viewport.SignalName.SizeChanged, Callable.From(RefreshSize));
        RefreshSize();
        Callable.From(RefreshSize).CallDeferred();
    }

    protected override void OnSubmenuShown()
    {
        base.OnSubmenuShown();
        EnsureBuilt();
        _contentPanel.Modulate = new Color(1f, 1f, 1f, 0f);
        RefreshSize();
        _scrollArea.InstantlyScrollToTop();
        WaitForLayoutAndFadeIn();
    }

    public override void _ExitTree()
    {
        GetViewport().Disconnect(Viewport.SignalName.SizeChanged, Callable.From(RefreshSize));
        base._ExitTree();
    }

    private void EnsureBuilt()
    {
        Rebuild();
        _contentBuilt = true;
        MainFile.Logger.Info($"[{MainFile.ModId}] submenu rebuilt, children={_content.GetChildCount()}, min={_content.GetMinimumSize()}, panelMin={_contentPanel.CustomMinimumSize}");
    }

    private void Rebuild()
    {
        foreach (var child in _content.GetChildren()) child.QueueFree();

        _content.AddChild(new NativeOptionRow(
            "在主選單顯示（需要重啟）",
            new NativeToggle(
                () => ModSettings.ShowInMainMenu,
                pressed =>
                {
                    ModSettings.ShowInMainMenu = pressed;
                    ModSettings.Save();
                })));

        _status = CreateTextLabel(_statusText, 20, _statusIsError ? new Color(0.92f, 0.45f, 0.45f) : new Color(0.68f, 0.72f, 0.80f));
        _status.Visible = !string.IsNullOrEmpty(_statusText);
        _content.AddChild(_status);
        _content.AddChild(CreateDivider());

        var targets = TargetRegistry.EnsureScan();
        var baseGame = targets.FirstOrDefault(t => t.Id == TargetRegistry.BaseGameId);
        var mods = targets.Where(t => t.Id != TargetRegistry.BaseGameId).ToList();
        var supported = mods.Where(t => t.Applicable).ToList();
        var unsupported = mods.Where(t => !t.Applicable).ToList();

        var baseSection = new FoldableSection("遊戲本體");
        if (baseGame is { Applicable: true })
            baseSection.Content.AddChild(BuildSupportedRow(baseGame));
        else
            baseSection.Content.AddChild(CreateTextLabel(baseGame?.Reason ?? "找不到遊戲本體的 zhs 原文。", 20, Colors.Gray));
        _content.AddChild(baseSection.Root);

        var supportedSection = new FoldableSection("支援轉換的模組");
        if (supported.Count == 0)
            supportedSection.Content.AddChild(CreateTextLabel("沒有偵測到支援轉換的模組。", 20, Colors.Gray));
        else
            foreach (var target in supported) supportedSection.Content.AddChild(BuildSupportedRow(target));
        _content.AddChild(supportedSection.Root);

        var unsupportedSection = new FoldableSection("不支援的模組", startExpanded: false);
        if (unsupported.Count == 0)
            unsupportedSection.Content.AddChild(CreateTextLabel("（沒有）", 20, Colors.Gray));
        else
            foreach (var target in unsupported)
                unsupportedSection.Content.AddChild(BuildUnsupportedRow(target));
        _content.AddChild(unsupportedSection.Root);

        _content.AddChild(CreateDivider());
        _content.AddChild(BuildFooter());
        RefreshSize();
        Callable.From(RefreshSize).CallDeferred();
    }

    private HBoxContainer BuildSupportedRow(LocTarget target)
    {
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 66),
        };

        var name = CreateTextLabel(
            string.IsNullOrEmpty(target.Version) ? target.Name : $"{target.Name}  ({target.Version})",
            22,
            new Color(0.91f, 0.86f, 0.74f));
        name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(name);

        var button = new NativeActionButton("重新套用", NativeActionButtonColor.Teal);
        button.CustomMinimumSize = new Vector2(ActionColumnWidth, 58);
        button.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        button.Connect(NClickableControl.SignalName.Released, Callable.From<NativeActionButton>(_ =>
        {
            ConfirmDialog.Show(
                "重新套用",
                $"對「{target.Name}」重新轉換並套用一次。要繼續嗎？",
                "重新套用",
                () => OnReapplyOne(target));
        }));
        row.AddChild(button);

        return row;
    }

    private static HBoxContainer BuildUnsupportedRow(LocTarget target)
    {
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 38),
        };
        row.AddThemeConstantOverride("separation", 16);

        var nameScroller = new ScrollContainer
        {
            Name = "NameScroller",
            ClipContents = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            CustomMinimumSize = new Vector2(0, 36),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };

        var name = CreateTextLabel(target.Name, 18, Colors.Gray);
        name.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        name.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        name.CustomMinimumSize = new Vector2(Mathf.Max(1f, name.GetMinimumSize().X), 30);
        nameScroller.AddChild(name);
        row.AddChild(nameScroller);

        var reason = CreateTextLabel(target.Reason, 18, Colors.Gray);
        reason.CustomMinimumSize = new Vector2(ActionColumnWidth, 36);
        reason.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        row.AddChild(reason);

        return row;
    }

    private HBoxContainer BuildFooter()
    {
        var footer = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 70),
        };
        footer.AddThemeConstantOverride("separation", 10);

        var folder = FooterButton("開啟快取資料夾", NativeActionButtonColor.Teal);
        folder.Connect(NClickableControl.SignalName.Released, Callable.From<NativeActionButton>(_ =>
        {
            try { OS.ShellShowInFileManager(CacheStore.Root); }
            catch (Exception ex) { MainFile.Logger.Warn($"[{MainFile.ModId}] 開啟資料夾失敗: {ex.Message}"); }
        }));
        footer.AddChild(folder);

        var all = FooterButton("全部重新套用", NativeActionButtonColor.Green);
        all.Connect(NClickableControl.SignalName.Released, Callable.From<NativeActionButton>(_ =>
        {
            ConfirmDialog.Show(
                "全部重新套用",
                "對清單上每一個可用目標都重新轉換並套用一次。要繼續嗎？",
                "重新套用",
                OnReapplyAll);
        }));
        footer.AddChild(all);

        footer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        var close = FooterButton("關閉", NativeActionButtonColor.Red);
        close.Connect(NClickableControl.SignalName.Released, Callable.From<NativeActionButton>(_ => _stack.Pop()));
        footer.AddChild(close);

        return footer;
    }

    private static NativeActionButton FooterButton(string text, NativeActionButtonColor color)
    {
        return new NativeActionButton(text, color)
        {
            CustomMinimumSize = new Vector2(250, 58),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
    }

    private void OnReapplyOne(LocTarget target)
    {
        var (tables, keys) = LocConverter.RegenerateAndApply(target, LocManager.Instance);
        SetStatus($"{target.Name}：{tables} 表格、{keys} 筆。", tables == 0);
        Rebuild();
    }

    private void OnReapplyAll()
    {
        var mgr = LocManager.Instance;
        int tables = 0, keys = 0, count = 0;
        foreach (var target in TargetRegistry.EnsureScan().Where(x => x.Applicable))
        {
            var (tb, kv) = LocConverter.RegenerateAndApply(target, mgr);
            tables += tb;
            keys += kv;
            count++;
        }
        SetStatus($"全部重新套用：{count} 個目標、{tables} 表格、{keys} 筆。", false);
        Rebuild();
    }

    private void SetStatus(string text, bool error)
    {
        _statusText = text;
        _statusIsError = error;
        if (_status == null) return;
        _status.Text = text;
        _status.Visible = !string.IsNullOrEmpty(text);
        _status.LabelSettings.FontColor = error ? new Color(0.92f, 0.45f, 0.45f) : new Color(0.68f, 0.72f, 0.80f);
    }

    private static Label CreateTextLabel(string text, int fontSize, Color color)
    {
        return new Label
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            LabelSettings = new LabelSettings
            {
                Font = NativeFonts.Regular,
                FontSize = fontSize,
                FontColor = color,
                ShadowSize = 2,
                ShadowColor = new Color(0f, 0f, 0f, 0.8f),
            },
        };
    }

    private static ColorRect CreateDivider()
    {
        return new ColorRect
        {
            Name = "Divider",
            CustomMinimumSize = new Vector2(0, 2),
            MouseFilter = MouseFilterEnum.Ignore,
            Color = new Color(0.909804f, 0.862745f, 0.745098f, 0.25098f),
        };
    }

    private void RefreshSize()
    {
        if (!IsInsideTree()) return;

        var screenSize = GetViewportRect().Size;
        var contentWidth = Mathf.Min(MaxContentWidth, Mathf.Max(640f, screenSize.X - 420f));
        var containerWidth = contentWidth + NativeScrollContainer.ScrollbarGutterWidth;
        var left = Mathf.Max(140f, (screenSize.X - containerWidth) * 0.5f);

        _scrollArea.Position = new Vector2(left, 0);
        _scrollArea.Size = new Vector2(containerWidth, screenSize.Y);

        _contentPanel.Position = Vector2.Zero;
        _contentPanel.CustomMinimumSize = new Vector2(_scrollArea.AvailableContentWidth, _content.GetMinimumSize().Y + 120f);
        _contentPanel.Size = _contentPanel.CustomMinimumSize;

        _content.Position = Vector2.Zero;
        _content.CustomMinimumSize = new Vector2(contentWidth, 0);
        _content.Size = new Vector2(contentWidth, _content.GetMinimumSize().Y);

        _title.OffsetLeft = left;
        _title.OffsetRight = left + contentWidth;
        _title.OffsetTop = 22f;
        _title.OffsetBottom = TopOffset - 10f;
    }

    private async void WaitForLayoutAndFadeIn()
    {
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            RefreshSize();
            MainFile.Logger.Info($"[{MainFile.ModId}] submenu layout ready, built={_contentBuilt}, min={_content.GetMinimumSize()}, size={_content.Size}, panel={_contentPanel.Size}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] submenu layout wait failed: {ex.Message}");
        }
        finally
        {
            if (IsInstanceValid(this) && IsInsideTree())
            {
                _fadeInTween?.Kill();
                _fadeInTween = CreateTween().SetParallel();
                _fadeInTween.TweenProperty(_contentPanel, "modulate", Colors.White, 0.5f)
                    .From(new Color(0, 0, 0, 0))
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Cubic);
            }
        }
    }

    private static Control? FindFirstFocusable(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is Control { Visible: true, FocusMode: not FocusModeEnum.None } control)
                return control;

            var nested = FindFirstFocusable(child);
            if (nested != null) return nested;
        }

        return null;
    }
}

internal static class TraditionalizeSubmenuOpener
{
    public static void Open(NMainMenuSubmenuStack stack)
    {
        stack.PushSubmenuType<TraditionalizeSubmenu>();
    }
}
