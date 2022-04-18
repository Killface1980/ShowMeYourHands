using FacialStuff.GraphicsFS;
using FacialStuff.Tweener;
using JetBrains.Annotations;
using RimWorld;
using ShowMeYourHands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ShowMeYourHands.FSWalking;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FacialStuff
{

    [ShowMeYourHandsMod.HotSwappable]
    [StaticConstructorOnStartup]
    public class CompBodyAnimator : ThingComp
    {
        private const float animationFactorForSmoothing = 0.5f;

        #region Public Fields

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
            WalkCycleDef lastCycle    = this.lastWalkCycle;
            float lerpFactor          = 1f;

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

                rightFoot      = Vector3.Lerp(rightFootLerp, rightFoot, lerpFactor);
                leftFoot       = Vector3.Lerp(leftFootLerp, leftFoot, lerpFactor);
                footAngleRight = Mathf.Lerp(footAngleRightLerp, footAngleRight, lerpFactor);
                footAngleLeft  = Mathf.Lerp(footAngleLeftLerp, footAngleLeft, lerpFactor);
                offsetJoint    = Mathf.Lerp(offsetJointLerp, offsetJoint, lerpFactor);


            }

            // smaller steps for smaller pawns
            if (bodysizeScaling < 1f)
            {
                SimpleCurve curve = new() { new CurvePoint(0f, 0.5f), new CurvePoint(1f, 1f) };
              
                float mod    = curve.Evaluate(bodysizeScaling);
                rightFoot.x *= mod;
                rightFoot.z *= mod;
                leftFoot.x  *= mod;
                leftFoot.z  *= mod;
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
                leftFoot.z = offsetZ.Evaluate(percentInverse);

                rightFoot.x += offsetJoint;
                leftFoot.x += offsetJoint;

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
            float x  = 0;
            float x2 = -x;
            float y  = Offsets.YOffset_HandsFeetOver ;
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
                WalkCycleDef lastCycle    = this.lastWalkCycle;
                float percent             = this.MovedPercent;
                float lerpFactor          = 1f;

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

                    shoulderAngle          = Mathf.Lerp(lerpShoulderAngle, shoulderAngle, lerpFactor);
                    shoulderPos.LeftJoint  = Vector3.Lerp(lerpShoulderPos.LeftJoint, shoulderPos.LeftJoint, lerpFactor);
                    shoulderPos.RightJoint = Vector3.Lerp(lerpShoulderPos.RightJoint, shoulderPos.RightJoint, lerpFactor);
                    z                      = Mathf.Lerp(lerpZ, z, lerpFactor);
                    z2                     = Mathf.Lerp(lerpZ2, z2, lerpFactor);

                }
            }

            if (/*MainTabWindow_BaseAnimator.Panic || */ this.pawn.Fleeing() || this.pawn.IsBurning())
            {
                float offset       = 1f + armLength;
                x                 *= offset;
                z                 *= offset;
                x2                *= offset;
                z2                *= offset;
                handSwingAngle[0] += 180f;
                handSwingAngle[1] += 180f;
                shoulderAngle      = 0f;
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
                float f      = lookie * offsetJoint;

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

        public List<PawnBodyDrawer> pawnBodyDrawers
        {
            get => _pawnBodyDrawers;
            private set => _pawnBodyDrawers = value;
        }

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
            if (this.pawnBodyDrawers == null)
            {
                return;
            }

            int i = 0;
            int count = this.pawnBodyDrawers.Count;
            while (i < count)
            {
                this.pawnBodyDrawers[i].ApplyBodyWobble(ref rootLoc, ref footPos);
                i++;
            }
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

        public void DrawFeet(float bodyAngle, Vector3 rootLoc, Vector3 bodyLoc)
        {
            if (!this.pawnBodyDrawers.NullOrEmpty())
            {
                int i = 0;
                int count = this.pawnBodyDrawers.Count;
                while (i < count)
                {
                    this.pawnBodyDrawers[i].DrawFeet(bodyAngle, rootLoc, bodyLoc);
                    i++;
                }
            }
        }

        // off for now
        public void DrawHands(float bodyAngle, Vector3 rootLoc, [CanBeNull] Thing carriedThing = null,
            bool flip = false)
        {
            if (this.pawnBodyDrawers.NullOrEmpty())
            {
                return;
            }

            int i = 0;
            int count = this.pawnBodyDrawers.Count;
            while (i < count)
            {
                this.pawnBodyDrawers[i].DrawHands(bodyAngle, rootLoc, carriedThing, flip);
                i++;
            }
        }

        // public override string CompInspectStringExtra()
        // {
        //     string extra = this.Pawn.DrawPos.ToString();
        //     return extra;
        // }
        public void InitializePawnDrawer()
        {
            if (this.BodyAnim.bodyDrawers.Any())
            {
                this.pawnBodyDrawers = new List<PawnBodyDrawer>();
                for (int i = 0; i < this.BodyAnim.bodyDrawers.Count; i++)
                {
                    PawnBodyDrawer bodyDrawer =
                    (PawnBodyDrawer)Activator.CreateInstance(this.BodyAnim.bodyDrawers[i].GetType());
                    bodyDrawer.compAnimator = this;
                    bodyDrawer.pawn = this.pawn;
                    this.pawnBodyDrawers.Add(bodyDrawer);
                    bodyDrawer.Initialize();
                }
            }
            else
            {
                this.pawnBodyDrawers = new List<PawnBodyDrawer>();
                PawnBodyDrawer bodyDrawer = this.BodyAnim.quadruped
                    ? (PawnBodyDrawer)Activator.CreateInstance(typeof(QuadrupedDrawer))
                    : (PawnBodyDrawer)Activator.CreateInstance(typeof(HumanBipedDrawer));
                bodyDrawer.compAnimator = this;
                bodyDrawer.pawn = this.pawn;
                this.pawnBodyDrawers.Add(bodyDrawer);
                bodyDrawer.Initialize();
            }
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
            if (walkCycle == null) return;

            float movedPercent = this.MovedPercent;

            Rot4 currentRotation = this.CurrentRotation;
            if (currentRotation.IsHorizontal)
            {
                if (walkCycle.BodyAngle.PointsCount > 0)
                {
                    this.BodyAngle = (currentRotation == Rot4.West ? -1 : 1)
                                     * walkCycle.BodyAngle.Evaluate(movedPercent);
                }            }
            else
            {
                if (walkCycle.BodyAngleVertical.PointsCount > 0)
            {
                    this.BodyAngle = (currentRotation == Rot4.South ? -1 : 1)
                                 * walkCycle.BodyAngleVertical.Evaluate(movedPercent);
            }            }
        }
        public void TickDrawers()
        {
            if (!this._initialized)
            {
                this.InitializePawnDrawer();
                this._initialized = true;
            }

            if (this.pawnBodyDrawers.NullOrEmpty())
            {
                return;
            }

            int i = 0;
            int count = this.pawnBodyDrawers.Count;
            while (i < count)
            {
                this.pawnBodyDrawers[i].Tick();
                i++;
            }
        }


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
        private List<PawnBodyDrawer> _pawnBodyDrawers;


        public float BodyOffsetZ
        {
            get
            {
                if (ShowMeYourHandsMod.instance.Settings.UseFeet)
                {
                    WalkCycleDef walkCycle = this.CurrentWalkCycle;

                    SimpleCurve curve = walkCycle.BodyOffsetZ;
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

            if (this.CurrentRotation == Rot4.West || this.CurrentRotation == Rot4.North)
            {
                weaponPosOffset.y = -Offsets.YOffset_Head - Offsets.YOffset_CarriedThing;
            }

            // Use y for the horizontal position. too lazy to add even more vectors
            bool isHorizontal = this.CurrentRotation.IsHorizontal;
            bool aiming = pawn.Aiming();
            Vector3 o = extensions.WeaponPositionOffset;
            Vector3 d = extensions.AimedWeaponPositionOffset;

            if (isHorizontal)
            {
                weaponPosOffset = new Vector3(o.y, 0, o.z);
                if (aiming)
                {
                    weaponPosOffset += new Vector3(d.y, 0, d.z);
                }
            }
            else
            {
                weaponPosOffset = new Vector3(o.x, 0, o.z);
                if (aiming)
                {
                    weaponPosOffset += new Vector3(d.x, 0, d.z);
                }
            }

            if (flipped)
            {

                // flip x position offset
                if (pawn.Rotation != Rot4.South)
                {
                    weaponPosOffset.x *= -1;
                }
            }

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
            leftBehind                = rightBehind = mainHandFlipped = offHandFlipped = false;
            hasSecondWeapon           = false;
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

            float aimAngle = this.CurrentRotation == Rot4.West ? 217f : 143f;

            bool aiming = false;

            Stance_Busy stance_Busy = ___pawn.stances.curStance as Stance_Busy;

            if (stance_Busy is { neverAimWeapon: false, focusTarg.IsValid: true })
            {
                Vector3 a = stance_Busy.focusTarg.HasThing
                    ? stance_Busy.focusTarg.Thing.DrawPos
                    : stance_Busy.focusTarg.Cell.ToVector3Shifted();

                if ((a - ___pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
                {
                    aimAngle = (a - ___pawn.DrawPos).AngleFlat();
                }

                aiming = true;
            }

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
            float mainHandAngle        = ShowMeYourHandsMain.weaponLocations[mainHandWeapon].Item2 ;
            float mainMeleeExtra = 0f;


            DoOffsets(mainHandWeapon, mainHandAngle, stance_Busy, out mainHandAngle, out bool mainMelee, out bool facingWestAiming, out bool showWeapon, out mainHandFlipped);

            if (mainMelee)
            {
                mainMeleeExtra = 0.0001f;
            }

            offHandFlipped = mainHandFlipped;


            Vector3 offhandWeaponLocation = mainWeaponLocation;
            float offHandAngle            = mainHandAngle;
            float offMeleeExtra           = 0f;

            bool offMelee                 = false;

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
                    offHandAngle          = ShowMeYourHandsMain.weaponLocations[offHandWeapon].Item2;

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

            Rot4 rotation = this.CurrentRotation;

            if (MainHand != Vector3.zero)
            {
                float x = MainHand.x * drawSize;
                float z = MainHand.z * drawSize;
                float y = MainHand.y < 0 ? -0.0001f : 0.001f;


                //rotation = mainHandFlipped ? Rot4.West : Rot4.East;

                if (rotation == Rot4.North && !mainMelee && !aiming)
                {
             //       z += 0.1f;
                }
                if (this.CurrentRotation == Rot4.West || this.CurrentRotation == Rot4.North)
                {
                    y *= -1f;
                }
                if (this.CurrentRotation.IsHorizontal)
                {
                    x += 0.01f;
                }

                if (mainHandFlipped)
                {
                    x *= -1;
                }

                
                this.MainHandPosition = mainWeaponLocation +
                                         AdjustRenderOffsetFromDir(rotation, mainHandWeapon as ThingWithComps) +
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
                    drawSize        = 1f;
                    secondHandThing = offHandWeapon;
                    propsOffHandAngle       = offhandComp.MainHandAngle;
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
                
                if (this.CurrentRotation == Rot4.East || this.CurrentRotation == Rot4.North)
                {
                    y2 = -1f;
                }

                if (this.CurrentRotation == Rot4.West)
                {
                  //  y2 *= -1f;
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
                                          AdjustRenderOffsetFromDir(rotation, secondHandThing as ThingWithComps) +
                                          new Vector3(x2, y2 + offMeleeExtra, z2).RotatedBy(offHandAngle);
                
                propsOffHandAngle *= (offHandFlipped ? -1f:1f);
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
            facingWestAiming = false;
            isFlipped = false;

            showWeapon = true;
            if (pawn.CurJob != null && pawn.CurJob.def.neverShowWeapon)
            {
                showWeapon = false;
            }

            if (showWeapon && CurrentlyAiming(stance_Busy))
            {
                if (pawn.Rotation == Rot4.West)
                {
                    facingWestAiming = true;
                }

                if (!eq.def.IsRangedWeapon || stance_Busy.verb.IsMeleeAttack)
                {
                    isMeleeWeapon = true;
                }
            }

            if (isMeleeWeapon)
            {
                if (facingWestAiming)
                {
                    isFlipped = true;
                    handAngle -= 270f;
                    handAngle -= eq.def.equippedAngleOffset;
                }
                else
                {
                    isFlipped = false;
                    handAngle += eq.def.equippedAngleOffset;
                }
            }
            else if (baseAngle > 20f && baseAngle < 160f)
            {
                isFlipped = false;
                handAngle += eq.def.equippedAngleOffset;
            }
            else if ((baseAngle > 200f && baseAngle < 340f) || facingWestAiming)
            {
                isFlipped = true;
                handAngle -= 180f;
                handAngle -= eq.def.equippedAngleOffset;
            }
            else
            {
                isFlipped = false;
                handAngle += eq.def.equippedAngleOffset;
            }

            handAngle %= 360f;
        }

        internal static bool CurrentlyAiming(Stance_Busy stance)
        {
            return stance != null && !stance.neverAimWeapon && stance.focusTarg.IsValid;
        }

        public float GetBodysizeScaling()
        {
            if (!PawnExtensions.pawnBodySizes.ContainsKey(pawn) || GenTicks.TicksAbs % GenTicks.TickLongInterval == 0)
            {
                float bodySize = 1f;
                if (pawn.story == null) // mechanoids and animals
                {
                    if (this.pawn.kindDef.lifeStages.Any())
                    {
                        Vector2 maxSize = this.pawn.kindDef.lifeStages.Last().bodyGraphicData.drawSize;
                        Vector2 sizePaws = this.pawn.ageTracker.CurKindLifeStage.bodyGraphicData.drawSize;
                        bodySize = sizePaws.x / maxSize.x;
                    }
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



        private Vector3 AdjustRenderOffsetFromDir(Rot4 rotation, ThingWithComps weapon)
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
        private float PawnMovedPercent(Pawn pawn)
        {
            this.IsMoving = false;
            Pawn_PathFollower pather = pawn?.pather;
            if (pather == null)
            {
                return 0f;
            }

            if (!pather.Moving) return 0f;

            if (pawn.stances.FullBodyBusy)
            {
                return 0f;
            }

            if (pather.BuildingBlockingNextPathCell() != null)
            {
                return 0f;
            }

            if (pather.NextCellDoorToWaitForOrManuallyOpen() != null)
            {
                return 0f;
            }

            if (pather.WillCollideWithPawnOnNextPathCell())
            {
                return 0f;
            }
            /*
            PawnExtensions.cellCostMin = Mathf.Min(pather.nextCellCostTotal, PawnExtensions.cellCostMin);
            PawnExtensions.cellCostMax = Mathf.Max(pather.nextCellCostTotal, PawnExtensions.cellCostMax);

            float costToPayThisTick = CostToPayThisTick(pather.nextCellCostTotal);
            PawnExtensions.costToPayMin = Mathf.Min(costToPayThisTick, PawnExtensions.costToPayMin);
            PawnExtensions.costToPayMax = Mathf.Max(costToPayThisTick, PawnExtensions.costToPayMax);

            Log.ErrorOnce(pawn.LabelCap + " - " + pawn.CurJob.locomotionUrgency + " - " + numbie.ToString("N2"), Mathf.FloorToInt(
                numbie * 100));
            */
            /*
            PawnExtensions.TicksPerMoveCardinalMin = Mathf.Min(perMoveCardinal, PawnExtensions.TicksPerMoveCardinalMin);
            PawnExtensions.TicksPerMoveCardinalMax = Mathf.Max(perMoveCardinal, PawnExtensions.TicksPerMoveCardinalMax);
            var message = "Costs:\ncellCostMin: " + PawnExtensions.cellCostMin +
                          "\ncellCostMax: " + PawnExtensions.cellCostMax +
                          "\ncostToPayMin: " + PawnExtensions.costToPayMin +
                          "\ncostToPayMax: " + PawnExtensions.costToPayMax +
                          "\nTicksPerMoveCardinalMin: " + PawnExtensions.TicksPerMoveCardinalMin +
                          "\nTicksPerMoveCardinalMax: " + PawnExtensions.TicksPerMoveCardinalMax;

            Log.ErrorOnce(message, 1000 + Mathf.FloorToInt(PawnExtensions.cellCostMin));
            Log.ErrorOnce(message, 2000 + Mathf.FloorToInt(PawnExtensions.cellCostMax));
            Log.ErrorOnce(message, 3000 + Mathf.FloorToInt(PawnExtensions.costToPayMin));
            Log.ErrorOnce(message, 4000 + Mathf.FloorToInt(PawnExtensions.costToPayMax));
            Log.ErrorOnce(message, 5000 + Mathf.FloorToInt(PawnExtensions.TicksPerMoveCardinalMin));
            Log.ErrorOnce(message, 6000 + Mathf.FloorToInt(PawnExtensions.TicksPerMoveCardinalMax));
            */
            /*
            int y = (int)AccessTools.Method(typeof(Pawn_PathFollower), "CostToMoveIntoCell", new[] { typeof(IntVec3) })
                .Invoke(pawn.pather, new object[] { pawn.pather.nextCell });
            */

            /*
            Log.Message("Pawn is movinng;" + pawn.LabelCap +
                        " total: " + pather.nextCellCostTotal +
                        " left: " + pather.nextCellCostLeft +
                        " float: " + costToPayThisTick +
                        " urgency: " + pawn.CurJob.locomotionUrgency +
                        " cell cost: " + perMoveCardinal);
            */
            this.IsMoving = true;
            // revert the walkcycle for drafted shooters
            float cellCostFactor = pather.nextCellCostLeft / pather.nextCellCostTotal;
            bool invert = false;
            if (CurrentRotation.IsHorizontal && CurrentRotation.FacingCell != pather.nextCell)
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
            if (!invert)
            {
                cellCostFactor = 1f - cellCostFactor;
            }
            return cellCostFactor;
        }
    }
}