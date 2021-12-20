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

namespace Trialogue.Systems
{
    public class PhysicsSystem : IEcsStartSystem, IEcsUpdateSystem, IEcsDestroySystem
    {
        private ILogger<PhysicsSystem> _log;

        private Simulation _simulation;

        private EcsFilter<Transform, Body> _entities;

        public PhysicsSystem(ILogger<PhysicsSystem> log)
        {
            _log = log;
        }

        public void OnStart(ref Context context)
        {
            var bufferPool = new BufferPool();
            var narrowPhaseCallbacks = new NarrowPhaseCallbacks();
            var poseIntegratorCallbacks = new PoseIntegratorCallbacks(new Vector3(0f, -9.8f, 0f));
            var solveDescription = new SolveDescription(4);

            _simulation = Simulation.Create(bufferPool, narrowPhaseCallbacks, poseIntegratorCallbacks, solveDescription);
        }

        private IDictionary<int, BodyHandle> bodies = new Dictionary<int, BodyHandle>();

        public void OnUpdate(ref Context context)
        {
            foreach (var i in _entities)
            {
                var id = _entities.GetEntity(i).Id;
                ref var transform = ref _entities.Get1(i);
                ref var body = ref _entities.Get2(i);
                
                if (!bodies.ContainsKey(id))
                {
                    _log.LogDebug("Adding body for entity");
                    
                    var handle = _simulation.Bodies.Add(new BodyDescription
                    {
                        Pose = new RigidPose(transform.Position, transform.Rotation),
                        Collidable = _simulation.Shapes.Add(new Sphere(5)),
                        LocalInertia = new BodyInertia { InverseMass = 1f / body.Mass },
                        Velocity = new BodyVelocity(Vector3.Zero, Vector3.Zero)
                    });

                    bodies.Add(id, handle);
                }
            }
            
            _simulation.Timestep(context.Time.Total / 1000f);
            
            foreach (var i in _entities)
            {
                var id = _entities.GetEntity(i).Id;
                ref var transform = ref _entities.Get1(i);
                ref var body = ref _entities.Get2(i);

                var handle = bodies[id];
                
                _simulation.Bodies[handle].LocalInertia.InverseMass = 1f / body.Mass;

                body.Info = _simulation.Bodies[handle];
                
                transform.Position = _simulation.Bodies[handle].Pose.Position;
                transform.Rotation = _simulation.Bodies[handle].Pose.Orientation;
            }
        }

        public void OnDestroy(ref Context context)
        {
            throw new System.NotImplementedException();
        }
    }
}