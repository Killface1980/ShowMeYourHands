using FacialStuff;
using HarmonyLib;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ShowMeYourHands.FSWalking.Harmony
{
    [ShowMeYourHandsMod.HotSwappable]
    [HarmonyPatch(typeof(JobDriver), nameof(JobDriver.ModifyCarriedThingDrawPos))]
    internal class ModifyCarriedThingDrawPos_Postfix
    {
        public static void PostFix(ref bool __result, JobDriver __instance, ref Vector3 drawPos, ref bool behind, ref bool flip, Pawn ___pawn)
        {
            Log.ErrorOnce("yes", 0);

            if (__result && behind)
            {
                return;
            }
            if (!ShowMeYourHandsMod.instance.Settings.ShowWhenCarry)
            {
                return;
            }
            if (!___pawn.GetCompAnim(out CompBodyAnimator anim))
            {
                return;
            }

            if (anim.CurrentRotation == Rot4.North)
            {
                behind = true;
                flip = true;
                __result = true;
            }
        }
    }
}
