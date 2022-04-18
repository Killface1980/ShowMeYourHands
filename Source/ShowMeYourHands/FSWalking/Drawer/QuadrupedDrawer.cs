using RimWorld;
using UnityEngine;
using Verse;

namespace FacialStuff
{
    public class QuadrupedDrawer : HumanBipedDrawer
    {

        public override void DrawFeet(float drawAngle, Vector3 rootLoc, Vector3 bodyLoc)
        {


            if (this.compAnimator.IsMoving)
            {
                drawAngle += -pawn.Drawer.renderer.BodyAngle();
            }

            // Fix the position, maybe needs new code in GetJointPositions()?
            Rot4 _compAnimatorCurrentRotation = this.compAnimator.CurrentRotation;
            if (!_compAnimatorCurrentRotation.IsHorizontal)
            {
                //       rootLoc.y -=  Offsets.YOffset_Behind;
            }
            rootLoc.y += _compAnimatorCurrentRotation == Rot4.South ? -Offsets.YOffset_HandsFeetOver : 0;

            Vector3 frontPawLoc = rootLoc;
            Vector3 rearPawLoc = rootLoc;

            if (!_compAnimatorCurrentRotation.IsHorizontal)
            {
                frontPawLoc.y += (_compAnimatorCurrentRotation == Rot4.North ? Offsets.YOffset_Behind : -Offsets.YOffset_Behind);
            }

            this.DrawFrontPaws(drawAngle, frontPawLoc);

            base.DrawFeet(drawAngle, rearPawLoc, bodyLoc);
        }


        public override void DrawHands(float bodyAngle, Vector3 drawPos, Thing carriedThing = null,
            bool flip = false)
        {
            // base.DrawHands(bodyQuat, drawPos, portrait, carrying, drawSide);
        }

        protected virtual void DrawFrontPaws(float drawQuat, Vector3 rootLoc)
        {
            if (!this.compAnimator.BodyAnim.quadruped)
            {
                return;
            }

            // Basic values
            BodyAnimDef body = this.compAnimator.BodyAnim;

            Rot4 rot = this.compAnimator.CurrentRotation;
            if (body == null)
            {
                return;
            }

            JointLister jointPositions = this.GetJointPositions(JointType.Shoulder,
                body.shoulderOffsets[rot.AsInt],
                body.shoulderOffsets[Rot4.North.AsInt].x);

            // get the actual hip height
            JointLister groundPos = this.GetJointPositions(JointType.Hip,
                body.hipOffsets[rot.AsInt],
                body.hipOffsets[Rot4.North.AsInt].x);

            jointPositions.LeftJoint.z = groundPos.LeftJoint.z;
            jointPositions.RightJoint.z = groundPos.RightJoint.z;

            Vector3 rightFootAnim = Vector3.zero;
            Vector3 leftFootAnim = Vector3.zero;
            float footAngleRight = 0f;

            float footAngleLeft = 0f;
            float offsetJoint = 0;

            WalkCycleDef cycle = this.compAnimator.CurrentWalkCycle;


            if (cycle != null && compAnimator.IsMoving)
            {
                offsetJoint = cycle.ShoulderOffsetHorizontalX.Evaluate(this.compAnimator.MovedPercent);

                // Center = drawpos of carryThing
                this.compAnimator.DoWalkCycleOffsets(
                    ref rightFootAnim,
                    ref leftFootAnim,
                    ref footAngleRight,
                    ref footAngleLeft,
                    ref offsetJoint,
                    cycle.FrontPawPositionX,
                    cycle.FrontPawPositionZ,
                    cycle.FrontPawAngle, compAnimator.MovedPercent, compAnimator.CurrentRotation);
            }
            float bodysizeScaling = compAnimator.GetBodysizeScaling();

            this.GetBipedMesh(out Mesh footMeshRight, out Mesh footMeshLeft);

            Material matLeft;

            Material matRight;
            /*
             if (MainTabWindow_BaseAnimator.Colored)
            {
                matRight = this.CompAnimator.PawnBodyGraphic?.FrontPawGraphicRightCol?.MatAt(rot);
                matLeft = this.CompAnimator.PawnBodyGraphic?.FrontPawGraphicLeftCol?.MatAt(rot);
            }
            else
            */

            switch (rot.AsInt)
            {
                default:
                    matRight = this.Flasher.GetDamagedMat(this.compAnimator.pawnBodyGraphic?.FrontPawGraphicRight
                        ?.MatAt(rot));
                    matLeft = this.Flasher.GetDamagedMat(this.compAnimator.pawnBodyGraphic?.FrontPawGraphicLeft
                        ?.MatAt(rot));
                    break;

                case 1:
                    matRight = this.Flasher.GetDamagedMat(this.compAnimator.pawnBodyGraphic?.FrontPawGraphicRight
                        ?.MatAt(rot));
                    matLeft = this.Flasher.GetDamagedMat(this.compAnimator.pawnBodyGraphic
                        ?.FrontPawGraphicLeftShadow?.MatAt(rot));
                    break;

                case 3:
                    matRight = this.Flasher.GetDamagedMat(this.compAnimator.pawnBodyGraphic
                        ?.FrontPawGraphicRightShadow?.MatAt(rot));
                    matLeft = this.Flasher.GetDamagedMat(this.compAnimator.pawnBodyGraphic?.FrontPawGraphicLeft
                        ?.MatAt(rot));
                    break;
            }

            Vector3 ground = rootLoc + (drawQuat * new Vector3(0, 0, OffsetGroundZ)) * bodysizeScaling;

            if (matLeft != null)
            {
                if (this.compAnimator.BodyStat.HandLeft != PartStatus.Missing)
                {
                    Vector3 position = ground + (jointPositions.LeftJoint + leftFootAnim) * bodysizeScaling;
                    Graphics.DrawMesh(
                        footMeshLeft,
                        position,
                        Quaternion.AngleAxis(drawQuat + footAngleLeft, Vector3.up),
                        matLeft,
                        0);
                }
            }

            if (matRight != null)
            {
                if (this.compAnimator.BodyStat.HandRight != PartStatus.Missing)
                {
                    Vector3 position = ground + (jointPositions.RightJoint + rightFootAnim) * bodysizeScaling;
                    Graphics.DrawMesh(
                        footMeshRight,
                        position,
                        Quaternion.AngleAxis(drawQuat + footAngleRight, Vector3.up),
                        matRight,
                        0);
                }
            }
            /*
            if (MainTabWindow_BaseAnimator.Develop)
            {
                // for debug
                Material centerMat = GraphicDatabase
                    .Get<Graphic_Single>("Hands/Ground", ShaderDatabase.Transparent, Vector2.one,
                        Color.cyan).MatSingle;

                GenDraw.DrawMeshNowOrLater(
                    footMeshLeft,
                    ground + jointPositions.LeftJoint +
                    new Vector3(offsetJoint, 0.301f, 0),
                    drawQuat * Quaternion.AngleAxis(0, Vector3.up),
                    centerMat,
                    false);

                GenDraw.DrawMeshNowOrLater(
                    footMeshRight,
                    ground + jointPositions.RightJoint +
                    new Vector3(offsetJoint, 0.301f, 0),
                    drawQuat * Quaternion.AngleAxis(0, Vector3.up),
                    centerMat,
                    false);

                // UnityEngine.Graphics.DrawMesh(handsMesh, center + new Vector3(0, 0.301f, z),
                // Quaternion.AngleAxis(0, Vector3.up), centerMat, 0);
                // UnityEngine.Graphics.DrawMesh(handsMesh, center + new Vector3(0, 0.301f, z2),
                // Quaternion.AngleAxis(0, Vector3.up), centerMat, 0);
            }
            */
        }
    }
}