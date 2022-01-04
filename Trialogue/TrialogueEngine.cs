using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trialogue.Ecs;
using Trialogue.Glfw;
using Trialogue.OpenGl;
using Trialogue.Systems.Rendering;
using Trialogue.Windows;
using Exception = Trialogue.Glfw.Exception;

namespace Trialogue;

public abstract class TrialogueEngine
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TrialogueEngine>? _log;
    private readonly WindowManager _window;
    
    private readonly EcsWorld _world;
    private readonly EcsSystems _logicSystems;
    private readonly EcsSystems _renderSystems;

    protected TrialogueEngine(IServiceProvider services)
    {
        _services = services;
        _log = services.GetService<ILogger<TrialogueEngine>>();
        _window = services.GetRequiredService<WindowManager>();
        
        _world = new EcsWorld();
        _logicSystems = new EcsSystems(_world);
        _renderSystems = new EcsSystems(_world);
    }
    
    protected void AddLogicSystem<T>() where T : IEcsSystem => _logicSystems.Add<T>(_services);
    
    protected void AddRenderSystem<T>() where T : IEcsSystem => _renderSystems.Add<T>(_services);

    protected EcsEntity CreateEntity(string name)
    {
        return _world.NewEntity(name);
    }
    
    internal void Start()
    {
        try
        {
            // todo: loading window here?

            OnSetup();
            
            _logicSystems.OnInitialise();
            _renderSystems.OnInitialise();
            
            _window.Create();

            var lastTime = _window.Time;
            uint frames = 0;
            
            // Main game loop
            while (!_window.ShouldClose())
            {
                frames++;

                var currentTime = _window.Time;
                var deltaTime = currentTime - lastTime;

                if (deltaTime > 1)
                {
                    lastTime = currentTime;
                    var tmf = deltaTime * 1000.0 / frames;
                    var fps = frames / deltaTime;
                    
                    GLFW.SetWindowTitle(_window.Native, $"{_window.Options.Title} | {tmf:00.00}ms | {fps:00}fps");
                    frames = 0;
                }

                GLFW.PollEvents();
                
                var context = BuildContext();
     
                _logicSystems.OnUpdate(ref context);

                GL.ClearColor(0.1f, 0.1f, 0.1f, 0);
                GL.Clear(GL.COLOR_BUFFER_BIT | GL.DEPTH_BUFFER_BIT);
                GL.Enable(GL.DEPTH_TEST);
                
                _renderSystems.OnUpdate(ref context);

                GLFW.SwapBuffers(_window.Native);
            }
            
            _window.Destroy();
            
            _logicSystems.OnDestroy();
            _renderSystems.OnDestroy();
            _world.Destroy();
        }
        catch (Exception ex)
        {
            _log.LogCritical(ex, "An critical error occurred that cannot be recovered from");
        }
    }
    
    private Context BuildContext()
    {
        return new Context
        {
            Time = _window.Time
        };
    }

    protected abstract void OnSetup();
}