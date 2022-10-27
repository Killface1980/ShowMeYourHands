// ReSharper disable StyleCop.SA1307
// ReSharper disable InconsistentNaming
// ReSharper disable StyleCop.SA1401
// ReSharper disable FieldCanBeMadeReadOnly.Global


using JetBrains.Annotations;

// ReSharper disable UnassignedField.Global

using System.Collections.Generic;
using UnityEngine;

namespace PawnAnimator
{
    public class CompLoaderTargets
    {
        #region Public Fields
        [NotNull] public List<string> thingTargets = new();
        /*
        public Vector3 firstHandPosition = Vector3.zero;
        public Vector3 secondHandPosition = Vector3.zero;


        public float? attackAngleOffset;

        public Vector3 weaponPositionOffset = Vector3.zero;
        public Vector3 aimedWeaponPositionOffset;
        */
        // Animals


        public string handTexPath = "Things/Pawn/Humanlike/Hands/Human_Hand";
        public string footTexPath = "Things/Pawn/Humanlike/Feet/Human_Foot";

        public bool quadruped;
        public bool bipedWithHands;
        [CanBeNull] public List<Vector3> hipOffsets = new();
        public List<Vector3> shoulderOffsets = new();
        public float armLength = 0f;
        public float extraLegLength = 0f;
        public float offCenterX = 0f;
        public float extremitySize = 1f;

        #endregion Public Fields
    }
}