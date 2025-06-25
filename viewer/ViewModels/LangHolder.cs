using CommunityToolkit.Mvvm.ComponentModel;

public class LangHolder : ObservableObject
{
    private string name = "", value = "";

    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    public string Value
    {
        get => value;
        set => SetProperty(ref this.value, value);
    }
}