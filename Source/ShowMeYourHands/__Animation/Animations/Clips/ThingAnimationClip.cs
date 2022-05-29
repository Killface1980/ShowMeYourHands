using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace Rimworld_Animations {
    public class ThingAnimationClip : BaseAnimationClip
    {
        public List<ThingKeyframe> keyframes;

        public SimpleCurve PositionX = new SimpleCurve();
        public SimpleCurve PositionZ = new SimpleCurve();
        public SimpleCurve Rotation = new SimpleCurve();


        public override void buildSimpleCurves() {
            int duration = 0;
            //getting the length of the whole clip
            foreach (ThingKeyframe frame in keyframes)
            {
                duration += frame.tickDuration;
            }

            //guarantees loops don't get cut off mid-anim
            this.duration = duration;

            int keyframePosition = 0;
            foreach (ThingKeyframe frame in keyframes)
            {

                if (frame.atTick.HasValue)
                {
                    if (frame.positionX.HasValue)
                        PositionX.Add((float)frame.atTick / (float)duration, frame.positionX.Value, true);

                    if (frame.positionZ.HasValue)
                        PositionZ.Add((float)frame.atTick / (float)duration, frame.positionZ.Value, true);

                    if (frame.rotation.HasValue)
                        Rotation.Add((float)frame.atTick / (float)duration, frame.rotation.Value, true);

                    if (frame.soundEffect != null)
                    {
                        SoundEffects.Add((int)frame.atTick, frame.soundEffect);
                    }


                }
                else
                {
                    if (frame.positionX.HasValue)
                        PositionX.Add((float)keyframePosition / (float)duration, frame.positionX.Value, true);

                    if (frame.positionZ.HasValue)
                        PositionZ.Add((float)keyframePosition / (float)duration, frame.positionZ.Value, true);

                    if (frame.rotation.HasValue)
                        Rotation.Add((float)keyframePosition / (float)duration, frame.rotation.Value, true);

                    if (frame.soundEffect != null)
                    {
                        SoundEffects.Add(keyframePosition, frame.soundEffect);
                    }
                    keyframePosition += frame.tickDuration;

                }

            }
        }
    }
}
