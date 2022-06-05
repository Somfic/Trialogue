using ImGuiNET;
using Trialogue.Ecs;

namespace Trialogue.Components;

public struct ComponentInfo : IEcsComponent
{
    public string ComponentName => "Component information";

    public string EntityName;

    public void DrawUi(ref EcsEntity ecsEntity)
    {
        ImGui.InputText("Name", ref EntityName, 20);
        ecsEntity.Update(this);
    }

    public void Dispose()
    {
    }
}