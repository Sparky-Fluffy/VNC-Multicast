using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using receiver;
using SkiaSharp;
using viewer.ViewModels;

namespace viewer.Views;

public partial class MainWindow : Window
{
    private int screenWidth = 1000;
    private int screenHeight = 1000;

    public double WindowWidth => Width;
    public double WindowHeight => Height;

    private string sessionName = "";

    private bool wasMaximized = false;

    public bool IsMaximized => WindowState == WindowState.Maximized;
    public bool IsFullScreen => WindowState == WindowState.FullScreen;

    public bool IsDark => RequestedThemeVariant == ThemeVariant.Dark;

    private string ThemeSwitchText => IsDark ? "Light" : "Dark";

    private Bitmap? screenViewBitmap;

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

        ThemeSwitch.Content = ThemeSwitchText;
    }

    private async void Select_OnClick(object? sender, RoutedEventArgs e)
    {
        Page1.IsVisible = false;
        Page2.IsVisible = true;
        await Task.Run(JopaZamenitely);
        
        ScreenViewImage.Background = new ImageBrush()
        {
            Source = screenViewBitmap,
            Stretch = Stretch.Fill
        };
    }

    private void Audio_OnClick(object? sender, RoutedEventArgs e)
    {
        
    }

    private void Cancel_OnClick(object? sender, RoutedEventArgs e)
    {
        Page1.IsVisible = true;
        Page2.IsVisible = false;
        SetWindowNormal();
    }

    private void Expand_OnClick(object? sender, RoutedEventArgs e)
    {
        if (IsFullScreen) SetWindowNormal();
        else SetWindowMaximized();
    }

    private void SwitchTheme_OnClick(object? sender, RoutedEventArgs e)
    {
        if (IsDark) RequestedThemeVariant = ThemeVariant.Light;
        else RequestedThemeVariant = ThemeVariant.Dark;
        ThemeSwitch.Content = ThemeSwitchText;
    }

    private void SetWindowNormal()
    {
        if (IsMaximized || wasMaximized)
            WindowState = WindowState.Maximized;
        else
            WindowState = WindowState.Normal;
        wasMaximized = false;
        SystemDecorations = SystemDecorations.Full;
        ScreenView.Classes.Remove("fullScreen");
        FunctionButtons.Classes.Remove("fullScreen");
        ViewHeader.IsVisible = true;
    }

    private void SetWindowMaximized()
    {
        if (IsMaximized) wasMaximized = true;
        WindowState = WindowState.FullScreen;
        SystemDecorations = SystemDecorations.None;
        ScreenView.Classes.Add("fullScreen");
        FunctionButtons.Classes.Add("fullScreen");
        ViewHeader.IsVisible = false;
    }

    private async Task JopaZamenitely()
    {
        Receiver receiver = new Receiver(IPAddress.Parse("239.0.0.0"), 8001);

        receiver.Connect();

        SKBitmap bitmap = new SKBitmap
        (
            receiver.Width, receiver.Height,
            SKColorType.Bgra8888,
            SKAlphaType.Premul
        );

        nint pixels = bitmap.GetPixels();
        byte[] source = new byte[bitmap.ByteCount];

        ushort rectCount = receiver.ReceiveRectCount();
        
        for (ushort i = 0; i < rectCount; i++)
        {
            ushort[] rectData = receiver.ReceiveRectData();

            for (int p = 0; p < rectData[2] * rectData[3]; p++)
                SetPixel(ref bitmap, p, receiver.ReceivePixel(), pixels);
        }

        using (var enc = bitmap.Encode(SKEncodedImageFormat.Jpeg, 100))
        {
            using (var ms = new MemoryStream())
            {
                enc.SaveTo(ms);
                ms.Position = 0;
                screenViewBitmap = new Bitmap(ms);
            }
        }
    }

    private void SetPixel(ref SKBitmap bitmap, int p, byte[] pixel, nint pixels)
    {
        Marshal.Copy(pixel, 0, pixels + p * pixel.Length, pixel.Length);
    }
}