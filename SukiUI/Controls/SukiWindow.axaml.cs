using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using SukiUI.Enums;
using SukiUI.Utilities.Background;

namespace SukiUI.Controls;

public class SukiWindow : Window
{
    protected override Type StyleKeyOverride => typeof(SukiWindow);

    public static readonly StyledProperty<double> TitleFontSizeProperty =
        AvaloniaProperty.Register<SukiWindow, double>(nameof(TitleFontSize), defaultValue: 13);

    public double TitleFontSize
    {
        get => GetValue(TitleFontSizeProperty);
        set => SetValue(TitleFontSizeProperty, value);
    }

    public static readonly StyledProperty<FontWeight> TitleFontWeightProperty =
        AvaloniaProperty.Register<SukiWindow, FontWeight>(nameof(TitleFontWeight),
            defaultValue: FontWeight.Bold);

    public FontWeight TitleFontWeight
    {
        get => GetValue(TitleFontWeightProperty);
        set => SetValue(TitleFontWeightProperty, value);
    }

    public static readonly StyledProperty<Control?> LogoContentProperty =
        AvaloniaProperty.Register<SukiWindow, Control?>(nameof(LogoContent));

    public Control? LogoContent
    {
        get => GetValue(LogoContentProperty);
        set => SetValue(LogoContentProperty, value);
    }

    public static readonly StyledProperty<bool> ShowBottomBorderProperty =
        AvaloniaProperty.Register<SukiWindow, bool>(nameof(ShowBottomBorder), defaultValue: true);

    public bool ShowBottomBorder
    {
        get => GetValue(ShowBottomBorderProperty);
        set => SetValue(ShowBottomBorderProperty, value);
    }

    public static readonly StyledProperty<bool> IsTitleBarVisibleProperty =
        AvaloniaProperty.Register<SukiWindow, bool>(nameof(IsTitleBarVisible), defaultValue: true);

    public bool IsTitleBarVisible
    {
        get => GetValue(IsTitleBarVisibleProperty);
        set => SetValue(IsTitleBarVisibleProperty, value);
    }

    public static readonly StyledProperty<bool> IsMenuVisibleProperty =
        AvaloniaProperty.Register<SukiWindow, bool>(nameof(IsMenuVisible), defaultValue: false);

    public bool IsMenuVisible
    {
        get => GetValue(IsMenuVisibleProperty);
        set => SetValue(IsMenuVisibleProperty, value);
    }

    public static readonly StyledProperty<AvaloniaList<MenuItem>?> MenuItemsProperty =
        AvaloniaProperty.Register<SukiWindow, AvaloniaList<MenuItem>?>(nameof(MenuItems));

    public AvaloniaList<MenuItem>? MenuItems
    {
        get => GetValue(MenuItemsProperty);
        set => SetValue(MenuItemsProperty, value);
    }


    public static readonly StyledProperty<bool> CanMinimizeProperty =
        AvaloniaProperty.Register<SukiWindow, bool>(nameof(CanMinimize), defaultValue: true);

    public bool CanMinimize
    {
        get => GetValue(CanMinimizeProperty);
        set => SetValue(CanMinimizeProperty, value);
    }

    public static readonly StyledProperty<bool> CanMoveProperty =
        AvaloniaProperty.Register<SukiWindow, bool>(nameof(CanMove), defaultValue: true);

    public bool CanMove
    {
        get => GetValue(CanMoveProperty);
        set => SetValue(CanMoveProperty, value);
    }

    // BACKGROUND PROPERTIES
    public static readonly StyledProperty<bool> BackgroundAnimationEnabledProperty =
        AvaloniaProperty.Register<SukiWindow, bool>(nameof(BackgroundAnimationEnabled), defaultValue: false);

    public bool BackgroundAnimationEnabled
    {
        get => GetValue(BackgroundAnimationEnabledProperty);
        set => SetValue(BackgroundAnimationEnabledProperty, value);
    }

    public static readonly StyledProperty<SukiBackgroundStyle> BackgroundStyleProperty =
        AvaloniaProperty.Register<SukiWindow, SukiBackgroundStyle>(nameof(BackgroundStyle),
            defaultValue: SukiBackgroundStyle.Waves);

    /// <summary>
    /// Which of the default background styles to use.
    /// </summary>
    public SukiBackgroundStyle BackgroundStyle
    {
        get => GetValue(BackgroundStyleProperty);
        set => SetValue(BackgroundStyleProperty, value);
    }

    public static readonly StyledProperty<string?> BackgroundShaderFileProperty =
        AvaloniaProperty.Register<SukiWindow, string?>(nameof(BackgroundShaderFile));

    /// <summary>
    /// Specify a filename of an EMBEDDED RESOURCE file of type `.SkSL` with or without extension and it will be loaded and displayed.
    /// This takes priority over the <see cref="BackgroundShaderCode"/> property, which in turns takes priority over <see cref="BackgroundStyle"/>.
    /// </summary>
    public string? BackgroundShaderFile
    {
        get => GetValue(BackgroundShaderFileProperty);
        set => SetValue(BackgroundShaderFileProperty, value);
    }

    public static readonly StyledProperty<string?> BackgroundShaderCodeProperty =
        AvaloniaProperty.Register<SukiWindow, string?>(nameof(BackgroundShaderCode));

    /// <summary>
    /// Specify the shader code to use directly, simpler if you don't want to create an .SkSL file or want to generate the shader effect at runtime in some way.
    /// This takes priority over the <see cref="BackgroundStyle"/> property, but is second in priority to <see cref="BackgroundShaderFile"/> if it is set.
    /// </summary>
    public string? BackgroundShaderCode
    {
        get => GetValue(BackgroundShaderCodeProperty);
        set => SetValue(BackgroundShaderCodeProperty, value);
    }

    public SukiWindow()
    {
        MenuItems = new AvaloniaList<MenuItem>();
    }

    private IDisposable? _subscriptionDisposables;
    private SukiBackground _background;

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;
        if (desktop.MainWindow is SukiWindow s && s != this)
        {
            if (Icon == null) Icon = s.Icon;
            // This would be nice to do, but obviously LogoContent is a control and you can't attach it twice.
            // if (LogoContent is null) LogoContent = s.LogoContent;
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        var stateObs = this.GetObservable(WindowStateProperty)
            .Do(OnWindowStateChanged)
            .Select(_ => Unit.Default);
        try
        {
            // Create handlers for buttons
            if (e.NameScope.Get<Button>("PART_MaximizeButton") is { } maximize)
            {
                maximize.Click += (_, _) =>
                {
                    if (!CanResize) return;
                    WindowState = WindowState == WindowState.Maximized
                        ? WindowState.Normal
                        : WindowState.Maximized;
                };
            }

            if (e.NameScope.Get<Button>("PART_MinimizeButton") is { } minimize)
                minimize.Click += (_, _) => WindowState = WindowState.Minimized;

            if (e.NameScope.Get<Button>("PART_CloseButton") is { } close)
                close.Click += (_, _) => Close();

            if (e.NameScope.Get<GlassCard>("PART_TitleBarBackground") is { } titleBar)
                titleBar.PointerPressed += OnTitleBarPointerPressed;

            if (e.NameScope.Get<SukiBackground>("PART_Background") is { } background)
            {
                _background = background;
                background.AnimationEnabled = BackgroundAnimationEnabled;
                HandleBackgroundStyleChanges();
                var bgAnimObs = this.GetObservable(BackgroundAnimationEnabledProperty)
                    .Do(enabled => background.AnimationEnabled = enabled)
                    .Select(_ => Unit.Default)
                    .Merge(stateObs);
                var bgStyleObs = this.GetObservable(BackgroundStyleProperty)
                    .Do(_ => HandleBackgroundStyleChanges())
                    .Select(_ => Unit.Default)
                    .Merge(bgAnimObs);
                var bgShaderFileObs = this.GetObservable(BackgroundShaderFileProperty)
                    .Do(_ => HandleBackgroundStyleChanges())
                    .Select(_ => Unit.Default)
                    .Merge(bgStyleObs);
                var bgShaderCodeObs = this.GetObservable(BackgroundShaderCodeProperty)
                    .Do(_ => HandleBackgroundStyleChanges())
                    .Select(_ => Unit.Default)
                    .Merge(bgShaderFileObs)
                    .ObserveOn(new AvaloniaSynchronizationContext());

                _subscriptionDisposables = bgShaderCodeObs.Subscribe();
            }
            else
            {
                // ensure the state observable is enabled even if the background isn't present.
                stateObs.ObserveOn(new AvaloniaSynchronizationContext()).Subscribe();
            }
        }
        catch
        {
        }
    }

    private void HandleBackgroundStyleChanges()
    {
        if (BackgroundShaderFile is not null)
            _background.BackgroundEffect = SukiBackgroundEffect.FromEmbeddedResource(BackgroundShaderFile);
        else if (BackgroundShaderCode is not null) 
            _background.BackgroundEffect = SukiBackgroundEffect.FromString(BackgroundShaderCode);
        else
            _background.BackgroundEffect = SukiBackgroundEffect.FromEmbeddedResource(BackgroundStyle.ToString());
    }

    private void OnWindowStateChanged(WindowState state)
    {
        if (state == WindowState.FullScreen)
            CanResize = CanMove = false;
        else
            CanResize = CanMove = true;
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        BeginMoveDrag(e);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _subscriptionDisposables?.Dispose();
    }
}