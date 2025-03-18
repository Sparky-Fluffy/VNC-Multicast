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

    private bool isWindowDragInEffect = false;
    private Point cursorPositionAtWindowDragStart = new(0, 0);

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

    private void WindowDragHandle_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (isWindowDragInEffect)
        {
            Point currentCursorPosition = e.GetPosition(this);
            Point cursorPositionDelta = currentCursorPosition - cursorPositionAtWindowDragStart;

            Position = this.PointToScreen(cursorPositionDelta);
        }
    }

    private void WindowDragHandle_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if ((e.Source is Control sourceControl) && (sourceControl.Name == "WindowDragHandle"))
        {
            isWindowDragInEffect = true;
            cursorPositionAtWindowDragStart = e.GetPosition(this);
        }
    }

    private void WindowDragHandle_OnPointerReleased(object? sender, PointerReleasedEventArgs e) => isWindowDragInEffect = false;

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        // if (double.TryParse(celsius.Text, out double C))
        // {
        //     var F = C * (9d / 5d) + 32;
        //     fahrenheit.Text = F.ToString("0.0");
        // }
        // else
        // {
        //     celsius.Text = "0";
        //     fahrenheit.Text = "0";
        // }

        // Console.WriteLine($"Click! Celsius={celsius.Text}");
    }
}