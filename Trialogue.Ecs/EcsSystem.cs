// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2021 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Trialogue.Ecs {
    /// <summary>
    /// Base interface for all systems.
    /// </summary>
    public interface IEcsSystem { }

    /// <summary>
    /// Interface for PreInit systems. PreInit() will be called before Init().
    /// </summary>
    public interface IEcsInitialiseSystem : IEcsSystem {
        void OnInitialise ();
    }

    /// <summary>
    /// Interface for Init systems. Init() will be called before Run().
    /// </summary>
    public interface IEcsStartSystem : IEcsSystem {
        void OnStart ();
    }

    /// <summary>
    /// Interface for PostDestroy systems. PostDestroy() will be called after Destroy().
    /// </summary>
    public interface IEcsPostDestroySystem : IEcsSystem {
        void OnPostDestroy ();
    }

    /// <summary>
    /// Interface for Destroy systems. Destroy() will be called last in system lifetime cycle.
    /// </summary>
    public interface IEcsDestroySystem : IEcsSystem {
        void OnDestroy ();
    }

    /// <summary>
    /// Interface for Update systems.
    /// </summary>
    public interface IEcsUpdateSystem : IEcsSystem {
        void OnUpdate ();
    }

#if DEBUG
    /// <summary>
    /// Debug interface for systems events processing.
    /// </summary>
    public interface IEcsSystemsDebugListener {
        void OnSystemsDestroyed (EcsSystems systems);
    }
#endif

    /// <summary>
    /// Logical group of systems.
    /// </summary>
#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    public sealed class EcsSystems : IEcsStartSystem, IEcsDestroySystem, IEcsUpdateSystem {
        public readonly string Name;
        public readonly EcsWorld World;
        readonly EcsGrowList<IEcsSystem> _allSystems = new EcsGrowList<IEcsSystem> (64);
        readonly EcsGrowList<EcsSystemsRunItem> _runSystems = new EcsGrowList<EcsSystemsRunItem> (64);
        readonly Dictionary<int, int> _namedRunSystems = new Dictionary<int, int> (64);
        int _renderSystemIndex = -1;
        readonly Dictionary<Type, object> _injections = new Dictionary<Type, object> (32);
        bool _injected;
#if DEBUG
        bool _initialized;
        bool _destroyed;
        readonly List<IEcsSystemsDebugListener> _debugListeners = new List<IEcsSystemsDebugListener> (4);

        /// <summary>
        /// Adds external event listener.
        /// </summary>
        /// <param name="listener">Event listener.</param>
        public void AddDebugListener (IEcsSystemsDebugListener listener) {
            if (listener == null) { throw new Exception ("listener is null"); }
            _debugListeners.Add (listener);
        }

        /// <summary>
        /// Removes external event listener.
        /// </summary>
        /// <param name="listener">Event listener.</param>
        public void RemoveDebugListener (IEcsSystemsDebugListener listener) {
            if (listener == null) { throw new Exception ("listener is null"); }
            _debugListeners.Remove (listener);
        }
#endif

        /// <summary>
        /// Creates new instance of EcsSystems group.
        /// </summary>
        /// <param name="world">EcsWorld instance.</param>
        /// <param name="name">Custom name for this group.</param>
        public EcsSystems (EcsWorld world, string name = null) {
            World = world;
            Name = name;
        }

        /// <summary>
        /// Adds new system to processing.
        /// </summary>
        /// <param name="system">System instance.</param>
        /// <param name="namedRunSystem">Optional name of system.</param>
        public EcsSystems Add<TSystem>(IServiceProvider services, string namedRunSystem = null) where TSystem : IEcsSystem
        {
            var system = ActivatorUtilities.CreateInstance<TSystem>(services);
            
#if DEBUG
            if (system == null) { throw new Exception ("System is null."); }
            if (_initialized) { throw new Exception ("Cant add system after initialization."); }
            if (_destroyed) { throw new Exception ("Cant touch after destroy."); }
            if (!string.IsNullOrEmpty (namedRunSystem) && !(system is IEcsUpdateSystem)) { throw new Exception ("Cant name non-IEcsRunSystem."); }
#endif
            _allSystems.Add (system);
            if (system is IEcsUpdateSystem) {
                if (namedRunSystem == null && system is EcsSystems ecsSystems) {
                    namedRunSystem = ecsSystems.Name;
                }
                if (namedRunSystem != null) {
#if DEBUG
                    if (_namedRunSystems.ContainsKey (namedRunSystem.GetHashCode ())) {
                        throw new Exception ($"Cant add named system - \"{namedRunSystem}\" name already exists.");
                    }
#endif
                    _namedRunSystems[namedRunSystem.GetHashCode ()] = _runSystems.Count;
                }

                if (system.GetType().Name.Contains("Render"))
                {
# if DEBUG
                    if (_renderSystemIndex != -1)
                    {
                        throw new Exception($"Cant add named system - \"{namedRunSystem}\", a render system is already defined");
                    }
#endif
                    _renderSystemIndex = _runSystems.Count;
                }

                _runSystems.Add (new EcsSystemsRunItem { Active = true, System = (IEcsUpdateSystem) system });
            }
            return this;
        }

        public int GetNamedRunSystem (string name) {
            return _namedRunSystems.TryGetValue (name.GetHashCode (), out var idx) ? idx : -1;
        }
        
        /// <summary>
        /// Processes injections immediately.
        /// Can be used to DI before Init() call.
        /// </summary>
        public EcsSystems ProcessInjects() {
#if DEBUG
            if (_initialized) { throw new Exception ("Cant inject after initialization"); }
            if (_destroyed) { throw new Exception ("Cant touch after destroy"); }
#endif
            if (!_injected) {
                _injected = true;
                for (int i = 0, iMax = _allSystems.Count; i < iMax; i++) {
                    if (_allSystems.Items[i] is EcsSystems nestedSystems) {
                        foreach (var pair in _injections) {
                            nestedSystems._injections[pair.Key] = pair.Value;
                        }
                        nestedSystems.ProcessInjects();
                    } else {
                        InjectDataToSystem (_allSystems.Items[i], World, _injections);
                    }
                }
            }
            return this;
        }
        
        /// <summary>
        /// Injects custom data to fields of ISystem instance.
        /// </summary>
        /// <param name="system">ISystem instance.</param>
        /// <param name="world">EcsWorld instance.</param>
        /// <param name="injections">Additional instances for injection.</param>
        public static void InjectDataToSystem (IEcsSystem system, EcsWorld world, Dictionary<Type, object> injections) {
            var systemType = system.GetType();
            var worldType = world.GetType();
            var filterType = typeof (EcsFilter);
            var ignoreType = typeof (EcsIgnoreInjectAttribute);

            foreach (var f in systemType.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                // skip statics or fields with [EcsIgnoreInject] attribute.
                if (f.IsStatic || Attribute.IsDefined (f, ignoreType)) {
                    continue;
                }
                // EcsWorld
                if (f.FieldType.IsAssignableFrom (worldType)) {
                    f.SetValue (system, world);
                    continue;
                }
                // EcsFilter
#if DEBUG
                if (f.FieldType == filterType) {
                    throw new Exception ($"Cant use EcsFilter type at \"{system}\" system for dependency injection, use generic version instead");
                }
#endif
                if (f.FieldType.IsSubclassOf (filterType)) {
                    f.SetValue (system, world.GetFilter (f.FieldType));
                    continue;
                }
                // Other injections.
                // foreach (var pair in injections) {
                //     if (f.FieldType.IsAssignableFrom (pair.Key)) {
                //         f.SetValue (system, pair.Value);
                //         break;
                //     }
                // }
            }
        }

        /// <summary>
        /// Sets IEcsRunSystem active state.
        /// </summary>
        /// <param name="idx">Index of system.</param>
        /// <param name="state">New state of system.</param>
        public void SetRunSystemState (int idx, bool state) {
#if DEBUG
            if (idx < 0 || idx >= _runSystems.Count) { throw new Exception ("Invalid index"); }
#endif
            _runSystems.Items[idx].Active = state;
        }

        /// <summary>
        /// Gets IEcsRunSystem active state.
        /// </summary>
        /// <param name="idx">Index of system.</param>
        public bool GetRunSystemState (int idx) {
#if DEBUG
            if (idx < 0 || idx >= _runSystems.Count) { throw new Exception ("Invalid index"); }
#endif
            return _runSystems.Items[idx].Active;
        }

        /// <summary>
        /// Get all systems. Important: Don't change collection!
        /// </summary>
        public EcsGrowList<IEcsSystem> GetAllSystems () {
            return _allSystems;
        }

        /// <summary>
        /// Gets all run systems. Important: Don't change collection!
        /// </summary>
        public EcsGrowList<EcsSystemsRunItem> GetRunSystems () {
            return _runSystems;
        }

        /// <summary>
        /// Closes registration for new systems, initialize all registered.
        /// </summary>
        public void OnStart () {
#if DEBUG
            if (_initialized) { throw new Exception ("Already initialized."); }
            if (_destroyed) { throw new Exception ("Cant touch after destroy."); }
#endif
            ProcessInjects();
            // IEcsPreInitSystem processing.
            for (int i = 0, iMax = _allSystems.Count; i < iMax; i++) {
                var system = _allSystems.Items[i];
                if (system is IEcsInitialiseSystem preInitSystem) {
                    preInitSystem.OnInitialise ();
#if DEBUG
                    World.CheckForLeakedEntities ($"{preInitSystem.GetType ().Name}.PreInit()");
#endif
                }
            }
            // IEcsInitSystem processing.
            for (int i = 0, iMax = _allSystems.Count; i < iMax; i++) {
                var system = _allSystems.Items[i];
                if (system is IEcsStartSystem initSystem) {
                    initSystem.OnStart ();
#if DEBUG
                    World.CheckForLeakedEntities ($"{initSystem.GetType ().Name}.Init()");
#endif
                }
            }
#if DEBUG
            _initialized = true;
#endif
        }

        /// <summary>
        /// Processes all IEcsRunSystem systems.
        /// </summary>
        public void OnUpdate() {
#if DEBUG
            if (!_initialized) { throw new Exception ($"[{Name ?? "NONAME"}] EcsSystems should be initialized before."); }
            if (_destroyed) { throw new Exception ("Cant touch after destroy."); }
#endif
            for (int i = 0, iMax = _runSystems.Count; i < iMax; i++) {
                var runItem = _runSystems.Items[i];
                if (runItem.Active) {
                    runItem.System.OnUpdate ();
                }
#if DEBUG
                if (World.CheckForLeakedEntities (null)) {
                    throw new Exception ($"Empty entity detected, possible memory leak in {_runSystems.Items[i].GetType ().Name}.Run ()");
                }
#endif
            }
        }
        
        /// <summary>
        /// Processes the render system.
        /// </summary>
        public void OnRender() {
#if DEBUG
            if (!_initialized) { throw new Exception ($"[{Name ?? "NONAME"}] EcsSystems should be initialized before"); }
            if (_destroyed) { throw new Exception ("Cant touch after destroy"); }

            if (_renderSystemIndex == -1) throw new Exception("No render system assigned");
#endif
            var system = _runSystems.Items[_renderSystemIndex];
            if (system.Active) {
                system.System.OnUpdate();
            }
#if DEBUG
            if (World.CheckForLeakedEntities (null)) {
                throw new Exception ($"Empty entity detected, possible memory leak in {system.GetType().Name}.OnUpdate()");
            }
#endif
        }

        /// <summary>
        /// Destroys registered data.
        /// </summary>
        public void OnDestroy () {
#if DEBUG
            if (_destroyed) { throw new Exception ("Already destroyed."); }
            _destroyed = true;
#endif
            // IEcsDestroySystem processing.
            for (var i = _allSystems.Count - 1; i >= 0; i--) {
                var system = _allSystems.Items[i];
                if (system is IEcsDestroySystem destroySystem) {
                    destroySystem.OnDestroy ();
#if DEBUG
                    World.CheckForLeakedEntities ($"{destroySystem.GetType ().Name}.Destroy ()");
#endif
                }
            }
            // IEcsPostDestroySystem processing.
            for (var i = _allSystems.Count - 1; i >= 0; i--) {
                var system = _allSystems.Items[i];
                if (system is IEcsPostDestroySystem postDestroySystem) {
                    postDestroySystem.OnPostDestroy ();
#if DEBUG
                    World.CheckForLeakedEntities ($"{postDestroySystem.GetType ().Name}.PostDestroy ()");
#endif
                }
            }
#if DEBUG
            for (int i = 0, iMax = _debugListeners.Count; i < iMax; i++) {
                _debugListeners[i].OnSystemsDestroyed (this);
            }
#endif
        }
    }

    /// <summary>
    /// System for removing OneFrame component.
    /// </summary>
    /// <typeparam name="T">OneFrame component type.</typeparam>
    sealed class RemoveOneFrame<T> : IEcsUpdateSystem where T : struct {
        readonly EcsFilter<T> _oneFrames = null;

        void IEcsUpdateSystem.OnUpdate () {
            for (var idx = _oneFrames.GetEntitiesCount () - 1; idx >= 0; idx--) {
                _oneFrames.GetEntity (idx).Del<T> ();
            }
        }
    }

    /// <summary>
    /// IEcsRunSystem instance with active state.
    /// </summary>
    public sealed class EcsSystemsRunItem {
        public bool Active;
        public IEcsUpdateSystem System;
    }
}