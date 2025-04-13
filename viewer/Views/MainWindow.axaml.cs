using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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

    private void Select_OnClick(object? sender, RoutedEventArgs e)
    {
        Page1.IsVisible = false;
        Page2.IsVisible = true;
        JopaZamenitely();
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

    private async void JopaZamenitely()
    {
        Receiver receiver = new Receiver(IPAddress.Parse("239.0.0.0"), 8001);

        receiver.Connect();
        screenViewBitmap = new Bitmap(PixelFormat.Rgba8888, AlphaFormat.Opaque, 0, 4, 100, 4);
        ushort rectCount = receiver.ReceiveRectCount();
        
        for (ushort i = 0; i < rectCount; i++)
        {
            ushort[] rectData = receiver.ReceiveRectData(); // x, y, width, height
            for (ushort y = 0; y < rectData[3]; y++)
            {
                for (ushort x = 0; x < rectData[2]; x++)
                {
                    byte[] pixel = receiver.ReceivePixel();
                    SetPixel(ref screenViewBitmap, x + rectData[0], y + rectData[1], pixel[0], pixel[1], pixel[2], pixel[3]);

                    ScreenViewImage.Background = new ImageBrush()
                    {
                        Source = screenViewBitmap,
                        Stretch = Stretch.Fill
                    };
                }
            }
        }
    }

    private unsafe void SetPixel
    (
        ref Bitmap bmp, int x, int y, byte r,
        byte g, byte b, byte? a = default, int quality = 100
    )
    {
        WriteableBitmap wbmp;
        using (var ms = new MemoryStream())
        {
            bmp.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);
            wbmp = WriteableBitmap.Decode(ms);
            
            using (var lockedBitmap = wbmp.Lock())
            {
                byte* bmpPtr = (byte*)lockedBitmap.Address;

                int stride = a.HasValue ? 4 : 3;
                int offset = stride * (wbmp.PixelSize.Width * y + x);

                *(bmpPtr + offset + 0) = b;
                *(bmpPtr + offset + 1) = g;
                *(bmpPtr + offset + 2) = r;
                if (a.HasValue)
                    *(bmpPtr + offset + 3) = a.Value;
            }
        }

        using (var outStream = new MemoryStream())
        {
            wbmp.Save(outStream, quality);
            outStream.Seek(0, SeekOrigin.Begin);
            bmp = new Bitmap(outStream);
        }
    }
}