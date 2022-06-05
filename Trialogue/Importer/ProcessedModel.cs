using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Veldrid;

namespace Trialogue.Importer;

public class ProcessedModel
{
    public Mesh[] MeshParts { get; internal set; }
    public Node Root { get; internal set; }
    public Animation[] Animations { get; internal set; }
}

public class Mesh
{
    public Mesh(
        string name,
        byte[] vertexData,
        VertexElementDescription[] vertexElements,
        byte[] indexData,
        IndexFormat indexFormat,
        uint indexCount,
        Dictionary<string, uint> boneIDsByName,
        Matrix4x4[] boneOffsets)
    {
        Name = name;
        VertexData = vertexData;
        VertexElements = vertexElements;
        IndexData = indexData;
        IndexFormat = indexFormat;
        IndexCount = indexCount;
        BoneIDsByName = boneIDsByName;
        BoneOffsets = boneOffsets;
    }

    public string Name { get; set; }

    public byte[] VertexData { get; set; }
    public VertexElementDescription[] VertexElements { get; set; }
    public byte[] IndexData { get; set; }
    public IndexFormat IndexFormat { get; set; }
    public uint IndexCount { get; set; }
    public Dictionary<string, uint> BoneIDsByName { get; set; }
    public Matrix4x4[] BoneOffsets { get; set; }

    public ModelResources CreateDeviceResources(
        GraphicsDevice gd,
        ResourceFactory factory)
    {
        var vertexBuffer = factory.CreateBuffer(new BufferDescription(
            (uint) VertexData.Length, BufferUsage.VertexBuffer));
        gd.UpdateBuffer(vertexBuffer, 0, VertexData);

        var indexBuffer = factory.CreateBuffer(new BufferDescription(
            (uint) IndexData.Length, BufferUsage.IndexBuffer));
        gd.UpdateBuffer(indexBuffer, 0, IndexData);

        return new ModelResources(vertexBuffer, indexBuffer, IndexFormat, IndexCount);
    }
}

public class Animation
{
    public Animation(
        string name,
        double durationInTicks,
        double ticksPerSecond,
        Dictionary<string, ProcessedAnimationChannel> animationChannels)
    {
        Name = name;
        DurationInTicks = durationInTicks;
        TicksPerSecond = ticksPerSecond;
        AnimationChannels = animationChannels;
    }

    public string Name { get; set; }
    public double DurationInTicks { get; set; }
    public double TicksPerSecond { get; set; }
    public Dictionary<string, ProcessedAnimationChannel> AnimationChannels { get; set; }

    public double DurationInSeconds => DurationInTicks * TicksPerSecond;
}

public class ProcessedAnimationChannel
{
    public ProcessedAnimationChannel(string nodeName, VectorKey[] positions, VectorKey[] scales,
        QuaternionKey[] rotations)
    {
        NodeName = nodeName;
        Positions = positions;
        Scales = scales;
        Rotations = rotations;
    }

    public string NodeName { get; set; }
    public VectorKey[] Positions { get; set; }
    public VectorKey[] Scales { get; set; }
    public QuaternionKey[] Rotations { get; set; }
}

public struct VectorKey
{
    public readonly double Time;
    public readonly Vector3 Value;

    public VectorKey(double time, Vector3 value)
    {
        Time = time;
        Value = value;
    }
}

public struct QuaternionKey
{
    public readonly double Time;
    public readonly Quaternion Value;

    public QuaternionKey(double time, Quaternion value)
    {
        Time = time;
        Value = value;
    }
}

public class Node
{
    public Node(ProcessedNode[] nodes, int rootNodeIndex, Matrix4x4 rootNodeInverseTransform)
    {
        Nodes = nodes;
        RootNodeIndex = rootNodeIndex;
        RootNodeInverseTransform = rootNodeInverseTransform;
    }

    public ProcessedNode[] Nodes { get; set; }
    public int RootNodeIndex { get; set; }
    public Matrix4x4 RootNodeInverseTransform { get; set; }
}

public class ProcessedNode
{
    public ProcessedNode(string name, Matrix4x4 transform, int parentIndex, int[] childIndices)
    {
        Name = name;
        Transform = transform;
        ParentIndex = parentIndex;
        ChildIndices = childIndices;
    }

    public string Name { get; set; }
    public Matrix4x4 Transform { get; set; }
    public int ParentIndex { get; set; }
    public int[] ChildIndices { get; set; }
}

public struct ModelResources
{
    public readonly DeviceBuffer VertexBuffer;
    public readonly DeviceBuffer IndexBuffer;
    public readonly IndexFormat IndexFormat;
    public readonly uint IndexCount;

    public ModelResources(DeviceBuffer vertexBuffer, DeviceBuffer indexBuffer, IndexFormat indexFormat,
        uint indexCount)
    {
        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;
        IndexFormat = indexFormat;
        IndexCount = indexCount;
    }
}

public class ProcessedModelSerializer : BinaryAssetSerializer<ProcessedModel>
{
    public override ProcessedModel ReadT(BinaryReader reader)
    {
        var parts = reader.ReadObjectArray(ReadMeshPart);

        return new ProcessedModel
        {
            MeshParts = parts
        };
    }

    public override void WriteT(BinaryWriter writer, ProcessedModel value)
    {
        writer.WriteObjectArray(value.MeshParts, WriteMeshPart);
    }

    private void WriteMeshPart(BinaryWriter writer, Mesh part)
    {
        writer.Write(part.Name);
        writer.WriteByteArray(part.VertexData);
        writer.WriteObjectArray(part.VertexElements, WriteVertexElementDesc);
        writer.WriteByteArray(part.IndexData);
        writer.WriteEnum(part.IndexFormat);
        writer.Write(part.IndexCount);
        //writer.WriteDictionary(part.BoneIDsByName);
        writer.WriteBlittableArray(part.BoneOffsets);
    }

    private Mesh ReadMeshPart(BinaryReader reader)
    {
        var name = reader.ReadString();
        var vertexData = reader.ReadByteArray();
        var vertexDescs = reader.ReadObjectArray(ReadVertexElementDesc);
        var indexData = reader.ReadByteArray();
        var format = reader.ReadEnum<IndexFormat>();
        var indexCount = reader.ReadUInt32();
        //Dictionary<string, uint> dict = reader.ReadDictionary<string, uint>();
        var boneOffsets = reader.ReadBlittableArray<Matrix4x4>();

        return new Mesh(
            name,
            vertexData,
            vertexDescs,
            indexData,
            format,
            indexCount,
            new Dictionary<string, uint>(),
            boneOffsets);
    }


    private void WriteVertexElementDesc(BinaryWriter writer, VertexElementDescription desc)
    {
        writer.Write(desc.Name);
        writer.WriteEnum(desc.Semantic);
        writer.WriteEnum(desc.Format);
    }

    public VertexElementDescription ReadVertexElementDesc(BinaryReader reader)
    {
        var name = reader.ReadString();
        var semantic = reader.ReadEnum<VertexElementSemantic>();
        var format = reader.ReadEnum<VertexElementFormat>();
        return new VertexElementDescription(name, format, semantic);
    }
}