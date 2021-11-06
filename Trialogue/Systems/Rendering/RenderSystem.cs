using System;
using Microsoft.Extensions.Logging;
using Trialogue.Ecs;
using Trialogue.Glfw;
using Trialogue.OpenGl;
using Exception = System.Exception;

namespace Trialogue.Systems.Rendering
{
    public class RenderSystem : IEcsInitialiseSystem, IEcsUpdateSystem
    {
        private readonly ILogger<RenderSystem> _log;
        private EcsFilter<MeshComponent, MaterialComponent> _filter;

        public RenderSystem(ILogger<RenderSystem> log)
        {
            _log = log;
        }

        public unsafe void OnInitialise()
        {
            foreach (var i in _filter)
            {
                ref var mesh = ref _filter.Get1(i);
                mesh.Vao = GL.GenVertexArray();
                mesh.Vbo = GL.GenBuffer();
                
                GL.BindVertexArray(mesh.Vao);
                
                GL.BindBuffer(GL.ARRAY_BUFFER, mesh.Vbo);
                fixed (float* v = &mesh.Vertices[0])
                {
                    GL.BufferData(GL.ARRAY_BUFFER, sizeof(float) * mesh.Vertices.Length, v, GL.STATIC_DRAW);
                }
                
                GL.VertexAttribPointer(0, 2, GL.FLOAT, false, sizeof(float) * 5, (void*) 0);
                GL.EnableVertexAttribArray(0);
                
                GL.VertexAttribPointer(1, 3, GL.FLOAT, false, sizeof(float) * 5, (void*)(sizeof(float) * 2));
                GL.EnableVertexAttribArray(1);

                GL.BindBuffer(GL.ARRAY_BUFFER, 0);
                GL.BindVertexArray(0);

                ref var material = ref _filter.Get2(i);
                
          
                material.ShaderProgram = GL.CreateProgram();
                
                for (var index = 0; index < material.Shaders.Length; index++)
                {
                    ref var shader = ref material.Shaders[index];
                    
                    shader.Id = GL.CreateShader((int) shader.Type);
                    GL.ShaderSource(shader.Id, shader.Glsl);
                    GL.CompileShader(shader.Id);
                    
                    var compileStatus = GL.GetShaderiv(shader.Id, GL.COMPILE_STATUS, 1);

                    if (compileStatus[0] == 0)
                    {
                        // Failed to compile shader
                        var error = GL.GetShaderInfoLog(shader.Id);
                        throw new Exception(error);
                    }

                    GL.AttachShader(material.ShaderProgram, shader.Id);
                }
                
                GL.LinkProgram(material.ShaderProgram);
                var status = GL.GetProgramiv(material.ShaderProgram, GL.LINK_STATUS, 1);
                if (status[0] == 0)
                {
                    // Failed to compile program
                    var error = GL.GetProgramInfoLog(material.ShaderProgram);
                    throw new Exception(error);
                }

                for (var index = 0; index < material.Shaders.Length; index++)
                {
                    ref var shader = ref material.Shaders[index];
                    
                    GL.DetachShader(material.ShaderProgram, shader.Id);
                    GL.DeleteShader(shader.Id);
                }
            }
        }
        
        public void OnUpdate()
        {
            GL.ClearColor(0, 0, 1, 0);
            GL.Clear(GL.COLOR_BUFFER_BIT | GL.DEPTH_BUFFER_BIT);
            GL.Enable(GL.DEPTH_TEST);
            
            foreach (var i in _filter)
            {
                ref var mesh = ref _filter.Get1(i);
                ref var material = ref _filter.Get2(i);

                GL.UseProgram(material.ShaderProgram);

                GL.BindVertexArray(mesh.Vao);
                GL.DrawArrays(GL.TRIANGLES, 0, 6);
                GL.BindVertexArray(0);
            } 
        }
    }
}