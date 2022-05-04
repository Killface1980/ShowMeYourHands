using UnityEngine;
using Verse;

namespace FacialStuff
{
    public class PawnBodyDrawer : BasicDrawer
    {
        #region Protected Fields

        #endregion Protected Fields

        #region Protected Constructors

        protected PawnBodyDrawer()
        {
        }

        #endregion Protected Constructors



        #region Public Methods

        public virtual void ApplyBodyWobble(ref Vector3 rootLoc, ref Vector3 footPos)
        {
        }

        public virtual void DrawFeet(float drawAngle, Vector3 rootLoc, Vector3 bodyLoc)
        {
        }

        public virtual void DrawHands(float bodyAngle, Vector3 drawPos, Thing carriedThing = null,
            bool flip = false)
        {
        }

        public virtual void Initialize()
        {
        }

        public virtual void Tick()
        {
        }

        #endregion Public Methods
    }
}