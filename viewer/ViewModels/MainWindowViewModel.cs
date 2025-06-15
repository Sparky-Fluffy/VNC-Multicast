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
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;

namespace viewer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    #region APP PARAMETERS

    private double percentWidth = 0.5;
    public double PercentWidth
    {
        get => percentWidth;
        set => percentWidth = value;
    }

    private double percentHeight = 0.7;
    public double PercentHeight
    {
        get => percentHeight;
        set => percentHeight = value;
    }

    public string AppTitleFirst => "VNC";
    public string AppTitleSecond => "Viewer";
    public int AppTitleFontSize => 0;

    #endregion

    #region PAGES PARAMETERS

    private ObservableCollection<bool> pagesVisible;
    public ObservableCollection<bool> PagesVisible
    {
        get => pagesVisible;
        set => this.RaiseAndSetIfChanged(ref pagesVisible, value);
    }

    #endregion

    #region FORM VARIABLES

    private IPAddress mcastIP;
    private ObservableCollection<string> ipParts;
    public ObservableCollection<string> IpParts
    {
        get => ipParts;
        set => this.RaiseAndSetIfChanged(ref ipParts, value);
    }

    private ushort mcastPort;
    private string mcastPortString;
    public string McastPortString
    {
        get => mcastPortString;
        set => this.RaiseAndSetIfChanged(ref mcastPortString, value);
    }

    #endregion

    #region LIST VARIABLES

    private AddressHolder selectedListItem;
    public AddressHolder SelectedListItem
    {
        get => selectedListItem;
        set => this.RaiseAndSetIfChanged(ref selectedListItem, value);
    }

    private ObservableCollection<AddressHolder> listItems;
    public ObservableCollection<AddressHolder> ListItems
    {
        get => listItems;
        set => this.RaiseAndSetIfChanged(ref listItems, value);
    }

    private ObservableCollection<bool> listButtonsVisible;
    public ObservableCollection<bool> ListButtonsVisible
    {
        get => listButtonsVisible;
        set => this.RaiseAndSetIfChanged(ref listButtonsVisible, value);
    }

    #endregion

    #region SCREENVIEW VARIABLES

    private Task? receivingTask;
    private CancellationTokenSource tokenSrc = new CancellationTokenSource();
    private CancellationToken cancelToken;

    private Bitmap? screenViewBitmap;
    public Bitmap? ScreenImage
    {
        get => screenViewBitmap;
        set => this.RaiseAndSetIfChanged(ref screenViewBitmap, value);
    }

    #endregion

    #region COMMANDS

    public ReactiveCommand<Unit, Unit> SelectSession { get; }
    public ReactiveCommand<Unit, Unit> AddSession { get; }
    public ReactiveCommand<Unit, Unit> DeleteSession { get; }

    #endregion

    private string jsonPath = @"addresses.json";

    public MainWindowViewModel()
    {
        PagesVisible = new ObservableCollection<bool> { false, false, false };
        ListButtonsVisible = new ObservableCollection<bool> { false, false, false, false };
        IpParts = new ObservableCollection<string> { "", "", "", "" };

        cancelToken = tokenSrc.Token;

        IObservable<bool> isFilledForm = this.WhenAnyValue
        (
            x => x.IpParts[0],
            x => x.IpParts[1],
            x => x.IpParts[2],
            x => x.IpParts[3],
            x => x.McastPortString,
            (ip0_s, ip1_s, ip2_s, ip3_s, port_s) =>
                byte.TryParse(ip0_s, out byte ip0) &&
                ip0 >= 224 && ip0 <= 239 &&
                byte.TryParse(ip1_s, out _) &&
                byte.TryParse(ip2_s, out _) &&
                byte.TryParse(ip3_s, out _) &&
                ushort.TryParse(port_s, out ushort port) &&
                port >= 1024
        );

        AddSession = ReactiveCommand.Create(Add, isFilledForm);

        IObservable<bool> isSelected =
            this.WhenAnyValue<MainWindowViewModel, bool, AddressHolder>
            (
                x => x.SelectedListItem,
                x => x != null
            );

        SelectSession = ReactiveCommand.Create(Select, isSelected);
        DeleteSession = ReactiveCommand.Create(Delete, isSelected);

        UpdateSessionList();
    }

    #region MAIN METHODS

    private void Add()
    {
        if (IPAddress.TryParse($"{IpParts[0]}.{IpParts[1]}.{IpParts[2]}.{IpParts[3]}", out mcastIP)
            && ushort.TryParse(McastPortString, out mcastPort))
        {
            IpParts = new ObservableCollection<string> { "", "", "", "" };
            McastPortString = "";

            JObject? mainNode = null;
            JArray? addressList = null;

            FileStream? file = null;

            if (!File.Exists(jsonPath))
            {
                file = File.Create(jsonPath);
                file?.Close();
            }

            else mainNode = TryParseJson(jsonPath);

            if (mainNode == null) mainNode = new JObject();
            else addressList = (JArray?)mainNode["addr-list"];

            if (addressList == null) addressList = new JArray();

            addressList.Add
            (
                (JObject)JToken.FromObject
                (
                    new AddressHolder
                    {
                        Ip = mcastIP.ToString(),
                        Port = mcastPort
                    }
                )
            );

            mainNode["addr-list"] = addressList;

            File.WriteAllText(jsonPath, mainNode.ToString());

            StartSession();
        }
    }

    private void Select()
    {
        mcastIP = IPAddress.Parse(selectedListItem.Ip);
        mcastPort = selectedListItem.Port;

        StartSession();
    }

    private void Delete()
    {
        if (File.Exists(jsonPath))
        {
            JObject? mainNode = null;
            JArray? addressList = null;
            mainNode = TryParseJson(jsonPath);
            addressList = (JArray?)mainNode?["addr-list"];
            if (addressList != null)
            {
                JToken t = JToken.FromObject(SelectedListItem);
                int index = -1;
                for (int i = 0; i < addressList.Count; i++)
                {
                    if ((string?)addressList[i]["Ip"] == (string)t["Ip"] &&
                        (ushort?)addressList[i]["Port"] == (ushort)t["Port"])
                    {
                        index = i;
                        break;
                    }
                }

                addressList.RemoveAt(index);
                mainNode["addr-list"] = addressList;
                File.WriteAllText(jsonPath, mainNode.ToString());
            }
        }
        UpdateSessionList();
    }

    public void OpenNewSessionForm()
    {
        SetVisible(ref listButtonsVisible, 0);
        SetPage(0);
    }

    private async void StartSession()
    {
        SetVisible(ref listButtonsVisible, -1);
        SetPage(2);

        tokenSrc = new CancellationTokenSource();
        cancelToken = tokenSrc.Token;

        receivingTask = Task.Run(ReceiveBitmap, cancelToken);
        await receivingTask;
    }

    public void CancelSession()
    {
        tokenSrc.Cancel();
        tokenSrc.Dispose();
        UpdateSessionList();
    }

    public void UpdateSessionList()
    {
        if (TryFetchAddresses())
        {
            SetVisible(ref listButtonsVisible, 0, true);
            SetPage(1);
            return;
        }

        SetVisible(ref listButtonsVisible, 1);
        SetPage(0);
        ListItems = new ObservableCollection<AddressHolder>();
    }

    public void SetPage(sbyte index)
    {
        SetVisible(ref pagesVisible, index);
    }

    private void SetVisible
    (
        ref ObservableCollection<bool> boolSheet,
        sbyte index, bool isReverse = false
    )
    {
        for (byte i = 0; i < boolSheet.Count; i++)
        {
            if (i == index)
            {
                boolSheet[i] = !isReverse;
                continue;
            }
            boolSheet[i] = isReverse;
        }
    }

    #endregion

    #region JSON METHODS

    private JObject? TryParseJson(string path)
    {
        try { return JObject.Parse(File.ReadAllText(path)); }
        catch { return null; }
    }

    private bool TryFetchAddresses()
    {
        if (!File.Exists(jsonPath)) return false;

        JObject? mainNode = null;
        mainNode = TryParseJson(jsonPath);
        if (mainNode == null) return false;

        JArray? addressList = (JArray?)mainNode["addr-list"];
        if (addressList == null) return false;

        if (addressList?.Count < 1) return false;

        ListItems = new ObservableCollection<AddressHolder>
        (
            addressList
            .Where(a => a["Ip"] != null && a["Port"] != null)
            .Select
            (
                a => new AddressHolder
                {
                    Ip = (string)a["Ip"],
                    Port = (ushort)a["Port"],
                }
            )
        );
        return true;
    }

    #endregion

    #region SESSION METHODS

    private async Task ReceiveBitmap()
    {
        try
        {
            cancelToken.ThrowIfCancellationRequested();

            Receiver receiver = new Receiver(mcastIP, mcastPort);
            cancelToken.Register(receiver.Close);

            if (cancelToken.IsCancellationRequested)
            {
                receiver.Close();
                cancelToken.ThrowIfCancellationRequested();
            }

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
                if (cancelToken.IsCancellationRequested)
                {
                    receiver.Close();
                    cancelToken.ThrowIfCancellationRequested();
                }

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

                if (cancelToken.IsCancellationRequested)
                {
                    receiver.Close();
                    cancelToken.ThrowIfCancellationRequested();
                }

                receiver.ReceivePixels();

                SetPixels(p * (y * W + x), pixelData, pixels);

                iter = H * (10 - x / receiver.rectWidth) - y;

                while (iter-- > 1)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        receiver.Close();
                        cancelToken.ThrowIfCancellationRequested();
                    }

                    receiver.ReceiveRectData();

                    if (cancelToken.IsCancellationRequested)
                    {
                        receiver.Close();
                        cancelToken.ThrowIfCancellationRequested();
                    }

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

    #endregion
}
