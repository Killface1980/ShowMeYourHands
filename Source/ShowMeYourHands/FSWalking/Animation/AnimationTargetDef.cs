﻿// ReSharper disable StyleCop.SA1307
// ReSharper disable InconsistentNaming
// ReSharper disable StyleCop.SA1401
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnassignedField.Global

using JetBrains.Annotations;
using System.Collections.Generic;
using Verse;

namespace PawnAnimator.Defs
{
    public class AnimationTargetDef : ThingDef
    {
        #region Public Fields

        [NotNull]
        public List<CompLoaderTargets> CompLoaderTargets = new();

        #endregion Public Fields
    }
}