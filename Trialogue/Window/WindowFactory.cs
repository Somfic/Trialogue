using Microsoft.Extensions.Logging;
using Trialogue.Glfw;
using Trialogue.Glfw.Enums;
using Trialogue.Glfw.Structs;
using Trialogue.OpenGl;

namespace Trialogue.Window
{
    internal class WindowFactory
    {
        private readonly ILogger<WindowFactory> _log;

        public WindowFactory(ILogger<WindowFactory> log)
        {
            _log = log;
        }
        
        public Glfw.Structs.Window Create(WindowOptions options)
        {
            _log.LogTrace("Creating window {Name}", options.Title);
            
            GLFW.Init();
            
            // OpenGL 3.3
            GLFW.WindowHint(Hint.ContextVersionMajor, 3);
            GLFW.WindowHint(Hint.ContextVersionMinor, 3);
            
            GLFW.WindowHint(Hint.OpenglProfile, Profile.Core);
            
            GLFW.WindowHint(Hint.FocusOnShow, options.FocusOnShow ? 1 : 0);
            GLFW.WindowHint(Hint.Resizable, options.Resizable ? 1 : 0);
            GLFW.WindowHint(Hint.Samples, options.SampleSize);
            GLFW.WindowHint(Hint.Floating, options.TopMost ? 1 : 0);

            var window = GLFW.CreateWindow(options.Size.Width, options.Size.Height, options.Title, Monitor.None, Glfw.Structs.Window.None);

            if (window == Glfw.Structs.Window.None)
            {
                GLFW.Terminate();
                throw new Exception("Could not create window");
            }
            
            GLFW.HideWindow(window);
            
            GLFW.MakeContextCurrent(window);
            GL.Import(GLFW.GetProcAddress);
            
            GL.Viewport(0, 0, options.Size.Width, options.Size.Height);
            GL.Enable(GL.DEPTH_TEST);
            GL.DepthMask(true);
            
            GLFW.SwapInterval(options.VSync ? 1 : 0);

            if (options.StartCentered)
            {
                var screen = GLFW.PrimaryMonitor.WorkArea;
                var x = (screen.Width - options.Size.Width) / 2;
                var y = (screen.Height - options.Size.Height) / 2;
                GLFW.SetWindowPosition(window, x, y);
            }
            else
            {
                GLFW.SetWindowPosition(window, (int) options.Position.X, (int) options.Position.Y);
            }
            
            GLFW.ShowWindow(window);

            _log.LogDebug("Created window {Name}", options.Title);
            return window;
        }
    }
}