using System.IO;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace Sts2ModTranslatorOpenCC.Ui;

internal static class NativeFonts
{
    public static Font Regular => PreloadManager.Cache.GetAsset<Font>("res://themes/kreon_regular_glyph_space_one.tres");
    public static Font Bold => PreloadManager.Cache.GetAsset<Font>("res://themes/kreon_bold_glyph_space_two.tres");
}

internal static class NodeTransfer
{
    public static T TransferAllNodes<T>(this T target, string sourceScene) where T : Node
    {
        var source = PreloadManager.Cache.GetScene(sourceScene).Instantiate();
        return target.TransferAllNodesFrom(source);
    }

    public static T TransferAllNodesFrom<T>(this T target, Node source) where T : Node
    {
        target.Name = source.Name;

        foreach (var child in source.GetChildren())
        {
            source.RemoveChild(child);
            target.AddChild(child);
            child.Owner = target;
            SetChildrenOwner(target, child);
        }

        source.QueueFree();
        return target;
    }

    private static void SetChildrenOwner(Node owner, Node node)
    {
        foreach (var child in node.GetChildren())
        {
            child.Owner = owner;
            SetChildrenOwner(owner, child);
        }
    }
}

internal enum NativeActionButtonColor
{
    Green,
    Teal,
    Red,
}

internal partial class NativeTextButton : NButton
{
    private readonly Label _label;
    private readonly Panel _backgroundPanel;
    private readonly StyleBoxFlat _styleBox;
    private Tween? _stateTween;
    private bool _isButtonDown;

    private readonly Color _textNormal = new(0.72f, 0.72f, 0.72f);
    private readonly Color _textHover = Colors.White;
    private readonly Color _textActive = StsColors.gold;
    private readonly Color _bgNormal = new(0f, 0f, 0f, 0.20f);
    private readonly Color _bgHover = new(0.15f, 0.15f, 0.15f, 0.52f);
    private readonly Color _bgPressed = new(0.20f, 0.20f, 0.20f, 0.72f);

    public bool Active { get; set; }

    public NativeTextButton(string text, float height = 64f, HorizontalAlignment alignment = HorizontalAlignment.Center)
    {
        CustomMinimumSize = new Vector2(0f, height);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Stop;

        _styleBox = new StyleBoxFlat
        {
            BgColor = _bgNormal,
            BorderColor = StsColors.gold,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
        };

        _backgroundPanel = new Panel { MouseFilter = MouseFilterEnum.Ignore };
        _backgroundPanel.SetAnchorsPreset(LayoutPreset.FullRect);
        _backgroundPanel.AddThemeStyleboxOverride("panel", _styleBox);
        AddChild(_backgroundPanel);

        _label = BuildLabel(alignment, NativeFonts.Regular, 24, _textNormal);
        AddChild(_label);
        ButtonText = text;
    }

    public string ButtonText
    {
        get => _label.Text;
        set
        {
            _label.Text = value;
            if (_label.LabelSettings != null)
                _label.LabelSettings.FontColor = Active ? _textActive : _textNormal;
        }
    }

    public override void _Ready()
    {
        ConnectSignals();
        UpdateVisualState(instant: true);
    }

    protected override void OnFocus()
    {
        base.OnFocus();
        UpdateVisualState();
    }

    protected override void OnUnfocus()
    {
        base.OnUnfocus();
        UpdateVisualState();
    }

    protected override void OnPress()
    {
        base.OnPress();
        _isButtonDown = true;
        UpdateVisualState();
    }

    protected override void OnRelease()
    {
        base.OnRelease();
        _isButtonDown = false;
        UpdateVisualState();
    }

    private void UpdateVisualState(bool instant = false)
    {
        var targetBg = _isButtonDown ? _bgPressed : IsFocused ? _bgHover : _bgNormal;
        var targetText = Active ? _textActive : IsFocused || _isButtonDown ? _textHover : _textNormal;
        var targetBorderWidth = Active ? 4 : 0;

        if (instant)
        {
            _styleBox.BgColor = targetBg;
            _styleBox.BorderWidthLeft = targetBorderWidth;
            if (_label.LabelSettings != null) _label.LabelSettings.FontColor = targetText;
            return;
        }

        _stateTween?.Kill();
        _stateTween = CreateTween().SetParallel().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _stateTween.TweenProperty(_styleBox, "bg_color", targetBg, 0.10f);
        _stateTween.TweenProperty(_styleBox, "border_width_left", targetBorderWidth, 0.18f);
        if (_label.LabelSettings != null)
            _stateTween.TweenProperty(_label.LabelSettings, "font_color", targetText, 0.15f);
    }

    private static Label BuildLabel(HorizontalAlignment alignment, Font font, int fontSize, Color fontColor)
    {
        var label = new Label
        {
            HorizontalAlignment = alignment,
            VerticalAlignment = VerticalAlignment.Center,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            LabelSettings = new LabelSettings
            {
                Font = font,
                FontSize = fontSize,
                FontColor = fontColor,
                ShadowSize = 2,
                ShadowColor = new Color(0f, 0f, 0f, 0.8f),
            },
        };
        label.SetAnchorsPreset(LayoutPreset.FullRect);
        label.OffsetLeft = 18f;
        label.OffsetRight = -18f;
        return label;
    }
}

internal partial class NativeActionButton : NSettingsButton
{
    private readonly new Label _label;
    private readonly new TextureRect _image;
    private Action? _onPressedAction;
    private static Texture2D? _buttonTexture;

    public NativeActionButton(string text, NativeActionButtonColor color, Action? onPressed = null)
    {
        CustomMinimumSize = new Vector2(324, 64);
        SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        SizeFlagsVertical = SizeFlags.Fill;
        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Stop;

        _image = new TextureRect
        {
            Name = "Image",
            CustomMinimumSize = new Vector2(64, 64),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            Texture = GetButtonTexture(),
        };
        _image.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_image);

        _label = BuildActionLabel();
        AddChild(_label);

        var reticleScene = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("ui/selection_reticle"));
        var reticle = reticleScene.Instantiate<NSelectionReticle>();
        reticle.Name = "SelectionReticle";
        reticle.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(reticle);

        ButtonText = text;
        SetColor(color);
        _onPressedAction = onPressed;
    }

    public string ButtonText
    {
        get => _label.Text;
        set => _label.Text = value;
    }

    public override void _Ready()
    {
        ConnectSignals();
        Connect(NClickableControl.SignalName.Released, Callable.From<NativeActionButton>(OnReleased));
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Disconnect(NClickableControl.SignalName.Released, Callable.From<NativeActionButton>(OnReleased));
    }

    public void SetColor(NativeActionButtonColor color)
    {
        _image.SelfModulate = color switch
        {
            NativeActionButtonColor.Red => Color.FromHtml("#b03f3f"),
            NativeActionButtonColor.Teal => Color.FromHtml("#3b7a83"),
            _ => Color.FromHtml("#4f9425"),
        };
    }

    private void OnReleased(NativeActionButton _)
    {
        _onPressedAction?.Invoke();
    }

    private static Label BuildActionLabel()
    {
        var label = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            LabelSettings = new LabelSettings
            {
                Font = NativeFonts.Bold,
                FontSize = 28,
                FontColor = new Color(0.91f, 0.86f, 0.74f),
                OutlineSize = 12,
                OutlineColor = new Color(0.29f, 0.14f, 0.14f),
                ShadowSize = 2,
                ShadowColor = new Color(0f, 0f, 0f, 0.8f),
            },
        };
        label.SetAnchorsPreset(LayoutPreset.FullRect);
        return label;
    }

    private static Texture2D? GetButtonTexture()
    {
        if (_buttonTexture != null) return _buttonTexture;

        try
        {
            var dllDir = Path.GetDirectoryName(typeof(NativeActionButton).Assembly.Location);
            if (string.IsNullOrEmpty(dllDir)) return null;

            var path = Path.Combine(dllDir, "Assets", "OpenCC_configbutton.png");
            if (!File.Exists(path)) return null;

            var image = Image.LoadFromFile(path);
            if (image == null || image.IsEmpty()) return null;

            _buttonTexture = ImageTexture.CreateFromImage(image);
            return _buttonTexture;
        }
        catch
        {
            return null;
        }
    }
}

internal partial class NativeToggle : NSettingsTickbox
{
    private readonly Func<bool> _getter;
    private readonly Action<bool> _setter;

    public NativeToggle(Func<bool> getter, Action<bool> setter)
    {
        _getter = getter;
        _setter = setter;

        SetCustomMinimumSize(new Vector2(324, 64));
        SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        SizeFlagsVertical = SizeFlags.Fill;
        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Pass;

        this.TransferAllNodes(SceneHelper.GetScenePath("screens/settings_tickbox"));
    }

    public override void _Ready()
    {
        ConnectSignals();
        IsTicked = _getter();
    }

    protected override void OnTick() => _setter(true);

    protected override void OnUntick() => _setter(false);
}

internal partial class NativeOptionRow : MarginContainer
{
    public NativeOptionRow(string labelText, Control settingControl)
    {
        CustomMinimumSize = new Vector2(0, 70);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        MouseFilter = MouseFilterEnum.Pass;
        AddThemeConstantOverride("margin_left", 12);
        AddThemeConstantOverride("margin_right", 12);

        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 70),
        };
        AddChild(row);

        var label = new Label
        {
            Text = labelText,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
            LabelSettings = new LabelSettings
            {
                Font = NativeFonts.Regular,
                FontSize = 24,
                FontColor = new Color(0.91f, 0.86f, 0.74f),
                ShadowSize = 2,
                ShadowColor = new Color(0f, 0f, 0f, 0.85f),
            },
        };
        row.AddChild(label);
        row.AddChild(settingControl);
    }
}
