using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PawnAnimator
{
    // ReSharper disable UnassignedField.Global
    // ReSharper disable StyleCop.SA1307
    // ReSharper disable StyleCop.SA1401
    // ReSharper disable InconsistentNaming
    public class CompProperties_BodyAnimator : CompProperties
    {
        #region Public Constructors

        public CompProperties_BodyAnimator()
        {
            this.compClass = typeof(CompBodyAnimator);
        }

        #endregion Public Constructors

        #region Public Fields

        public string handTexPath = "Things/Pawn/Humanlike/Hands/Human_Hand";
        public string footTexPath = "Things/Pawn/Humanlike/Feet/Human_Foot";
        public List<Vector3> hipOffsets = new();
        public List<Vector3> shoulderOffsets = new();

        public bool bipedWithHands;
        public bool quadruped;
        public float extremitySize = 1f;
        public float armLength = 0f;
        public float extraLegLength = 0f;
        public float offCenterX = 0f;

        public Dictionary<LocomotionUrgency, WalkCycleDef> walkCycles = new();

        public string WalkCycleType = "Undefined";

        #endregion Public Fields
    }
}