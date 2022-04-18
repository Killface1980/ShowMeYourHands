using FacialStuff.Tweener;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using ShowMeYourHands;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FacialStuff
{
    [ShowMeYourHandsMod.HotSwappable]
    public class HumanBipedDrawer : PawnBodyDrawer
    {
        #region Protected Fields

        protected const float OffsetGroundZ = -0.575f;

        protected DamageFlasher Flasher;

        #endregion Protected Fields

        #region Private Fields

        //  private PawnFeetTweener feetTweener;
        private float _animatedPercent;

        #endregion Private Fields

        #region Public Properties

        public Material LeftHandMat =>
        this.Flasher.GetDamagedMat(this.compAnimator.pawnBodyGraphic?.HandGraphicLeft?.MatSingle);

        public Material LeftHandShadowMat => this.Flasher.GetDamagedMat(this.compAnimator.pawnBodyGraphic
                                                                           ?.HandGraphicLeftShadow?.MatSingle);

        public Material RightHandMat =>
        this.Flasher.GetDamagedMat(this.compAnimator.pawnBodyGraphic?.HandGraphicRight?.MatSingle);

        public Material RightHandShadowMat => this.Flasher.GetDamagedMat(this.compAnimator.pawnBodyGraphic
                                                                            ?.HandGraphicRightShadow?.MatSingle);

        #endregion Public Properties

        #region Public Methods

        public override void ApplyBodyWobble(ref Vector3 rootLoc, ref Vector3 footPos)
        {
            if (this.compAnimator.BodyAnim == null)
            {
                return;
            }
            this.compAnimator.ModifyBodyAndFootPos(ref rootLoc, ref footPos);
            if (this.compAnimator.IsMoving)
            {
                WalkCycleDef walkCycle = this.compAnimator.CurrentWalkCycle;
                if (walkCycle != null)
                {
                    float bodysizeScaling = compAnimator.GetBodysizeScaling();
                    float bam = this.compAnimator.BodyOffsetZ * bodysizeScaling;

                    rootLoc.z += bam;
                    this.compAnimator.SetBodyAngle();

                    // Log.Message(CompFace.Pawn + " - " + this.movedPercent + " - " + bam.ToString());
                }
            }
            base.ApplyBodyWobble(ref rootLoc, ref footPos);

            // Adds the leg length to the rootloc and relocates the feet to keep the pawn in center, e.g. for shields
        }

        public void ApplyEquipmentWobble(ref Vector3 rootLoc)
        {
            if (this.compAnimator.IsMoving)
            {
                WalkCycleDef walkCycle = this.compAnimator.CurrentWalkCycle;
                if (walkCycle != null)
                {
                    float bam = this.compAnimator.BodyOffsetZ;
                    rootLoc.z += bam;

                    // Log.Message(CompFace.Pawn + " - " + this.movedPercent + " - " + bam.ToString());
                }
            }

            return;
            // cannot move root pos so the stuff below not working
        }

        public override bool CarryStuff()
        {
            Pawn pawn = this.pawn;

            Thing carriedThing = pawn.carryTracker?.CarriedThing;
            if (carriedThing != null)
            {
                return true;
            }

            return base.CarryStuff();
        }

        private Material OverrideMaterialIfNeeded(Material original, Pawn pawn, bool portrait = false)
        {
            Material baseMat = ((!portrait && pawn.IsInvisible()) ? InvisibilityMatPool.GetInvisibleMat(original) : original);
            return pawn.Drawer.renderer.graphics.flasher.GetDamagedMat(baseMat);
        }

        public override void DrawFeet(float drawAngle, Vector3 rootLoc, Vector3 bodyLoc)
        {
            if (this.ShouldBeIgnored())
            {
                return;
            }
            /// No feet while sitting at a table
            Job curJob = this.pawn.CurJob;
            if (curJob != null)
            {
                if (curJob.def == JobDefOf.Ingest && !this.compAnimator.CurrentRotation.IsHorizontal)
                {
                    if (curJob.targetB.IsValid)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            Rot4 rotty = new(i);
                            IntVec3 intVec = this.pawn.Position + rotty.FacingCell;
                            if (intVec == curJob.targetB)
                            {
                                return;
                            }
                        }
                    }
                }
            }
            // Color unused = this.CompAnimator.FootColor;

            if (pawn.GetPosture() == PawnPosture.Standing) // keep the feet straight while standing, ignore the bodyQuat
            {
                drawAngle = 0f;
            }

            if (this.compAnimator.IsMoving)
            {
                // drawQuat *= Quaternion.AngleAxis(-pawn.Drawer.renderer.BodyAngle(), Vector3.up);
            }

            Rot4 rot = this.compAnimator.CurrentRotation;

            // Basic values
            BodyAnimDef body = this.compAnimator.BodyAnim;
            if (body == null)
            {
                return;
            }

            JointLister groundPos = this.GetJointPositions(JointType.Hip,
                                                           body.hipOffsets[rot.AsInt],
                                                           body.hipOffsets[Rot4.North.AsInt].x);

            Vector3 rightFootCycle = Vector3.zero;
            Vector3 leftFootCycle = Vector3.zero;
            float footAngleRight = 0;
            float footAngleLeft = 0;
            float offsetJoint = 0;
            WalkCycleDef cycle = this.compAnimator.CurrentWalkCycle;
            if (this.compAnimator.IsMoving && cycle != null)
            {
                offsetJoint = cycle.HipOffsetHorizontalX.Evaluate(this.compAnimator.MovedPercent);
                this.compAnimator.DoWalkCycleOffsets(
                    ref rightFootCycle,
                    ref leftFootCycle,
                    ref footAngleRight,
                    ref footAngleLeft,
                    ref offsetJoint,
                    cycle.FootPositionX,
                    cycle.FootPositionZ,
                    cycle.FootAngle, 
                    compAnimator.MovedPercent, 
                    compAnimator.CurrentRotation);
            }
            float bodysizeScaling = compAnimator.GetBodysizeScaling();

            // pawn jumping too high,move the feet
            if (!compAnimator.IsMoving && pawn.GetPosture() == PawnPosture.Standing)
            {
                Vector3 footVector = rootLoc;

                // Arms too far away from body
                while (Vector3.Distance(bodyLoc, footVector) > body.extraLegLength * bodysizeScaling * 3f)
                {
                    float step = 0.025f;
                    footVector = Vector3.MoveTowards(footVector, bodyLoc, step);
                }

                footVector.y = rootLoc.y;
                if (this.compAnimator.CurrentRotation == Rot4.North) // put the hands behind the pawn
                {
                    footVector.y -= Offsets.YOffset_Behind;
                }
                rootLoc = footVector;
            }

            this.GetBipedMesh(out Mesh footMeshRight, out Mesh footMeshLeft);

            Material matRight;
            Material matLeft;
            /*
            if (MainTabWindow_BaseAnimator.Colored)
            {
                matRight = this.CompAnimator.PawnBodyGraphic?.FootGraphicRightCol?.MatAt(rot);
                matLeft = this.CompAnimator.PawnBodyGraphic?.FootGraphicLeftCol?.MatAt(rot);
            }
            else
            */
            {
                Material rightFootMat = this.compAnimator.pawnBodyGraphic?.FootGraphicRight?.MatAt(rot);
                Material leftFootMat = this.compAnimator.pawnBodyGraphic?.FootGraphicLeft?.MatAt(rot);
                Material leftShadowMat = this.compAnimator.pawnBodyGraphic?.FootGraphicLeftShadow?.MatAt(rot);
                Material rightShadowMat = this.compAnimator.pawnBodyGraphic?.FootGraphicRightShadow?.MatAt(rot);

                switch (rot.AsInt)
                {
                    default:
                        matRight = this.Flasher.GetDamagedMat(rightFootMat);
                        matLeft = this.Flasher.GetDamagedMat(leftFootMat);
                        break;
                    /*
                    case 0:
                        matRight = this.Flasher.GetDamagedMat(rightShadowMat);
                        matLeft = this.Flasher.GetDamagedMat(leftShadowMat);
                        break;
                    */
                    case 1:
                        matRight = this.Flasher.GetDamagedMat(rightFootMat);
                        matLeft = this.Flasher.GetDamagedMat(leftShadowMat);
                        break;

                    case 3:

                        matRight = this.Flasher.GetDamagedMat(rightShadowMat);
                        matLeft = this.Flasher.GetDamagedMat(leftFootMat);
                        break;
                }
            }

            if (pawn.IsInvisible())
            {
                matLeft = InvisibilityMatPool.GetInvisibleMat(matLeft);
                matRight = InvisibilityMatPool.GetInvisibleMat(matRight);
            }


            bool drawRight = matRight != null && this.compAnimator.BodyStat.FootRight != PartStatus.Missing;
            bool drawLeft = matLeft != null && this.compAnimator.BodyStat.FootLeft != PartStatus.Missing;

            groundPos.LeftJoint = groundPos.LeftJoint.RotatedBy(drawAngle);
            groundPos.RightJoint = groundPos.RightJoint.RotatedBy(drawAngle);
            leftFootCycle = leftFootCycle.RotatedBy(drawAngle);
            rightFootCycle = rightFootCycle.RotatedBy(drawAngle);

            // rootLoc.y -= Offsets.YOffset_CarriedThing; // feet pop out in front of other pawns


            Vector3 ground = rootLoc + new Vector3(0, 0, OffsetGroundZ).RotatedBy(drawAngle) * bodysizeScaling;
            

            if (drawLeft)
            {
                // TweenThing leftFoot = TweenThing.FootLeft;
                // PawnPartsTweener tweener = this.CompAnimator.PartTweener;
                // if (tweener != null)
                {
                    Vector3 position = ground + (groundPos.LeftJoint + leftFootCycle) * bodysizeScaling;
                    // tweener.PartPositions[(int)leftFoot] = position;
                    // tweener.PreThingPosCalculation(leftFoot, spring: SpringTightness.Stff);

                    Graphics.DrawMesh(
                        footMeshLeft,
                        position, // tweener.TweenedPartsPos[(int)leftFoot],
                        Quaternion.AngleAxis(footAngleLeft + drawAngle, Vector3.up),
                        matLeft,
                        0);
                }
            }

            if (drawRight)
            {
                // TweenThing rightFoot = TweenThing.FootRight;
                // PawnPartsTweener tweener = this.CompAnimator.PartTweener;
                // if (tweener != null)
                // {
                Vector3 position = ground + (groundPos.RightJoint + rightFootCycle) * bodysizeScaling;

                // tweener.PartPositions[(int)rightFoot] = position;
                //     tweener.PreThingPosCalculation(rightFoot, spring: SpringTightness.Stff);
                Graphics.DrawMesh(
                    footMeshRight,
                    position, // tweener.TweenedPartsPos[(int)rightFoot],
                     Quaternion.AngleAxis(footAngleRight + drawAngle, Vector3.up),
                    matRight,
                    0);

                // }
            }
            /*
            if (MainTabWindow_BaseAnimator.Develop)
            {
                // for debug
                Material centerMat = GraphicDatabase
                                    .Get<Graphic_Single>("Hands/Ground", ShaderDatabase.Transparent, Vector2.one,
                                                         Color.red).MatSingle;

                GenDraw.DrawMeshNowOrLater(
                                           footMeshLeft,
                                           ground + groundPos.LeftJoint +
                                           new Vector3(offsetJoint, -0.301f, 0),
                                           drawQuat * Quaternion.AngleAxis(0, Vector3.up),
                                           centerMat,
                                           false);

                GenDraw.DrawMeshNowOrLater(
                                           footMeshRight,
                                           ground + groundPos.RightJoint +
                                           new Vector3(offsetJoint, 0.301f, 0),
                                           drawQuat * Quaternion.AngleAxis(0, Vector3.up),
                                           centerMat,
                                           false);

                Material hipMat = GraphicDatabase
                    .Get<Graphic_Single>("Hands/Human_Hand_dev", ShaderDatabase.Transparent, Vector2.one,
                        Color.blue).MatSingle;

                GenDraw.DrawMeshNowOrLater(
                    footMeshLeft,
                    groundPos.LeftJoint +
                    new Vector3(offsetJoint, -0.301f, 0),
                    drawQuat * Quaternion.AngleAxis(0, Vector3.up),
                    hipMat,
                    false);

                // UnityEngine.Graphics.DrawMesh(handsMesh, center + new Vector3(0, 0.301f, z),
                // Quaternion.AngleAxis(0, Vector3.up), centerMat, 0);
                // UnityEngine.Graphics.DrawMesh(handsMesh, center + new Vector3(0, 0.301f, z2),
                // Quaternion.AngleAxis(0, Vector3.up), centerMat, 0);
            }
            */
        }

        public override void DrawHands(float bodyAngle, Vector3 drawPos,
            Thing carriedThing = null, bool flip = false)
        {
            if (this.ShouldBeIgnored())
            {
                return;
            }

            BodyAnimDef body = this.compAnimator.BodyAnim;
            if (body == null)
            {
                return;
            }
            if (!this.compAnimator.BodyAnim.bipedWithHands)
            {
                return;
            }

           

            float bodySizeScaling = compAnimator.GetBodysizeScaling();

            this.compAnimator.MainHandPosition = this.compAnimator.SecondHandPosition = Vector3.zero;
            bool hasSecondWeapon                = false;
            ThingWithComps eq                   = pawn?.equipment?.Primary;
            bool leftBehind                     = false;
            bool rightBehind                    = false;
            bool leftHandFlipped                     = false;
            bool rightHandFlipped                    = false;

            if (eq != null && pawn?.CurJob?.def != null && !pawn.CurJob.def.neverShowWeapon)
            {
                Type baseType = pawn.Drawer.renderer.GetType();
                MethodInfo methodInfo = baseType.GetMethod("CarryWeaponOpenly", BindingFlags.NonPublic | BindingFlags.Instance);
                object result = methodInfo?.Invoke(pawn.Drawer.renderer, null);
                if (result != null && (bool)result)
                {
                    this.compAnimator.DoHandOffsetsOnWeapon(eq, out hasSecondWeapon, out leftBehind, out rightBehind, out rightHandFlipped, out leftHandFlipped);
                }
            }
            bool carrying = carriedThing != null;
            if (this.compAnimator.CurrentRotation != Rot4.North)
            {
                // drawPos.y += 0.03474903f;

            }
            if (carrying)
            {
                if (this.compAnimator.CurrentRotation != Rot4.North)
                {
                    //drawPos.y += Offsets.YOffset_CarriedThing;
                    drawPos.z -= 0.1f;
                }
                /*    this.compAnimator.DoHandOffsetsOnWeapon(carriedThing,
                        this.compAnimator.CurrentRotation == Rot4.West ? 217f : 143f, out _, out _,
                        out _);*/
            }
            else
            {
                // needed for the apparel layer with caching disabled; otherwise hands are drawn beneath the apparel
               // drawPos.y += 0.001f;
            }
            
            // return if hands already drawn on carrything

            Rot4 rot = this.compAnimator.CurrentRotation;
            bool isFacingNorth = rot == Rot4.North;

            float animationAngle = 0f;
            Vector3 animationPosOffset = Vector3.zero;
            
            if (!carrying && !compAnimator.IsMoving)
            {
                DoAnimationHands(ref animationPosOffset, ref animationAngle);
                animationPosOffset.y = 0f;
            }
            bool poschanged = false;
            if (animationAngle != 0f)
            {
                animationAngle *= 3.8f;
                bodyAngle += animationAngle;
            }

            if (animationPosOffset != Vector3.zero)
            {
                drawPos += animationPosOffset.RotatedBy(animationAngle) * 1.35f * bodySizeScaling;

                //this.compAnimator.FirstHandPosition += animationPosOffset.RotatedBy(animationAngle);
                //this.compAnimator.SecondHandPosition += animationPosOffset.RotatedBy(-animationAngle);
                poschanged = true;
            }

            if (!carrying)
            {
                //drawPos.y -= 0.0001f; // feet pop out in front of other pawns

            }

            JointLister shoulperPos = this.GetJointPositions(JointType.Shoulder,
                body.shoulderOffsets[rot.AsInt],
                body.shoulderOffsets[Rot4.North.AsInt].x,
                carrying, this.pawn.ShowWeaponOpenly());

            Vector3 shoulperPosLeftJoint  = shoulperPos.LeftJoint;
            Vector3 shoulperPosRightJoint = shoulperPos.RightJoint;
            Vector3 MainHandPosition      = this.compAnimator.MainHandPosition;
            Vector3 offHandPosition       = this.compAnimator.SecondHandPosition;
            if (carrying)
            {
                // this.ApplyEquipmentWobble(ref drawPos);

                Vector3 handVector = drawPos;
                // handVector.z += 0.2f; // hands too high on carriedthing - edit: looks good on smokeleaf joints

                //handVector.y += Offsets.YOffset_CarriedThing;
                // Arms too far away from body

                var props = carriedThing.def?.GetCompProperties<WhandCompProps>();
                if ( props != null)
                {
                    MainHandPosition = props.MainHand;
                }
                else if (!this.compAnimator.IsMoving)
                {
                    float f = this.compAnimator.CurrentRotation.IsHorizontal ? 1.5f : 0.65f;

                    while (Vector3.Distance(this.pawn.DrawPos, handVector) > body.armLength * bodySizeScaling * f)
                    {
                        float step = 0.025f;
                        handVector = Vector3.MoveTowards(handVector, this.pawn.DrawPos, step);
                    }

                    // carriedThing.DrawAt(drawPos, flip);
                    handVector.y = drawPos.y;

                    drawPos = handVector;

                    DoAnimationHands(ref animationPosOffset, ref animationAngle);
                    animationPosOffset.y = 0f;
                    animationAngle *= 3.8f;
                    bodyAngle += animationAngle;
                    drawPos += animationPosOffset.RotatedBy(animationAngle) * 1.35f * bodySizeScaling;
                    shoulperPosLeftJoint.x -= animationPosOffset.z * 0.75f;
                    shoulperPosRightJoint.x -= animationPosOffset.z * 0.75f;
                }



                if (isFacingNorth) // put the hands behind the pawn
                {
                    drawPos.y = Offsets.YOffset_Behind;
                }
            }


            List<float> handSwingAngle = new() { 0f, 0f };
            float shoulderAngle = 0f;
            Vector3 rightHandVector = Vector3.zero;
            Vector3 leftHandVector = Vector3.zero;
            WalkCycleDef walkCycle = this.compAnimator.CurrentWalkCycle;
            //PoseCycleDef poseCycle = this.CompAnimator.PoseCycle;

            if (!carrying)
            {
                float offsetJoint = walkCycle.ShoulderOffsetHorizontalX.Evaluate(this.compAnimator.MovedPercent);

                this.compAnimator.DoWalkCycleOffsets(
                                        body.armLength,
                                        ref rightHandVector,
                                        ref leftHandVector,
                                        ref shoulderAngle,
                                        ref handSwingAngle,
                                        ref shoulperPos,
                                        offsetJoint);
            }

            // this.DoAttackAnimationHandOffsets(ref handSwingAngle, ref rightHand, false);

            this.GetBipedMesh(out Mesh handMeshRight, out Mesh handMeshLeft, rightHandFlipped, leftHandFlipped);

            Material matLeft = this.LeftHandMat;
            Material matRight = this.RightHandMat;

            /*if (MainTabWindow_BaseAnimator.Colored)
            {
                matLeft = this.CompAnimator.PawnBodyGraphic?.HandGraphicLeftCol?.MatSingle;
                matRight = this.CompAnimator.PawnBodyGraphic?.HandGraphicRightCol?.MatSingle;
            }
            else */
            if (!carrying)
            {
                // Should draw shadow if inner side of the palm is facing to camera?
                switch (rot.AsInt)
                {
                    case 1:
                        matLeft = this.LeftHandShadowMat;
                        break;

                    case 3:
                        matRight = this.RightHandShadowMat;
                        break;
                }
            }
             if (isFacingNorth)
            {
                matLeft = this.LeftHandShadowMat;
                matRight = this.RightHandShadowMat;
            }

             if (pawn.IsInvisible())
             {
                 matLeft  = InvisibilityMatPool.GetInvisibleMat(matLeft);
                 matRight = InvisibilityMatPool.GetInvisibleMat(matRight);
             }
            bool drawLeft  = matLeft != null && this.compAnimator.BodyStat.HandLeft != PartStatus.Missing;
            bool drawRight = matRight != null && this.compAnimator.BodyStat.HandRight != PartStatus.Missing;

            //float shouldRotate = pawn.GetPosture() == PawnPosture.Standing ? 0f : 90f;

            if (drawLeft)
            {
                Quaternion quat;
                Vector3 position = Vector3.zero;
                bool noTween = false;
                if (this.compAnimator.BodyStat.HandRight == PartStatus.Missing &&
                    MainHandPosition != Vector3.zero)
                {
                    quat = Quaternion.AngleAxis(this.compAnimator.MainHandAngle - 90f, Vector3.up);
                    position = MainHandPosition;
                    if (compAnimator.CurrentRotation == Rot4.West) // put the second hand behind while turning right
                    {
                      //  quat *= Quaternion.AngleAxis(180f, Vector3.up);
                    }
                    if (rightBehind)
                    {
                        matRight = this.RightHandShadowMat;
                    }

                    noTween = true;
                }
                else if (hasSecondWeapon || offHandPosition != Vector3.zero && pawn.stances.curStance is Stance_Busy stance_Busy && !stance_Busy.neverAimWeapon && stance_Busy.focusTarg.IsValid) // only draw he second hand on weapon while aiming
                {
                    position = offHandPosition;
                    quat = Quaternion.AngleAxis(this.compAnimator.OffHandAngle - 90f, Vector3.up);
                    if (compAnimator.CurrentRotation == Rot4.East) // put the second hand behind while turning right
                    {
                       // quat *= Quaternion.AngleAxis(180f, Vector3.up);
                    }

                    if (leftBehind)
                    {
                        matLeft = this.LeftHandShadowMat;
                    }
                    noTween = true;
                }
                else
                {
                    shoulperPosLeftJoint = shoulperPosLeftJoint.RotatedBy(bodyAngle);
                    leftHandVector = leftHandVector.RotatedBy(bodyAngle - handSwingAngle[0] - shoulderAngle + animationAngle);

                    position = drawPos + (shoulperPosLeftJoint + leftHandVector) * bodySizeScaling;
                    if (carrying) // grabby angle
                    {
                        quat = Quaternion.AngleAxis(bodyAngle + 105f, Vector3.up);
                    }
                    else
                    {
                        quat = Quaternion.AngleAxis(bodyAngle - handSwingAngle[0] - shoulderAngle, Vector3.up);
                    }
                    quat *= Quaternion.AngleAxis(animationAngle*1.25f, Vector3.up);

                }

                TweenThing handLeft = TweenThing.HandLeft;
                this.DrawTweenedHand(position, handMeshLeft, matLeft, quat, handLeft, noTween);
                //GenDraw.DrawMeshNowOrLater(
                //                           handMeshLeft, position,
                //                           quat,
                //                           matLeft,
                //                           portrait);
            }

            if (drawRight)
            {
                Quaternion quat;
                Vector3 position;
                bool noTween = false;
                if (MainHandPosition != Vector3.zero)
                {
                    quat = Quaternion.AngleAxis(this.compAnimator.MainHandAngle - 90f, Vector3.up);
                    position = MainHandPosition;
                    if (compAnimator.CurrentRotation == Rot4.West) // put the second hand behind while turning right
                    {
                        quat *= Quaternion.AngleAxis(180f, Vector3.up);
                    }
                    if (rightBehind)
                    {
                        matRight = this.RightHandShadowMat;
                    }

                    noTween = true;
                }
                else
                {
                    shoulperPosRightJoint = shoulperPosRightJoint.RotatedBy(bodyAngle);
                    rightHandVector = rightHandVector.RotatedBy(bodyAngle + handSwingAngle[1] - shoulderAngle + animationAngle);

                    position = drawPos + (shoulperPosRightJoint + rightHandVector) * bodySizeScaling;
                    if (carrying) // grabby angle
                    {
                        quat = Quaternion.AngleAxis(bodyAngle - 115f, Vector3.up);
                    }
                    else
                    {
                        quat = Quaternion.AngleAxis(bodyAngle  + handSwingAngle[1] - shoulderAngle, Vector3.up);
                    }
                    quat *= Quaternion.AngleAxis(animationAngle*1.25f, Vector3.up);

                    /*else if (compAnimator.CurrentRotation.IsHorizontal)
                    {
                        quat *= Quaternion.AngleAxis(compAnimator.CurrentRotation == Rot4.West ? +90f : -90f, Vector3.up);
                    }*/
                }

                TweenThing handRight = TweenThing.HandRight;
                this.DrawTweenedHand(position, handMeshRight, matRight, quat, handRight, noTween);
                // GenDraw.DrawMeshNowOrLater(
                //                            handMeshRight, position,
                //                            quat,
                //                            matRight,
                //                            portrait);
            }
            /*
            if (MainTabWindow_BaseAnimator.Develop)
            {
                // for debug
                Material centerMat = GraphicDatabase.Get<Graphic_Single>(
                                                                         "Hands/Human_Hand_dev",
                                                                         ShaderDatabase.CutoutSkin,
                                                                         Vector2.one,
                                                                         Color.white).MatSingle;

                GenDraw.DrawMeshNowOrLater(
                                           handMeshLeft,
                                           drawPos + shoulperPos.LeftJoint + new Vector3(0, -0.301f, 0),
                                           bodyQuat * Quaternion.AngleAxis(-shoulderAngle[0], Vector3.up),
                                           centerMat,
                                           false);

                GenDraw.DrawMeshNowOrLater(
                                           handMeshRight,
                                           drawPos + shoulperPos.RightJoint + new Vector3(0, 0.301f, 0),
                                           bodyQuat * Quaternion.AngleAxis(-shoulderAngle[1], Vector3.up),
                                           centerMat,
                                           false);
            }
            */
        }

        public enum aniType
        { none, doSomeThing, social, smash, idle, gameCeremony, crowd, solemn }

        private void DoAnimationHands(ref Vector3 posOffset, ref float animationAngle)
        {
            Job curJob = pawn.CurJob;
            if (curJob == null)
            {
                return;
            }
            int tick = 0;
            float f;
            int t2;
            Rot4 r;
            int IdTick = pawn.thingIDNumber * 20;
            Rot4 rot = compAnimator.CurrentRotation;
            Rot4 tr = rot;
            aniType aniType = aniType.none;
            float angle = 0f;
            Vector3 pos = Vector3.zero;
            int total;

            switch (curJob.def.defName)
            {
                // do something
                case "UseArtifact":
                case "UseNeurotrainer":
                case "UseStylingStation":
                case "UseStylingStationAutomatic":
                case "Wear":
                case "SmoothWall":
                case "UnloadYourInventory":
                case "UnloadInventory":
                case "Uninstall":
                case "Train":
                case "TendPatient":
                case "Tame":
                case "TakeBeerOutOfFermentingBarrel":
                case "StudyThing":
                case "Strip":
                case "SmoothFloor":
                case "SlaveSuppress":
                case "SlaveExecution":
                case "DoBill": // 제작, 조리
                case "Deconstruct":
                case "FinishFrame": // 건설
                case "Equip":
                case "ExtractRelic":
                case "ExtractSkull":
                case "ExtractTree":
                case "GiveSpeech":
                case "Hack":
                case "InstallRelic":
                case "Insult":
                case "Milk":
                case "Open":
                case "Play_MusicalInstrument":
                case "PruneGauranlenTree":
                case "RearmTurret":
                case "RearmTurretAtomic":
                case "RecolorApparel":
                case "Refuel":
                case "RefuelAtomic":
                case "Reload":
                case "RemoveApparel":
                case "RemoveFloor":
                case "RemoveRoof":
                case "Repair":
                case "Research":
                case "Resurrect":
                case "Sacrifice":
                case "Scarify":
                case "Shear":
                case "Slaughter":
                case "Ignite":
                case "ManTurret":
                    aniType = aniType.doSomeThing;
                    break;

                // social
                case "GotoAndBeSociallyActive":
                case "StandAndBeSociallyActive":
                case "VisitSickPawn":
                case "SocialRelax":
                    aniType = aniType.social;
                    break;

                // idle
                case "Wait_Combat":
                case "Wait":
                    aniType = aniType.idle;
                    break;

                case "Vomit":
                    tick = (Find.TickManager.TicksGame + IdTick) % 200;
                    if (!PawnExtensions.Ani(ref tick, 25, ref angle, 15f, 35f, -1f, ref pos, rot))
                        if (!PawnExtensions.Ani(ref tick, 25, ref angle, 35f, 25f, -1f, ref pos, rot))
                            if (!PawnExtensions.Ani(ref tick, 25, ref angle, 25f, 35f, -1f, ref pos, rot))
                                if (!PawnExtensions.Ani(ref tick, 25, ref angle, 35f, 25f, -1f, ref pos, rot))
                                    if (!PawnExtensions.Ani(ref tick, 25, ref angle, 25f, 35f, -1f, ref pos, rot))
                                        if (!PawnExtensions.Ani(ref tick, 25, ref angle, 35f, 25f, -1f, ref pos, rot))
                                            if (!PawnExtensions.Ani(ref tick, 25, ref angle, 25f, 35f, -1f, ref pos, rot))
                                                if (!PawnExtensions.Ani(ref tick, 25, ref angle, 35f, 15f, -1f, ref pos, rot)) ;
                    break;

                case "Clean":
                    aniType = aniType.doSomeThing;
                    break;

                case "Mate":
                    break;

                case "MarryAdjacentPawn":
                    tick = (Find.TickManager.TicksGame) % 310;

                    if (!PawnExtensions.AnimationHasTicksLeft(ref tick, 150))
                    {
                        if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 5f, -1f, ref pos, Vector3.zero, new Vector3(0.05f, 0f, 0f), rot))
                            if (!PawnExtensions.Ani(ref tick, 50, ref angle, 5f, 10f, -1f, ref pos, new Vector3(0.05f, 0f, 0f), new Vector3(0.05f, 0f, 0f), rot))
                                if (!PawnExtensions.Ani(ref tick, 50, ref angle, 10, 10f, -1f, ref pos, new Vector3(0.05f, 0f, 0f), new Vector3(0.05f, 0f, 0f), rot))
                                    if (!PawnExtensions.Ani(ref tick, 40, ref angle, 10f, 0f, -1f, ref pos, new Vector3(0.05f, 0f, 0f), Vector3.zero, rot)) ;
                    }

                    break;

                case "SpectateCeremony": // 각종 행사, 의식 (결혼식, 장례식, 이념행사)
                    LordJob_Ritual ritualJob = PawnExtensions.GetPawnRitual(pawn);
                    if (ritualJob == null) // 기본
                    {
                        aniType = aniType.crowd;
                    }
                    else if (ritualJob.Ritual == null)
                    {
                        // 로얄티 수여식 관중
                        aniType = aniType.solemn;
                    }
                    else
                    {
                        switch (ritualJob.Ritual.def.defName)
                        {
                            default:
                                aniType = aniType.crowd;
                                break;

                            case "Funeral": // 장례식
                                aniType = aniType.solemn;
                                break;
                        }
                    }
                    break;

                case "BestowingCeremony": // 로얄티 수여식 받는 대상
                    aniType = aniType.solemn;
                    break;

                case "Dance":
                    break;

                // joy

                case "Play_Hoopstone":
                    tick = (Find.TickManager.TicksGame + IdTick) % 60;
                    if (!PawnExtensions.Ani(ref tick, 30, ref angle, 10f, -20f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot))
                    {
                        if (!PawnExtensions.Ani(ref tick, 30, ref angle, -20f, 10f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot)) ;
                    }
                    break;

                case "Play_Horseshoes":
                    tick = (Find.TickManager.TicksGame + IdTick) % 60;
                    if (!PawnExtensions.Ani(ref tick, 30, ref angle, 10f, -20f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot))
                    {
                        if (!PawnExtensions.Ani(ref tick, 30, ref angle, -20f, 10f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot)) ;
                    }
                    break;

                case "Play_GameOfUr":
                    tick = (Find.TickManager.TicksGame + IdTick * 27) % 900;
                    if (tick <= 159) { aniType = aniType.gameCeremony; }
                    else { aniType = aniType.doSomeThing; }
                    break;

                case "Play_Poker":
                    tick = (Find.TickManager.TicksGame + IdTick * 27) % 900;
                    if (tick <= 159) { aniType = aniType.gameCeremony; }
                    else { aniType = aniType.doSomeThing; }
                    break;

                case "Play_Billiards":
                    tick = (Find.TickManager.TicksGame + IdTick * 27) % 900;
                    if (tick <= 159) { aniType = aniType.gameCeremony; }
                    else { aniType = aniType.doSomeThing; }
                    break;

                case "Play_Chess":
                    tick = (Find.TickManager.TicksGame + IdTick * 27) % 900;
                    if (tick <= 159) { aniType = aniType.gameCeremony; }
                    else { aniType = aniType.doSomeThing; }
                    break;

                case "ExtinguishSelf": // 스스로 불 끄기
                    // custom anim, laying
                    break;

                case "Sow": // 씨뿌리기
                    tick = (Find.TickManager.TicksGame + IdTick) % 50;

                    if (!PawnExtensions.AnimationHasTicksLeft(ref tick, 35))
                        if (!PawnExtensions.Ani(ref tick, 5, ref angle, 0f, 10f, -1f, ref pos, rot))
                            if (!PawnExtensions.Ani(ref tick, 10, ref angle, 10f, 0f, -1f, ref pos, rot)) ;
                    break;

                case "CutPlant": // 식물 베기
                    if (curJob.targetA.Thing?.def.plant?.IsTree != null && curJob.targetA.Thing.def.plant.IsTree)
                    {
                        aniType = aniType.smash;
                    }
                    else
                    {
                        aniType = aniType.doSomeThing;
                    }
                    break;

                case "Harvest": // 자동 수확
                    if (curJob.targetA.Thing?.def.plant?.IsTree != null && curJob.targetA.Thing.def.plant.IsTree)
                    {
                        aniType = aniType.smash;
                    }
                    else
                    {
                        aniType = aniType.doSomeThing;
                    }
                    break;

                case "HarvestDesignated": // 수동 수확
                    if (curJob.targetA.Thing?.def.plant?.IsTree != null && curJob.targetA.Thing.def.plant.IsTree)
                    {
                        aniType = aniType.smash;
                    }
                    else
                    {
                        aniType = aniType.doSomeThing;
                    }
                    break;

                case "Mine": // 채굴
                    aniType = aniType.smash;
                    break;

                case "Ingest": // 밥먹기
                    tick = (Find.TickManager.TicksGame + IdTick) % 150;
                    f = 0.03f;
                    if (!PawnExtensions.Ani(ref tick, 10, ref angle, 0f, 15f, -1f, ref pos, Vector3.zero, new Vector3(0f, 0f, 0f), rot))
                        if (!PawnExtensions.Ani(ref tick, 10, ref angle, 15f, 0f, -1f, ref pos, Vector3.zero, new Vector3(0f, 0f, 0f), rot))
                            if (!PawnExtensions.Ani(ref tick, 10, ref angle, 0f, 0f, -1f, ref pos, Vector3.zero, new Vector3(0f, 0f, f), rot))
                                if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, f), new Vector3(0f, 0f, -f), rot))
                                    if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, -f), new Vector3(0f, 0f, f), rot))
                                        if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, f), new Vector3(0f, 0f, -f), rot))
                                            if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, -f), new Vector3(0f, 0f, f), rot))
                                                if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, f), new Vector3(0f, 0f, -f), rot))
                                                    if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, -f), new Vector3(0f, 0f, f), rot)) ;

                    break;
            }

            switch (aniType)
            {
                case aniType.solemn:
                    tick = (Find.TickManager.TicksGame + (IdTick % 25)) % 660;

                    if (!PawnExtensions.AnimationHasTicksLeft(ref tick, 300))
                    {
                        if (!PawnExtensions.Ani(ref tick, 30, ref angle, 0f, 15f, -1f, ref pos, Vector3.zero, Vector3.zero, rot))
                            if (!PawnExtensions.Ani(ref tick, 300, ref angle, 15f, 15f, -1f, ref pos, Vector3.zero, Vector3.zero, rot))
                                if (!PawnExtensions.Ani(ref tick, 30, ref angle, 15f, 0f, -1f, ref pos, Vector3.zero, Vector3.zero, rot)) ;
                    }
                    break;

                case aniType.crowd:
                    total = 143;
                    t2 = (Find.TickManager.TicksGame + IdTick) % (total * 2);
                    tick = t2 % total;
                    r = PawnExtensions.Rot90(rot);
                    tr = rot;
                    if (!PawnExtensions.AnimationHasTicksLeft(ref tick, 20))
                    {
                        if (!PawnExtensions.Ani(ref tick, 5, ref angle, 0f, 10f, -1f, ref pos, r))
                            if (!PawnExtensions.Ani(ref tick, 20, ref angle, 10f, 10f, -1f, ref pos, r))
                                if (!PawnExtensions.Ani(ref tick, 5, ref angle, 10f, -10f, -1f, ref pos, r))
                                    if (!PawnExtensions.Ani(ref tick, 20, ref angle, -10f, -10f, -1f, ref pos, r))
                                    {
                                        if (!PawnExtensions.Ani(ref tick, 5, ref angle, -10f, 0f, -1f, ref pos, r))
                                        {
                                            tr = t2 >= total ? PawnExtensions.Rot90(rot) : PawnExtensions.Rot90b(rot);
                                            if (!PawnExtensions.Ani(ref tick, 15, ref angle, 0f, 0f, -1f, ref pos, rot)) // 85
                                            {
                                                tr = rot;
                                                if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, rot)) // 105

                                                    if (t2 >= total)
                                                    {
                                                        if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0.60f), rot))
                                                            if (!PawnExtensions.Ani(ref tick, 13, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0.60f), new Vector3(0f, 0f, 0f), rot, PawnExtensions.tweenType.line)) ;
                                                    }
                                                    else
                                                    {
                                                        if (!PawnExtensions.AnimationHasTicksLeft(ref tick, 33)) ;
                                                    }
                                            }
                                        }
                                    }
                    }

                    rot = tr;
                    break;

                case aniType.gameCeremony:

                    // need 159 tick

                    r = PawnExtensions.Rot90(rot);
                    tr = rot;

                    if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0.60f), rot))
                    {
                        if (!PawnExtensions.Ani(ref tick, 13, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0.60f), new Vector3(0f, 0f, 0f), rot, PawnExtensions.tweenType.line))

                            if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0.60f), rot))
                                if (!PawnExtensions.Ani(ref tick, 13, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0.60f), new Vector3(0f, 0f, 0f), rot, PawnExtensions.tweenType.line))
                                {
                                    rot = PawnExtensions.Rot90b(rot);
                                    if (!PawnExtensions.Ani(ref tick, 10, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot))
                                    {
                                        rot = PawnExtensions.Rot90b(rot);
                                        if (!PawnExtensions.Ani(ref tick, 10, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot))

                                            if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0.60f), rot))
                                                if (!PawnExtensions.Ani(ref tick, 13, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0.60f), new Vector3(0f, 0f, 0f), rot, PawnExtensions.tweenType.line))
                                                    if (!PawnExtensions.Ani(ref tick, 10, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot))
                                                    {
                                                        rot = PawnExtensions.Rot90b(rot);
                                                        if (!PawnExtensions.Ani(ref tick, 10, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot))
                                                        {
                                                            rot = PawnExtensions.Rot90b(rot);
                                                            if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot)) ;
                                                        }
                                                    }
                                    }
                                }
                    }

                    break;

                case aniType.idle:
                    tick = (Find.TickManager.TicksGame + IdTick * 13) % 800;
                    f = 4.5f;
                    r = PawnExtensions.Rot90(rot);
                    if (!PawnExtensions.Ani(ref tick, 500, ref angle, 0f, 0f, -1f, ref pos, r))
                        if (!PawnExtensions.Ani(ref tick, 25, ref angle, 0f, f, -1f, ref pos, r))
                            if (!PawnExtensions.Ani(ref tick, 50, ref angle, f, -f, -1f, ref pos, r))
                                if (!PawnExtensions.Ani(ref tick, 50, ref angle, -f, f, -1f, ref pos, r))
                                    if (!PawnExtensions.Ani(ref tick, 50, ref angle, f, -f, -1f, ref pos, r))
                                        if (!PawnExtensions.Ani(ref tick, 50, ref angle, -f, f, -1f, ref pos, r))
                                            if (!PawnExtensions.Ani(ref tick, 50, ref angle, f, -f, -1f, ref pos, r))
                                                if (!PawnExtensions.Ani(ref tick, 25, ref angle, -f, 0f, -1f, ref pos, r)) ;
                    break;

                case aniType.smash:
                    tick = (Find.TickManager.TicksGame + IdTick) % 133;

                    if (!PawnExtensions.Ani(ref tick, 70, ref angle, 0f, -20f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot))
                    {
                        if (!PawnExtensions.Ani(ref tick, 3, ref angle, -20f, 10f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot, PawnExtensions.tweenType.line))
                            if (!PawnExtensions.Ani(ref tick, 20, ref angle, 10f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot))
                                if (!PawnExtensions.Ani(ref tick, 40, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot)) ;
                    }
                    break;

                case aniType.doSomeThing:
                    total = 121;
                    t2 = (Find.TickManager.TicksGame + IdTick) % (total * 2);
                    tick = t2 % total;
                    r = PawnExtensions.Rot90(rot);
                    tr = rot;
                    if (!PawnExtensions.AnimationHasTicksLeft(ref tick, 20))
                        if (!PawnExtensions.Ani(ref tick, 5, ref angle, 0f, 10f, -1f, ref pos, r))
                            if (!PawnExtensions.Ani(ref tick, 20, ref angle, 10f, 10f, -1f, ref pos, r))
                                if (!PawnExtensions.Ani(ref tick, 5, ref angle, 10f, -10f, -1f, ref pos, r))
                                    if (!PawnExtensions.Ani(ref tick, 20, ref angle, -10f, -10f, -1f, ref pos, r))
                                    {
                                        if (!PawnExtensions.Ani(ref tick, 5, ref angle, -10f, 0f, -1f, ref pos, r))
                                        {
                                            //tr = t2 >= total ? PawnExtensions.Rot90(rot) : PawnExtensions.Rot90b(rot);
                                            if (!PawnExtensions.Ani(ref tick, 15, ref angle, 0f, 0f, -1f, ref pos, rot)) // 85
                                            {
                                                //tr = rot;
                                                if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, rot)) // 105
                                                    if (!PawnExtensions.Ani(ref tick, 5, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0.05f), rot))
                                                        if (!PawnExtensions.Ani(ref tick, 6, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0.05f), new Vector3(0f, 0f, 0f), rot)) ;
                                            }
                                        }
                                    }

                    rot = tr;
                    break;

                case aniType.social:
                    total = 221;
                    t2 = (Find.TickManager.TicksGame + IdTick) % (total * 2);
                    tick = t2 % total;
                    r = PawnExtensions.Rot90(rot);
                    tr = rot;
                    if (!PawnExtensions.AnimationHasTicksLeft(ref tick, 20))
                        if (!PawnExtensions.Ani(ref tick, 5, ref angle, 0f, 10f, -1f, ref pos, r))
                            if (!PawnExtensions.Ani(ref tick, 20, ref angle, 10f, 10f, -1f, ref pos, r))
                                if (!PawnExtensions.Ani(ref tick, 5, ref angle, 10f, -10f, -1f, ref pos, r))
                                    if (!PawnExtensions.Ani(ref tick, 20, ref angle, -10f, -10f, -1f, ref pos, r))
                                    {
                                        if (!PawnExtensions.Ani(ref tick, 5, ref angle, -10f, 0f, -1f, ref pos, r))
                                        {
                                            tr = t2 >= total ? PawnExtensions.Rot90(rot) : PawnExtensions.Rot90b(rot);
                                            if (!PawnExtensions.Ani(ref tick, 15, ref angle, 0f, 0f, -1f, ref pos, rot)) // 85
                                            {
                                                tr = rot;
                                                if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, rot)) // 105
                                                    if (!PawnExtensions.Ani(ref tick, 5, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0.05f), rot))
                                                        if (!PawnExtensions.Ani(ref tick, 6, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0.05f), new Vector3(0f, 0f, 0f), rot))

                                                            if (!PawnExtensions.Ani(ref tick, 35, ref angle, 0f, 0f, -1f, ref pos, rot))
                                                                if (!PawnExtensions.Ani(ref tick, 10, ref angle, 0f, 10f, -1f, ref pos, rot))
                                                                    if (!PawnExtensions.Ani(ref tick, 10, ref angle, 10f, 0f, -1f, ref pos, rot))
                                                                        if (!PawnExtensions.Ani(ref tick, 10, ref angle, 0f, 10f, -1f, ref pos, rot))
                                                                            if (!PawnExtensions.Ani(ref tick, 10, ref angle, 10f, 0f, -1f, ref pos, rot))
                                                                                if (!PawnExtensions.Ani(ref tick, 25, ref angle, 0f, 0f, -1f, ref pos, rot)) ;
                                            }
                                        }
                                    }

                    rot = tr;
                    break;
            }
            pos = new Vector3(pos.x, 0f, pos.z);

            animationAngle += angle;
            posOffset += pos;

            // New hand n feet animation
            /*
            pdd.offset_angle = angle;
            pdd.fixed_rot = rot;
            op = new Vector3(op.x, 0f, op.z);
            pdd.offset_pos = op;
            pos += op;
            */
        }

        public override void Initialize()
        {
            this.Flasher = this.pawn.Drawer.renderer.graphics.flasher;

            // this.feetTweener = new PawnFeetTweener();
            base.Initialize();
        }

        public float currentCellCostTotal = 0;


        public virtual void SelectWalkcycle(bool pawnInEditor)
        {
            if (!pawn.RaceProps.Humanlike)
            {
                if (compAnimator.BodyAnim != null)
                    this.compAnimator.SetWalkCycle(compAnimator.BodyAnim.walkCycles.FirstOrDefault().Value);
                return;
            }
            /*
            if (pawnInEditor)
            {
                this.CompAnimator.SetWalkCycle(Find.WindowStack.WindowOfType<MainTabWindow_WalkAnimator>().EditorWalkcycle);
                return;
            }
            */

            // Define the walkcycle by the actual move speed of the pawn instead of the urgency.
            // Faster pawns use faster cycles, this avoids slow.mo pawns.
            if (pawn.pather == null || Math.Abs(pawn.pather.nextCellCostTotal - this.currentCellCostTotal) == 0f) return;

            this.currentCellCostTotal = pawn.pather.nextCellCostTotal;

            Dictionary<LocomotionUrgency, WalkCycleDef> cycles = compAnimator.BodyAnim?.walkCycles;

            if (cycles.NullOrEmpty()) return;

            LocomotionUrgency locomotionUrgency = LocomotionUrgency.Walk;

            int moves = pawn.pather.nextCell.z == pawn.Position.z ?  pawn.TicksPerMoveCardinal : pawn.TicksPerMoveDiagonal;
            
            float pawnSpeedPerTick = moves / currentCellCostTotal; // 
            pawnSpeedPerTick *= Mathf.InverseLerp(100, -100,  currentCellCostTotal) * 2.25f;

            if (pawnSpeedPerTick > 0.95f)
            {
                locomotionUrgency = LocomotionUrgency.Sprint;
            }
            else if (pawnSpeedPerTick > 0.7f)
            {
                locomotionUrgency = LocomotionUrgency.Jog;
            }
            else if (pawnSpeedPerTick > 0.45f)
            {
                locomotionUrgency = LocomotionUrgency.Walk;
            }
            else
            {
                locomotionUrgency = LocomotionUrgency.Amble;
            }

            float rangeAmble = 0f;
            float rangeWalk = 0.45f;
            float rangeJog = 0.65f;
            float rangeSprint = 0.85f;

            if (cycles.TryGetValue(locomotionUrgency, out WalkCycleDef cycle))
            {
                if (cycle != null)
                {
                    this.compAnimator.SetWalkCycle(cycle);
                }
            }
            else
            {
                this.compAnimator.SetWalkCycle(compAnimator.BodyAnim.walkCycles.FirstOrDefault().Value);
            }
        }

        public override void Tick()
        {
            base.Tick();

            // BodyAnimator animator = CompAnimator.BodyAnimator;
            /*
             if (animator != null)
             {
                 animator.IsPosing(out this._animatedPercent);
             }
             */
            // var curve = bodyFacing.IsHorizontal ? this.walkCycle.BodyOffsetZ : this.walkCycle.BodyOffsetVerticalZ;
            //bool pawnInEditor = HarmonyPatchesFS.AnimatorIsOpen() && MainTabWindow_BaseAnimator.pawn == this.pawn;
            // if (GenTicks.TicksAbs % 10 == 0)
            {
                this.SelectWalkcycle(false);
                // this.SelectPosecycle();
            }
        }

        #endregion Public Methods

        #region Protected Methods

        protected void GetBipedMesh(out Mesh meshRight, out Mesh meshLeft)
        {
            GetBipedMesh(out meshRight, out meshLeft, null, null);
        }
        protected void GetBipedMesh(out Mesh meshRight, out Mesh meshLeft, bool? rightFlipped, bool? leftFlipped)
        {
            Rot4 rot = this.compAnimator.CurrentRotation;

            if (rightFlipped.HasValue && leftFlipped.HasValue)
            {
                meshRight = rightFlipped.Value ? this.compAnimator.pawnBodyMeshFlipped : this.compAnimator.pawnBodyMesh;
                meshLeft  = leftFlipped.Value ? this.compAnimator.pawnBodyMeshFlipped : this.compAnimator.pawnBodyMesh;
                return;
            }

            switch (rot.AsInt)
            {
                default:
                    meshRight = this.compAnimator.pawnBodyMesh;// MeshPool.plane10;
                    meshLeft  = this.compAnimator.pawnBodyMeshFlipped; // MeshPool.plane10Flip;
                    break;

                case 1:
                    meshRight = this.compAnimator.pawnBodyMesh;
                    meshLeft  = this.compAnimator.pawnBodyMesh;
                    break;

                case 3:
                    meshRight = compAnimator.pawnBodyMeshFlipped;// MeshPool.plane10Flip;
                    meshLeft  = compAnimator.pawnBodyMeshFlipped;// MeshPool.plane10Flip;
                    break;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void DrawTweenedHand(Vector3 position, Mesh handsMesh, Material material, Quaternion quat, TweenThing tweenThing, bool noTween)
        {
            if (position == Vector3.zero || handsMesh == null || material == null)
            {
                return;
            }

            // todo removed the tweener for now, will be used with hand animations

            if (this.ShouldBeIgnored())
            {
                return;
            }

            if (Find.TickManager.TicksGame == this.compAnimator.LastPosUpdate[(int)tweenThing])
            {
                position = this.compAnimator.LastPosition[(int)tweenThing];
            }
            else
            {
                Pawn_PathFollower pawnPathFollower = this.pawn.pather;
                if (pawnPathFollower != null && pawnPathFollower.MovedRecently(5))
                {
                    noTween = true;
                }

                this.compAnimator.LastPosUpdate[(int)tweenThing] = Find.TickManager.TicksGame;

                Vector3Tween tween = this.compAnimator.Vector3Tweens[(int)tweenThing];
                Vector3 start = this.compAnimator.LastPosition[(int)tweenThing];
                start.y = position.y;
                float distance = Vector3.Distance(start, position);

                switch (tween.State)
                {
                    case TweenState.Running:
                        if (noTween || this.compAnimator.IsMoving || distance > 1f)
                        {
                            tween.Stop(StopBehavior.ForceComplete);
                        }
                        else
                        {
                            position = tween.CurrentValue;
                        }
                        break;

                    case TweenState.Paused:
                        break;

                    case TweenState.Stopped:
                        if (noTween || this.compAnimator.IsMoving)
                        {
                            break;
                        }

                        ScaleFunc scaleFunc = ScaleFuncs.SineEaseOut;

                        float duration = Mathf.Abs(distance * 50f);
                        if (start != Vector3.zero && duration > 12f)
                        {
                            tween.Start(start, position, duration, scaleFunc);
                            position = start;
                        }

                        break;
                }

                this.compAnimator.LastPosition[(int)tweenThing] = position;
            }

            //  tweener.PreThingPosCalculation(tweenThing, noTween);

            Graphics.DrawMesh(handsMesh, position, quat, material, 0);
        }

        public bool ShouldBeIgnored()
        {
            return this.pawn.Dead || !this.pawn.Spawned || this.pawn.InContainerEnclosed;
        }

        #endregion Private Methods
    }
}