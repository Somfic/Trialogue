using System;
using System.Drawing;
using Microsoft.Extensions.Logging;
using Trialogue.Ecs;
using Trialogue.Glfw;
using Trialogue.OpenGl;
using Trialogue.Windows;

namespace Trialogue.Systems.Rendering;

public class WindowSystem : IEcsUpdateSystem
{
    private readonly ILogger<WindowSystem>? _log;
    private readonly WindowManager _window;

    public WindowSystem(ILogger<WindowSystem>? log, WindowManager window)
    {
        _log = log;
        _window = window;
    }

    public void OnUpdate(ref Context context)
    {
        GLFW.GetWindowSize(_window.Native, out var width, out var height);
        
        if (width != _window.Size.Width || height != _window.Size.Height)
        {
            _window.Size = new Size(width, height);
            _log.LogInformation("Window resized to {Width}x{Height}", width, height);
            GL.Viewport(0, 0, width, height);
        }
    }
}