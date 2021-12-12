using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Assimp;
using Veldrid;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace Trialogue.Importer
{
    public enum Shading
    {
        Flat,
        Smooth
    }

    public class AssimpProcessor : BinaryAssetProcessor<ProcessedModel>
    {
        public override unsafe ProcessedModel ProcessT(Stream stream, string extension, Shading shadingMode)
        {
            var normalsStep = shadingMode == Shading.Smooth
                ? PostProcessSteps.GenerateSmoothNormals
                : PostProcessSteps.GenerateNormals;

            var ac = new AssimpContext();
            var scene = ac.ImportFileFromStream(
                stream, normalsStep
                        | PostProcessSteps.FlipWindingOrder
                        | PostProcessSteps.FlipUVs
                        | PostProcessSteps.FixInFacingNormals
                        | PostProcessSteps.Triangulate
                        | PostProcessSteps.JoinIdenticalVertices
                        // | PostProcessSteps.OptimizeMeshes
                        // | PostProcessSteps.OptimizeGraph
                        | PostProcessSteps.SortByPrimitiveType,
                extension);
            var rootNodeInverseTransform = scene.RootNode.Transform;
            rootNodeInverseTransform.Inverse();

            var parts = new List<Mesh>();
            var animations = new List<Animation>();

            var encounteredNames = new HashSet<string>();
            for (var meshIndex = 0; meshIndex < scene.MeshCount; meshIndex++)
            {
                var mesh = scene.Meshes[meshIndex];
                var meshName = mesh.Name;
                if (string.IsNullOrEmpty(meshName)) meshName = $"mesh_{meshIndex}";
                var counter = 1;
                while (!encounteredNames.Add(meshName))
                {
                    meshName = mesh.Name + "_" + counter;
                    counter += 1;
                }

                var vertexCount = mesh.VertexCount;

                var positionOffset = 0;
                var normalOffset = 12;
                var texCoordsOffset = -1;
                var boneWeightOffset = -1;
                var boneIndicesOffset = -1;

                var elementDescs = new List<VertexElementDescription>();
                elementDescs.Add(new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float3));
                elementDescs.Add(new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float3));
                normalOffset = 12;

                var vertexSize = 24;

                var hasTexCoords = mesh.HasTextureCoords(0);
                elementDescs.Add(new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float2));
                texCoordsOffset = vertexSize;
                vertexSize += 8;

                var hasBones = mesh.HasBones;
                if (hasBones)
                {
                    elementDescs.Add(new VertexElementDescription("BoneWeights",
                        VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));
                    elementDescs.Add(new VertexElementDescription("BoneIndices",
                        VertexElementSemantic.TextureCoordinate, VertexElementFormat.UInt4));

                    boneWeightOffset = vertexSize;
                    vertexSize += 16;

                    boneIndicesOffset = vertexSize;
                    vertexSize += 16;
                }

                var vertexData = new byte[vertexCount * vertexSize];
                var builder = new VertexDataBuilder(vertexData, vertexSize);
                var min = vertexCount > 0 ? mesh.Vertices[0].ToSystemVector3() : Vector3.Zero;
                var max = vertexCount > 0 ? mesh.Vertices[0].ToSystemVector3() : Vector3.Zero;

                for (var i = 0; i < vertexCount; i++)
                {
                    var position = mesh.Vertices[i].ToSystemVector3();
                    min = Vector3.Min(min, position);
                    max = Vector3.Max(max, position);

                    builder.WriteVertexElement(
                        i,
                        positionOffset,
                        position);

                    var normal = mesh.Normals[i].ToSystemVector3();
                    builder.WriteVertexElement(i, normalOffset, normal);

                    if (mesh.HasTextureCoords(0))
                        builder.WriteVertexElement(
                            i,
                            texCoordsOffset,
                            new Vector2(mesh.TextureCoordinateChannels[0][i].X,
                                mesh.TextureCoordinateChannels[0][i].Y));
                    else
                        builder.WriteVertexElement(
                            i,
                            texCoordsOffset,
                            new Vector2());
                }

                var indices = new List<int>();
                foreach (var face in mesh.Faces)
                    if (face.IndexCount == 3)
                    {
                        indices.Add(face.Indices[0]);
                        indices.Add(face.Indices[1]);
                        indices.Add(face.Indices[2]);
                    }

                var boneIDsByName = new Dictionary<string, uint>();
                var boneOffsets = new Matrix4x4[mesh.BoneCount];

                if (hasBones)
                {
                    var assignedBoneWeights = new Dictionary<int, int>();
                    for (uint boneID = 0; boneID < mesh.BoneCount; boneID++)
                    {
                        var bone = mesh.Bones[(int) boneID];
                        var boneName = bone.Name;
                        var suffix = 1;
                        while (boneIDsByName.ContainsKey(boneName))
                        {
                            boneName = bone.Name + "_" + suffix;
                            suffix += 1;
                        }

                        boneIDsByName.Add(boneName, boneID);
                        foreach (var weight in bone.VertexWeights)
                        {
                            var relativeBoneIndex =
                                GetAndIncrementRelativeBoneIndex(assignedBoneWeights, weight.VertexID);
                            builder.WriteVertexElement(weight.VertexID,
                                boneIndicesOffset + relativeBoneIndex * sizeof(uint), boneID);
                            builder.WriteVertexElement(weight.VertexID,
                                boneWeightOffset + relativeBoneIndex * sizeof(float), weight.Weight);
                        }

                        var offsetMat = bone.OffsetMatrix.ToSystemMatrixTransposed();
                        Matrix4x4.Decompose(offsetMat, out var scale, out var rot, out var trans);
                        offsetMat = Matrix4x4.CreateScale(scale)
                                    * Matrix4x4.CreateFromQuaternion(rot)
                                    * Matrix4x4.CreateTranslation(trans);

                        boneOffsets[boneID] = offsetMat;
                    }
                }

                builder.FreeGCHandle();

                var indexCount = (uint) indices.Count;

                var int32Indices = indices.ToArray();
                var indexData = new byte[indices.Count * sizeof(uint)];
                fixed (byte* indexDataPtr = indexData)
                {
                    fixed (int* int32Ptr = int32Indices)
                    {
                        Buffer.MemoryCopy(int32Ptr, indexDataPtr, indexData.Length, indexData.Length);
                    }
                }

                var part = new Mesh(
                    meshName,
                    vertexData,
                    elementDescs.ToArray(),
                    indexData,
                    IndexFormat.UInt32,
                    (uint) indices.Count,
                    boneIDsByName,
                    boneOffsets);
                parts.Add(part);
            }

            // Nodes
            var rootNode = scene.RootNode;
            var processedNodes = new List<ProcessedNode>();
            ConvertNode(rootNode, -1, processedNodes);

            var nodes = new Node(processedNodes.ToArray(), 0, rootNodeInverseTransform.ToSystemMatrixTransposed());

            for (var animIndex = 0; animIndex < scene.AnimationCount; animIndex++)
            {
                var animation = scene.Animations[animIndex];
                var channels = new Dictionary<string, ProcessedAnimationChannel>();
                for (var channelIndex = 0; channelIndex < animation.NodeAnimationChannelCount; channelIndex++)
                {
                    var nac = animation.NodeAnimationChannels[channelIndex];
                    channels[nac.NodeName] = ConvertChannel(nac);
                }

                var baseAnimName = animation.Name;
                if (string.IsNullOrEmpty(baseAnimName)) baseAnimName = "anim_" + animIndex;

                var animationName = baseAnimName;


                var counter = 1;
                while (!encounteredNames.Add(animationName))
                {
                    animationName = baseAnimName + "_" + counter;
                    counter += 1;
                }
            }

            return new ProcessedModel
            {
                MeshParts = parts.ToArray(),
                Animations = animations.ToArray(),
                Root = nodes
            };
        }

        private int GetAndIncrementRelativeBoneIndex(Dictionary<int, int> assignedBoneWeights, int vertexID)
        {
            var currentCount = 0;
            assignedBoneWeights.TryGetValue(vertexID, out currentCount);
            assignedBoneWeights[vertexID] = currentCount + 1;
            return currentCount;
        }

        private ProcessedAnimationChannel ConvertChannel(NodeAnimationChannel nac)
        {
            var nodeName = nac.NodeName;
            var positions = new VectorKey[nac.PositionKeyCount];
            for (var i = 0; i < nac.PositionKeyCount; i++)
            {
                var assimpKey = nac.PositionKeys[i];
                positions[i] = new VectorKey(assimpKey.Time, assimpKey.Value.ToSystemVector3());
            }

            var scales = new VectorKey[nac.ScalingKeyCount];
            for (var i = 0; i < nac.ScalingKeyCount; i++)
            {
                var assimpKey = nac.ScalingKeys[i];
                scales[i] = new VectorKey(assimpKey.Time, assimpKey.Value.ToSystemVector3());
            }

            var rotations = new QuaternionKey[nac.RotationKeyCount];
            for (var i = 0; i < nac.RotationKeyCount; i++)
            {
                var assimpKey = nac.RotationKeys[i];
                rotations[i] = new QuaternionKey(assimpKey.Time, assimpKey.Value.ToSystemQuaternion());
            }

            return new ProcessedAnimationChannel(nodeName, positions, scales, rotations);
        }

        private int ConvertNode(Assimp.Node node, int parentIndex, List<ProcessedNode> processedNodes)
        {
            var currentIndex = processedNodes.Count;
            var childIndices = new int[node.ChildCount];
            var nodeTransform = node.Transform.ToSystemMatrixTransposed();
            var pn = new ProcessedNode(node.Name, nodeTransform, parentIndex, childIndices);
            processedNodes.Add(pn);

            for (var i = 0; i < childIndices.Length; i++)
            {
                var childIndex = ConvertNode(node.Children[i], currentIndex, processedNodes);
                childIndices[i] = childIndex;
            }

            return currentIndex;
        }

        private unsafe struct VertexDataBuilder
        {
            private readonly GCHandle _gch;
            private readonly byte* _dataPtr;
            private readonly int _vertexSize;

            public VertexDataBuilder(byte[] data, int vertexSize)
            {
                _gch = GCHandle.Alloc(data, GCHandleType.Pinned);
                _dataPtr = (byte*) _gch.AddrOfPinnedObject();
                _vertexSize = vertexSize;
            }

            public void WriteVertexElement<T>(int vertex, int elementOffset, ref T data)
            {
                var dst = _dataPtr + _vertexSize * vertex + elementOffset;
                Unsafe.Copy(dst, ref data);
            }

            public void WriteVertexElement<T>(int vertex, int elementOffset, T data)
            {
                var dst = _dataPtr + _vertexSize * vertex + elementOffset;
                Unsafe.Copy(dst, ref data);
            }

            public void FreeGCHandle()
            {
                _gch.Free();
            }
        }
    }
}