using System.Drawing;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Trialogue;
using Trialogue.Windows;
using Valsom.Logging.PrettyConsole;

var sandbox = TrialogueEngineFactory.Create<Sandbox>(new WindowOptions()
{
    Position = new Vector2(int.MaxValue, 50),
    Size = new Size(958, 1008),
    IsTopMost = true,
    UseVerticalSync = true,
    TargetRefreshRate = 30,
    StartFocused = false
});

sandbox.ConfigureLogging(log =>
{
    log.ClearProviders();
    log.SetMinimumLevel(LogLevel.Trace);
    log.AddPrettyConsole();
});

sandbox.Run();