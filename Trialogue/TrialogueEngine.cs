using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trialogue.Ecs;
using Trialogue.Glfw;
using Trialogue.Systems.Rendering;
using Trialogue.Window;

namespace Trialogue
{
    public class TrialogueEngine
    {
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
        /// Creates a new TrialogueEngine object
        /// </summary>
        /// <typeparam name="T">The implementation of the window</typeparam>
        /// <returns></returns>
        public static TrialogueEngine Create<T>() where T : Window.Window
        {
            return Create<T>(new WindowOptions());
        }
        
        /// <summary>
        /// Creates a new TrialogueEngine object
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

        private WindowOptions _windowOptions;
        private readonly IHostBuilder _hostBuilder;
        private IHost _host;

        /// <summary>
        /// Injects a service into the engine
        /// </summary>
        /// <typeparam name="TService">The type of service to inject</typeparam>
        public void Inject<TService>() where TService : class
        {
            _hostBuilder.ConfigureServices(e =>
            {
                e.AddSingleton<TService>();
            });
        }
        
        /// <summary>
        /// Injects a service into the engine
        /// </summary>
        /// <typeparam name="TService">The type of service to inject</typeparam>
        /// <typeparam name="TImplementation">The type of implementation to use</typeparam>
        public void Inject<TService, TImplementation>() where TService : class where TImplementation : class, TService
        {
            _hostBuilder.ConfigureServices(e =>
            {
                e.AddSingleton<TService, TImplementation>();
            });
        }

        public void Run()
        {
            // Build the host
            _host = _hostBuilder.Build();
  
            // Create the window
            var nativeWindow = _host.Services.GetRequiredService<WindowFactory>().Create(_windowOptions);
            
            // Get the window implementation
            var window = _host.Services.GetRequiredService<Window.Window>();
            window._world = new EcsWorld();
            window._systems = new EcsSystems(window._world);
            window._serviceProvider = _host.Services;

            window.OnInitialise();
            window.CreateSystem<RenderSystem>();
            
            window._systems.OnStart();
            window._hasInitialised = true;

            var lastTime = GLFW.Time;
            while (!GLFW.WindowShouldClose(nativeWindow))
            {
                GLFW.PollEvents();
                
                var time = GLFW.Time;
                var context = new Context
                {
                    Time = time,
                    DeltaTime = lastTime - time 
                };
                lastTime = time;

                window.OnUpdate();
                window._systems.OnUpdate();
                
                window.OnRender();
                window._systems.OnRender();
                
                GLFW.SwapBuffers(nativeWindow);
            }

            window.OnDestroy();
            window._systems.OnDestroy();
            window._world.Destroy();
        }
    }
    
    public struct Context
    {
        public double DeltaTime;

        public double Time;
    }
}