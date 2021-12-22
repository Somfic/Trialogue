using System;
using System.Collections.Generic;
using System.Linq;
using Veldrid;

namespace Trialogue.Systems.Rendering
{
    public class UniformSet
    {
        public readonly uint Set;
        private readonly Uniform[] _uniforms;

        public UniformSet(uint set, params Uniform[] uniforms)
        {
            Set = set;
            _uniforms = uniforms;
        }

        public ResourceSet CreateSet(ResourceFactory resourceFactory, ResourceLayout layout = null)
        {
            foreach (var uniform in _uniforms)
            {
                uniform.Initialise(resourceFactory);
            }

            layout ??= CreateLayout(resourceFactory);

            var resources = _uniforms.Select(u => u.Buffer).ToArray();
            var setDescription = new ResourceSetDescription(
                layout, resources
            );
            
            return resourceFactory.CreateResourceSet(setDescription);
        }

        public ResourceLayout CreateLayout(ResourceFactory resourceFactory)
        {
            var elementDescriptions = _uniforms.Select(u => new ResourceLayoutElementDescription(u.Name, u.Kind, u.Stages)).ToArray();
            var layoutDescription = new ResourceLayoutDescription(elementDescriptions);

            return resourceFactory.CreateResourceLayout(layoutDescription);
        }
    }

    public abstract class Uniform
    {
        internal abstract void Initialise(ResourceFactory resourceFactory);

        internal DeviceBuffer Buffer;
        internal string Name;
        internal uint Size;
        internal BufferUsage Usage;
        internal ResourceKind Kind;
        internal ShaderStages Stages;
    }

    public class Uniform<T> : Uniform where T : struct
    {
        public Uniform(string name, uint size = 64, ShaderStages stages = ShaderStages.Vertex,
            ResourceKind kind = ResourceKind.UniformBuffer, BufferUsage usage = BufferUsage.UniformBuffer)
        {
            Stages = stages;
            Kind = kind;
            Name = name;
            Size = size;
            Usage = usage;
        }

        internal override void Initialise(ResourceFactory resourceFactory)
        {
            Buffer = resourceFactory.CreateBuffer(new BufferDescription(Size, Usage));
        }

        public void Update(CommandList commandList, T value, uint byteOffset = 0)
        {
            commandList.UpdateBuffer(Buffer, byteOffset, value);
        }

        public void Update(CommandList commandList, ref T value, uint byteOffset = 0)
        {
            commandList.UpdateBuffer(Buffer, byteOffset, ref value);
        }
    }
}