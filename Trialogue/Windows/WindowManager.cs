using System;
using System.Drawing;
using Microsoft.Extensions.Logging;
using Trialogue.Glfw;
using Trialogue.Glfw.Enums;
using Trialogue.Glfw.Structs;
using Trialogue.OpenGl;
using Exception = System.Exception;

namespace Trialogue.Windows;

public class WindowManager
{
    private readonly ILogger<WindowManager> _log;
    public readonly WindowOptions Options;

    public WindowManager(ILogger<WindowManager> log, WindowOptions options)
    {
        _log = log;
        Options = options;
    }

    public double Time => GLFW.Time;
    public Size Size { get; set; }
    public Window Native { get; private set; }

    public void Create()
    {
        _log?.LogDebug("Creating window", Options.Title);

        GLFW.Init();

        // OpenGL 3.3
        GLFW.WindowHint(Hint.ContextVersionMajor, 3);
        GLFW.WindowHint(Hint.ContextVersionMinor, 3);

        // Core profile
        GLFW.WindowHint(Hint.OpenGLProfile, Profile.Core);

        // Forward compatible
        GLFW.WindowHint(Hint.OpenGLForwardCompatible, true);

        // OpenGL debug context
        GLFW.WindowHint(Hint.OpenGLDebugContext, true);

        // Options
        GLFW.WindowHint(Hint.Resizable, Options.IsResizable);
        GLFW.WindowHint(Hint.Decorated, Options.HasBorder);
        GLFW.WindowHint(Hint.Floating, Options.IsTopMost);
        GLFW.WindowHint(Hint.Maximized, Options.StartMaximized);
        GLFW.WindowHint(Hint.Stereo, Options.UseStereoscopic);
        GLFW.WindowHint(Hint.Samples, Options.MultiSamples);
        GLFW.WindowHint(Hint.SrgbCapable, Options.UseSRGB);
        GLFW.WindowHint(Hint.DoubleBuffer, Options.UseDoubleBuffer);
        GLFW.WindowHint(Hint.RefreshRate, Options.TargetRefreshRate);
        GLFW.WindowHint(Hint.CenterCursor, Options.StartCursorCentered);
        GLFW.WindowHint(Hint.FocusOnShow, Options.StartFocused);

        Native = GLFW.CreateWindow(Options.Size.Width, Options.Size.Width, Options.Title, Monitor.None, Window.None);

        // Check if the window was created
        if (Native == Window.None)
        {
            Destroy();
            throw new Exception("Could not create window");
        }

        // Hide the window while we set things up
        GLFW.HideWindow(Native);

        GLFW.MakeContextCurrent(Native);
        GL.Import(GLFW.GetProcAddress);

        GLFW.SetWindowSize(Native, Options.Size.Width, Options.Size.Height);
        GL.Viewport(0, 0, Options.Size.Width, Options.Size.Height);
        GL.Enable(GL.DEPTH_TEST);
        GL.DepthMask(true);

        GLFW.SwapInterval(Options.UseVerticalSync ? 1 : 0);

        if (Options.StartCentered)
        {
            var screen = GLFW.PrimaryMonitor.WorkArea;
            var x = (screen.Width - Options.Size.Width) / 2;
            var y = (screen.Height - Options.Size.Height) / 2;
            GLFW.SetWindowPosition(Native, x, y);
        }
        else
        {
            // Clamp the position so that the window is not outside the screen
            var screen = GLFW.PrimaryMonitor.WorkArea;
            var x = (int)Math.Max(0, Math.Min(Options.Position.X, screen.Width - Options.Size.Width));
            var y = (int)Math.Max(0, Math.Min(Options.Position.Y, screen.Height - Options.Size.Height));
            GLFW.SetWindowPosition(Native, x, y);
        }

        GLFW.ShowWindow(Native);

        Size = Options.Size;
        
        _log.LogDebug("Created window", Options.Title);
    }

    public void Destroy()
    {
        _log.LogDebug("Destroying window");
        GLFW.Terminate();
    }

    public bool ShouldClose()
    {
        return GLFW.WindowShouldClose(Native);
    }
}