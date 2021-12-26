using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Microsoft.Extensions.Logging;
using Trialogue.Components;
using Trialogue.Ecs;
using Trialogue.Window;
using StaticBody = Trialogue.Components.Static;

namespace Trialogue.Systems.Physics
{
    public class PhysicsSystem : IEcsStartSystem, IEcsUpdateSystem, IEcsDestroySystem
    {
        private ILogger<PhysicsSystem> _log;

        private Simulation _simulation;

        private EcsFilter<Transform, DynamicBody> _bodies;
        private EcsFilter<Transform, StaticBody> _statics;

        public PhysicsSystem(ILogger<PhysicsSystem> log)
        {
            _log = log;
        }

        public void OnStart(ref Context context)
        {
            var bufferPool = new BufferPool();
            var narrowPhaseCallbacks = new NarrowPhaseCallbacks();
            var poseIntegratorCallbacks = new PoseIntegratorCallbacks(new Vector3(0f, -9.8f, 0f));
            var solveDescription = new SolveDescription(8);

            _threadDispatcher = new ThreadDispatcher(Environment.ProcessorCount);
            _simulation =
                Simulation.Create(bufferPool, narrowPhaseCallbacks, poseIntegratorCallbacks, solveDescription);
        }

        private IDictionary<int, DynamicBody> bodies = new Dictionary<int, DynamicBody>();
        private IDictionary<int, StaticBody> statics = new Dictionary<int, StaticBody>();
        
        private ThreadDispatcher _threadDispatcher;

        public void OnUpdate(ref Context context)
        {
            var bodyIds = new List<int>();
            var staticIds = new List<int>();

            // Add new bodies
            foreach (var i in _bodies)
            {
                var id = _bodies.GetEntity(i).Id;
                bodyIds.Add(id);

                ref var transform = ref _bodies.Get1(i);
                ref var body = ref _bodies.Get2(i);
                
                if(body.Shape == null)
                    continue;

                if (!bodies.ContainsKey(id))
                {
                    _log.LogDebug("Adding body for entity");

                    var pose = new RigidPose(transform.Position, transform.Rotation);
                    var shape = AddCollidable(body.Shape);

                    var inertia = new BodyInertia {InverseMass = 1f / body.Mass};
                    var activity = new BodyActivityDescription(10000);
                    body.Handle = _simulation.Bodies.Add(BodyDescription.CreateDynamic(pose, inertia, shape, activity));

                    bodies.Add(id, body);
                }
            }
            
            // Add new statics
            foreach (var i in _statics)
            {
                var id = _statics.GetEntity(i).Id;
                staticIds.Add(id);

                ref var transform = ref _statics.Get1(i);
                ref var staticBody = ref _statics.Get2(i);
                
                if(staticBody.Shape == null)
                    continue;

                if (!statics.ContainsKey(id))
                {
                    _log.LogDebug("Adding static for entity");

                    var pose = new RigidPose(transform.Position, transform.Rotation);
                    var shape = AddCollidable(staticBody.Shape);
                    staticBody.Handle = _simulation.Statics.Add(new StaticDescription(pose, shape));

                    statics.Add(id, staticBody);
                }
            }

            // Remove bodies
            foreach (var id in bodies.Keys.ToList().Where(id => !bodyIds.Contains(id)))
            {
                _log.LogDebug("Removing body for entity");

                _simulation.Bodies.Remove(bodies[id].Handle);
                bodies.Remove(id);
            }
            
            // Remove statics
            foreach (var id in statics.Keys.ToList().Where(id => !staticIds.Contains(id)))
            {
                _log.LogDebug("Removing static for entity");

                _simulation.Statics.Remove(statics[id].Handle);
                statics.Remove(id);
            }

            // Update bodies
            foreach (var i in _bodies)
            {
                var id = _bodies.GetEntity(i).Id;
                ref var transform = ref _bodies.Get1(i);
                ref var body = ref _bodies.Get2(i);
                
                if(body.Shape == null)
                    continue;

                _simulation.Bodies[body.Handle].LocalInertia.InverseMass = 1f / body.Mass;
                transform.Position = _simulation.Bodies[body.Handle].Pose.Position;
                transform.Rotation = _simulation.Bodies[body.Handle].Pose.Orientation;
            }
            
            // Update statics
            foreach (var i in _statics)
            {
                var id = _statics.GetEntity(i).Id;
                ref var transform = ref _statics.Get1(i);
                ref var staticBody = ref _statics.Get2(i);
                
                if(staticBody.Shape == null)
                    continue;

                _simulation.Statics[staticBody.Handle].Pose = new RigidPose(transform.Position, transform.Rotation);
            }

            // Process physics
            _simulation.Timestep(1 / 60f, _threadDispatcher);
        }

        public void OnDestroy(ref Context context)
        {
        
        }

        private TypedIndex AddCollidable(IShape shape)
        {
            return shape switch
            {
                Box box => _simulation.Shapes.Add(box),
                Capsule capsule => _simulation.Shapes.Add(capsule),
                Compound compound => _simulation.Shapes.Add(compound),
                ConvexHull convexHull => _simulation.Shapes.Add(convexHull),
                Cylinder cylinder => _simulation.Shapes.Add(cylinder),
                BigCompound bigCompound => _simulation.Shapes.Add(bigCompound),
                Mesh mesh => _simulation.Shapes.Add(mesh),
                Sphere sphere => _simulation.Shapes.Add(sphere),
                Triangle triangle => _simulation.Shapes.Add(triangle),
                _ => throw new Exception($"Unsupported shape type: {shape.GetType()}")
            };
        }
    }
}