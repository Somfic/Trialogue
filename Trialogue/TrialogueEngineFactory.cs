using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trialogue.Ecs;
using Trialogue.Glfw;
using Trialogue.Windows;
using Exception = System.Exception;

namespace Trialogue;

public class TrialogueEngineFactory 
{
    private readonly IHostBuilder _builder;

    private TrialogueEngineFactory()
    {
        _builder = Host.CreateDefaultBuilder();
    }

    public void ConfigureLogging(Action<HostBuilderContext, ILoggingBuilder> configuration) => _builder.ConfigureLogging(configuration);
    public void ConfigureLogging(Action<ILoggingBuilder> configuration) => _builder.ConfigureLogging(configuration);
    public void ConfigureServices(Action<HostBuilderContext, IServiceCollection> configuration) => _builder.ConfigureServices(configuration);
    public void ConfigureServices(Action<IServiceCollection> configuration) => _builder.ConfigureServices(configuration);
    public void ConfigureConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configuration) => _builder.ConfigureAppConfiguration(configuration);
    public void ConfigureConfiguration(Action<IConfigurationBuilder> configuration) => _builder.ConfigureAppConfiguration(configuration);
    
    public static TrialogueEngineFactory Create<T>(WindowOptions options) where T : TrialogueEngine
    {
        var factory = new TrialogueEngineFactory();
        
        factory.ConfigureServices(services =>
        {
            // Add the game
            services.AddSingleton<TrialogueEngine, T>();

            // Window manager
            services.AddSingleton<WindowManager>();
            services.AddSingleton(options);
        });

        return factory;
    }

    public void Run() =>_builder.Build().Services.GetRequiredService<TrialogueEngine>().Start();
}

public class TestSystem : IEcsSystem
{
}