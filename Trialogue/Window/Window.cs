using System;
using Trialogue.Ecs;

namespace Trialogue.Window
{
    public abstract class Window
    {
        internal EcsWorld _world;
        internal EcsSystems _systems;
        internal bool _hasInitialised;
        internal IServiceProvider _serviceProvider;
        
        public virtual void OnInitialise()
        {
            
        }

        public virtual void OnUpdate()
        {
            
        }

        public virtual void OnRender()
        {
            
        }
        
        public virtual void OnDestroy()
        {
            
        }

        public void CreateSystem<T>(string namedRunSystem = null) where T : IEcsSystem
        {
            if (_hasInitialised)
            {
                throw new Exception("Cannot add system after initialisation");
            }
            
            _systems.Add<T>(_serviceProvider, namedRunSystem);
        }

        public EcsEntity CreateEntity()
        {
            return _world.NewEntity();
        }
    }
}