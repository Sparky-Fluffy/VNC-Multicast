using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Net;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using ReactiveUI;
using receiver;
using SkiaSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace viewer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private double percentWidth = 0.5;
    private double percentHeight = 0.7;

    public string AppTitleFirst => "VNC";
    public string AppTitleSecond => "Viewer";
    public int AppTitleFontSize => 0;

    public double PercentWidth
    {
        get => percentWidth;
        set => percentWidth = value;
    }
    public double PercentHeight
    {
        get => percentHeight;
        set => percentHeight = value;
    }

    private ObservableCollection<string> ipParts;
    public ObservableCollection<string> IpParts
    {
        get => ipParts;
        set => this.RaiseAndSetIfChanged(ref ipParts, value);
    }
    
    private ObservableCollection<AddressHolder> listItems;
    public ObservableCollection<AddressHolder> ListItems
    {
        get => listItems;
        set => this.RaiseAndSetIfChanged(ref listItems, value);
    }

    private Bitmap? screenViewBitmap;
    public Bitmap? ScreenImage
    {
        get => screenViewBitmap;
        set => this.RaiseAndSetIfChanged(ref screenViewBitmap, value);
    }
    private Task? receivingTask;

    private IPAddress mcastIP;
    private ushort mcastPort;
    private string mcastPortString;
    public string McastPortString
    {
        get => mcastPortString;
        set => this.RaiseAndSetIfChanged(ref mcastPortString, value);
    }

    private ObservableCollection<bool> partsVisible;

    public ObservableCollection<bool> PartsVisible
    {
        get => partsVisible;
        set => this.RaiseAndSetIfChanged(ref partsVisible, value);
    }

    public MainWindowViewModel()
    {
        ListItems = FetchItems();
        IpParts = new ObservableCollection<string> { "", "", "", "" };
        PartsVisible = new ObservableCollection<bool> { true, false, false };
    }

    public ObservableCollection<AddressHolder> FetchItems()
    {
        return new ObservableCollection<AddressHolder>
        {
            new AddressHolder
            {
                Name = "teacher_main",
                Ip = "239.0.0.0",
                Port = 8001
            }
        };
    }

    public async void SelectSession()
    {
        if (IPAddress.TryParse($"{IpParts[0]}.{IpParts[1]}.{IpParts[2]}.{IpParts[3]}", out mcastIP) && ushort.TryParse(McastPortString, out mcastPort))
        {
            PartsVisible[0] = false;
            PartsVisible[1] = false;
            PartsVisible[2] = true;
            string path = @"addresses.json";
            if (!HasJson(path)) CreateJson(path);

            JObject f = JObject.Parse(File.ReadAllText(path));
            JArray addressList = new JArray();
            JObject address = (JObject)JToken.FromObject(new AddressHolder
                    { Name = "teacher_main", Ip = mcastIP.ToString(), Port = mcastPort });
            addressList.Add(address);
            f["addr-list"] = addressList; 
            File.WriteAllText(path, f.ToString());
            receivingTask = Task.Run(ReceiveBitmap);
            await receivingTask;
        }
    }

    private bool HasJson(string path) => File.Exists(path) && File.ReadAllText(path) != string.Empty;

    private void CreateJson(string path)
    {
        using (FileStream fs = File.Create(path))
        {
            Byte[] info =
                new UTF8Encoding(true).GetBytes(@"{}");
            fs.Write(info, 0, info.Length);
        }
    }

    public void CancelSession()
    {
        PartsVisible[1] = true;
        PartsVisible[2] = false;
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

                using (enc = bitmap.Encode(SKEncodedImageFormat.Jpeg, 100))
                {
                    using (ms = new MemoryStream())
                    {
                        enc.SaveTo(ms);
                        ms.Position = 0;
                        screenViewBitmap = new Bitmap(ms);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void SetPixels(int offset, byte[] pixels, nint pixelsDest)
    {
        Marshal.Copy(pixels, 0, pixelsDest + offset, pixels.Length);
    }
}
