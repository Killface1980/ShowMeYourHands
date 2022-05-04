using JetBrains.Annotations;
using System;
using ShowMeYourHands;
using UnityEngine;
using Verse;

namespace FacialStuff
{
    [ShowMeYourHandsMod.HotSwappable]
    public abstract class BasicDrawer 
    {
        #region Protected Fields

        [NotNull]
        public Pawn pawn;

        #endregion Protected Fields

        #region Protected Methods

        #endregion Protected Methods
    }
}