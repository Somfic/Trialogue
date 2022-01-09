namespace Trialogue.ECS;

public struct ComponentInfo : IEcsComponent
{
    public string Name { get; set; }

    public void Dispose()
    {
        
    }
}