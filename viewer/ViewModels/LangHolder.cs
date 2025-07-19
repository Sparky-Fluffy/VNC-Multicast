using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

public class LangHolder : ObservableObject
{
    private string name = "", value = "";

    [JsonProperty(Required = Required.Always)]
    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    [JsonProperty(Required = Required.Always)]
    public string Value
    {
        get => value;
        set => SetProperty(ref this.value, value);
    }
}