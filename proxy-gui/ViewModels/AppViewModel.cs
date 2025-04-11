using System;

namespace proxy_gui.ViewModels;

public partial class AppViewModel : ViewModelBase
{
    public void ExitTrayItem()
    {
        Environment.Exit(0);
    }
}

