using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using rjw;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Rimworld_Animations {
    public class CompBodyAnimator : ThingComp
    {
        public Pawn pawn => base.parent as Pawn;
        public PawnGraphicSet Graphics;

        //public CompProperties_BodyAnimator Props => (CompProperties_BodyAnimator)(object)base.props;

        public bool isAnimating {
            get {
                return Animating;
            }
            set {
                Animating = value;

                if (value == true) {
                    SexUtility.DrawNude(pawn);
                } else {
                    pawn.Drawer.renderer.graphics.ResolveAllGraphics();
                    actorsInCurrentAnimation = null;
                }

                PortraitsCache.SetDirty(pawn);
            }
        }
        private bool Animating = false;
        private bool mirror = false, quiver = false, shiver = false;
        private int actor;

        private int lastDrawFrame = -1;

        private int animTicks = 0, stageTicks = 0, clipTicks = 0;
        private int curStage = 0;
        private float clipPercent = 0;

        public Vector3 anchor = Vector3.zero, deltaPos = Vector3.zero, headBob = Vector3.zero;
        public float bodyAngle = 0, headAngle = 0, genitalAngle = 0;
        public Rot4 headFacing = Rot4.North, bodyFacing = Rot4.North;

        public List<Pawn> actorsInCurrentAnimation;

        public bool controlGenitalAngle = false;

        private AnimationDef anim;
        private AnimationStage stage {
            get
            {
                return anim.animationStages[curStage];
            }
            
        }
        private PawnAnimationClip clip => (PawnAnimationClip)stage.animationClips[actor];

        public bool Mirror {
            get {
                return mirror;
            }
        }

        public void setAnchor(IntVec3 pos)
        {
            anchor = pos.ToVector3Shifted();
        }
        public void setAnchor(Thing thing) {
            
            //center on bed
            if(thing is Building_Bed) {
                anchor = thing.Position.ToVector3();
                if (((Building_Bed)thing).SleepingSlotsCount == 2) {
                    if (thing.Rotation.AsInt == 0) {
                        anchor.x += 1;
                        anchor.z += 1;
                    }
                    else if (thing.Rotation.AsInt == 1) {
                        anchor.x += 1;
                    }
                    else if(thing.Rotation.AsInt == 3) {
                        anchor.z += 1;
                    }

                }
                else {
                    if(thing.Rotation.AsInt == 0) {
                        anchor.x += 0.5f;
                        anchor.z += 1f;
                    }
                    else if(thing.Rotation.AsInt == 1) {
                        anchor.x += 1f;
                        anchor.z += 0.5f;
                    }
                    else if(thing.Rotation.AsInt == 2) {
                        anchor.x += 0.5f;
                    } else {
                        anchor.z += 0.5f;
                    }
                }
            }
            else {
                anchor = thing.Position.ToVector3Shifted();
            }
        }
        public void StartAnimation(AnimationDef anim, List<Pawn> actors, int actor, bool mirror = false, bool shiver = false, bool fastAnimForQuickie = false) {

            actorsInCurrentAnimation = actors;

            if (anim.actors.Count <= actor)
            {
                return;
            }
            AlienRaceOffset raceOffset = anim?.actors[actor]?.raceOffsets?.Find(x => x.defName == pawn.def.defName);

            if (raceOffset != null) {
                anchor.x += mirror ? raceOffset.offset.x * -1f : raceOffset.offset.x;
                anchor.z += raceOffset.offset.y;
            }

            //change the offset based on pawn body type
            if(pawn?.story?.bodyType != null) {
                if (pawn.story.bodyType == BodyTypeDefOf.Fat && anim?.actors[actor]?.bodyTypeOffset?.Fat != null) {
                    anchor.x += anim.actors[actor].bodyTypeOffset.Fat.Value.x * (mirror ? -1f : 1f);
                    anchor.z += anim.actors[actor].bodyTypeOffset.Fat.Value.y;
                }
                else if (pawn.story.bodyType == BodyTypeDefOf.Female && anim?.actors[actor]?.bodyTypeOffset?.Female != null) {
                    anchor.x += anim.actors[actor].bodyTypeOffset.Female.Value.x * (mirror ? -1f : 1f);
                    anchor.z += anim.actors[actor].bodyTypeOffset.Female.Value.y;
                }
                else if (pawn.story.bodyType == BodyTypeDefOf.Male && anim?.actors[actor]?.bodyTypeOffset?.Male != null) {
                    anchor.x += anim.actors[actor].bodyTypeOffset.Male.Value.x * (mirror ? -1f : 1f);
                    anchor.z += anim.actors[actor].bodyTypeOffset.Male.Value.y;
                }
                else if (pawn.story.bodyType == BodyTypeDefOf.Thin && anim?.actors[actor]?.bodyTypeOffset?.Thin != null) {
                    anchor.x += anim.actors[actor].bodyTypeOffset.Thin.Value.x * (mirror ? -1f : 1f);
                    anchor.z += anim.actors[actor].bodyTypeOffset.Thin.Value.y;
                }
                else if (pawn.story.bodyType == BodyTypeDefOf.Hulk && anim?.actors[actor]?.bodyTypeOffset?.Hulk != null) {
                    anchor.x += anim.actors[actor].bodyTypeOffset.Hulk.Value.x * (mirror ? -1f : 1f);
                    anchor.z += anim.actors[actor].bodyTypeOffset.Hulk.Value.y;
                }
            }

            pawn.jobs.posture = PawnPosture.Standing;

            this.actor = actor;
            this.anim = anim;
            this.mirror = mirror;
            if(fastAnimForQuickie)
            {
                curStage = 1;
                animTicks = anim.animationStages[0].playTimeTicks;
            } else
            {
                curStage = 0;
                animTicks = 0;
            }
            
            stageTicks = 0;
            clipTicks = 0;

            quiver = false;
            this.shiver = shiver && AnimationSettings.rapeShiver;

            controlGenitalAngle = anim.actors[actor].controlGenitalAngle;

            isAnimating = true;
            //tick once for initialization
            tickAnim();

        }

        public override void CompTick() {

            base.CompTick();

            if(isAnimating) {

                GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);

                if (pawn.Dead || pawn?.jobs?.curDriver == null || (pawn?.jobs?.curDriver != null && !(pawn?.jobs?.curDriver is rjw.JobDriver_Sex))) {
                    isAnimating = false;
                }
                else {
                    tickAnim();
                }
            }
        }
        public void animatePawnBody(ref Vector3 rootLoc, ref float angle, ref Rot4 bodyFacing) {

            if(!isAnimating) {
                return;
            }
            rootLoc = anchor + deltaPos;
            angle = bodyAngle;
            bodyFacing = this.bodyFacing;

        }

        public Rot4 AnimateHeadFacing()
        {
            return this.headFacing;
        }


        public void tickGraphics(PawnGraphicSet graphics) {
            this.Graphics = graphics;
        }

        public void tickAnim() {

            

            if (!isAnimating) return;

            if (anim == null) {
                isAnimating = false;
                return;
            }

            animTicks++;
            
            if (animTicks < anim.animationTimeTicks) {
                tickStage();
            } else {

                if(LoopNeverending())
                {
                    ResetOnLoop();
                } else
                {
                    isAnimating = false;
                }
                
                
            }


            
        }

        public void tickStage()
        {

            if(stage == null)
            {
                isAnimating = false;
                return;
            }

            stageTicks++;

            if(stageTicks >= stage.playTimeTicks) {

                curStage++;

                stageTicks = 0;
                clipTicks = 0;
                clipPercent = 0;
            }

            if(curStage >= anim.animationStages.Count) {
                if (LoopNeverending())
                {
                    ResetOnLoop();
                }
                else
                {
                    isAnimating = false;
                    pawn.jobs.curDriver.ReadyForNextToil();
                }

            } else {
                tickClip();
            }

            
            
        }

        public void tickClip() {

            clipTicks++;

            //play sound effect
            if(rjw.RJWSettings.sounds_enabled && clip.SoundEffects.ContainsKey(clipTicks) && AnimationSettings.soundOverride) {
                

                SoundInfo sound = new TargetInfo(pawn.Position, pawn.Map);
                string soundEffectName = clip.SoundEffects[clipTicks];


                if ((pawn.jobs.curDriver as JobDriver_Sex).isAnimalOnAnimal)
                {
                    sound.volumeFactor *= RJWSettings.sounds_animal_on_animal_volume;
                }

                if(soundEffectName.StartsWith("Voiceline_"))
                {
                    sound.volumeFactor *= RJWSettings.sounds_voice_volume;
                }

                if (clip.SoundEffects[clipTicks] == "Cum") {

                    sound.volumeFactor *= RJWSettings.sounds_cum_volume;
                    considerApplyingSemen();
                        
                } else
                {
                    sound.volumeFactor *= RJWSettings.sounds_sex_volume;
                }

                SoundDef.Named(soundEffectName).PlayOneShot(sound);

            }
            if(AnimationSettings.orgasmQuiver && clip.quiver.ContainsKey(clipTicks)) {
                quiver = clip.quiver[clipTicks];
            }
            
            //loop animation if possible
            if (clipPercent >= 1 && stage.isLooping) {
                clipTicks = 1;//warning: don't set to zero or else calculations go wrong
            }
            clipPercent = (float)clipTicks / (float)clip.duration;

            calculateDrawValues();
        }

        public void considerApplyingSemen()
        {
            if(AnimationSettings.applySemenOnAnimationOrgasm && (pawn?.jobs?.curDriver is JobDriver_Sex))
            {

                if (anim.sexTypes.Contains((pawn.jobs.curDriver as JobDriver_Sex).Sexprops.sexType))
                {
                    SemenHelper.calculateAndApplySemen((pawn.jobs.curDriver as JobDriver_Sex).Sexprops);
                }
            }
        }

        public void calculateDrawValues() {

            /*if(Find.TickManager.TickRateMultiplier > 1 && (lastDrawFrame + 1 >= RealTime.frameCount || RealTime.deltaTime < 0.05f)) {
                return;
            }*/

            deltaPos = new Vector3(clip.BodyOffsetX.Evaluate(clipPercent) * (mirror ? -1 : 1), clip.layer.AltitudeFor(), clip.BodyOffsetZ.Evaluate(clipPercent));

            string bodyTypeDef = (pawn.story?.bodyType != null) ? pawn.story.bodyType.ToString() : "";

            if (AnimationSettings.offsets != null && AnimationSettings.offsets.ContainsKey(CurrentAnimation.defName + pawn.def.defName + bodyTypeDef + ActorIndex)) {
                deltaPos.x += AnimationSettings.offsets[CurrentAnimation.defName + pawn.def.defName + bodyTypeDef + ActorIndex].x * (mirror ? -1 : 1);
                deltaPos.z += AnimationSettings.offsets[CurrentAnimation.defName + pawn.def.defName + bodyTypeDef + ActorIndex].y;
            }


            bodyAngle = (clip.BodyAngle.Evaluate(clipPercent) + (quiver || shiver ? ((Rand.Value * AnimationSettings.shiverIntensity) - (AnimationSettings.shiverIntensity / 2f)) : 0f)) * (mirror ? -1 : 1);
            headAngle = clip.HeadAngle.Evaluate(clipPercent) * (mirror ? -1 : 1);

            if (controlGenitalAngle) {
                genitalAngle = clip.GenitalAngle.Evaluate(clipPercent) * (mirror ? -1 : 1);
            }

            if (AnimationSettings.rotation != null && AnimationSettings.rotation.ContainsKey(CurrentAnimation.defName + pawn.def.defName + bodyTypeDef + ActorIndex)) {
                float offsetRotation = AnimationSettings.rotation[CurrentAnimation.defName + pawn.def.defName + bodyTypeDef + ActorIndex] * (Mirror ? -1 : 1);
                genitalAngle += offsetRotation;
                bodyAngle += offsetRotation;
                headAngle += offsetRotation;
            }


            //don't go past 360 or less than 0
            
            if (bodyAngle < 0) bodyAngle = 360 - ((-1f*bodyAngle) % 360);
            if (bodyAngle > 360) bodyAngle %= 360;

            
            if (headAngle < 0) headAngle = 360 - ((-1f * headAngle) % 360);
            if (headAngle > 360) headAngle %= 360;

            if (genitalAngle < 0) genitalAngle = 360 - ((-1f * genitalAngle) % 360);
            if (genitalAngle > 360) genitalAngle %= 360; 


            bodyFacing = mirror ? new Rot4((int)clip.BodyFacing.Evaluate(clipPercent)).Opposite : new Rot4((int)clip.BodyFacing.Evaluate(clipPercent));

            bodyFacing = new Rot4((int)clip.BodyFacing.Evaluate(clipPercent));
            if(bodyFacing.IsHorizontal && mirror) {
                bodyFacing = bodyFacing.Opposite;
            }

            headFacing = new Rot4((int)clip.HeadFacing.Evaluate(clipPercent));
            if(headFacing.IsHorizontal && mirror) {
                headFacing = headFacing.Opposite;
            }
            headBob = new Vector3(0, 0, clip.HeadBob.Evaluate(clipPercent));

            lastDrawFrame = RealTime.frameCount;

        }

        public Vector3 getPawnHeadPosition() {

            Vector3 headPos = anchor + deltaPos + Quaternion.AngleAxis(bodyAngle, Vector3.up) * (pawn.Drawer.renderer.BaseHeadOffsetAt(headFacing) + headBob);

            return headPos;

        }

        public Vector3 getPawnHeadOffset()
        {
            return Quaternion.AngleAxis(bodyAngle, Vector3.up) * (pawn.Drawer.renderer.BaseHeadOffsetAt(headFacing) + headBob);
            
        }

        public AnimationDef CurrentAnimation {
            get {
                return anim;
            }
        }

        public int ActorIndex {
            get {
                return actor;
            }
        }

        public override void PostExposeData() {
            base.PostExposeData();
            
            Scribe_Defs.Look(ref anim, "RJWAnimations-Anim");

            Scribe_Values.Look(ref animTicks, "RJWAnimations-animTicks", 1);
            Scribe_Values.Look(ref stageTicks, "RJWAnimations-stageTicks", 1);
            Scribe_Values.Look(ref clipTicks, "RJWAnimations-clipTicks", 1);
            Scribe_Values.Look(ref clipPercent, "RJWAnimations-clipPercent", 1);
            Scribe_Values.Look(ref mirror, "RJWAnimations-mirror");

            Scribe_Values.Look(ref curStage, "RJWAnimations-curStage", 0);
            Scribe_Values.Look(ref actor, "RJWAnimations-actor");

            Scribe_Values.Look(ref anchor, "RJWAnimations-anchor");
            Scribe_Values.Look(ref deltaPos, "RJWAnimations-deltaPos");
            Scribe_Values.Look(ref headBob, "RJWAnimations-headBob");
            Scribe_Values.Look(ref bodyAngle, "RJWAnimations-bodyAngle");
            Scribe_Values.Look(ref headAngle, "RJWAnimations-headAngle");

            Scribe_Values.Look(ref genitalAngle, "RJWAnimations-GenitalAngle");
            Scribe_Values.Look(ref controlGenitalAngle, "RJWAnimations-controlGenitalAngle");

            Scribe_Values.Look(ref headFacing, "RJWAnimations-headFacing");
            Scribe_Values.Look(ref headFacing, "RJWAnimations-bodyFacing");

            Scribe_Values.Look(ref quiver, "RJWAnimations-orgasmQuiver");                             
        }

        public void shiftActorPositionAndRestartAnimation() {
            actor = (actor == anim.actors.Count - 1 ? 0 : actor + 1);

            if (pawn?.story?.bodyType != null) {
                if (pawn.story.bodyType == BodyTypeDefOf.Fat && anim?.actors[actor]?.bodyTypeOffset?.Fat != null) {
                    anchor.x += anim.actors[actor].bodyTypeOffset.Fat.Value.x * (mirror ? -1f : 1f);
                    anchor.z += anim.actors[actor].bodyTypeOffset.Fat.Value.y;
                }
                else if (pawn.story.bodyType == BodyTypeDefOf.Female && anim?.actors[actor]?.bodyTypeOffset?.Female != null) {
                    anchor.x += anim.actors[actor].bodyTypeOffset.Female.Value.x * (mirror ? -1f : 1f);
                    anchor.z += anim.actors[actor].bodyTypeOffset.Female.Value.y;
                }
                else if (pawn.story.bodyType == BodyTypeDefOf.Male && anim?.actors[actor]?.bodyTypeOffset?.Male != null) {
                    anchor.x += anim.actors[actor].bodyTypeOffset.Male.Value.x * (mirror ? -1f : 1f);
                    anchor.z += anim.actors[actor].bodyTypeOffset.Male.Value.y;
                }
                else if (pawn.story.bodyType == BodyTypeDefOf.Thin && anim?.actors[actor]?.bodyTypeOffset?.Thin != null) {
                    anchor.x += anim.actors[actor].bodyTypeOffset.Thin.Value.x * (mirror ? -1f : 1f);
                    anchor.z += anim.actors[actor].bodyTypeOffset.Thin.Value.y;
                }
                else if (pawn.story.bodyType == BodyTypeDefOf.Hulk && anim?.actors[actor]?.bodyTypeOffset?.Hulk != null) {
                    anchor.x += anim.actors[actor].bodyTypeOffset.Hulk.Value.x * (mirror ? -1f : 1f);
                    anchor.z += anim.actors[actor].bodyTypeOffset.Hulk.Value.y;
                }
            }

            curStage = 0;
            animTicks = 0;
            stageTicks = 0;
            clipTicks = 0;

            controlGenitalAngle = anim.actors[actor].controlGenitalAngle;

            tickAnim();
        }

        public bool LoopNeverending()
        {
            if(pawn?.jobs?.curDriver != null && 
                (pawn.jobs.curDriver is JobDriver_Sex) && (pawn.jobs.curDriver as JobDriver_Sex).neverendingsex ||
                (pawn.jobs.curDriver is JobDriver_SexBaseReciever) && (pawn.jobs.curDriver as JobDriver_Sex).Partner?.jobs?.curDriver != null && ((pawn.jobs.curDriver as JobDriver_Sex).Partner.jobs.curDriver as JobDriver_Sex).neverendingsex)
            {
                return true;
            }

            return false;
        }

        public void ResetOnLoop()
        {
            curStage = 1;
            animTicks = 0;
            stageTicks = 0;
            clipTicks = 0;

            tickAnim();
        }
    }
}
