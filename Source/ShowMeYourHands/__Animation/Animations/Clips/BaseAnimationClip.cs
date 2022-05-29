using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Rimworld_Animations {
    public abstract class BaseAnimationClip
    {
        public Dictionary<int, string> SoundEffects = new Dictionary<int, string>();
        public List<ThingDef> types; //types of participants
        public int duration;
        public abstract void buildSimpleCurves();
        public string soundDef = null; //for playing sounds
        public int actor;

    }
}
