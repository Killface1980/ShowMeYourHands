// ReSharper disable InconsistentNaming

using UnityEngine;
using Verse;

namespace PawnAnimator
{
    public static class Offsets
    {
        //// total max with repetitions: LayerSpacing = 0.46875f;


        public const float YOffset_Head = 0.02734375f;

        // bodyoffset + appareloffset
        public const float YOffset_HandsFeetOver = 0.0289575271f; // FS
        //public const float YOffset_HandsFeetOver = 0.01447876334f; // FS
        public const float YOffset_Behind = 0.00289575267f;


        public const float YOffset_CarriedThing = 0.03474903f;

        // Verse.Listing_Standard
        public static float Slider(this Listing_Standard listing, float value, float leftValue, float rightValue, bool middleAlignment = false, string label = null, string leftAlignedLabel = null, string rightAlignedLabel = null, float roundTo = -1f)
        {
            Rect rect = listing.GetRect(22f);
            float result = Widgets.HorizontalSlider(rect, value, leftValue, rightValue, middleAlignment, label, leftAlignedLabel, rightAlignedLabel, roundTo);
            listing.Gap(listing.verticalSpacing);
            return result;
        }
    }
}