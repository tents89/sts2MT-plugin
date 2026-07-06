using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace Sts2ModTranslatorOpenCC.Ui;

internal partial class NativeScrollContainer : NScrollableContainer
{
    private readonly Control _clipper;
    private readonly TextureRect _fadeMask;
    private readonly Gradient _maskGradient;
    private readonly float _topPadding;
    private readonly float _bottomPadding;

    public const float ScrollbarGutterWidth = 60f;
    private const float BottomFade = 70f;
    private const float TopFade = 24f;

    public float AvailableContentWidth => Mathf.Max(0f, Size.X - ScrollbarGutterWidth);

    public NativeScrollContainer(float topPadding = 0f, float bottomPadding = 0f)
    {
        Name = "Sts2OpenCCNativeScrollContainer";
        ClipChildren = ClipChildrenMode.Only;
        _topPadding = topPadding;
        _bottomPadding = bottomPadding;

        SetAnchorsPreset(LayoutPreset.FullRect);

        _maskGradient = new Gradient
        {
            Colors =
            [
                new Color(1f, 1f, 1f, 0f),
                new Color(1f, 1f, 1f, 0.4f),
                new Color(1f, 1f, 1f, 1f),
                new Color(1f, 1f, 1f, 1f),
                new Color(1f, 1f, 1f, 0f),
            ],
        };

        _fadeMask = new TextureRect
        {
            Name = "Mask",
            ClipChildren = ClipChildrenMode.Only,
            MouseFilter = MouseFilterEnum.Ignore,
            Texture = new GradientTexture2D
            {
                FillFrom = new Vector2(0f, 1f),
                FillTo = Vector2.Zero,
                Gradient = _maskGradient,
            },
        };
        _fadeMask.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_fadeMask);

        _clipper = new Control
        {
            Name = "Clipper",
            ClipContents = true,
            OffsetTop = topPadding,
            OffsetBottom = -bottomPadding,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _clipper.SetAnchorsPreset(LayoutPreset.FullRect, true);
        _clipper.OffsetRight = -ScrollbarGutterWidth;
        _fadeMask.AddChild(_clipper);

        var scrollbar = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("ui/scrollbar")).Instantiate<NScrollbar>();
        scrollbar.Name = "Scrollbar";
        scrollbar.SetAnchorsPreset(LayoutPreset.RightWide);
        scrollbar.OffsetLeft = -48f;
        scrollbar.OffsetRight = 0f;
        scrollbar.OffsetTop = topPadding + 64f;
        scrollbar.OffsetBottom = -bottomPadding - 64f;
        AddChild(scrollbar);

        Resized += OnContainerResized;
    }

    public void AttachContent(Control contentPanel)
    {
        if (_content != null) _content.Resized -= OnContentResized;

        _clipper.AddChild(contentPanel);
        SetContent(contentPanel);
        _content!.Resized += OnContentResized;
        OnContainerResized();
    }

    public new void InstantlyScrollToTop()
    {
        if (_content == null) return;
        _targetDragPosY = 0f;
        _content.Position = _content.Position with { Y = _paddingTop };
        Scrollbar.SetValueWithoutAnimation(0.0);
    }

    private void OnContainerResized()
    {
        var actualHeight = Size.Y;
        if (actualHeight <= 0) return;

        _maskGradient.Offsets =
        [
            0f,
            BottomFade * 0.4f / actualHeight,
            BottomFade / actualHeight,
            FromTop(_topPadding + TopFade),
            FromTop(_topPadding),
        ];

        UpdateScrollLimitBottomOverride();
        OnContentResized();
        return;

        float FromTop(float px) => 1f - px / actualHeight;
    }

    [HarmonyPatch(typeof(NScrollableContainer), nameof(NScrollableContainer.UpdateScrollLimitBottom))]
    public static class NScrollableContainerUpdateScrollLimitBottomPatch
    {
        public static bool Prefix(NScrollableContainer __instance)
        {
            if (__instance is not NativeScrollContainer self) return true;

            self.UpdateScrollLimitBottomOverride();
            return false;
        }
    }

    private void UpdateScrollLimitBottomOverride()
    {
        if (_content == null || Scrollbar == null) return;

        var wasVisible = Scrollbar.Visible;
        const float epsilon = 1f;

        var contentFits = _content.Size.Y + _paddingTop + _paddingBottom - epsilon <= ScrollViewportSize;
        var scrollIsAtTop = -_content.Position.Y <= _paddingTop + epsilon;
        Scrollbar.Visible = !contentFits || !scrollIsAtTop;
        Scrollbar.MouseFilter = Scrollbar.Visible ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;

        if (!wasVisible && Scrollbar.Visible)
            _targetDragPosY = _content.Position.Y - _paddingTop;

        _fadeMask.ClipChildren = Scrollbar.Visible ? ClipChildrenMode.Only : ClipChildrenMode.Disabled;
        _fadeMask.SelfModulate = new Color(1f, 1f, 1f, Scrollbar.Visible ? 1f : 0f);

        if (!Scrollbar.Visible) return;

        var scrollDistanceFromTop = Mathf.Max(0f, _paddingTop - _content.Position.Y);
        var topAlpha = 1f - Mathf.Clamp(scrollDistanceFromTop / TopFade, 0f, 1f);

        var colors = _maskGradient.Colors;
        colors[4] = new Color(1f, 1f, 1f, topAlpha);
        _maskGradient.Colors = colors;
    }

    private void OnContentResized()
    {
        if (_content == null || Scrollbar == null) return;
        Scrollbar.SetValueNoSignal(Mathf.Clamp((_content.Position.Y - _paddingTop) / ScrollLimitBottom, 0.0f, 1f) * 100.0);
    }
}
