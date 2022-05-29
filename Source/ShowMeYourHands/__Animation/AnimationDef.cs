using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Rimworld_Animations {
    public class AnimationDef : Def
    {
        public List<AnimationStage> animationStages;
        public List<Actor> actors;
        public int animationTimeTicks = 0; //do not set manually
        public bool sounds = false;
        //public List<rjw.xxx.rjwSextype> sexTypes = null;
        public List<String> interactionDefTypes = null;

        public override void PostLoad() {
            base.PostLoad();
            foreach(AnimationStage stage in animationStages) {
                stage.initialize();
                animationTimeTicks += stage.playTimeTicks;
            }
        }
    }
}
