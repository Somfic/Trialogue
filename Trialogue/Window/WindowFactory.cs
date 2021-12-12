using Microsoft.Extensions.Logging;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Trialogue.Window
{
    internal class WindowFactory
    {
        private readonly ILogger<WindowFactory> _log;

        public WindowFactory(ILogger<WindowFactory> log)
        {
            _log = log;
        }

        public (Sdl2Window, GraphicsDevice, CommandList) Create(WindowOptions options)
        {
            _log.LogTrace("Creating window {Name}", options.Title);

            var window = VeldridStartup.CreateWindow(new WindowCreateInfo
            {
                WindowTitle = options.Title,
                WindowWidth = options.Size.Width,
                WindowHeight = options.Size.Height
            });

            window.Resizable = options.Resizable;
            window.X = (int) options.Position.X;
            window.Y = (int) options.Position.Y;

#if DEBUG
            var isDebug = true;
#else
            bool isDebug = false;
#endif

            var graphicsDevice = VeldridStartup.CreateGraphicsDevice(window, new GraphicsDeviceOptions
            {
                Debug = isDebug,
                SyncToVerticalBlank = options.VSync,
                PreferStandardClipSpaceYDirection = true,
                PreferDepthRangeZeroToOne = true,
                SwapchainDepthFormat = PixelFormat.R16_UNorm
            }, options.Backend);

            var commandList = graphicsDevice.ResourceFactory.CreateCommandList();

            return (window, graphicsDevice, commandList);
        }
    }
}