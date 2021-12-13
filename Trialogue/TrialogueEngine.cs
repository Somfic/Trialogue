using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trialogue.Ecs;
using Trialogue.Window;
using Veldrid;

namespace Trialogue
{
    public class TrialogueEngine
    {
        private readonly IHostBuilder _hostBuilder;
        private IHost _host;

        private WindowOptions _windowOptions;

        private TrialogueEngine()
        {
            _hostBuilder = Host.CreateDefaultBuilder();

            _hostBuilder.ConfigureLogging(logger =>
            {
                logger.ClearProviders();
                logger.SetMinimumLevel(LogLevel.Trace);
                logger.AddConsole();
            });
        }

        /// <summary>
        ///     Creates a new TrialogueEngine object
        /// </summary>
        /// <typeparam name="T">The implementation of the window</typeparam>
        /// <returns></returns>
        public static TrialogueEngine Create<T>() where T : Window.Window
        {
            return Create<T>(new WindowOptions());
        }

        /// <summary>
        ///     Creates a new TrialogueEngine object
        /// </summary>
        /// <param name="windowOptions">The options for the window</param>
        /// <typeparam name="T">The implementation of the window</typeparam>
        /// <returns></returns>
        public static TrialogueEngine Create<T>(WindowOptions windowOptions) where T : Window.Window
        {
            var t = new TrialogueEngine
            {
                _windowOptions = windowOptions
            };

            t.Inject<Window.Window, T>();
            t.Inject<WindowFactory>();

            return t;
        }

        /// <summary>
        ///     Injects a service into the engine
        /// </summary>
        /// <typeparam name="TService">The type of service to inject</typeparam>
        public void Inject<TService>() where TService : class
        {
            _hostBuilder.ConfigureServices(e => { e.AddSingleton<TService>(); });
        }

        /// <summary>
        ///     Injects a service into the engine
        /// </summary>
        /// <typeparam name="TService">The type of service to inject</typeparam>
        /// <typeparam name="TImplementation">The type of implementation to use</typeparam>
        public void Inject<TService, TImplementation>() where TService : class where TImplementation : class, TService
        {
            _hostBuilder.ConfigureServices(e => { e.AddSingleton<TService, TImplementation>(); });
        }

        public void Run()
        {
            // Build the host
            _host = _hostBuilder.Build();

            // Create the window
            var (nativeWindow, graphicsDevice, commandList) =
                _host.Services.GetRequiredService<WindowFactory>().Create(_windowOptions);

            // Get the window implementation
            var window = _host.Services.GetRequiredService<Window.Window>();

            window.World = new EcsWorld();
            window.Systems = new EcsSystems(window.World);
            window.ServiceProvider = _host.Services;
            window.GraphicsDevice = graphicsDevice;
            window.ResourceFactory = graphicsDevice.ResourceFactory;

            window.OnInitialise();

            var context = new Context();
            context.Window.Size = _windowOptions.Size;
            context.Window.Native = nativeWindow;
            context.Window.GraphicsDevice = graphicsDevice;
            context.Window.CommandList = commandList;
            context.Process = Process.GetCurrentProcess();

            nativeWindow.Resized += () =>
            {
                context.Window.Size.Width = nativeWindow.Width;
                context.Window.Size.Height = nativeWindow.Height;
                context.Window.GraphicsDevice.MainSwapchain.Resize((uint) nativeWindow.Width,
                    (uint) nativeWindow.Height);
            };

            window.Systems.OnStart(ref context);
            window.HasInitialised = true;

            var stopwatch = Stopwatch.StartNew();
            float lastTime = 0;
            float lastGarbageCollection = 0;
            while (nativeWindow.Exists)
            {
                var snapshot = nativeWindow.PumpEvents();
                context.Input = snapshot;

                context.Time.Total = (float) stopwatch.Elapsed.TotalSeconds;
                context.Time.Delta = context.Time.Total - lastTime;
                lastTime = context.Time.Total;

                window.OnUpdate(ref context);
                window.Systems.OnUpdate(ref context);

                commandList.Begin();
                commandList.SetFramebuffer(graphicsDevice.MainSwapchain.Framebuffer);
                commandList.ClearDepthStencil(1f);

                window.OnRender(ref context);
                window.Systems.OnRender(ref context);

                commandList.End();
                graphicsDevice.SubmitCommands(commandList);
                graphicsDevice.SwapBuffers(graphicsDevice.MainSwapchain);
                graphicsDevice.WaitForIdle();

                // Collect garbage every 10 seconds
                if (lastGarbageCollection + 10 < context.Time.Total)
                {
                    GC.Collect();
                    lastGarbageCollection = context.Time.Total;
                }
            }

            graphicsDevice.WaitForIdle();
            commandList.Dispose();
            graphicsDevice.Dispose();

            window.OnDestroy(ref context);
            window.Systems.OnDestroy(ref context);
            window.World.Destroy();
        }
    }
}