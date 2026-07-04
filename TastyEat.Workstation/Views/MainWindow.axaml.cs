using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using TastyEat.Workstation.ViewModels;

namespace TastyEat.Workstation.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    private ScrollViewer? _tabScrollViewer;

    public MainWindow()
    {
        InitializeComponent();

        this.Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _tabScrollViewer = MainTabControl.FindControl<ScrollViewer>("TabScrollViewer");

        MainTabControl.PointerWheelChanged += OnTabWheel;
        MainTabControl.SelectionChanged += OnTabSelectionChanged;

        CenterSelectedTab();
    }

    private void OnTabWheel(object? sender, PointerWheelEventArgs e)
    {
        if (_tabScrollViewer is null)
            return;

        var delta = e.Delta.Y * 48;
        var newOffset = _tabScrollViewer.Offset.X - delta;
        var maxOffset = Math.Max(0, _tabScrollViewer.Extent.Width - _tabScrollViewer.Viewport.Width);

        _tabScrollViewer.Offset = _tabScrollViewer.Offset.WithX(Math.Clamp(newOffset, 0, maxOffset));
        e.Handled = true;
    }

    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        CenterSelectedTab();
    }

    private void CenterSelectedTab()
    {
        if (_tabScrollViewer is null)
            return;

        var tabItem = MainTabControl.ContainerFromIndex(MainTabControl.SelectedIndex) as TabItem;
        if (tabItem is null)
            return;

        var tabBounds = tabItem.Bounds;
        var viewport = _tabScrollViewer.Viewport.Width;
        var extent = _tabScrollViewer.Extent.Width;
        var maxOffset = Math.Max(0, extent - viewport);

        var target = tabBounds.X + tabBounds.Width / 2 - viewport / 2;
        var offset = Math.Clamp(target, 0, maxOffset);

        _tabScrollViewer.Offset = _tabScrollViewer.Offset.WithX(offset);
    }
}
