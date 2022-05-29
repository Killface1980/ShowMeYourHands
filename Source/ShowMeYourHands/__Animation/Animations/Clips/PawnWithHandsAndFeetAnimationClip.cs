using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Rimworld_Animations {
    public class PawnWithHandsAndFeetAnimationClip : BaseAnimationClip {

        public List<PawnWithHandsAndFeetKeyframe> keyframes;
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

        public SimpleCurve HandLAngle = new SimpleCurve();
        public SimpleCurve HandRAngle = new SimpleCurve();
        public SimpleCurve FootLAngle = new SimpleCurve();
        public SimpleCurve FootRAngle = new SimpleCurve();
        public SimpleCurve HandLOffsetX = new SimpleCurve();
        public SimpleCurve HandROffsetX = new SimpleCurve();
        public SimpleCurve FootLOffsetX = new SimpleCurve();
        public SimpleCurve FootROffsetX = new SimpleCurve();
        public SimpleCurve HandLOffsetZ = new SimpleCurve();
        public SimpleCurve HandROffsetZ = new SimpleCurve();
        public SimpleCurve FootLOffsetZ = new SimpleCurve();
        public SimpleCurve FootROffsetZ = new SimpleCurve();
        

        public override void buildSimpleCurves() {


            int duration = 0;
            //getting the length of the whole clip
            foreach(PawnWithHandsAndFeetKeyframe frame in keyframes) {
                duration += frame.tickDuration;
            }

            //guarantees loops don't get cut off mid-anim
            this.duration = duration;

            int keyframePosition = 0;
            foreach (PawnWithHandsAndFeetKeyframe frame in keyframes)
            {
                float frameAtTick = frame.atTick.HasValue ? frame.atTick.Value : (float)keyframePosition;

                if (frame.bodyAngle.HasValue)
                    BodyAngle.Add((float)frameAtTick / (float)duration, frame.bodyAngle.Value, true);

                if (frame.headAngle.HasValue)
                    HeadAngle.Add((float)frameAtTick / (float)duration, frame.headAngle.Value, true);

                if (frame.bodyOffsetX.HasValue)
                    BodyOffsetX.Add((float)frameAtTick / (float)duration, frame.bodyOffsetX.Value, true);

                if (frame.bodyOffsetZ.HasValue)
                    BodyOffsetZ.Add((float)frameAtTick / (float)duration, frame.bodyOffsetZ.Value, true);

                if (frame.headFacing.HasValue)
                    HeadFacing.Add((float)frameAtTick / (float)duration, frame.headFacing.Value, true);

                if (frame.bodyFacing.HasValue)
                    BodyFacing.Add((float)frameAtTick / (float)duration, frame.bodyFacing.Value, true);

                if (frame.headBob.HasValue)
                    HeadBob.Add((float)frameAtTick / (float)duration, frame.headBob.Value, true);

                if (frame.genitalAngle.HasValue)
                    GenitalAngle.Add((float)frameAtTick / (float)duration, frame.genitalAngle.Value, true);

                if (frame.handlAngle.HasValue)
                    HandLAngle.Add((float)frameAtTick / (float)duration, frame.handlAngle.Value, true);
                if (frame.handrAngle.HasValue)
                    HandRAngle.Add((float)frameAtTick / (float)duration, frame.handrAngle.Value, true);
                if (frame.footlAngle.HasValue)
                    FootLAngle.Add((float)frameAtTick / (float)duration, frame.footlAngle.Value, true);
                if (frame.footrAngle.HasValue)
                    FootRAngle.Add((float)frameAtTick / (float)duration, frame.footrAngle.Value, true);

                if (frame.handlOffsetX.HasValue)
                    HandLOffsetX.Add((float)frameAtTick / (float)duration, frame.handlOffsetX.Value, true);
                if (frame.handrOffsetX.HasValue)
                    HandROffsetX.Add((float)frameAtTick / (float)duration, frame.handrOffsetX.Value, true);
                if (frame.footlOffsetX.HasValue)
                    FootLOffsetX.Add((float)frameAtTick / (float)duration, frame.footlOffsetX.Value, true);
                if (frame.footrOffsetX.HasValue)
                    FootROffsetX.Add((float)frameAtTick / (float)duration, frame.footrOffsetX.Value, true);

                if (frame.handlOffsetZ.HasValue)
                    HandLOffsetZ.Add((float)frameAtTick / (float)duration, frame.handlOffsetZ.Value, true);
                if (frame.handrOffsetZ.HasValue)
                    HandROffsetZ.Add((float)frameAtTick / (float)duration, frame.handrOffsetZ.Value, true);
                if (frame.footlOffsetZ.HasValue)
                    FootLOffsetZ.Add((float)frameAtTick / (float)duration, frame.footlOffsetZ.Value, true);
                if (frame.footrOffsetZ.HasValue)
                    FootROffsetZ.Add((float)frameAtTick / (float)duration, frame.footrOffsetZ.Value, true);


                if (frame.soundEffect != null)
                {
                    SoundEffects.Add((int)frameAtTick, frame.soundEffect);
                }

                if (!frame.atTick.HasValue)
                {
                    if (frame.tickDuration != 1 && frame.quiver.HasValue)
                    {
                        quiver.Add(keyframePosition, true);
                        quiver.Add(keyframePosition + frame.tickDuration - 1, false);
                    }

                    keyframePosition += frame.tickDuration;
                }
                else
                {
                }
            }

        }

    }
}
