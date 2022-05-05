using FacialStuff.GraphicsFS;
using FacialStuff.Tweener;
using JetBrains.Annotations;
using RimWorld;
using ShowMeYourHands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace FacialStuff
{
    [ShowMeYourHandsMod.HotSwappable]
    [StaticConstructorOnStartup]
    public class CompBodyAnimator : ThingComp
    {
        private const float animationFactorForSmoothing = 0.5f;

        public bool IsAlien = false;

        #region Public Fields

        public JointLister GetJointPositions(JointType jointType, Vector3 offsets,
                                                float jointWidth,
                                        bool carrying = false, bool armed = false)
        {
            Rot4 rot = this.CurrentRotation;
            JointLister joints = new()
            {
                jointType = jointType
            };
            float leftX = offsets.x;
            float rightX = offsets.x;
            float leftZ = offsets.z;
            float rightZ = offsets.z;

            float offsetY = Offsets.YOffset_HandsFeetOver;

            bool offsetsCarrying = false;

            switch (jointType)
            {
                case JointType.Shoulder:
                    offsetY = Offsets.YOffset_HandsFeetOver;
                    if (carrying) { offsetsCarrying = true; }
                    break;
            }

            float leftY = offsetY;
            float rightY = offsetY;

            if (offsetsCarrying)
            {
                leftX = jointWidth / 1.4f;
                rightX = -jointWidth / 1.4f;
                leftZ = -0.025f;
                rightZ = -leftZ;
            }
            else if (rot.IsHorizontal)
            {
                float offsetX = jointWidth * 0.1f;
                float offsetZ = jointWidth * 0.2f;

                if (rot == Rot4.East)
                {
                    leftY = -Offsets.YOffset_Behind;
                    leftZ += +offsetZ;
                }
                else
                {
                    rightY = -Offsets.YOffset_Behind;
                    rightZ += offsetZ;
                }

                leftX += offsetX;
                rightX -= offsetX;
            }
            else
            {
                leftX = -rightX;
            }

            if (rot == Rot4.North)
            {
                leftY = rightY = -Offsets.YOffset_Behind;
                // leftX *= -1;
                // rightX *= -1;
            }

            joints.RightJoint = new Vector3(rightX, rightY, rightZ);
            joints.LeftJoint = new Vector3(leftX, leftY, leftZ);

            return joints;
        }

        public readonly Vector3Tween[] Vector3Tweens = new Vector3Tween[(int)TweenThing.Max];
        public readonly FloatTween[] FloatTweens = new FloatTween[(int)TweenThing.Max];
        [CanBeNull] public BodyAnimDef BodyAnim;
        public BodyPartStats BodyStat;
        public bool Deactivated;
        public Vector3 MainHandPosition;
        public bool IgnoreRenderer;
        public float JitterMax = 0.35f;
        //   [CanBeNull] public PawnPartsTweener PartTweener;

        [CanBeNull] public PawnBodyGraphic pawnBodyGraphic;
        public Vector3 lastMainHandPosition;

        //[CanBeNull] public PoseCycleDef PoseCycle;
        public Vector3 SecondHandPosition;

        public WalkCycleDef CurrentWalkCycle { get; private set; }

        public void DoWalkCycleOffsets(ref Vector3 rightFoot,
                    ref Vector3 leftFoot,
            ref float footAngleRight,
            ref float footAngleLeft,
            ref float offsetJoint,
            SimpleCurve offsetX,
            SimpleCurve offsetZ,
            SimpleCurve angle, float percent, Rot4 rot)
        {
            if (!this.IsMoving)
            {
                return;
            }
            float bodysizeScaling = GetBodysizeScaling();

            float percentInverse = percent;
            if (percentInverse <= 0.5f)
            {
                percentInverse += 0.5f;
            }
            else
            {
                percentInverse -= 0.5f;
            }

            WalkCycleDef currentCycle = this.CurrentWalkCycle;
            WalkCycleDef lastCycle = this.lastWalkCycle;
            float lerpFactor = 1f;

            if (currentCycle != lastCycle && lastCycle != null)
            {
                if (percent < animationFactorForSmoothing)
                {
                    lerpFactor = Mathf.InverseLerp(0f, animationFactorForSmoothing, percent);
                }
                else
                {
                    this.lastWalkCycle = currentCycle;
                }
            }

            float footAngleRightLerp = footAngleRight;
            float footAngleLeftLerp = footAngleLeft;
            float offsetJointLerp = offsetJoint;

            GetWalkCycleOffsetsFeet(out rightFoot, out leftFoot, ref footAngleRight, ref footAngleLeft, ref offsetJoint, offsetX, offsetZ, angle, percent, rot, percentInverse);

            if (lerpFactor < 1f)
            {
                GetWalkCycleOffsetsFeet(out Vector3 rightFootLerp, out Vector3 leftFootLerp, ref footAngleRightLerp,
                    ref footAngleLeftLerp, ref offsetJointLerp, offsetX, offsetZ, angle, percent, rot, percentInverse);

                rightFoot = Vector3.Lerp(rightFootLerp, rightFoot, lerpFactor);
                leftFoot = Vector3.Lerp(leftFootLerp, leftFoot, lerpFactor);
                footAngleRight = Mathf.Lerp(footAngleRightLerp, footAngleRight, lerpFactor);
                footAngleLeft = Mathf.Lerp(footAngleLeftLerp, footAngleLeft, lerpFactor);
                offsetJoint = Mathf.Lerp(offsetJointLerp, offsetJoint, lerpFactor);
            }

            // smaller steps for smaller pawns
            if (bodysizeScaling < 1f)
            {
                SimpleCurve curve = new() { new CurvePoint(0f, 0.5f), new CurvePoint(1f, 1f) };

                float mod = curve.Evaluate(bodysizeScaling);
                rightFoot.x *= mod;
                rightFoot.z *= mod;
                leftFoot.x *= mod;
                leftFoot.z *= mod;
            }
        }

        private void GetWalkCycleOffsetsFeet(out Vector3 rightFoot, out Vector3 leftFoot, ref float footAngleRight,
            ref float footAngleLeft, ref float offsetJoint, SimpleCurve offsetX, SimpleCurve offsetZ, SimpleCurve angle,
            float percent, Rot4 rot, float percentInverse)
        {
            rightFoot = Vector3.zero;
            leftFoot = Vector3.zero;

            if (rot.IsHorizontal)
            {
                rightFoot.x = offsetX.Evaluate(percent);
                leftFoot.x = offsetX.Evaluate(percentInverse);
                if (this.BodyStat.FootRight != PartStatus.Artificial)
                {
                    footAngleRight = angle.Evaluate(percent);
                }
                if (this.BodyStat.FootLeft != PartStatus.Artificial)
                {
                    footAngleLeft = angle.Evaluate(percentInverse);
                }
                rightFoot.z = offsetZ.Evaluate(percent);
                leftFoot.z  = offsetZ.Evaluate(percentInverse);

                rightFoot.x += offsetJoint;
                leftFoot.x  += offsetJoint;

                if (rot == Rot4.West)
                {
                    rightFoot.x    *= -1f;
                    leftFoot.x     *= -1f;
                    footAngleLeft  *= -1f;
                    footAngleRight *= -1f;
                    offsetJoint    *= -1;
                }
            }
            else
            {
                rightFoot.z = offsetZ.Evaluate(percent);
                leftFoot.z = offsetZ.Evaluate(percentInverse);
                offsetJoint = 0;
            }
        }

        public void DoWalkCycleOffsets(
        float armLength,
        ref Vector3 rightHand,
        ref Vector3 leftHand,
        ref float shoulderAngle,
        ref List<float> handSwingAngle,
        ref JointLister shoulderPos,
        float offsetJoint)
        {
            // Has the pawn something in his hands?

            float bodysizeScaling = GetBodysizeScaling();

            Rot4 rot = this.CurrentRotation;

            // Basic values if pawn is carrying stuff
            float x = 0;
            float x2 = -x;
            float y = Offsets.YOffset_HandsFeetOver;
            float y2 = y;
            float z;
            float z2;

            // Offsets for hands from the pawn center
            z = z2 = -armLength;

            if (rot.IsHorizontal)
            {
                x = x2 = 0f;
                if (rot == Rot4.East)
                {
                    y2 *= -1f;
                }
                else
                {
                    y *= -1f;
                }
            }
            else if (rot == Rot4.North)
            {
                y = y2 = -Offsets.YOffset_HandsFeetOver;
                x *= -1;
                x2 *= -1;
            }

            // Swing the hands, try complete the cycle
            if (this.IsMoving)
            {
                WalkCycleDef currentCycle = this.CurrentWalkCycle;
                WalkCycleDef lastCycle = this.lastWalkCycle;
                float percent = this.MovedPercent;
                float lerpFactor = 1f;

                if (currentCycle != lastCycle && lastCycle != null)
                {
                    if (percent < animationFactorForSmoothing)
                    {
                        lerpFactor = Mathf.InverseLerp(0f, animationFactorForSmoothing, percent);
                    }
                    else
                    {
                        this.lastWalkCycle = currentCycle;
                    }
                }

                JointLister lerpShoulderPos = shoulderPos;
                float lerpZ = z;
                float lerpZ2 = z2;

                GetWalkCycleOffsetsHands(handSwingAngle, offsetJoint, rot, currentCycle, percent, out shoulderAngle, ref shoulderPos, ref z, ref z2);

                if (lerpFactor < 1f)
                {
                    GetWalkCycleOffsetsHands(handSwingAngle, offsetJoint, rot, lastCycle, percent, out float lerpShoulderAngle, ref lerpShoulderPos, ref lerpZ, ref lerpZ2);

                    shoulderAngle = Mathf.Lerp(lerpShoulderAngle, shoulderAngle, lerpFactor);
                    shoulderPos.LeftJoint = Vector3.Lerp(lerpShoulderPos.LeftJoint, shoulderPos.LeftJoint, lerpFactor);
                    shoulderPos.RightJoint = Vector3.Lerp(lerpShoulderPos.RightJoint, shoulderPos.RightJoint, lerpFactor);
                    z = Mathf.Lerp(lerpZ, z, lerpFactor);
                    z2 = Mathf.Lerp(lerpZ2, z2, lerpFactor);
                }
            }

            if (/*MainTabWindow_BaseAnimator.Panic || */ this.pawn.Fleeing() || this.pawn.IsBurning())
            {
                float offset = 1f + armLength;
                x *= offset;
                z *= offset;
                x2 *= offset;
                z2 *= offset;
                handSwingAngle[0] += 180f;
                handSwingAngle[1] += 180f;
                shoulderAngle = 0f;
            }

            rightHand = new Vector3(x, y, z) * bodysizeScaling;
            leftHand = new Vector3(x2, y2, z2) * bodysizeScaling;

            lastMainHandPosition = rightHand;
        }

        private static void GetWalkCycleOffsetsHands(List<float> handSwingAngle, float offsetJoint,
            Rot4 rot, WalkCycleDef currentCycle, float percent, out float shoulderAngle, ref JointLister shoulderPos,
            ref float z, ref float z2)
        {
            if (rot.IsHorizontal)
            {
                float lookie = rot == Rot4.West ? -1f : 1f;
                float f = lookie * offsetJoint;

                shoulderAngle = lookie * currentCycle?.shoulderAngle ?? 0f;

                shoulderPos.RightJoint.x += f;
                shoulderPos.LeftJoint.x += f;

                handSwingAngle[0] = handSwingAngle[1] =
                    (rot == Rot4.West ? -1 : 1) * currentCycle.HandsSwingAngle.Evaluate(percent);
            }
            else
            {
                shoulderAngle = 0f;
                z += currentCycle.HandsSwingAngle.Evaluate(percent) / 500;
                z2 -= currentCycle.HandsSwingAngle.Evaluate(percent) / 500;

                z += currentCycle?.shoulderAngle / 800 ?? 0f;
                z2 += currentCycle?.shoulderAngle / 800 ?? 0f;
            }
        }

        #endregion Public Fields

        #region Private Fields

        private static readonly FieldInfo _infoJitterer;
        [NotNull] private readonly List<Material> _cachedNakedMatsBodyBase = new();
        private readonly List<Material> _cachedSkinMatsBodyBase = new();
        private int _cachedNakedMatsBodyBaseHash = -1;
        private int _cachedSkinMatsBodyBaseHash = -1;
        private Rot4? _currentRotationOverride = null;
        private bool _initialized;
        private int _lastRoomCheck;
        [CanBeNull] private Room _theRoom;

        #endregion Private Fields

        #region Public Properties

        /*
        public BodyAnimator BodyAnimator
        {
            get => _bodyAnimator;
            private set => _bodyAnimator = value;
        }
        */

        public JitterHandler Jitterer
            => GetHiddenValue(typeof(Pawn_DrawTracker), this.pawn.Drawer, "jitterer", _infoJitterer) as
                JitterHandler;

        [NotNull]
        public Pawn pawn => this.parent as Pawn;

        public CompProperties_BodyAnimator Props => (CompProperties_BodyAnimator)this.props;

        #endregion Public Properties

        #region Private Properties

        [CanBeNull]
        private Room TheRoom
        {
            get
            {
                if (this.pawn.Dead)
                {
                    return null;
                }

                if (Find.TickManager.TicksGame < this._lastRoomCheck + 60f)
                {
                    return this._theRoom;
                }

                this._theRoom = this.pawn.GetRoom();
                this._lastRoomCheck = Find.TickManager.TicksGame;

                return this._theRoom;
            }
        }

        #endregion Private Properties

        #region Public Methods

        public static object GetHiddenValue(Type type, object __instance, string fieldName, [CanBeNull] FieldInfo info)
        {
            if (info == null)
            {
                info = type.GetField(fieldName, GenGeneric.BindingFlagsAll);
            }

            return info?.GetValue(__instance);
        }

        public void ApplyBodyWobble(ref Vector3 rootLoc, ref Vector3 footPos)
        {
            if (this.BodyAnim == null)
            {
                return;
            }
            this.ModifyBodyAndFootPos(ref rootLoc, ref footPos);
            if (this.IsMoving)
            {
                WalkCycleDef walkCycle = this.CurrentWalkCycle;
                if (walkCycle != null)
                {
                    float bodysizeScaling = GetBodysizeScaling();
                    float bam = this.BodyOffsetZ * bodysizeScaling;

                    rootLoc.z += bam;
                    this.SetBodyAngle();

                    // Log.Message(CompFace.Pawn + " - " + this.movedPercent + " - " + bam.ToString());
                }
            }

            // Adds the leg length to the rootloc and relocates the feet to keep the pawn in center, e.g. for shields
        }

        // Verse.PawnGraphicSet
        public void ClearCache()
        {
            this._cachedSkinMatsBodyBaseHash = -1;
            this._cachedNakedMatsBodyBaseHash = -1;
        }

        public override string CompInspectStringExtra()
        {
            // if (this.CurrentWalkCycle == null || pawn.pather == null)
            {
                return base.CompInspectStringExtra();
            }
            float numbie = pawn.TicksPerMoveCardinal / pawn.pather.nextCellCostTotal;

            numbie *= Mathf.InverseLerp(100, -100, pawn.pather.nextCellCostTotal) * 2.25f;

            string text = "Walkcycle: " + this.CurrentWalkCycle.defName + ", MoveSpeed: " + numbie.ToString("N2") + "\nTicksPerMoveCardinal: " +
                          pawn.TicksPerMoveCardinal + " nextCellCostTotal: " + pawn.pather.nextCellCostTotal;
            return text;
            // var tween = Vector3Tweens[(int)TweenThing.Equipment];
            // var log = tween.State + " =>"+  tween.StartValue + " - " + tween.EndValue + " / " + tween.CurrentTime + " / " + tween.CurrentValue;
            // return log;
            //  return MoveState.ToString() + " - " + MovedPercent;

            //  return  lastAimAngle.ToString() ;

            return base.CompInspectStringExtra();
        }

        public void DrawFeet(float drawAngle, Vector3 rootLoc, Vector3 bodyLoc)
        {
            if (this.ShouldBeIgnored())
            {
                return;
            }

            /*
            /// No feet while sitting at a table
            Job curJob = this.pawn.CurJob;
            if (curJob != null)
            {
                if (curJob.def == JobDefOf.Ingest && !this.CurrentRotation.IsHorizontal)
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
            */
            // Color unused = this.FootColor;

            if (pawn.GetPosture() == PawnPosture.Standing) // keep the feet straight while standing, ignore the bodyQuat
            {
                drawAngle = 0f;
            }

            if (this.IsMoving)
            {
                // drawQuat *= Quaternion.AngleAxis(-pawn.Drawer.renderer.BodyAngle(), Vector3.up);
            }

            Rot4 rot = this.CurrentRotation;

            // Basic values
            BodyAnimDef body = this.BodyAnim;
            if (body == null)
            {
                return;
            }

            JointLister groundPos = this.GetJointPositions(JointType.Hip,
                                                           body.hipOffsets[rot.AsInt],
                                                           body.hipOffsets[Rot4.North.AsInt].x);

            Vector3 rightFootCycle = Vector3.zero;
            Vector3 leftFootCycle  = Vector3.zero;
            float footAngleRight   = 0;
            float footAngleLeft    = 0;
            float offsetJoint      = 0;
            WalkCycleDef cycle     = this.CurrentWalkCycle;

            if (this.IsMoving && cycle != null)
            {
                offsetJoint = cycle.HipOffsetHorizontalX.Evaluate(this.MovedPercent);
                this.DoWalkCycleOffsets(
                    ref rightFootCycle,
                    ref leftFootCycle,
                    ref footAngleRight,
                    ref footAngleLeft,
                    ref offsetJoint,
                    cycle.FootPositionX,
                    cycle.FootPositionZ,
                    cycle.FootAngle,
                    MovedPercent,
                    CurrentRotation);
            }
            float bodysizeScaling = GetBodysizeScaling();

            this.GetBipedMesh(out Mesh footMeshRight, out Mesh footMeshLeft);

            Material matRight;
            Material matLeft;
            /*
            if (MainTabWindow_BaseAnimator.Colored)
            {
                matRight = this.PawnBodyGraphic?.FootGraphicRightCol?.MatAt(rot);
                matLeft = this.PawnBodyGraphic?.FootGraphicLeftCol?.MatAt(rot);
            }
            else
            */
            (matRight, matLeft) = GetMaterialFor(this.pawnBodyGraphic?.FootGraphicRight, this.pawnBodyGraphic?.FootGraphicLeft, this.pawnBodyGraphic?.FootGraphicRightShadow, this.pawnBodyGraphic?.FootGraphicLeftShadow);

            bool drawRight = matRight != null && this.BodyStat.FootRight != PartStatus.Missing;
            bool drawLeft  = matLeft != null && this.BodyStat.FootLeft != PartStatus.Missing;

            groundPos.LeftJoint  = groundPos.LeftJoint.RotatedBy(drawAngle);
            groundPos.RightJoint = groundPos.RightJoint.RotatedBy(drawAngle);
            leftFootCycle        = leftFootCycle.RotatedBy(drawAngle);
            rightFootCycle       = rightFootCycle.RotatedBy(drawAngle);

            // rootLoc.y -= Offsets.YOffset_CarriedThing; // feet pop out in front of other pawns

            Vector3 ground = rootLoc + new Vector3(0, 0, OffsetGroundZ).RotatedBy(drawAngle) * bodysizeScaling;

            if (drawLeft)
            {
                // TweenThing leftFoot = TweenThing.FootLeft;
                // PawnPartsTweener tweener = this.PartTweener;
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
                // PawnPartsTweener tweener = this.PartTweener;
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

        private (Material matRight, Material matLeft) GetMaterialFor(Graphic graphicRight, Graphic graphicLeft,
            Graphic graphicRightShadow, Graphic graphicLeftShadow, bool carrying = false)
        {
            Rot4 rot = this.CurrentRotation;
            Material matRight;
            Material matLeft;
            if (carrying)
            {
                matRight = OverrideMaterialIfNeeded(graphicRight?.MatAt(rot));
                matLeft = OverrideMaterialIfNeeded(graphicLeft?.MatAt(rot));
            }
            else
            {
                switch (rot.AsInt)
                {
                    default:
                        matRight = OverrideMaterialIfNeeded(graphicRight?.MatAt(rot));
                        matLeft = OverrideMaterialIfNeeded(graphicLeft?.MatAt(rot));
                        break;

                    case 0:
                        matRight = OverrideMaterialIfNeeded(graphicRightShadow?.MatAt(rot));
                        matLeft = OverrideMaterialIfNeeded(graphicLeftShadow?.MatAt(rot));
                        break;

                    case 1:
                        matRight = OverrideMaterialIfNeeded(graphicRight?.MatAt(rot));
                        matLeft = OverrideMaterialIfNeeded(graphicLeftShadow?.MatAt(rot));
                        break;

                    case 3:

                        matRight = OverrideMaterialIfNeeded(graphicRightShadow?.MatAt(rot));
                        matLeft = OverrideMaterialIfNeeded(graphicLeft?.MatAt(rot));
                        break;
                }
            }

            return (matRight, matLeft);
        }

        // off for now
        public void DrawHands(float bodyAngle, Vector3 drawPos,
            Thing carriedThing = null, bool flip = false)
        {
            if (this.ShouldBeIgnored())
            {
                return;
            }

            BodyAnimDef body = this.BodyAnim;
            if (body == null)
            {
                return;
            }

            if (!this.BodyAnim.bipedWithHands)
            {
                return;
            }

            float bodySizeScaling = GetBodysizeScaling();

            this.MainHandPosition = this.SecondHandPosition = Vector3.zero;
            bool hasSecondWeapon = false;
            ThingWithComps eq = pawn?.equipment?.Primary;
            bool leftBehind = false;
            bool rightBehind = false;
            bool leftHandFlipped = false;
            bool rightHandFlipped = false;
            bool carriesWeaponOpenly = false;

            Job curJob = pawn?.CurJob;
            if (eq != null && curJob?.def != null && !curJob.def.neverShowWeapon)
            {
                Type baseType = pawn.Drawer.renderer.GetType();
                MethodInfo methodInfo = baseType.GetMethod("CarryWeaponOpenly", BindingFlags.NonPublic | BindingFlags.Instance);
                object result = methodInfo?.Invoke(pawn.Drawer.renderer, null);
                if (result != null && (bool)result)
                {
                    this.DoHandOffsetsOnWeapon(eq, out hasSecondWeapon, out leftBehind, out rightBehind, out rightHandFlipped, out leftHandFlipped);
                    carriesWeaponOpenly = true;
                }
            }

            bool carrying = carriedThing != null;
            bool isEating = false;

            if (curJob != null && carrying && !this.IsMoving && curJob.def == JobDefOf.Ingest && curJob.targetB.IsValid)
            {
                if (this.pawn.Position.DistanceTo(((IntVec3)curJob.targetB)) > 0.65f)
                {
                    isEating = true;
                    drawPos = pawn.Drawer.DrawPos;
                }
            }

            if (this.CurrentRotation != Rot4.North)
            {
                // drawPos.y += 0.03474903f;
            }
            if (carrying && !isEating)
            {
                if (this.CurrentRotation != Rot4.North)
                {
                    //drawPos.y += Offsets.YOffset_CarriedThing;
                    drawPos.z -= 0.1f * bodySizeScaling;
                }

                /*    this.DoHandOffsetsOnWeapon(carriedThing,
                        this.CurrentRotation == Rot4.West ? 217f : 143f, out _, out _,
                        out _);*/
            }
            else
            {
                // needed for the apparel layer with caching disabled; otherwise hands are drawn beneath the apparel
                // drawPos.y += 0.001f;
            }

            // return if hands already drawn on carrything

            Rot4 rot = this.CurrentRotation;
            bool isFacingNorth = rot == Rot4.North;

            float animationAngle = 0f;
            Vector3 animationPosOffset = Vector3.zero;

            if ((!carrying && !IsMoving || isEating))
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

                //this.FirstHandPosition += animationPosOffset.RotatedBy(animationAngle);
                //this.SecondHandPosition += animationPosOffset.RotatedBy(-animationAngle);
                poschanged = true;
            }

            JointLister shoulperPos = this.GetJointPositions(JointType.Shoulder,
                body.shoulderOffsets[rot.AsInt],
                body.shoulderOffsets[Rot4.North.AsInt].x,
                carrying && !isEating, this.pawn.ShowWeaponOpenly());

            Vector3 shoulperPosLeftJoint = shoulperPos.LeftJoint;
            Vector3 shoulperPosRightJoint = shoulperPos.RightJoint;
            Vector3 MainHandPosition = this.MainHandPosition;
            Vector3 offHandPosition = this.SecondHandPosition;
            float mainHandAngle = 0f;
            if (carrying && !isEating)
            {
                // this.ApplyEquipmentWobble(ref drawPos);

                Vector3 handVector = drawPos;
                // handVector.z += 0.2f; // hands too high on carriedthing - edit: looks good on smokeleaf joints

                //handVector.y += Offsets.YOffset_CarriedThing;
                // Arms too far away from body

                /*
                var props = carriedThing.def?.GetCompProperties<WhandCompProps>();
                if ( props != null)
                {
                    MainHandPosition = props.MainHand;
                    mainHandAngle = props.MainHandAngle + carriedThing.def.equippedAngleOffset;
                }
                else */
                if (!this.IsMoving)
                {
                    float f = this.CurrentRotation.IsHorizontal ? 1.5f : 0.65f;

                    while (Vector3.Distance(this.pawn.DrawPos, handVector) > body.armLength * bodySizeScaling * f)
                    {
                        float step = 0.025f;
                        handVector = Vector3.MoveTowards(handVector, this.pawn.DrawPos, step);
                    }

                    // carriedThing.DrawAt(drawPos, flip);
                    handVector.y = drawPos.y;

                    drawPos = handVector;

                    // DoAnimationHands(ref animationPosOffset, ref animationAngle);
                    animationPosOffset.y = 0f;
                    animationAngle *= 3.8f;
                    bodyAngle += animationAngle * 2;
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
            WalkCycleDef walkCycle = this.CurrentWalkCycle;
            //PoseCycleDef poseCycle = this.PoseCycle;

            if (!carrying || isEating)
            {
                float offsetJoint = walkCycle.ShoulderOffsetHorizontalX.Evaluate(this.MovedPercent);

                this.DoWalkCycleOffsets(
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

            (Material matRight, Material matLeft) = GetMaterialFor(this.pawnBodyGraphic?.HandGraphicRight, this.pawnBodyGraphic?.HandGraphicLeft, this.pawnBodyGraphic?.HandGraphicRightShadow, this.pawnBodyGraphic?.HandGraphicLeftShadow, carrying);

            /*if (MainTabWindow_BaseAnimator.Colored)
            {
                matLeft = this.PawnBodyGraphic?.HandGraphicLeftCol?.MatSingle;
                matRight = this.PawnBodyGraphic?.HandGraphicRightCol?.MatSingle;
            }
            else */

            if (isFacingNorth && !carrying && !carriesWeaponOpenly)
            {
                matLeft = this.LeftHandShadowMat;
                matRight = this.RightHandShadowMat;
            }

            if (pawn.IsInvisible())
            {
                matLeft = InvisibilityMatPool.GetInvisibleMat(matLeft);
                matRight = InvisibilityMatPool.GetInvisibleMat(matRight);
            }
            bool drawLeft = matLeft != null && this.BodyStat.HandLeft != PartStatus.Missing;
            bool drawRight = matRight != null && this.BodyStat.HandRight != PartStatus.Missing;

            //float shouldRotate = pawn.GetPosture() == PawnPosture.Standing ? 0f : 90f;

            bool pawnIsAiming = pawn.stances.curStance is Stance_Busy stance_Busy && !stance_Busy.neverAimWeapon &&
                                stance_Busy.focusTarg.IsValid;
            bool ignoreRight = false;
            ThingWithComps thingWithComps = this.pawn.equipment.Primary;

            if (drawRight)
            {
                Quaternion quat;
                Vector3 position;
                bool noTween = carrying || pawn.Aiming();
                

                if (this.IsMoving)
                {
                    if (!pawnIsAiming && carriesWeaponOpenly && (!hasSecondWeapon || offHandPosition != Vector3.zero))
                    {
                        if (thingWithComps != null)
                        {
                            ShowMeYourHandsMain.rightHandLocations[thingWithComps] = new Tuple<Vector3, float>(
                                GetRightHandPosition(bodyAngle, drawPos, shoulperPosRightJoint, rightHandVector,
                                    handSwingAngle, shoulderAngle, animationAngle, bodySizeScaling) - MainHandPosition, handSwingAngle[1]);
                            ignoreRight = true;
                        }
                    }
                    else
                    {
                        if (thingWithComps != null)
                        {
                            ShowMeYourHandsMain.rightHandLocations[thingWithComps] =
                                new Tuple<Vector3, float>(Vector3.zero, 0f);
                            ignoreRight = false;
                        }                }
                }
                else
                {
                    if (thingWithComps != null)
                    {
                        ShowMeYourHandsMain.rightHandLocations[thingWithComps] =
                            new Tuple<Vector3, float>(Vector3.zero, 0f);
                        ignoreRight = false;
                    }
                }

                if (MainHandPosition != Vector3.zero && !ignoreRight)
                {
                    quat = Quaternion.AngleAxis(this.MainHandAngle - 90f + mainHandAngle, Vector3.up);
                    position = MainHandPosition;
                    if (CurrentRotation == Rot4.West) // put the second hand behind while turning right
                    {
                        //   quat *= Quaternion.AngleAxis(180f, Vector3.up);
                    }
                    if (rightBehind)
                    {
                        matRight = this.RightHandShadowMat;
                    }

                    noTween = true;
                }
                else // standard
                {
                    position = GetRightHandPosition(bodyAngle, drawPos, shoulperPosRightJoint, rightHandVector, handSwingAngle, shoulderAngle, animationAngle, bodySizeScaling);
                    if (carrying && !isEating) // grabby angle
                    {
                        quat = Quaternion.AngleAxis(bodyAngle - 115f, Vector3.up);
                    }
                    else
                    {
                        quat = Quaternion.AngleAxis(bodyAngle + handSwingAngle[1] - shoulderAngle, Vector3.up);
                    }
                    quat *= Quaternion.AngleAxis(animationAngle * 1.25f, Vector3.up);

                    /*else if (CurrentRotation.IsHorizontal)
                    {
                        quat *= Quaternion.AngleAxis(CurrentRotation == Rot4.West ? +90f : -90f, Vector3.up);
                    }*/
                }

                TweenThing handRight = TweenThing.HandRight;
                //ToDo: tweening is too general, use it only for animation stuff and not for correcting positions
                noTween = true;
                this.DrawTweenedHand(position, handMeshRight, matRight, quat, handRight, noTween);
                // GenDraw.DrawMeshNowOrLater(
                //                            handMeshRight, position,
                //                            quat,
                //                            matRight,
                //                            portrait);
            }

            if (drawLeft)
            {
                bool ignoreLeft = false;
                List<ThingWithComps> listForReading = pawn.equipment.AllEquipmentListForReading;
                if (hasSecondWeapon && listForReading != null && listForReading.Count == 2)
                {
                    ThingWithComps thing = (from weapon in listForReading
                                            where pawn.equipment?.Primary != null && weapon != pawn.equipment.Primary
                                            select weapon).First();
                    if ( thing != null)
                    {
                        if (this.IsMoving && !pawnIsAiming && carriesWeaponOpenly)
                        {
                            ShowMeYourHandsMain.leftHandLocations[thing] = new Tuple<Vector3, float>(
                                GetLeftHandPosition(bodyAngle, drawPos, shoulperPosLeftJoint, leftHandVector,
                                    handSwingAngle, shoulderAngle, animationAngle, bodySizeScaling) - offHandPosition,
                                handSwingAngle[0]);
                            ignoreLeft = true;
                        }
                        else
                        {
                            ShowMeYourHandsMain.leftHandLocations[thing] = new Tuple<Vector3, float>(Vector3.zero, 0f);
                        }
                    }
                }

                Quaternion quat;
                Vector3 position = Vector3.zero;
                bool noTween = carrying || pawn.Aiming();
                if (this.BodyStat.HandRight == PartStatus.Missing &&
                    MainHandPosition != Vector3.zero && ignoreRight)
                {
                    quat = Quaternion.AngleAxis(this.MainHandAngle - 90f, Vector3.up);
                    position = MainHandPosition;
                    if (CurrentRotation == Rot4.West) // put the second hand behind while turning right
                    {
                        //  quat *= Quaternion.AngleAxis(180f, Vector3.up);
                    }
                    noTween = true;
                }
                else if ((hasSecondWeapon || offHandPosition != Vector3.zero && pawnIsAiming) && !ignoreLeft) // only draw he second hand on weapon while aiming
                {
                    position = offHandPosition;
                    quat = Quaternion.AngleAxis(this.OffHandAngle - 90f, Vector3.up);
                    if (CurrentRotation == Rot4.East) // put the second hand behind while turning right
                    {
                        // quat *= Quaternion.AngleAxis(180f, Vector3.up);
                    }

                    if (leftBehind)
                    {
                        matLeft = this.LeftHandShadowMat;
                    }
                    noTween = true;
                }
                else // standard
                {
                    position = GetLeftHandPosition(bodyAngle, drawPos, shoulperPosLeftJoint, leftHandVector, handSwingAngle, shoulderAngle, animationAngle, bodySizeScaling);
                    if (carriesWeaponOpenly && !pawnIsAiming && !this.CurrentRotation.IsHorizontal) // pawn has free left hand
                    {
                        position.y = ShowMeYourHandsMain.weaponLocations[thingWithComps].Item1.y - 0.01f;
                    }
                    if (carrying && !isEating) // grabby angle
                    {
                        quat = Quaternion.AngleAxis(bodyAngle + 105f, Vector3.up);
                    }
                    else
                    {
                        quat = Quaternion.AngleAxis(bodyAngle - handSwingAngle[0] - shoulderAngle, Vector3.up);
                    }
                    quat *= Quaternion.AngleAxis(animationAngle * 1.25f, Vector3.up);
                }

                TweenThing handLeft = TweenThing.HandLeft;

                //ToDo: tweening is too general, use it only for animation stuff and not for correcting positions
                noTween = true;
                this.DrawTweenedHand(position, handMeshLeft, matLeft, quat, handLeft, noTween);
                //GenDraw.DrawMeshNowOrLater(
                //                           handMeshLeft, position,
                //                           quat,
                //                           matLeft,
                //                           portrait);
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

        private static Vector3 GetRightHandPosition(float bodyAngle, Vector3 drawPos, Vector3 shoulperPosRightJoint,
            Vector3 rightHandVector, List<float> handSwingAngle, float shoulderAngle, float animationAngle, float bodySizeScaling)
        {
            Vector3 position;
            shoulperPosRightJoint = shoulperPosRightJoint.RotatedBy(bodyAngle);
            rightHandVector = rightHandVector.RotatedBy(bodyAngle + handSwingAngle[1] - shoulderAngle + animationAngle);
            position = drawPos + (shoulperPosRightJoint + rightHandVector) * bodySizeScaling;
            return position;
        }

        private static Vector3 GetLeftHandPosition(float bodyAngle, Vector3 drawPos, Vector3 shoulperPosLeftJoint, Vector3 leftHandVector,
            List<float> handSwingAngle, float shoulderAngle, float animationAngle, float bodySizeScaling)
        {
            Vector3 position;
            shoulperPosLeftJoint = shoulperPosLeftJoint.RotatedBy(bodyAngle);
            leftHandVector = leftHandVector.RotatedBy(bodyAngle - handSwingAngle[0] - shoulderAngle + animationAngle);
            position = drawPos + (shoulperPosLeftJoint + leftHandVector) * bodySizeScaling;
            return position;
        }

        // public override string CompInspectStringExtra()
        // {
        //     string extra = this.Pawn.DrawPos.ToString();
        //     return extra;
        // }
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

            if (Find.TickManager.TicksGame == this.LastPosUpdate[(int)tweenThing])
            {
                position = this.LastPosition[(int)tweenThing];
            }
            else
            {
                Pawn_PathFollower pawnPathFollower = this.pawn.pather;
                if (pawnPathFollower != null && pawnPathFollower.MovedRecently(5))
                {
                    noTween = true;
                }

                this.LastPosUpdate[(int)tweenThing] = Find.TickManager.TicksGame;

                Vector3Tween tween = this.Vector3Tweens[(int)tweenThing];
                Vector3 start = this.LastPosition[(int)tweenThing];
                start.y = position.y;
                float distance = Vector3.Distance(start, position);

                switch (tween.State)
                {
                    case TweenState.Running:
                        if (noTween || this.IsMoving || distance > 1f)
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
                        if (noTween || this.IsMoving)
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

                this.LastPosition[(int)tweenThing] = position;
            }

            //  tweener.PreThingPosCalculation(tweenThing, noTween);

            Graphics.DrawMesh(handsMesh, position, quat, material, 0);
        }

        public bool ShouldBeIgnored()
        {
            return this.pawn.Dead || !this.pawn.Spawned || this.pawn.InContainerEnclosed;
        }

        public void ModifyBodyAndFootPos(ref Vector3 rootLoc, ref Vector3 footPos)
        {
            float bodysizeScaling = GetBodysizeScaling();
            //float bodysizeScaling = Mathf.Max(GetBodysizeScaling(), 0.5f);
            float legModifier = this.BodyAnim.extraLegLength * bodysizeScaling;
            float posModB = legModifier * 0.75f;
            float posModF = -legModifier * 0.25f;
            Vector3 vector3 = new(0, 0, posModB);
            Vector3 vector4 = new(0, 0, posModF);

            // No rotation when moving
            if (this.IsMoving)
            {
                vector3 = vector3.RotatedBy(BodyAngle);
                vector4 = vector4.RotatedBy(BodyAngle);
            }

            if (!this.IsRider)
            {
            }

            rootLoc += vector3;
            footPos += vector4;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            //   Scribe_Values.Look(ref this._lastRoomCheck, "lastRoomCheck");
            // Scribe_Values.Look(ref this.PawnBodyGraphic, "PawnBodyGraphic");
        }

        public const float OffsetGroundZ = -0.575f;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            for (int i = 0; i < this.Vector3Tweens.Length; i++)
            {
                this.Vector3Tweens[i] = new Vector3Tween();
            }
            for (int i = 0; i < this.FloatTweens.Length; i++)
            {
                this.FloatTweens[i] = new FloatTween();
            }

            string bodyType = "Undefined";

            if (this.pawn.story?.bodyType != null)
            {
                bodyType = this.pawn.story.bodyType.ToString();
            }

            string defaultName = "BodyAnimDef_" + this.pawn.def.defName + "_" + bodyType;
            List<string> names = new()
            {
                defaultName,
                // "BodyAnimDef_" + ThingDefOf.Human.defName + "_" + bodyType
            };

            bool needsNewDef = true;
            foreach (string name in names)
            {
                BodyAnimDef dbDef = DefDatabase<BodyAnimDef>.GetNamedSilentFail(name);
                if (dbDef == null)
                {
                    continue;
                }

                this.BodyAnim = dbDef;
                needsNewDef = false;
                break;
            }

            if (needsNewDef)
            {
                this.BodyAnim = new BodyAnimDef { defName = defaultName, label = defaultName };
                DefDatabase<BodyAnimDef>.Add(this.BodyAnim);
            }

            //this.BodyAnimator = new BodyAnimator(this.pawn, this);
            this.pawnBodyGraphic = new PawnBodyGraphic(this);
        }

        public void SetBodyAngle()
        {
            WalkCycleDef walkCycle = this.CurrentWalkCycle;
            if (walkCycle == null)
            {
                return;
            }

            float movedPercent = this.MovedPercent;

            Rot4 currentRotation = this.CurrentRotation;
            if (currentRotation.IsHorizontal)
            {
                if (walkCycle.BodyAngle.PointsCount > 0)
                {
                    this.BodyAngle = (currentRotation == Rot4.West ? -1 : 1)
                                     * walkCycle.BodyAngle.Evaluate(movedPercent);
                }
            }
            else
            {
                if (walkCycle.BodyAngleVertical.PointsCount > 0)
                {
                    this.BodyAngle = (currentRotation == Rot4.South ? -1 : 1)
                                 * walkCycle.BodyAngleVertical.Evaluate(movedPercent);
                }
            }
        }

        public void SelectWalkcycle(bool pawnInEditor)
        {
            if (pawn.pather == null || Math.Abs(pawn.pather.nextCellCostTotal - this.currentCellCostTotal) == 0f)
            {
                return;
            }

            if (pawn.RaceProps.Animal)
            {
                if (BodyAnim != null)
                {
                    this.SetWalkCycle(BodyAnim.walkCycles.FirstOrDefault().Value);
                }

                return;
            }
            /*
            if (pawnInEditor)
            {
                this.SetWalkCycle(Find.WindowStack.WindowOfType<MainTabWindow_WalkAnimator>().EditorWalkcycle);
                return;
            }
            */

            // Define the walkcycle by the actual move speed of the pawn instead of the urgency.
            // Faster pawns use faster cycles, this avoids slow.mo pawns.

            this.currentCellCostTotal = pawn.pather.nextCellCostTotal;

            Dictionary<LocomotionUrgency, WalkCycleDef> cycles = BodyAnim?.walkCycles;

            if (cycles.NullOrEmpty())
            {
                return;
            }

            LocomotionUrgency locomotionUrgency = LocomotionUrgency.Walk;

            int moves = pawn.pather.nextCell.z == pawn.Position.z ? pawn.TicksPerMoveCardinal : pawn.TicksPerMoveDiagonal;

            float pawnSpeedPerTick = moves / currentCellCostTotal; //
            pawnSpeedPerTick *= Mathf.InverseLerp(100, -100, currentCellCostTotal) * 2.25f;

            if (pawnSpeedPerTick > 1f)
            {
                locomotionUrgency = LocomotionUrgency.Sprint;
            }
            else if (pawnSpeedPerTick > 0.75f)
            {
                locomotionUrgency = LocomotionUrgency.Jog;
            }
            else if (pawnSpeedPerTick > 0.5f)
            {
                locomotionUrgency = LocomotionUrgency.Walk;
            }
            else
            {
                locomotionUrgency = LocomotionUrgency.Amble;
            }
            if (false)
            {
                if (this.sustainer is { Ended: false })
                {
                    float number = Mathf.Lerp(1.1f, 3f, pawnSpeedPerTick);

                    if (Find.Selector.SelectedPawns.Contains(pawn))
                    {
                        // if (Find.Selector.SelectedPawns.FirstOrDefault() == pawn)
                        {
                            Log.Message(pawn + " - " + pawnSpeedPerTick + " - " + number);
                        }

                        List<SubSoundDef> subSounds = this.sustainer?.def?.subSounds;
                        if (!subSounds.NullOrEmpty())
                        {
                            subSounds.FirstOrDefault().pitchRange =
                                new FloatRange(number, number);
                        }
                    }
                }
            }
            float rangeAmble = 0f;
            float rangeWalk = 0.45f;
            float rangeJog = 0.65f;
            float rangeSprint = 0.85f;
            this.SetWalkCycle(BodyAnim.walkCycles.FirstOrDefault().Value);

            if (cycles.TryGetValue(locomotionUrgency, out WalkCycleDef cycle))
            {
                if (cycle != null)
                {
                    this.SetWalkCycle(cycle);
                }
            }
            else
            {
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            this.SelectWalkcycle(false);
        }

        public Material LeftHandShadowMat => OverrideMaterialIfNeeded(this.pawnBodyGraphic
            ?.HandGraphicLeftShadow?.MatSingle);

        public Material RightHandShadowMat => OverrideMaterialIfNeeded(this.pawnBodyGraphic
            ?.HandGraphicRightShadow?.MatSingle);

        #endregion Public Methods

        //  public float lastWeaponAngle = 53f;
        public readonly Vector3[] LastPosition = new Vector3[(int)TweenThing.Max];

        public float BodyAngle = 0f;

        public float DrawOffsetY;

        public float MainHandAngle;

        public Color FootColorLeft;

        public Color FootColorRight;

        public Color HandColorLeft;

        public Color HandColorRight;

        public bool IsMoving;

        public bool IsRider = false;

        public readonly float[] LastAimAngle = new float[(int)TweenThing.Max];

        public Vector3 LastEqPos = Vector3.zero;

        public void ApplyEquipmentWobble(ref Vector3 rootLoc)
        {
            if (this.IsMoving)
            {
                WalkCycleDef walkCycle = this.CurrentWalkCycle;
                if (walkCycle != null)
                {
                    float bam = this.BodyOffsetZ;
                    rootLoc.z += bam;

                    // Log.Message(CompFace.Pawn + " - " + this.movedPercent + " - " + bam.ToString());
                }
            }

            return;
            // cannot move root pos so the stuff below not working
        }

        private Material OverrideMaterialIfNeeded(Material original, bool portrait = false)
        {
            Material baseMat = ((!portrait && this.pawn.IsInvisible()) ? InvisibilityMatPool.GetInvisibleMat(original) : original);
            return this.pawn.Drawer.renderer.graphics.flasher.GetDamagedMat(baseMat);
        }

        public enum aniType
        { none, doSomeThing, social, smash, idle, gameCeremony, crowd, solemn }

        public void DoAnimationHands(ref Vector3 posOffset, ref float animationAngle)
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
            Rot4 rot = CurrentRotation;
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
                    {
                        if (!PawnExtensions.Ani(ref tick, 25, ref angle, 35f, 25f, -1f, ref pos, rot))
                        {
                            if (!PawnExtensions.Ani(ref tick, 25, ref angle, 25f, 35f, -1f, ref pos, rot))
                            {
                                if (!PawnExtensions.Ani(ref tick, 25, ref angle, 35f, 25f, -1f, ref pos, rot))
                                {
                                    if (!PawnExtensions.Ani(ref tick, 25, ref angle, 25f, 35f, -1f, ref pos, rot))
                                    {
                                        if (!PawnExtensions.Ani(ref tick, 25, ref angle, 35f, 25f, -1f, ref pos, rot))
                                        {
                                            if (!PawnExtensions.Ani(ref tick, 25, ref angle, 25f, 35f, -1f, ref pos, rot))
                                            {
                                                if (!PawnExtensions.Ani(ref tick, 25, ref angle, 35f, 15f, -1f, ref pos, rot))
                                                {
                                                    ;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

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
                        {
                            if (!PawnExtensions.Ani(ref tick, 50, ref angle, 5f, 10f, -1f, ref pos, new Vector3(0.05f, 0f, 0f), new Vector3(0.05f, 0f, 0f), rot))
                            {
                                if (!PawnExtensions.Ani(ref tick, 50, ref angle, 10, 10f, -1f, ref pos, new Vector3(0.05f, 0f, 0f), new Vector3(0.05f, 0f, 0f), rot))
                                {
                                    if (!PawnExtensions.Ani(ref tick, 40, ref angle, 10f, 0f, -1f, ref pos, new Vector3(0.05f, 0f, 0f), Vector3.zero, rot))
                                    {
                                        ;
                                    }
                                }
                            }
                        }
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
                        if (!PawnExtensions.Ani(ref tick, 30, ref angle, -20f, 10f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot))
                        {
                            ;
                        }
                    }
                    break;

                case "Play_Horseshoes":
                    tick = (Find.TickManager.TicksGame + IdTick) % 60;
                    if (!PawnExtensions.Ani(ref tick, 30, ref angle, 10f, -20f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot))
                    {
                        if (!PawnExtensions.Ani(ref tick, 30, ref angle, -20f, 10f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot))
                        {
                            ;
                        }
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
                    {
                        if (!PawnExtensions.Ani(ref tick, 5, ref angle, 0f, 10f, -1f, ref pos, rot))
                        {
                            if (!PawnExtensions.Ani(ref tick, 10, ref angle, 10f, 0f, -1f, ref pos, rot))
                            {
                                ;
                            }
                        }
                    }

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
                    {
                        if (!PawnExtensions.Ani(ref tick, 10, ref angle, 15f, 0f, -1f, ref pos, Vector3.zero, new Vector3(0f, 0f, 0f), rot))
                        {
                            if (!PawnExtensions.Ani(ref tick, 10, ref angle, 0f, 0f, -1f, ref pos, Vector3.zero, new Vector3(0f, 0f, f), rot))
                            {
                                if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, f), new Vector3(0f, 0f, -f), rot))
                                {
                                    if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, -f), new Vector3(0f, 0f, f), rot))
                                    {
                                        if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, f), new Vector3(0f, 0f, -f), rot))
                                        {
                                            if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, -f), new Vector3(0f, 0f, f), rot))
                                            {
                                                if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, f), new Vector3(0f, 0f, -f), rot))
                                                {
                                                    if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, -f), new Vector3(0f, 0f, f), rot))
                                                    {
                                                        ;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    break;
            }

            switch (aniType)
            {
                case aniType.solemn:
                    tick = (Find.TickManager.TicksGame + (IdTick % 25)) % 660;

                    if (!PawnExtensions.AnimationHasTicksLeft(ref tick, 300))
                    {
                        if (!PawnExtensions.Ani(ref tick, 30, ref angle, 0f, 15f, -1f, ref pos, Vector3.zero, Vector3.zero, rot))
                        {
                            if (!PawnExtensions.Ani(ref tick, 300, ref angle, 15f, 15f, -1f, ref pos, Vector3.zero, Vector3.zero, rot))
                            {
                                if (!PawnExtensions.Ani(ref tick, 30, ref angle, 15f, 0f, -1f, ref pos, Vector3.zero, Vector3.zero, rot))
                                {
                                    ;
                                }
                            }
                        }
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
                        {
                            if (!PawnExtensions.Ani(ref tick, 20, ref angle, 10f, 10f, -1f, ref pos, r))
                            {
                                if (!PawnExtensions.Ani(ref tick, 5, ref angle, 10f, -10f, -1f, ref pos, r))
                                {
                                    if (!PawnExtensions.Ani(ref tick, 20, ref angle, -10f, -10f, -1f, ref pos, r))
                                    {
                                        if (!PawnExtensions.Ani(ref tick, 5, ref angle, -10f, 0f, -1f, ref pos, r))
                                        {
                                            tr = t2 >= total ? PawnExtensions.Rot90(rot) : PawnExtensions.Rot90b(rot);
                                            if (!PawnExtensions.Ani(ref tick, 15, ref angle, 0f, 0f, -1f, ref pos, rot)) // 85
                                            {
                                                tr = rot;
                                                if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, rot)) // 105

                                                {
                                                    if (t2 >= total)
                                                    {
                                                        if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0.60f), rot))
                                                        {
                                                            if (!PawnExtensions.Ani(ref tick, 13, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0.60f), new Vector3(0f, 0f, 0f), rot, PawnExtensions.tweenType.line))
                                                            {
                                                                ;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (!PawnExtensions.AnimationHasTicksLeft(ref tick, 33))
                                                        {
                                                            ;
                                                        }
                                                    }
                                                }
                                            }
                                        }
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

                        {
                            if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0.60f), rot))
                            {
                                if (!PawnExtensions.Ani(ref tick, 13, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0.60f), new Vector3(0f, 0f, 0f), rot, PawnExtensions.tweenType.line))
                                {
                                    rot = PawnExtensions.Rot90b(rot);
                                    if (!PawnExtensions.Ani(ref tick, 10, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot))
                                    {
                                        rot = PawnExtensions.Rot90b(rot);
                                        if (!PawnExtensions.Ani(ref tick, 10, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot))

                                        {
                                            if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0.60f), rot))
                                            {
                                                if (!PawnExtensions.Ani(ref tick, 13, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0.60f), new Vector3(0f, 0f, 0f), rot, PawnExtensions.tweenType.line))
                                                {
                                                    if (!PawnExtensions.Ani(ref tick, 10, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot))
                                                    {
                                                        rot = PawnExtensions.Rot90b(rot);
                                                        if (!PawnExtensions.Ani(ref tick, 10, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot))
                                                        {
                                                            rot = PawnExtensions.Rot90b(rot);
                                                            if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot))
                                                            {
                                                                ;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
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
                    {
                        if (!PawnExtensions.Ani(ref tick, 25, ref angle, 0f, f, -1f, ref pos, r))
                        {
                            if (!PawnExtensions.Ani(ref tick, 50, ref angle, f, -f, -1f, ref pos, r))
                            {
                                if (!PawnExtensions.Ani(ref tick, 50, ref angle, -f, f, -1f, ref pos, r))
                                {
                                    if (!PawnExtensions.Ani(ref tick, 50, ref angle, f, -f, -1f, ref pos, r))
                                    {
                                        if (!PawnExtensions.Ani(ref tick, 50, ref angle, -f, f, -1f, ref pos, r))
                                        {
                                            if (!PawnExtensions.Ani(ref tick, 50, ref angle, f, -f, -1f, ref pos, r))
                                            {
                                                if (!PawnExtensions.Ani(ref tick, 25, ref angle, -f, 0f, -1f, ref pos, r))
                                                {
                                                    ;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    break;

                case aniType.smash:
                    tick = (Find.TickManager.TicksGame + IdTick) % 133;

                    if (!PawnExtensions.Ani(ref tick, 70, ref angle, 0f, -20f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot))
                    {
                        if (!PawnExtensions.Ani(ref tick, 3, ref angle, -20f, 10f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot, PawnExtensions.tweenType.line))
                        {
                            if (!PawnExtensions.Ani(ref tick, 20, ref angle, 10f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot))
                            {
                                if (!PawnExtensions.Ani(ref tick, 40, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), rot))
                                {
                                    ;
                                }
                            }
                        }
                    }
                    break;

                case aniType.doSomeThing:
                    total = 121;
                    t2 = (Find.TickManager.TicksGame + IdTick) % (total * 2);
                    tick = t2 % total;
                    r = PawnExtensions.Rot90(rot);
                    tr = rot;
                    if (!PawnExtensions.AnimationHasTicksLeft(ref tick, 20))
                    {
                        if (!PawnExtensions.Ani(ref tick, 5, ref angle, 0f, 10f, -1f, ref pos, r))
                        {
                            if (!PawnExtensions.Ani(ref tick, 20, ref angle, 10f, 10f, -1f, ref pos, r))
                            {
                                if (!PawnExtensions.Ani(ref tick, 5, ref angle, 10f, -10f, -1f, ref pos, r))
                                {
                                    if (!PawnExtensions.Ani(ref tick, 20, ref angle, -10f, -10f, -1f, ref pos, r))
                                    {
                                        if (!PawnExtensions.Ani(ref tick, 5, ref angle, -10f, 0f, -1f, ref pos, r))
                                        {
                                            //tr = t2 >= total ? PawnExtensions.Rot90(rot) : PawnExtensions.Rot90b(rot);
                                            if (!PawnExtensions.Ani(ref tick, 15, ref angle, 0f, 0f, -1f, ref pos, rot)) // 85
                                            {
                                                //tr = rot;
                                                if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, rot)) // 105
                                                {
                                                    if (!PawnExtensions.Ani(ref tick, 5, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0.05f), rot))
                                                    {
                                                        if (!PawnExtensions.Ani(ref tick, 6, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0.05f), new Vector3(0f, 0f, 0f), rot))
                                                        {
                                                            ;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
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
                    {
                        if (!PawnExtensions.Ani(ref tick, 5, ref angle, 0f, 10f, -1f, ref pos, r))
                        {
                            if (!PawnExtensions.Ani(ref tick, 20, ref angle, 10f, 10f, -1f, ref pos, r))
                            {
                                if (!PawnExtensions.Ani(ref tick, 5, ref angle, 10f, -10f, -1f, ref pos, r))
                                {
                                    if (!PawnExtensions.Ani(ref tick, 20, ref angle, -10f, -10f, -1f, ref pos, r))
                                    {
                                        if (!PawnExtensions.Ani(ref tick, 5, ref angle, -10f, 0f, -1f, ref pos, r))
                                        {
                                            tr = t2 >= total ? PawnExtensions.Rot90(rot) : PawnExtensions.Rot90b(rot);
                                            if (!PawnExtensions.Ani(ref tick, 15, ref angle, 0f, 0f, -1f, ref pos, rot)) // 85
                                            {
                                                tr = rot;
                                                if (!PawnExtensions.Ani(ref tick, 20, ref angle, 0f, 0f, -1f, ref pos, rot)) // 105
                                                {
                                                    if (!PawnExtensions.Ani(ref tick, 5, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0.05f), rot))
                                                    {
                                                        if (!PawnExtensions.Ani(ref tick, 6, ref angle, 0f, 0f, -1f, ref pos, new Vector3(0f, 0f, 0.05f), new Vector3(0f, 0f, 0f), rot))

                                                        {
                                                            if (!PawnExtensions.Ani(ref tick, 35, ref angle, 0f, 0f, -1f, ref pos, rot))
                                                            {
                                                                if (!PawnExtensions.Ani(ref tick, 10, ref angle, 0f, 10f, -1f, ref pos, rot))
                                                                {
                                                                    if (!PawnExtensions.Ani(ref tick, 10, ref angle, 10f, 0f, -1f, ref pos, rot))
                                                                    {
                                                                        if (!PawnExtensions.Ani(ref tick, 10, ref angle, 0f, 10f, -1f, ref pos, rot))
                                                                        {
                                                                            if (!PawnExtensions.Ani(ref tick, 10, ref angle, 10f, 0f, -1f, ref pos, rot))
                                                                            {
                                                                                if (!PawnExtensions.Ani(ref tick, 25, ref angle, 0f, 0f, -1f, ref pos, rot))
                                                                                {
                                                                                    ;
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
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

        public float currentCellCostTotal = 0;

        public float Offset_Angle = 0f;

        public Vector3 Offset_Pos = Vector3.zero;

        public Mesh pawnBodyMesh;

        public Mesh pawnBodyMeshFlipped;

        public float OffHandAngle;

        internal readonly int[] LastPosUpdate = new int[(int)TweenThing.Max];

        internal int LastAngleTick;

        internal float LastWeaponAngle;

        internal bool MeshFlipped;

        private static int LastDrawn;

        private static Vector3 MainHand;

        private static Vector3 OffHand;

        private float _movedPercent;

        //private BodyAnimator _bodyAnimator;

        public float BodyOffsetZ
        {
            get
            {
                if (ShowMeYourHandsMod.instance.Settings.UseFeet)
                {
                    SimpleCurve curve = this.CurrentWalkCycle.BodyOffsetZ;
                    if (curve.PointsCount > 0)
                    {
                        return curve.Evaluate(this.MovedPercent);
                    }
                }

                return 0f;
            }
        }

        public Rot4 CurrentRotation
        {
            get => _currentRotationOverride ?? pawn.Rotation;
            set => _currentRotationOverride = value;
        }

        // public readonly FloatTween AimAngleTween = new();
        public bool HasLeftHandPosition => this.SecondHandPosition != Vector3.zero;

        // unused since 1.3
        public float CurrentHeadAngle
        {
            get
            {
                if (ShowMeYourHandsMod.instance.Settings.UseFeet)
                {
                    WalkCycleDef walkCycle = this.CurrentWalkCycle;
                    if (walkCycle != null)
                    {
                        SimpleCurve curve = this.CurrentRotation.IsHorizontal ? walkCycle.HeadAngleX : walkCycle.HeadOffsetZ;
                        if (curve.PointsCount > 0)
                        {
                            return curve.Evaluate(this.MovedPercent);
                        }
                    }
                }

                return 0f;
            }
        }

        public float HeadAngleX
        {
            get
            {
                if (ShowMeYourHandsMod.instance.Settings.UseFeet)
                {
                    WalkCycleDef walkCycle = this.CurrentWalkCycle;
                    if (walkCycle != null)
                    {
                        SimpleCurve curve = walkCycle.HeadAngleX;
                        if (curve.PointsCount > 0)
                        {
                            return curve.Evaluate(this.MovedPercent);
                        }
                    }
                }

                return 0f;
            }
        }

        public float HeadffsetZ
        {
            get
            {
                if (ShowMeYourHandsMod.instance.Settings.UseFeet)
                {
                    WalkCycleDef walkCycle = this.CurrentWalkCycle;
                    if (walkCycle != null)
                    {
                        SimpleCurve curve = walkCycle.HeadOffsetZ;
                        if (curve.PointsCount > 0)
                        {
                            return curve.Evaluate(this.MovedPercent);
                        }
                    }
                }

                return 0f;
            }
        }

        public float MovedPercent
        {
            get => _movedPercent;
            private set => _movedPercent = value;
        }

        public void CalculatePositionsWeapon(WhandCompProps extensions,
                                             out Vector3 weaponPosOffset, bool flipped)
        {
            weaponPosOffset = Vector3.zero;

            if (flipped)
            {
                weaponPosOffset.y = -Offsets.YOffset_Head - Offsets.YOffset_CarriedThing;
            }

            // Use y for the horizontal position. too lazy to add even more vectors
            bool isHorizontal = this.CurrentRotation.IsHorizontal || flipped;

            bool aiming = pawn.Aiming();
            Vector3 weaponPositionOffset = extensions.WeaponPositionOffset;
            Vector3 aimedWeaponPositionOffset = extensions.AimedWeaponPositionOffset;

            if (isHorizontal)
            {
                weaponPosOffset = new Vector3(weaponPositionOffset.y, 0, weaponPositionOffset.z);
                if (aiming)
                {
                    weaponPosOffset += new Vector3(aimedWeaponPositionOffset.y, 0, aimedWeaponPositionOffset.z);
                }
            }
            else
            {
                weaponPosOffset = new Vector3(weaponPositionOffset.x, 0, weaponPositionOffset.z);
                if (aiming)
                {
                    weaponPosOffset += new Vector3(aimedWeaponPositionOffset.x, 0, aimedWeaponPositionOffset.z);
                }
            }

            if (flipped)
            {
                // flip x position offset
                // if (pawn.Rotation != Rot4.South)
                {
                    weaponPosOffset.x *= -1;
                }
            }
            float bodySizeScaling = this.GetBodysizeScaling();
            weaponPosOffset *= bodySizeScaling * BodyAnim.extremitySize;
        }

        public void CheckMovement()
        {
            /*
            if (HarmonyPatchesFS.AnimatorIsOpen() && MainTabWindow_BaseAnimator.pawn == this.pawn)
            {
                this.IsMoving = true;
                this.MovedPercent = MainTabWindow_BaseAnimator.AnimationPercent;
                return;
            }
            */
            if (this.IsRider)
            {
                this.IsMoving = false;
                return;
            }
            // pawn started pathing

            this.MovedPercent = PawnMovedPercent(pawn);
        }

        public void DoHandOffsetsOnWeapon(ThingWithComps eq, out bool hasSecondWeapon, out bool leftBehind,
            out bool rightBehind, out bool mainHandFlipped, out bool offHandFlipped)
        {
            leftBehind = rightBehind = mainHandFlipped = offHandFlipped = false;
            hasSecondWeapon = false;
            WhandCompProps extensions = eq?.def?.GetCompProperties<WhandCompProps>();

            Pawn ___pawn = this.pawn;

            ThingWithComps mainHandWeapon = eq;

            if (___pawn == null || extensions == null || mainHandWeapon == null)
            {
                return;
            }

            // Prepare everything for DrawHands, but don't draw

            if (!ShowMeYourHandsMain.weaponLocations.ContainsKey(mainHandWeapon))
            {
                if (ShowMeYourHandsMod.instance.Settings.VerboseLogging)
                {
                    Log.ErrorOnce(
                        $"[ShowMeYourHands]: Could not find the position for {mainHandWeapon.def.label} from the mod {mainHandWeapon.def.modContentPack.Name}, equipped by {___pawn.Name}. Please report this issue to the author of Show Me Your Hands if possible.",
                        mainHandWeapon.def.GetHashCode());
                }

                return;
            }
            WhandCompProps compProperties = mainHandWeapon?.def?.GetCompProperties<WhandCompProps>();
            if (compProperties != null)
            {
                MainHand = compProperties.MainHand;
                OffHand = compProperties.SecHand;
            }
            else
            {
                OffHand = Vector3.zero;
                MainHand = Vector3.zero;
            }

            ThingWithComps offHandWeapon = null;
            WhandCompProps offhandComp = null;

            if (___pawn?.equipment?.AllEquipmentListForReading != null && ___pawn.equipment.AllEquipmentListForReading.Count == 2)
            {
                offHandWeapon = (from weapon in ___pawn.equipment.AllEquipmentListForReading
                                 where weapon != mainHandWeapon
                                 select weapon).First();
                offhandComp = offHandWeapon?.def?.GetCompProperties<WhandCompProps>();

                if (offhandComp != null)
                {
                    OffHand = offhandComp.MainHand;
                    hasSecondWeapon = true;
                }
            }

            bool aiming = false;

            Stance_Busy stance_Busy = ___pawn.stances.curStance as Stance_Busy;
            // if (pawn.Aiming())
            // {
            //     Vector3 a = stance_Busy.focusTarg.HasThing
            //         ? stance_Busy.focusTarg.Thing.DrawPos
            //         : stance_Busy.focusTarg.Cell.ToVector3Shifted();
            //
            //     // if ((a - ___pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
            //     // {
            //     //     aimAngle = (a - ___pawn.DrawPos).AngleFlat();
            //     // }
            //
            //     aiming = true;
            // }

            if (!ShowMeYourHandsMain.weaponLocations.ContainsKey(mainHandWeapon))
            {
                if (ShowMeYourHandsMod.instance.Settings.VerboseLogging)
                {
                    Log.ErrorOnce(
                        $"[ShowMeYourHands]: Could not find the position for {mainHandWeapon.def.label} from the mod {mainHandWeapon.def.modContentPack.Name}, equipped by {___pawn.Name}. Please report this issue to the author of Show Me Your Hands if possible.",
                        mainHandWeapon.def.GetHashCode());
                }

                return;
            }

            Vector3 mainWeaponLocation = ShowMeYourHandsMain.weaponLocations[mainHandWeapon].Item1;
            float mainHandAngle = ShowMeYourHandsMain.weaponLocations[mainHandWeapon].Item2;
            float mainMeleeExtra = 0f;

            DoOffsets(mainHandWeapon, mainHandAngle, stance_Busy, out mainHandAngle, out bool mainMelee, out bool facingWestAiming, out bool showWeapon, out mainHandFlipped);

            if (mainMelee)
            {
                mainMeleeExtra = 0.0001f;
            }

            offHandFlipped = mainHandFlipped;

            Vector3 offhandWeaponLocation = mainWeaponLocation;
            float offHandAngle = mainHandAngle;
            float offMeleeExtra = 0f;

            bool offMelee = false;

            if (offHandWeapon != null)
            {
                if (!ShowMeYourHandsMain.weaponLocations.ContainsKey(offHandWeapon))
                {
                    if (ShowMeYourHandsMod.instance.Settings.VerboseLogging)
                    {
                        Log.ErrorOnce(
                            $"[ShowMeYourHands]: Could not find the position for {offHandWeapon.def.label} from the mod {offHandWeapon.def.modContentPack.Name}, equipped by {___pawn.Name}. Please report this issue to the author of Show Me Your Hands if possible.",
                            offHandWeapon.def.GetHashCode());
                    }
                }
                else
                {
                    offhandWeaponLocation = ShowMeYourHandsMain.weaponLocations[offHandWeapon].Item1;
                    offHandAngle = ShowMeYourHandsMain.weaponLocations[offHandWeapon].Item2;

                    DoOffsets(offHandWeapon, offHandAngle, stance_Busy, out offHandAngle, out offMelee, out bool _, out bool _, out offHandFlipped);
                    if (offMelee)
                    {
                        offMeleeExtra = 0.0001f;
                    }
                }
            }

            /*            bool mainHandFlipped = this.CurrentRotation == Rot4.West || aimAngle is > 200f and < 340f;
            bool offHandFlipped = this.CurrentRotation == Rot4.West || aimAngle is > 200f and < 340f;
            */

            float drawSize = 1f;
            LastDrawn = GenTicks.TicksAbs;

            if (ShowMeYourHandsMod.instance.Settings.RepositionHands && mainHandWeapon?.def?.graphicData != null &&
                mainHandWeapon?.def?.graphicData?.drawSize.x != 1f)
            {
                drawSize = mainHandWeapon.def.graphicData.drawSize.x;
            }

            Rot4 mainHandWeaponRot = Pawn_RotationTracker.RotFromAngleBiased(mainHandAngle);// this.CurrentRotation;
            Rot4 offHandWeaponRot = Pawn_RotationTracker.RotFromAngleBiased(offHandAngle);// this.CurrentRotation;

            // Log.Message(mainHandAngle.ToString("N0"));
            if (MainHand != Vector3.zero)
            {
                float x = MainHand.x * drawSize;
                float z = MainHand.z * drawSize;
                float y = MainHand.y < 0 ? -0.0001f : 0.001f;

                //rotation = mainHandFlipped ? Rot4.West : Rot4.East;

                if (this.CurrentRotation == Rot4.West || this.CurrentRotation == Rot4.North)
                {
                    y *= -1f;
                }
                if (this.CurrentRotation.IsHorizontal)
                {
                    //    x += 0.01f;
                }

                if (mainHandFlipped)
                {
                    x *= -1;
                    x -= 0.03f;
                }

                this.MainHandPosition = mainWeaponLocation +
                                         AdjustRenderOffsetFromDir(mainHandWeaponRot, mainHandWeapon as ThingWithComps) +
                                         new Vector3(x, y + mainMeleeExtra, z).RotatedBy(mainHandAngle);
                float propertiesMainHandAngle = compProperties.MainHandAngle;
                propertiesMainHandAngle *= mainHandFlipped ? -1f : 1f;
                propertiesMainHandAngle += mainHandFlipped ? 180f : 0f;
                //   propertiesMainHandAngle += mainMelee && mainHandFlipped ? 180f : 0f;
                propertiesMainHandAngle %= 360f;

                this.MainHandAngle = mainHandAngle + propertiesMainHandAngle;
                rightBehind = y <= 0f;
            }
            else
            {
                this.MainHandPosition = Vector3.zero;
            }
            float propsOffHandAngle = compProperties.SecHandAngle;

            if (OffHand != Vector3.zero)
            {
                float x2 = OffHand.x * drawSize;
                float z2 = OffHand.z * drawSize;
                float y2 = OffHand.y < 0 ? -0.0001f : 0.001f;
                ThingWithComps secondHandThing = mainHandWeapon;

                if (offHandWeapon != null)
                {
                    drawSize = 1f;
                    secondHandThing = offHandWeapon;
                    propsOffHandAngle = offhandComp.MainHandAngle;
                    //rotation        = offHandFlipped ? Rot4.West : Rot4.East;

                    if (ShowMeYourHandsMod.instance.Settings.RepositionHands && offHandWeapon.def.graphicData != null &&
                        offHandWeapon.def?.graphicData?.drawSize.x != 1f)
                    {
                        drawSize = offHandWeapon.def.graphicData.drawSize.x;
                    }

                    x2 = OffHand.x * drawSize;
                    z2 = OffHand.z * drawSize;
                    /*
                    if (isHorizonzal && !offMelee)
                    {
                        if (this.CurrentRotation == Rot4.South)
                        {
                    //        z2 += 0.05f;
                        }
                        else
                        {
                            z2 -= 0.05f;
                        }
                    }
                    */
                }

                if (this.CurrentRotation == Rot4.East)
                {
                    y2 = -1f;
                }
                if (this.CurrentRotation == Rot4.North)
                {
                    y2 = 0.001f;
                }

                if (this.CurrentRotation == Rot4.West)
                {
                    if (offHandWeapon == null)
                    {
                        y2 *= -1f;
                    }
                }

                if (this.CurrentRotation.IsHorizontal)
                {
                    x2 += 0.01f;
                }

                if (offHandFlipped)
                {
                    x2 *= -1;
                }

                this.SecondHandPosition = offhandWeaponLocation +
                                          AdjustRenderOffsetFromDir(offHandWeaponRot, secondHandThing as ThingWithComps) +
                                          new Vector3(x2, y2 + offMeleeExtra, z2).RotatedBy(offHandAngle);

                propsOffHandAngle *= (offHandFlipped ? -1f : 1f);
                propsOffHandAngle += (offHandFlipped ? 180f : 0f);
                propsOffHandAngle %= 360f;

                this.OffHandAngle = offHandAngle + propsOffHandAngle;

                leftBehind = y2 <= 0f;
            }
            else
            {
                this.SecondHandPosition = Vector3.zero;
            }
            /*
            CompEquippable compEquippable = eq.TryGetComp<CompEquippable>();
            if (compEquippable != null)
            {
                EquipmentUtility.Recoil(eq.def, EquipmentUtility.GetRecoilVerb(compEquippable.AllVerbs), out var drawOffset, out var angleOffset, aimAngle);
                drawLoc += drawOffset;
                num += angleOffset;
            }
            */
        }

        private void DoOffsets(ThingWithComps eq, float baseAngle, Stance_Busy stance_Busy, out float handAngle,
            out bool isMeleeWeapon, out bool facingWestAiming, out bool showWeapon, out bool isFlipped)
        {
            handAngle = baseAngle - 90f;
            isMeleeWeapon = false;
            isFlipped = baseAngle is > 200f and < 340f;
            facingWestAiming = false;
            showWeapon = true;
            if (pawn.CurJob != null && pawn.CurJob.def.neverShowWeapon)
            {
                showWeapon = false;
            }

            //if (showWeapon && CurrentlyAiming(stance_Busy))
            //{
            //    if (this.CurrentRotation == Rot4.West)
            //    {
            //        facingWestAiming = true;
            //    }
            //
            //    if (!eq.def.IsRangedWeapon || stance_Busy.verb.IsMeleeAttack)
            //    {
            //        isMeleeWeapon = true;
            //    }
            //}
            if (isFlipped)
            {
                handAngle -= 180f;
                handAngle -= eq.def.equippedAngleOffset;
            }
            else
            {
                handAngle += eq.def.equippedAngleOffset;
            }

            handAngle %= 360f;
        }

        public float GetBodysizeScaling()
        {
            if (!PawnExtensions.pawnBodySizes.ContainsKey(pawn) || GenTicks.TicksAbs % GenTicks.TickLongInterval == 0)
            {
                float bodySize = 1f;
                if (!pawn.RaceProps.Humanlike && this.pawn.kindDef.lifeStages.Any())
                {
                    Vector2 maxSize = this.pawn.kindDef.lifeStages.Last().bodyGraphicData.drawSize;
                    Vector2 sizePaws = this.pawn.ageTracker.CurKindLifeStage.bodyGraphicData.drawSize;
                    bodySize = sizePaws.x / maxSize.x;
                }
                else if (ShowMeYourHandsMod.instance.Settings.ResizeHands)
                {
                    bodySize = pawn.BodySize;
                    bodySize = Mathf.Max(bodySize, 0.5f);
                    /*
                    if (pawn.RaceProps != null)
                    {
                        bodySize = pawn.RaceProps.baseBodySize;
                    }
                    // this only returns the current size per life age stage
                    if ((ShowMeYourHandsMain.BabysAndChildrenLoaded || ShowMeYourHandsMain.BabysAndChildrenLoaded2 || ShowMeYourHandsMain.BabysAndChildrenLoaded3) && ShowMeYourHandsMain.GetBodySizeScaling != null)
                    {
                        bodySize = (float)ShowMeYourHandsMain.GetBodySizeScaling.Invoke(null, new object[] { pawn });
                    }
                    */
                }

                PawnExtensions.pawnBodySizes[pawn] = bodySize;
                // FS walks & so need 1f for regular pawns; 80% drawsize looks fine
                this.pawnBodyMesh = MeshMakerPlanes.NewPlaneMesh(bodySize * this.BodyAnim.extremitySize * 0.8f);
                this.pawnBodyMeshFlipped = MeshMakerPlanes.NewPlaneMesh(bodySize * this.BodyAnim.extremitySize * 0.8f, true);
            }

            return PawnExtensions.pawnBodySizes[pawn];
        }

        private WalkCycleDef lastWalkCycle;

        public void SetWalkCycle(WalkCycleDef newWalkCycleDef)
        {
            this.lastWalkCycle = this.CurrentWalkCycle;
            this.CurrentWalkCycle = newWalkCycleDef;
        }

        private static Vector3 AdjustRenderOffsetFromDir(Rot4 rotation, ThingWithComps weapon)
        {
            if (!ShowMeYourHandsMain.OversizedWeaponLoaded && !ShowMeYourHandsMain.EnableOversizedLoaded)
            {
                return Vector3.zero;
            }

            switch (rotation.AsInt)
            {
                case 0:
                    return ShowMeYourHandsMain.northOffsets.TryGetValue(weapon.def, out Vector3 northValue)
                        ? northValue
                        : Vector3.zero;

                case 1:
                    return ShowMeYourHandsMain.eastOffsets.TryGetValue(weapon.def, out Vector3 eastValue)
                        ? eastValue
                        : Vector3.zero;

                case 2:
                    return ShowMeYourHandsMain.southOffsets.TryGetValue(weapon.def, out Vector3 southValue)
                        ? southValue
                        : Vector3.zero;

                case 3:
                    return ShowMeYourHandsMain.westOffsets.TryGetValue(weapon.def, out Vector3 westValue)
                        ? westValue
                        : Vector3.zero;

                default:
                    return Vector3.zero;
            }
        }

        private static float CalcShortestRot(float from, float to)
        {
            // If from or to is a negative, we have to recalculate them.
            // For an example, if from = -45 then from(-45) + 360 = 315.
            if (@from < 0)
            {
                @from += 360;
            }

            if (to < 0)
            {
                to += 360;
            }

            // Do not rotate if from == to.
            if (@from == to ||
                @from == 0 && to == 360 ||
                @from == 360 && to == 0)
            {
                return 0;
            }

            // Pre-calculate left and right.
            float left = (360 - @from) + to;
            float right = @from - to;

            // If from < to, re-calculate left and right.
            if (@from < to)
            {
                if (to > 0)
                {
                    left = to - @from;
                    right = (360 - to) + @from;
                }
                else
                {
                    left = (360 - to) + @from;
                    right = to - @from;
                }
            }

            // Determine the shortest direction.
            return ((left <= right) ? left : (right * -1));
        }

        private float CostToPayThisTick(float nextCellCostTotal)
        {
            float num = 1f;
            if (pawn.stances.Staggered)
            {
                num *= 0.17f;
            }
            if (num < nextCellCostTotal / 450f)
            {
                num = nextCellCostTotal / 450f;
            }
            return num;
        }

        private static bool doSmoothWalk = false;
        public Sustainer sustainer;

        private float PawnMovedPercent(Pawn pawn)
        {
            this.IsMoving = false;
            Pawn_PathFollower pather = pawn?.pather;
            if (pather == null)
            {
                this.sustainer?.End();
                return 0f;
            }

            if (!pather.Moving)
            {
                this.sustainer?.End();
                return 0f;
            }

            if (pawn.stances.FullBodyBusy)
            {
                this.sustainer?.End();
                return 0f;
            }

            if (pather.BuildingBlockingNextPathCell() != null)
            {
                this.sustainer?.End();
                return 0f;
            }

            if (pather.NextCellDoorToWaitForOrManuallyOpen() != null)
            {
                this.sustainer?.End();
                return 0f;
            }

            if (pather.WillCollideWithPawnOnNextPathCell())
            {
                this.sustainer?.End();
                return 0f;
            }
            if (false)
            {
                if (this.sustainer == null || this.sustainer.Ended)
                {
                    if (Find.Selector.SelectedPawns.Contains(pawn))
                    {
                        this.sustainer = DefDatabase<SoundDef>.GetNamedSilentFail("Sound_Walking_Snow")
                            .TrySpawnSustainer(SoundInfo.InMap(pawn, MaintenanceType.PerTick));
                        this.sustainer.def.maxVoices = 20;
                        this.sustainer.def.maxSimultaneous = 20;
                    }
                    // DefDatabase<SoundDef>.GetNamedSilentFail("Sound_Walking_Snow").TrySpawnSustainer(SoundInfo.InMap(pawn, MaintenanceType.PerTick));
                }
                else
                {
                    this.sustainer?.Maintain();
                }
            }

            this.IsMoving = true;
            // revert the walkcycle for drafted shooters
            float cellCostFactor = pather.nextCellCostLeft / pather.nextCellCostTotal;

            // if (pather.nextCellCostLeft == pather.nextCellCostTotal) // starts moving
            // {
            //     remainingPercent = pather.nextCellCostTotal - pather.nextCellCostLeft;
            //     // SimpleCurve moveSpeedCurve = new()
            //     // {
            //     //     new CurvePoint(0, 1), // concrete, wood floor, carpets
            //     //     new CurvePoint(1, 0.93f), // carpet, wood floor
            //     //     new CurvePoint(2, 0.87f), // soil, rough stones
            //     //     new CurvePoint(5, 0.72f), // tilled soil
            //     //     // new(4, 0.xxf), // sand
            //     //     new CurvePoint(14, 0.48f), // mud, ice
            //     //     new CurvePoint(30, 0.3f), // shallow water
            //     // };
            //     // int walkSpeed = pawn.Map.terrainGrid.TerrainAt(pawn.Position).pathCost;
            //     // terrainModifier = moveSpeedCurve.Evaluate(walkSpeed);
            // }

            // if (movedPercent is > 0.5f and < 0.51f)
            // {
            //
            // }
            // movedPercent *= terrainModifier;

            // sustainer.def.subSounds.FirstOrDefault().pitchRange = new FloatRange(cellCostFactor, cellCostFactor);

            bool invert = false;
            if (/*CurrentRotation.IsHorizontal &&*/ CurrentRotation.FacingCell != pather.nextCell)
            {
                IntVec3 intVec = pather.nextCell - pawn.Position;
                if (intVec.x > 0)
                {
                    invert = CurrentRotation != Rot4.East;
                }
                else if (intVec.x < 0)
                {
                    invert = CurrentRotation != Rot4.West;
                }
                else if (intVec.z > 0)
                {
                    invert = CurrentRotation != Rot4.North;
                }
                else
                {
                    invert = CurrentRotation != Rot4.South;
                }
            }
            // not working as intended
            /*
            if (!pather.MovedRecently(60)) // pawn starts walking
            {
                doSmoothWalk = true;
            }
            else if (pather.Destination.Cell == pather.nextCell && cellCostFactor > 0.5f) // slow down when reaching target
            {
                cellCostFactor = EasingFunction.EaseOutCubic(0.5f, 1f, Mathf.Lerp(0.5f, 1f, cellCostFactor));
            }

            if (doSmoothWalk)
            {
                if (pather.Destination.Cell == pather.nextCell)
                {
                    cellCostFactor = EasingFunction.EaseOutCubic(0f, 1f, Mathf.Lerp(0f, 1f, cellCostFactor));
                }
                else if (cellCostFactor < 0.5f)
                {
                    cellCostFactor = EasingFunction.EaseOutCubic(0f, 0.5f, Mathf.Lerp(0, 0.5f, cellCostFactor));
                }
            }
            else
            {
                doSmoothWalk = false;
            }
            */
            // float modifiedAnimationSpeed = cellCostFactor;
            //  modifiedAnimationSpeed *= terrainModifier;
            //
            //
            // modifiedAnimationSpeed += remainingPercent;
            //
            // if (modifiedAnimationSpeed > 1f)
            // {
            //     modifiedAnimationSpeed -= 1f;
            // }
            //
            // if (pather.nextCellCostLeft > 100)
            // {
            //     remainingPercent = terrainModifier;
            // }

            if (!invert)
            {
                cellCostFactor = 1f - cellCostFactor;
            }

            return cellCostFactor;
        }

        private static float remainingPercent = 0f;
        private static float terrainModifier = 1f;

        public void GetBipedMesh(out Mesh meshRight, out Mesh meshLeft)
        {
            GetBipedMesh(out meshRight, out meshLeft, null, null);
        }

        public void GetBipedMesh(out Mesh meshRight, out Mesh meshLeft, bool? rightFlipped, bool? leftFlipped)
        {
            Rot4 rot = this.CurrentRotation;

            if (rightFlipped.HasValue && leftFlipped.HasValue)
            {
                meshRight = rightFlipped.Value ? this.pawnBodyMeshFlipped : this.pawnBodyMesh;
                meshLeft = leftFlipped.Value ? this.pawnBodyMeshFlipped : this.pawnBodyMesh;
                return;
            }

            switch (rot.AsInt)
            {
                default:
                    meshRight = this.pawnBodyMesh;// MeshPool.plane10;
                    meshLeft = this.pawnBodyMeshFlipped; // MeshPool.plane10Flip;
                    break;

                case 1:
                    meshRight = this.pawnBodyMesh;
                    meshLeft = this.pawnBodyMesh;
                    break;

                case 3:
                    meshRight = pawnBodyMeshFlipped;// MeshPool.plane10Flip;
                    meshLeft = pawnBodyMeshFlipped;// MeshPool.plane10Flip;
                    break;
            }
        }

        public void DrawAnimalFeet(float drawAngle, Vector3 rootLoc, Vector3 bodyLoc)
        {
            if (this.BodyAnim.bipedWithHands)
            {
                return;
            }

            if (this.IsMoving)
            {
                drawAngle += -pawn.Drawer.renderer.BodyAngle();
            }

            // Fix the position, maybe needs new code in GetJointPositions()?
            Rot4 _compAnimatorCurrentRotation = this.CurrentRotation;
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
            this.DrawFeet(drawAngle, rearPawLoc, bodyLoc);
        }

        public void DrawFrontPaws(float drawQuat, Vector3 rootLoc)
        {
            BodyAnimDef body = this.BodyAnim;
            if (body == null)
            {
                return;
            }

            if (!body.quadruped)
            {
                return;
            }

            // Basic values

            Rot4 rot = this.CurrentRotation;

            JointLister jointPositions = this.GetJointPositions(JointType.Shoulder,
                body.shoulderOffsets[rot.AsInt],
                body.shoulderOffsets[Rot4.North.AsInt].x);

            // get the actual hip height
            JointLister groundPos = this.GetJointPositions(JointType.Hip,
                body.hipOffsets[rot.AsInt],
                body.hipOffsets[Rot4.North.AsInt].x);

            jointPositions.LeftJoint.z = groundPos.LeftJoint.z;
            jointPositions.RightJoint.z = groundPos.RightJoint.z;

            Vector3 rightFootCycle = Vector3.zero;
            Vector3 leftFootCycle = Vector3.zero;
            float footAngleRight = 0f;

            float footAngleLeft = 0f;
            float offsetJoint = 0;

            WalkCycleDef cycle = this.CurrentWalkCycle;

            if (cycle != null && IsMoving)
            {
                offsetJoint = cycle.ShoulderOffsetHorizontalX.Evaluate(this.MovedPercent);

                // Center = drawpos of carryThing
                this.DoWalkCycleOffsets(
                    ref rightFootCycle,
                    ref leftFootCycle,
                    ref footAngleRight,
                    ref footAngleLeft,
                    ref offsetJoint,
                    cycle.FrontPawPositionX,
                    cycle.FrontPawPositionZ,
                    cycle.FrontPawAngle, MovedPercent, CurrentRotation);
            }
            float bodysizeScaling = GetBodysizeScaling();

            this.GetBipedMesh(out Mesh footMeshRight, out Mesh footMeshLeft);

            Material matLeft;

            Material matRight;
            /*
             if (MainTabWindow_BaseAnimator.Colored)
            {
                matRight = this.PawnBodyGraphic?.FrontPawGraphicRightCol?.MatAt(rot);
                matLeft = this.PawnBodyGraphic?.FrontPawGraphicLeftCol?.MatAt(rot);
            }
            else
            */

            (matRight, matLeft) = GetMaterialFor(this.pawnBodyGraphic?.FrontPawGraphicRight, this.pawnBodyGraphic?.FrontPawGraphicLeft, this.pawnBodyGraphic?.FrontPawGraphicRightShadow, this.pawnBodyGraphic?.FrontPawGraphicLeftShadow);

            groundPos.LeftJoint = groundPos.LeftJoint.RotatedBy(drawQuat);
            groundPos.RightJoint = groundPos.RightJoint.RotatedBy(drawQuat);
            leftFootCycle = leftFootCycle.RotatedBy(drawQuat);
            rightFootCycle = rightFootCycle.RotatedBy(drawQuat);

            //Log.Message(matRight?.name);
            Vector3 ground = rootLoc + new Vector3(0, 0, OffsetGroundZ).RotatedBy(drawQuat) * bodysizeScaling;
            if (matLeft != null)
            {
                if (this.BodyStat.HandLeft != PartStatus.Missing)
                {
                    Vector3 position = ground + (jointPositions.LeftJoint + leftFootCycle) * bodysizeScaling;
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
                if (this.BodyStat.HandRight != PartStatus.Missing)
                {
                    Vector3 position = ground + (jointPositions.RightJoint + rightFootCycle) * bodysizeScaling;
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