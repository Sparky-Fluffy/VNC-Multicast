using DynamicData;
using ReactiveUI;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace viewer.ViewModels;

public class Lang : SwitchManager<Lang, string>
{
    public string cultureID = "";
    private CultureInfo? culture = null;

    private FileSystemWatcher watcher;
    private string path = @"language/";
    public string LangPath
    {
        get => path;
        set => value = path;
    }

    private Lang()
    {
        watcher = new FileSystemWatcher(path);
        watcher.NotifyFilter = NotifyFilters.LastWrite
                             | NotifyFilters.Size
                             | NotifyFilters.LastAccess;

        watcher.Filter = "*.lang";
        watcher.Changed += OnDirectoryChanged;
        watcher.Created += OnDirectoryChanged;
        watcher.Deleted += OnDirectoryChanged;
        watcher.Renamed += OnDirectoryChanged;
        watcher.EnableRaisingEvents = true;
    }

    protected override void SetItem(int index)
    {
        if (list == null || index < 0) return;
        
        if (JsonManager.TryFetchTranslations(list.ElementAt(index), out var items))
        {
            TitleFirst = items["TitleFirst"] ?? TitleFirst;
            TitleSecond = items["TitleSecond"] ?? TitleSecond;
            PortName = items["PortName"] ?? PortName;
            AddName = items["AddName"] ?? AddName;
            SelectName = items["SelectName"] ?? SelectName;
            LightThemeName = items["LightThemeName"] ?? LightThemeName;
            DarkThemeName = items["DarkThemeName"] ?? DarkThemeName;
        }

        cultureID = Path.GetFileNameWithoutExtension(list.ElementAt(index));
        try
        {
            culture = new CultureInfo(cultureID);
            CultureName = culture.ThreeLetterISOLanguageName;
        }
        catch
        {
            culture = null;
            CultureName = cultureID;
        }
    }

    private void OnDirectoryChanged(object sender, FileSystemEventArgs e) => GetList(); 

    public override void GetList()
    {
        list = Directory.EnumerateFiles(path).ToArray();
        base.GetList();
    }

    protected override int GetIndex() => list.IndexOf(path + cultureID + ".lang");

    #region LOCALIZE STRINGS

    private string titleFirst = "VNC";
    private string titleSecond = "Viewer";
    private string portName = "Port";
    private string addName = "ADD";
    private string selectName = "SELECT";
    private string lightThemeName = "Light";
    private string darkThemeName = "Dark";
    private string cultureName = "en-US";

    public string CultureName
    {
        get => cultureName;
        set => this.RaiseAndSetIfChanged(ref cultureName, value);
    }

    public string TitleFirst
    {
        get => titleFirst;
        set => this.RaiseAndSetIfChanged(ref titleFirst, value);
    }

    public string TitleSecond
    {
        get => titleSecond;
        set => this.RaiseAndSetIfChanged(ref titleSecond, value);
    }

    public string PortName
    {
        get => portName;
        set => this.RaiseAndSetIfChanged(ref portName, value);
    }

    public string AddName
    {
        get => addName;
        set => this.RaiseAndSetIfChanged(ref addName, value);
    }

    public string SelectName
    {
        get => selectName;
        set => this.RaiseAndSetIfChanged(ref selectName, value);
    }

    public string LightThemeName
    {
        get => lightThemeName;
        set => this.RaiseAndSetIfChanged(ref lightThemeName, value);
    }

    public string DarkThemeName
    {
        get => darkThemeName;
        set => this.RaiseAndSetIfChanged(ref darkThemeName, value);
    }

    #endregion
}
