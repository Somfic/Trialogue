using System;
using System.Collections.Generic;

namespace Trialogue.OpenGl
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "InconsistentNaming")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "IdentifierTypo")]
    public static partial class GL
    {
        public static void CheckError(string title)
        {
#if DEBUG
            var error = GetError();

            if (error == NO_ERROR)
                return;

            string text = error switch
            {
                INVALID_ENUM => "Invalid enum",
                INVALID_VALUE => "Invalid value",
                INVALID_OPERATION => "Invalid operation",
                INVALID_FRAMEBUFFER_OPERATION => "Invalid framebuffer operation",
                OUT_OF_MEMORY => "Out of memory",
                _ => "Unknown error"
            };

            throw new Exception($"{title}: {text} ({error})");
#endif
        }
        
        
        private static IDictionary<uint, IDictionary<string, int>> _uniforms;
        public static int GetUniformLocationCached(uint shaderProgram, string name)
        {
            _uniforms ??= new Dictionary<uint, IDictionary<string, int>>();

            if (_uniforms.ContainsKey(shaderProgram))
            {
                var keys = _uniforms[shaderProgram];
                if (keys.ContainsKey(name))
                {
                    return keys[name];
                }
            
                var uniformLocation = GL.GetUniformLocation(shaderProgram, name);
                _uniforms[shaderProgram].Add(name, uniformLocation);
                return uniformLocation;
            }
            else
            {
                var uniformLocation = GL.GetUniformLocation(shaderProgram, name);
                var keys = new Dictionary<string, int> {{name, uniformLocation}};
                _uniforms.Add(shaderProgram, keys);
                return uniformLocation;
            }
        }
    }
}