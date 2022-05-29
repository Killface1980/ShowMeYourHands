using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rimworld_Animations {
    public class AnimationStage
    {
        public string stageName;
        public int stageIndex;
        public int playTimeTicks = 0;
        public bool isLooping;
        public List<BaseAnimationClip> animationClips;


        public void initialize() {
            foreach (BaseAnimationClip clip in animationClips) {
                clip.buildSimpleCurves();
                //select playTimeTicks as longest playtime of all the animations
                if(clip.duration > playTimeTicks) {
                    playTimeTicks = clip.duration;
                }
            }
        }
    }
}
