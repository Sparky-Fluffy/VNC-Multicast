using System;
using System.Collections.Generic;
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
using Avalonia.Threading;
using Avalonia.VisualTree;
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
    private Task? receivingTask;

    private IPAddress mcastIP;
    private ushort mcastPort;

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
        if (IPAddress.TryParse($"{IpInput.Text}.{IpInput1.Text}.{IpInput2.Text}.{IpInput3.Text}", out mcastIP) && ushort.TryParse(PortInput.Text, out mcastPort))
        {
            FormPage.IsVisible = false;
            ViewPage.IsVisible = true;
            receivingTask = Task.Run(ReceiveBitmap);
            await receivingTask;
        }
    }

    private void Audio_OnClick(object? sender, RoutedEventArgs e)
    {
        
    }

    private void Cancel_OnClick(object? sender, RoutedEventArgs e)
    {
        FormPage.IsVisible = true;
        ViewPage.IsVisible = false;
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

    private void SetScreenViewImage()
    {
        ScreenViewImage.Source = screenViewBitmap;
    }

    private async Task ReceiveBitmap()
    {
        try
        {
            Receiver receiver = new Receiver(mcastIP, mcastPort);

            int W = 0;
            int H = 0;
            ref ushort x = ref receiver.rectX;
            ref ushort y = ref receiver.rectY;
            ref byte[] pixelData = ref receiver.pixelData;
            ref byte p = ref receiver.pixelFormat;

            SKBitmap bitmap;
            SKData enc;
            MemoryStream ms;
            nint pixels;

            int iter;
            int i = 1;

            while (true)
            {
                receiver.ReceiveRectData();

                W = receiver.rectWidth * 10;
                H = receiver.rectHeight * 10;
                
                bitmap = new SKBitmap
                (
                    W, H,
                    SKColorType.Bgra8888,
                    SKAlphaType.Premul
                );

                pixels = bitmap.GetPixels();

                receiver.ReceivePixels();
                SetPixels(p * (y * W + x), pixelData, pixels);

                iter = H * (10 - x / receiver.rectWidth) - y;

                while (iter-- > 1)
                {
                    receiver.ReceiveRectData();
                    receiver.ReceivePixels();
                    SetPixels(p * (y * W + x), pixelData, pixels);
                }

                using(enc = bitmap.Encode(SKEncodedImageFormat.Jpeg, 100))
                {
                    using (ms = new MemoryStream())
                    {
                        enc.SaveTo(ms);
                        ms.Position = 0;
                        screenViewBitmap = new Bitmap(ms);
                    }
                }

                Dispatcher.UIThread.Invoke(SetScreenViewImage);
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void SetPixels(int offset, byte[] pixels, nint pixelsDest)
    {
        Marshal.Copy(pixels, 0, pixelsDest + offset, pixels.Length);
    }
}