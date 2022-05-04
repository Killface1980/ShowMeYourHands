// ReSharper disable StyleCop.SA1307
// ReSharper disable StyleCop.SA1401
// ReSharper disable MissingXmlDoc
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable InconsistentNaming
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnassignedField.Global

// ReSharper disable StyleCop.SA1310
// ReSharper disable CheckNamespace

using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimWorld
{
    public class BodyAnimDef : Def
    {
        #region Public Fields
        public string thingTarget = null;

        public string handTexPath = "Things/Pawn/Humanlike/Hands/Human_Hand";
        public string footTexPath = "Things/Pawn/Humanlike/Feet/Human_Foot";
        public bool quadruped;
        public bool bipedWithHands;

        public float armLength = 0f;

        public float extraLegLength = 0f;

        public float extremitySize = 1f;

        public List<Vector3> hipOffsets = new() { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };

        public Vector2 headOffset = Vector2.zero;

        public float offCenterX = 0f;

        public List<Vector3> shoulderOffsets =
            new()
            { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };

        [NotNull]
        public Dictionary<LocomotionUrgency, WalkCycleDef> walkCycles = new();

        public string WalkCycleType = "Undefined";


        #endregion Public Fields

        // public float hipOffsetVerticalFromCenter;
    }
}