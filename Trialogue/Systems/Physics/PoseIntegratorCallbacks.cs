using System.Numerics;
using BepuPhysics;
using BepuUtilities;

namespace Trialogue.Systems.Physics
{
    internal struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
    {
        public Vector3 Gravity;

        public PoseIntegratorCallbacks(Vector3 gravity) : this()
        {
            Gravity = gravity;
        }

        Vector3Wide gravityWideDt;

        public void Initialize(Simulation simulation)
        {
        }

        public void PrepareForIntegration(float dt)
        {
            gravityWideDt = Vector3Wide.Broadcast(Gravity * dt);
        }

        public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation,
            BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt,
            ref BodyVelocityWide velocity)
        {
            velocity.Linear += gravityWideDt;
        }

        public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.ConserveMomentum;
        public bool AllowSubstepsForUnconstrainedBodies => true;
        public bool IntegrateVelocityForKinematics => true;
    }
}