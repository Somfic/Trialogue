using System;
using Microsoft.Extensions.Logging;
using Trialogue.Components;
using Trialogue.Ecs;
using Trialogue.Window;

namespace Trialogue.Systems
{
    public class CameraSystem : IEcsUpdateSystem
    {
        private readonly ILogger<CameraSystem> _log;

        private EcsFilter<Camera, Transform> _filter;
        private readonly float _bottomMaxRot = 90;

        private readonly float _mouseSensitivity = 0.1f;
        private readonly float _topMaxRot = 0;

        private float _xRotation;

        public CameraSystem(ILogger<CameraSystem> log)
        {
            _log = log;
        }

        public void OnUpdate(ref Context context)
        {
            foreach (var i in _filter)
            {
                ref var camera = ref _filter.Get1(i);
                ref var transform = ref _filter.Get2(i);

                // get mouse axis
                var mouseX = context.Input.MousePosition.X * _mouseSensitivity;
                var mouseY = context.Input.MousePosition.Y * _mouseSensitivity;

                // clamp cam rotation
                _xRotation -= mouseY;
                _xRotation = Math.Clamp(_xRotation, _topMaxRot, _bottomMaxRot);

                // rotate camera around x axis
                //transform.Rotation = Quaternion.CreateFromYawPitchRoll(mouseX / 10f, mouseY / 10f, 0f);
            }
        }
    }
}