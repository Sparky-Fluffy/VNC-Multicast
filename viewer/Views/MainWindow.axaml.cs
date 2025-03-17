using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using viewer.ViewModels;

namespace viewer.Views;

public partial class MainWindow : Window
{
    private int screenWidth = 1000;
    private int screenHeight = 1000;
    private MainWindowViewModel viewModel;

    public MainWindow()
    {
        InitializeComponent();
        
        viewModel = new MainWindowViewModel();

        screenWidth = Screens.Primary!.Bounds.Width;
        screenHeight = Screens.Primary!.Bounds.Height;

        Width = screenWidth * viewModel.PercentWidth;
        Height = screenHeight * viewModel.PercentHeight;
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (double.TryParse(celsius.Text, out double C))
        {
            var F = C * (9d / 5d) + 32;
            fahrenheit.Text = F.ToString("0.0");
        }
        else
        {
            celsius.Text = "0";
            fahrenheit.Text = "0";
        }

        Console.WriteLine($"Click! Celsius={celsius.Text}");
    }
}