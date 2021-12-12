using System;
using Trialogue.Ecs;
using Veldrid;

namespace Trialogue.Window
{
    public abstract class Window
    {
        internal EcsWorld _world;
        internal EcsSystems _systems;
        internal bool _hasInitialised;
        internal IServiceProvider _serviceProvider;
        
        public GraphicsDevice GraphicsDevice { get; internal set; }
        public ResourceFactory ResourceFactory { get; internal set; }
        
        public virtual void OnInitialise()
        {
        }

        public virtual void OnUpdate(ref Context context)
        {
        }

        public virtual void OnRender(ref Context context)
        {
        }

        public virtual void OnDestroy(ref Context context)
        {
        }

        public void AddSystem<T>(string namedRunSystem = null) where T : IEcsSystem
        {
            if (_hasInitialised)
            {
                throw new Exception("Cannot add system after initialisation");
            }

            _systems.Add<T>(_serviceProvider, namedRunSystem);
        }

        public EcsEntity CreateEntity(string name)
        {
            return _world.NewEntity(name);
        }
        //
        // public (EcsEntity entity, T1) CreateEntity<T1>(string name) 
        //     where T1 : struct, IEcsComponent
        // {
        //     var entity = _world.NewEntity(name);
        //     return (entity, entity.Get<T1>());
        // }
        //
        // public (EcsEntity entity, T1, T2) CreateEntity<T1, T2>(string name)
        //     where T1 : struct, IEcsComponent 
        //     where T2 : struct, IEcsComponent
        // {
        //     var entity = _world.NewEntity(name);
        //     ref var t1 = ref entity.Get<T1>();
        //     ref var t2 = ref entity.Get<T2>();
        //
        //     return (entity, t1, t2);
        // }
        //
        // public (EcsEntity entity, T1, T2, T3) CreateEntity<T1, T2, T3>(string name) 
        //     where T1 : struct, IEcsComponent
        //     where T2 : struct, IEcsComponent
        //     where T3 : struct, IEcsComponent
        // {
        //     var entity = _world.NewEntity(name);
        //     
        //
        //     return (entity, entity.Get<T1>(), entity.Get<T2>(), entity.Get<T3>());
        // }
        //
        // public (EcsEntity, T1, T2, T3, T4) CreateEntity<T1, T2, T3, T4>(string name)
        //     where T1 : struct, IEcsComponent
        //     where T2 : struct, IEcsComponent
        //     where T3 : struct, IEcsComponent
        //     where T4 : struct, IEcsComponent
        // {
        //     var entity = _world.NewEntity(name);
        //    
        //
        //     return (entity, entity.Get<T1>(), entity.Get<T2>(), entity.Get<T3>(), entity.Get<T4>());
        // }
        //
        // public (EcsEntity entity, T1, T2, T3, T4, T5) CreateEntity<T1, T2, T3, T4, T5>(string name) 
        //     where T1 : struct, IEcsComponent
        //     where T2 : struct, IEcsComponent
        //     where T3 : struct, IEcsComponent
        //     where T4 : struct, IEcsComponent
        //     where T5 : struct, IEcsComponent
        // {
        //     var entity = _world.NewEntity(name);
        //     
        //     return (entity, entity.Get<T1>(), entity.Get<T2>(), entity.Get<T3>(), entity.Get<T4>(), entity.Get<T5>());
        // }
        //
        // public (EcsEntity entity, T1, T2, T3, T4, T5, T6) CreateEntity<T1, T2, T3, T4, T5, T6>(string name) 
        //     where T1 : struct, IEcsComponent
        //     where T2 : struct, IEcsComponent
        //     where T3 : struct, IEcsComponent
        //     where T4 : struct, IEcsComponent
        //     where T5 : struct, IEcsComponent
        //     where T6 : struct, IEcsComponent
        // {
        //     var entity = _world.NewEntity(name);
        //     
        //     return (entity, entity.Get<T1>(), entity.Get<T2>(), entity.Get<T3>(), entity.Get<T4>(), entity.Get<T5>(), entity.Get<T6>());
        // }
    }
}