﻿using Hypercube.Client.Graphics.Events;
using Hypercube.Client.Runtimes.Loop;
using Hypercube.Shared.Dependency;
using Hypercube.Shared.EventBus;
using Hypercube.Shared.Logging;
using Hypercube.Shared.Runtimes;
using Hypercube.Shared.Runtimes.Event;
using Hypercube.Shared.Runtimes.Loop;

namespace Hypercube.Client.Runtimes;

public sealed class Runtime : IRuntime, IEventSubscriber, IPostInject
{
    [Dependency] private readonly IEventBus _eventBus = default!;
    [Dependency] private readonly IRuntimeLoop _loop = default!;

    private readonly ILogger _logger =  LoggingManager.GetLogger("runtime");
    private bool _initialized;

    public void PostInject()
    {
        _eventBus.Subscribe<MainWindowClosedEvent>(this, OnMainWindowClosed);
    }
    
    public void Run()
    {
        Initialize();
        _logger.EngineInfo("Started");
        
        RunLoop();
        
        _logger.EngineInfo("Bye-bye, see you later");
    }
    
    private void Shutdown(string? reason = null)
    {
        // Already got shut down I assume,
        if (!_loop.Running)
            return;

        reason = reason is null ? "Shutting down" : $"Shutting down, reason: {reason}";
        
        _logger.EngineInfo(reason);
        _eventBus.Raise(new RuntimeShutdownEvent(reason));
        _loop.Shutdown();
    }
    
    private void RunLoop()
    {
        _logger.EngineInfo("Startup");
        _eventBus.Raise(new RuntimeStartupEvent());

        _loop.Run();
    }
    
    private void Initialize()
    {
        if (_initialized)
            throw new InvalidOperationException("Unable to initialize, initialized runtime"); 
        
        _logger.EngineInfo("Initialization");
        _eventBus.Raise(new RuntimeInitializationEvent());
        
        _logger.EngineInfo("Initialized");
        _initialized = true;
    }
    
    private void OnMainWindowClosed(ref MainWindowClosedEvent obj)
    {
        Shutdown("Main window closed");
    }
}