using System;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using viewer.ViewModels;

namespace viewer.Views;

public partial class MainWindow : Window
{
    private int screenWidth = 1000;
    private int screenHeight = 1000;

    private string sessionName = "";

    private bool isFullScreen = false;

    public MainWindow()
    {
        InitializeComponent();

        if (Screens.Primary is Screen screen)
        {
            screenWidth = screen.Bounds.Width;
            screenHeight = screen.Bounds.Height;
        }

        DataContext = new MainWindowViewModel();

        if (DataContext is MainWindowViewModel vm)
        {
            Width = screenWidth * vm.PercentWidth;
            Height = screenHeight * vm.PercentHeight;
        }

        SessionPicker.ItemsSource = new string[]
                                    {"cat", "camel", "cow", "chameleon", "mouse", "lion", "zebra" }.OrderBy(x => x);
    }

    private void Select_OnClick(object? sender, RoutedEventArgs e)
    {
        Page1.IsVisible = false;
        Page2.IsVisible = true;
    }

    private void Cancel_OnClick(object? sender, RoutedEventArgs e)
    {
        Page1.IsVisible = true;
        Page2.IsVisible = false;
        SetWindowNormal();
    }

    private void Expand_OnClick(object? sender, RoutedEventArgs e)
    {
        if (isFullScreen) SetWindowNormal();
        else SetWindowMaximized();
    }

    private void SetWindowNormal()
    {
        WindowState = WindowState.Normal;
        SystemDecorations = SystemDecorations.Full;
        isFullScreen = false;
    }

    private void SetWindowMaximized()
    {
        WindowState = WindowState.FullScreen;
        SystemDecorations = SystemDecorations.None;
        ViewHeader.IsVisible = false;
        ScreenView.Width = screenWidth;
        ScreenView.Height = screenHeight;
        ScreenView.Margin = new Thickness(0, 0, 0, 0);
        FunctionButtons.Margin = new Thickness(0, 0, 0, 0);
        isFullScreen = true;
        
    }
}