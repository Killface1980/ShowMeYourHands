using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rimworld_Animations {
    public abstract class Keyframe
    {
        public int tickDuration = 1;
        public float? atTick;
        public string soundEffect;
    }
}
