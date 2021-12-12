using System;
using Microsoft.Extensions.Logging;
using Trialogue.Components;
using Trialogue.Ecs;
using Trialogue.Window;

public class CameraSystem : IEcsUpdateSystem
{
    private readonly ILogger<CameraSystem> _log;

    private EcsFilter<Camera, Transform> _filter;
    private readonly float bottomMaxRot = 90;

    private readonly float mouseSensitivity = 0.1f;
    private readonly float topMaxRot = 0;

    private float xRotation;

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
            var mouseX = context.Input.MousePosition.X * mouseSensitivity;
            var mouseY = context.Input.MousePosition.Y * mouseSensitivity;

            // clamp cam rotation
            xRotation -= mouseY;
            xRotation = Math.Clamp(xRotation, topMaxRot, bottomMaxRot);

            // rotate camera around x axis
            //transform.Rotation = Quaternion.CreateFromYawPitchRoll(mouseX / 10f, mouseY / 10f, 0f);
        }
    }
}