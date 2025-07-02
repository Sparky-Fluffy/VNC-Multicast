using Avalonia.Styling;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Reactive;

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

    private string themeName;
    public string ThemeName
    {
        get => themeName;
        set => this.RaiseAndSetIfChanged(ref themeName, value);
    }

    private ThemeVariant theme;
    public ThemeVariant Theme
    {
        get => theme;
        set => this.RaiseAndSetIfChanged(ref theme, value);
    }
    public bool IsDark => theme == ThemeVariant.Dark;

    public NetIfaceManager NetIface
    {
        get => NetIfaceManager.Instance;
        set => this.RaiseAndSetIfChanged(ref NetIfaceManager.Instance, value);
    }

    public LangManager Lang
    {
        get => LangManager.Instance;
        set => this.RaiseAndSetIfChanged(ref LangManager.Instance, value);
    }

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

    private int mcastIface = 0;

    private string mcastIP;
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

    private Session mainSession = new Session();
    public Session MainSession => mainSession;

    #endregion

    #region COMMANDS

    public ReactiveCommand<Unit, Unit> SelectSession { get; }
    public ReactiveCommand<Unit, Unit> AddSession { get; }
    public ReactiveCommand<Unit, Unit> DeleteSession { get; }

    #endregion

    private bool IsAddress(string ip, ushort port)
    {
        if (IPAddress.TryParse(ip, out var ips))
        {
            byte f = ips.GetAddressBytes()[0];
            return f >= 224 && f <= 239 && port >= 1024;
        }
        return false;
    }

    public MainWindowViewModel()
    {
        GetSettings();

        PagesVisible = new ObservableCollection<bool> { false, false, false };
        ListButtonsVisible = new ObservableCollection<bool> { false, false, false, false };
        IpParts = new ObservableCollection<string> { "", "", "", "" };

        var isFilledForm = this.WhenAnyValue
        (
            x => x.IpParts[0],
            x => x.IpParts[1],
            x => x.IpParts[2],
            x => x.IpParts[3],
            x => x.McastPortString,
            (ip0_s, ip1_s, ip2_s, ip3_s, port_s) =>
                ushort.TryParse(port_s, out ushort port) &&
                IsAddress($"{ip0_s}.{ip1_s}.{ip2_s}.{ip3_s}", port)
        );

        AddSession = ReactiveCommand.Create(Add, isFilledForm);

        var isSelectedSession = this.WhenAnyValue
        (
            x => x.SelectedListItem,
            x => x != null && IsAddress(x.Ip, x.Port)
        );

        var isSelected = this.WhenAnyValue<MainWindowViewModel, bool, AddressHolder>
        (
            x => x.SelectedListItem,
            x => x != null
        );

        SelectSession = ReactiveCommand.Create(Select, isSelectedSession);
        DeleteSession = ReactiveCommand.Create(Delete, isSelected);

        UpdateSessionList();
    }

    #region PAGES METHODS

    private void Add()
    {
        mcastIP = $"{IpParts[0]}.{IpParts[1]}.{IpParts[2]}.{IpParts[3]}";
        mcastPort = ushort.Parse(McastPortString);

        IpParts = new ObservableCollection<string> { "", "", "", "" };
        McastPortString = "";

        JsonManager.Add(Paths.Address, new AddressHolder { Ip = mcastIP, Port = mcastPort });
        StartSession();
    }

    private void Select()
    {
        mcastIP = selectedListItem.Ip;
        mcastPort = selectedListItem.Port;

        StartSession();
    }

    private void Delete()
    {
        JsonManager.Delete(Paths.Address, SelectedListItem);
        UpdateSessionList();
    }

    public void OpenNewSessionForm() => SetPage(0, 0);

    public void UpdateSessionList()
    {
        if (JsonManager.TryFetchAddresses(Paths.Address, out IList<AddressHolder> items))
        {
            ListItems = new ObservableCollection<AddressHolder>(items);
            SetPage(1, 0, true);
            return;
        }

        SetPage(0, 1);
        ListItems = new ObservableCollection<AddressHolder>();
    }

    public void SetPage(sbyte index, sbyte buttonsIndex, bool reverse = false)
    {
        SetVisible(PagesVisible, index, false);
        SetVisible(ListButtonsVisible, buttonsIndex, reverse);
    }

    private void SetVisible(ObservableCollection<bool> boolSheet, sbyte index, bool reverse)
    {
        for (byte i = 0; i < boolSheet.Count; i++)
            boolSheet[i] = i == index ? !reverse : reverse;
    }

    #endregion

    #region SESSION METHODS

    private void StartSession()
    {
        mainSession.Start(mcastIP, mcastPort, mcastIface);
        SetPage(2, -1);
    }

    public void CancelSession()
    {
        mainSession.Cancel();
        UpdateSessionList();
    }

    #endregion

    #region SETTINGS METHODS

    private void GetSettings()
    {
        Lang.LangPath = Paths.Language;
        Lang.GetList();

        SettingsHolder? settings;

        JsonManager.TryFetchSettings(Paths.Settings, out settings);

        if (settings?.Theme == "Light") Theme = ThemeVariant.Light;
        else if (settings?.Theme == "Dark") Theme = ThemeVariant.Dark;
        else Theme = ThemeVariant.Dark;

        if (settings != null)
        {
            Lang.cultureID = settings.Lang;
            Lang.GetList();
            Lang.SetCurrent();
            ThemeName = IsDark ? Lang.DarkThemeName : Lang.LightThemeName;
        }
        else SwitchLang();

        if (settings!= null)
        {
            NetIface.NetIfaceName = settings.NetInterface;
            NetIface.GetList();
            NetIface.SetCurrent();
            mcastIface = NetIface.NetIface;
        }
        else SwitchNetIface();
    }

    public void SwitchLang()
    {
        Lang.SetNext();
        ThemeName = IsDark ? Lang.DarkThemeName : Lang.LightThemeName;
        SaveSettings();
    }

    public void SwitchTheme()
    {
        Theme = IsDark ? ThemeVariant.Light : ThemeVariant.Dark;
        ThemeName = IsDark ? Lang.DarkThemeName : Lang.LightThemeName;
        SaveSettings();
    }

    public void SwitchNetIface()
    {
        NetIface.GetList();
        NetIface.SetNext();
        mcastIface = NetIface.NetIface;
        SaveSettings();
    }

    public void SaveSettings()
    {
        JsonManager.SaveSettings
        (
            Paths.Settings,
            new SettingsHolder
            {
                Lang = Lang.cultureID,
                Theme = IsDark ? "Dark" : "Light",
                NetInterface = NetIface.NetIfaceName
            }
        );
    }

    #endregion
}
