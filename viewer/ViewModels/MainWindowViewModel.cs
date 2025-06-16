using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Reactive;
using Avalonia.Media.Imaging;
using ReactiveUI;
using System.Collections.Generic;

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

    private Session mainSession;
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

        mainSession = new Session(ref screenViewBitmap);

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

            JsonManager.Add(jsonPath, mcastIP, mcastPort);
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
        JsonManager.Delete(jsonPath, SelectedListItem);
        UpdateSessionList();
    }

    public void OpenNewSessionForm()
    {
        SetVisible(ref listButtonsVisible, 0);
        SetPage(0);
    }

    private void StartSession()
    {
        mainSession.Start(mcastIP, mcastPort);
        SetVisible(ref listButtonsVisible, -1);
        SetPage(2);
    }

    public void CancelSession()
    {
        mainSession.Cancel();
        UpdateSessionList();
    }

    public void UpdateSessionList()
    {
        if (JsonManager.TryFetchAddresses(jsonPath, out IList<AddressHolder> items))
        {
            ListItems = new ObservableCollection<AddressHolder>(items);
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
}
