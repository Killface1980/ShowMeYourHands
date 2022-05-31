using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FacialStuff;
using UnityEngine;
using Verse;
using static ShowMeYourHands.ShowMeYourHandsMod;

namespace Rimworld_Animations
{
    [HotSwappable]
    [HarmonyPatch(typeof(Pawn_DrawTracker), "DrawPos", MethodType.Getter)]
    public static class HarmonyPatch_Pawn_DrawTracker
    {
        public static bool Prefix(ref Pawn ___pawn, ref Vector3 __result)
        {
            CompBodyAnimator bodyAnim = ___pawn.TryGetComp<CompBodyAnimator>();

            if (bodyAnim != null && bodyAnim.isAnimating)
            {
                Vector3? anchorVec = ___pawn.TryGetComp<CompBodyAnimator>().anchor;
                __result = (anchorVec.HasValue ? anchorVec.Value : __result) + ___pawn.TryGetComp<CompBodyAnimator>().deltaPos;

                return false;
            }
            return true;
        }
    }
}