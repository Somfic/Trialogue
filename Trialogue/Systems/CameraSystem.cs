using System;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Trialogue.Components;
using Trialogue.Ecs;
using Trialogue.Window;

public class CameraSystem : IEcsUpdateSystem
{
    private readonly ILogger<CameraSystem> _log;

    public CameraSystem(ILogger<CameraSystem> log)
    {
        _log = log;
    }

    private EcsFilter<Camera, Transform> _filter;

    private float mouseSensitivity = 0.1f;

    private float xRotation = 0;
    private float topMaxRot = 0;
    private float bottomMaxRot = 90;
    
    public void OnUpdate(ref Context context)
    {
        foreach (var i in _filter)
        {
            ref var camera = ref _filter.Get1(i);
            ref var transform = ref _filter.Get2(i);
            
            // get mouse axis
            float mouseX = context.Input.MousePosition.X * mouseSensitivity;
            float mouseY = context.Input.MousePosition.Y * mouseSensitivity;

            // clamp cam rotation
            xRotation -= mouseY;
            xRotation = Math.Clamp(xRotation, topMaxRot, bottomMaxRot);
 
            // rotate camera around x axis
            //transform.Rotation = Quaternion.CreateFromYawPitchRoll(mouseX / 10f, mouseY / 10f, 0f);
        }
    }
}