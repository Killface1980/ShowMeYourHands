// ReSharper disable StyleCop.SA1307
// ReSharper disable InconsistentNaming
// ReSharper disable StyleCop.SA1401
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable NotNullMemberIsNotInitialized
// ReSharper disable CheckNamespace

using System.Collections.Generic;
using PawnAnimator.Defs;
using JetBrains.Annotations;
using Verse;
using Verse.AI;

namespace RimWorld
{
    public class WalkCycleDef : Def
    {
        #region Public Fields

        [NotNull]
        public string WalkCycleType;

        public float shoulderAngle;

        [NotNull]
        public List<PawnKeyframe> keyframes = new();

        [NotNull]
        public SimpleCurve BodyAngle = new();

        [NotNull]
        public SimpleCurve BodyAngleVertical = new();

        [NotNull]
        public SimpleCurve HeadAngleX = new();

        [NotNull]
        public SimpleCurve HeadOffsetZ = new();

        [NotNull]
        public SimpleCurve BodyOffsetZ = new();

        [NotNull]
        public SimpleCurve FootAngle = new();

        [NotNull]
        public SimpleCurve FootPositionX = new();

        [NotNull]
        public SimpleCurve FootPositionZ = new();

        [NotNull]
        public SimpleCurve HandsSwingAngle = new();

        [NotNull]
        public SimpleCurve HandsSwingPosVertical = new();

        // public SimpleCurve FootPositionVerticalZ = new SimpleCurve();

        // public SimpleCurve BodyOffsetVerticalZ = new SimpleCurve();
        [NotNull]
        public SimpleCurve FrontPawAngle = new();

        [NotNull]
        public SimpleCurve FrontPawPositionX = new();

        [NotNull]
        public SimpleCurve FrontPawPositionZ = new();

        // public SimpleCurve FrontPawPositionVerticalZ = new SimpleCurve();
        [NotNull]
        public SimpleCurve ShoulderOffsetHorizontalX = new();

        [NotNull]
        public SimpleCurve HipOffsetHorizontalX = new();

        public LocomotionUrgency locomotionUrgency = LocomotionUrgency.None;

        #endregion Public Fields
    }
}