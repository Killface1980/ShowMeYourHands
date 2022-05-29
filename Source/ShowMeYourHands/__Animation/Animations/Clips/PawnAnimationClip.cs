using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Rimworld_Animations {
    public class PawnAnimationClip : BaseAnimationClip {

        public List<PawnKeyframe> keyframes;
        public AltitudeLayer layer = AltitudeLayer.Pawn;

        public Dictionary<int, bool> quiver = new Dictionary<int, bool>();
        public SimpleCurve GenitalAngle = new SimpleCurve();
        public SimpleCurve BodyAngle = new SimpleCurve();
        public SimpleCurve HeadAngle = new SimpleCurve();
        public SimpleCurve HeadBob = new SimpleCurve();
        public SimpleCurve BodyOffsetX = new SimpleCurve();
        public SimpleCurve BodyOffsetZ = new SimpleCurve();
        public SimpleCurve HeadFacing = new SimpleCurve();
        public SimpleCurve BodyFacing = new SimpleCurve();
        

        public override void buildSimpleCurves() {


            int duration = 0;
            //getting the length of the whole clip
            foreach(PawnKeyframe frame in keyframes) {
                duration += frame.tickDuration;
            }

            //guarantees loops don't get cut off mid-anim
            this.duration = duration;

            int keyframePosition = 0;
            foreach (PawnKeyframe frame in keyframes) {

                if (frame.atTick.HasValue) {
                    if (frame.bodyAngle.HasValue)
                        BodyAngle.Add((float)frame.atTick / (float)duration, frame.bodyAngle.Value, true);

                    if (frame.headAngle.HasValue)
                        HeadAngle.Add((float)frame.atTick / (float)duration, frame.headAngle.Value, true);

                    if (frame.bodyOffsetX.HasValue)
                        BodyOffsetX.Add((float)frame.atTick / (float)duration, frame.bodyOffsetX.Value, true);

                    if (frame.bodyOffsetZ.HasValue)
                        BodyOffsetZ.Add((float)frame.atTick / (float)duration, frame.bodyOffsetZ.Value, true);

                    if (frame.headFacing.HasValue)
                        HeadFacing.Add((float)frame.atTick / (float)duration, frame.headFacing.Value, true);

                    if (frame.bodyFacing.HasValue)
                        BodyFacing.Add((float)frame.atTick / (float)duration, frame.bodyFacing.Value, true);

                    if (frame.headBob.HasValue)
                        HeadBob.Add((float)frame.atTick / (float)duration, frame.headBob.Value, true);

                    if (frame.genitalAngle.HasValue)
                        GenitalAngle.Add((float)frame.atTick / (float)duration, frame.genitalAngle.Value, true);

                    if (frame.soundEffect != null) {
                        SoundEffects.Add((int)frame.atTick, frame.soundEffect);
                    }

                    
                }
                else {
                    if (frame.bodyAngle.HasValue)
                        BodyAngle.Add((float)keyframePosition / (float)duration, frame.bodyAngle.Value, true);

                    if (frame.headAngle.HasValue)
                        HeadAngle.Add((float)keyframePosition / (float)duration, frame.headAngle.Value, true);

                    if (frame.bodyOffsetX.HasValue)
                        BodyOffsetX.Add((float)keyframePosition / (float)duration, frame.bodyOffsetX.Value, true);

                    if (frame.bodyOffsetZ.HasValue)
                        BodyOffsetZ.Add((float)keyframePosition / (float)duration, frame.bodyOffsetZ.Value, true);

                    if (frame.headFacing.HasValue)
                        HeadFacing.Add((float)keyframePosition / (float)duration, frame.headFacing.Value, true);

                    if (frame.bodyFacing.HasValue)
                        BodyFacing.Add((float)keyframePosition / (float)duration, frame.bodyFacing.Value, true);

                    if (frame.headBob.HasValue)
                        HeadBob.Add((float)keyframePosition / (float)duration, frame.headBob.Value, true);

                    if (frame.genitalAngle.HasValue)
                        GenitalAngle.Add((float)keyframePosition / (float)duration, frame.genitalAngle.Value, true);

                    if (frame.soundEffect != null) {
                        SoundEffects.Add(keyframePosition, frame.soundEffect);
                    }

                    if(frame.tickDuration != 1 && frame.quiver.HasValue) {

                        quiver.Add(keyframePosition, true);
                        quiver.Add(keyframePosition + frame.tickDuration - 1, false);
                    }
                    keyframePosition += frame.tickDuration;

                }

            }

        }

    }
}
