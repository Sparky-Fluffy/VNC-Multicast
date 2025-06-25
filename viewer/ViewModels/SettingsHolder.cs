using CommunityToolkit.Mvvm.ComponentModel;

public class SettingsHolder : ObservableObject
{
    private string lang = "", theme = "", netInterface = "";

    public string Lang
    {
        get => lang;
        set => SetProperty(ref lang, value);
    }

    public string Theme
    {
        get => theme;
        set => SetProperty(ref theme, value);
    }

    public string NetInterface
    {
        get => netInterface;
        set => SetProperty(ref netInterface, value);
    }
}