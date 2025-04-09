using System;

namespace vnc_server.ViewModels;

public partial class AppViewModel : ViewModelBase
{
    public void ExitTrayItem()
    {
        Environment.Exit(0);
    }
}