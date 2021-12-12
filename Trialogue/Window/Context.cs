using System.Diagnostics;
using System.Drawing;
using Veldrid;
using Veldrid.Sdl2;

namespace Trialogue.Window
{
    public struct Context
    {
        public WindowContext Window;

        public TimeContext Time;

        public Process Process;

        public struct WindowContext
        {
            public Size Size;

            public Sdl2Window Native;

            public GraphicsDevice GraphicsDevice;

            public CommandList CommandList;
        }

        public struct TimeContext
        {
            public float Total;

            public float Delta;
        }

        public InputSnapshot Input;
    }
}