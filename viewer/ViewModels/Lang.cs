using System;
using System.Globalization;
using System.IO;
using System.Linq;
using DynamicData;
using ReactiveUI;

namespace viewer.ViewModels;

public class Lang : ReactiveObject
{
    public string cultureID = "";
    private CultureInfo? culture = null;

    private string titleFirst = "VNC";
    private string titleSecond = "Viewer";
    private string portName = "Port";
    private string addName = "ADD";
    private string selectName = "SELECT";
    private string lightThemeName = "Light";
    private string darkThemeName = "Dark";
    private string cultureName = "en-US";

    private string[] localizations;
    private FileSystemWatcher watcher;
    private string path = @"language/";

    public static int Count
    {
        get;
        private set;
    }

    private int localizationIndex => localizations.IndexOf(path + cultureID + ".lang");

    private Lang()
    {
        watcher = new FileSystemWatcher(path);
        watcher.NotifyFilter = NotifyFilters.FileName
                            | NotifyFilters.LastWrite
                            | NotifyFilters.Size;

        watcher.Changed += OnDirectoryChanged;
        watcher.Created += OnDirectoryChanged;
        watcher.Deleted += OnDirectoryChanged;
        watcher.Renamed += OnDirectoryChanged;
    }

    private static Lang? instance;
    public static ref Lang Instance
    {
        get
        {
            if (instance == null) instance = new Lang();
            return ref instance!;
        }
    }

    private void OnDirectoryChanged(object sender, FileSystemEventArgs e) => GetLocalizations();

    public void GetLocalizations()
    {
        localizations = Directory.EnumerateFiles(path).ToArray();
        Count = localizations.Count();
    }

    public void Localize() => Localize(localizationIndex);
    public void LocalizeNext() => Localize((localizationIndex + 1) % Count);

    private void Localize(int index)
    {
        if (JsonManager.TryFetchTranslations(localizations.ElementAt(index), out var items))
        {
            TitleFirst = items["TitleFirst"] ?? TitleFirst;
            TitleSecond = items["TitleSecond"] ?? TitleSecond;
            PortName = items["PortName"] ?? PortName;
            AddName = items["AddName"] ?? AddName;
            SelectName = items["SelectName"] ?? SelectName;
            LightThemeName = items["LightThemeName"] ?? LightThemeName;
            DarkThemeName = items["DarkThemeName"] ?? DarkThemeName;
        }

        cultureID = Path.GetFileNameWithoutExtension(localizations.ElementAt(index));
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
}