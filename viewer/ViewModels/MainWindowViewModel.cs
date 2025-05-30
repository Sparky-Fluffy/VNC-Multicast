﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace viewer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private double percentWidth = 0.5;
    private double percentHeight = 0.7;

    public string AppTitleFirst => "VNC";
    public string AppTitleSecond => "Viewer";
    public int AppTitleFontSize => 0;

    public double PercentWidth { get => percentWidth; set => percentWidth = value; }
    public double PercentHeight { get => percentHeight; set => percentHeight = value; }
}
