using System.Drawing;
using System.Numerics;

namespace Trialogue.Windows;

public record WindowOptions
{
    public string Title { get; init; } = "Trialogue";
    
    public Size Size { get; init; } = new Size(400, 400);
    
    public Vector2 Position { get; init; } = new (10, 40);
    public bool IsResizable { get; init; } = true;
    public bool HasBorder { get; init; } = true;
    public bool IsTopMost { get; init; } = false;
    public bool UseSRGB { get; init; } = false;
    public bool StartMaximized { get; init; } = false;
    public bool StartCentered { get; init; } = false;
    public bool UseStereoscopic { get; init; } = false;
    public int MultiSamples { get; init; } = 0;
    public bool UseDoubleBuffer { get; init; } = true;
    public bool UseVerticalSync { get; init; } = true;
    public int TargetRefreshRate { get; init; } = 60;
    public bool StartCursorCentered { get; init; } = false;
    public bool StartFocused { get; init; } = true;
}