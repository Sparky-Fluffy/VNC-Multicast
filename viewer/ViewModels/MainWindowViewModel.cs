using Avalonia.Styling;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    public Lang Lang
    {
        get => Lang.Instance;
        set => this.RaiseAndSetIfChanged(ref Lang.Instance, value);
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

    private string jsonPath = @"addresses.json";
    //private string langPath = @"language/";
    private string settingsPath = @"settings.json";

    public MainWindowViewModel()
    {
        Lang.GetLocalizations();
        
        Dictionary<string, string>? settings;

        JsonManager.TryFetchSettings(settingsPath, out settings);
            
        if (settings.TryGetValue("Theme", out var themeStr))
        {
            if (themeStr == "Light") Theme = ThemeVariant.Light;
            else if (themeStr == "Dark") Theme = ThemeVariant.Dark;
        }
        else Theme = ThemeVariant.Dark;

        if (settings.TryGetValue("Lang", out var langStr))
        {
            Lang.cultureID = langStr;
            Lang.Localize();
            ThemeName = IsDark ? Lang.DarkThemeName : Lang.LightThemeName;
        }
        else SwitchLang();

        if (settings.TryGetValue("NetInterface", out var ifaceStr))
        {
            NetIface.NetIfaceName = ifaceStr;
            NetIface.SetCurrent();
            mcastIface = NetIface.NetIface;
        }
        else SwitchNetIface();

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
                byte.TryParse(ip0_s, out byte ip0) &&
                ip0 >= 224 && ip0 <= 239 &&
                byte.TryParse(ip1_s, out _) &&
                byte.TryParse(ip2_s, out _) &&
                byte.TryParse(ip3_s, out _) &&
                ushort.TryParse(port_s, out ushort port) &&
                port >= 1024
        );

        AddSession = ReactiveCommand.Create(Add, isFilledForm);

        var isSelected = this.WhenAnyValue<MainWindowViewModel, bool, AddressHolder>
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
        IpParts = new ObservableCollection<string> { "", "", "", "" };
        McastPortString = "";

        mcastIP = $"{IpParts[0]}.{IpParts[1]}.{IpParts[2]}.{IpParts[3]}";
        mcastPort = ushort.Parse(McastPortString);

        JsonManager.Add(jsonPath, mcastIP, mcastPort);
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
        JsonManager.Delete(jsonPath, SelectedListItem);
        UpdateSessionList();
    }

    public void OpenNewSessionForm() => SetPage(0, 0);

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

    public void UpdateSessionList()
    {
        if (JsonManager.TryFetchAddresses(jsonPath, out IList<AddressHolder> items))
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

    public void SwitchLang()
    {
        Lang.LocalizeNext();
        ThemeName = IsDark ? Lang.DarkThemeName : Lang.LightThemeName;
    }

    public void SwitchTheme()
    {
        Theme = IsDark ? ThemeVariant.Light : ThemeVariant.Dark;
        ThemeName = IsDark ? Lang.DarkThemeName : Lang.LightThemeName;
    }

    public void SwitchNetIface()
    {
        NetIface.SetNext();
        mcastIface = NetIface.NetIface;
        Console.WriteLine(mcastIface);
    }

    #endregion
}
