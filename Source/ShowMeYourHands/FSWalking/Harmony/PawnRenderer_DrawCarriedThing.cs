using FacialStuff;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ShowMeYourHands;

[ShowMeYourHandsMod.HotSwappable]
[HarmonyPatch(typeof(PawnRenderer), "DrawCarriedThing")]
public static class PawnRenderer_DrawCarriedThing
{
    public static void Prefix(ref Vector3 drawLoc, Pawn ___pawn)
    {
        Thing carriedThing = ___pawn?.carryTracker?.CarriedThing;
        if (carriedThing == null)
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

        Vector3 handPos = drawLoc;

        bool behind = false;
        bool flip = false;
        if (___pawn.CurJob == null || !___pawn.jobs.curDriver.ModifyCarriedThingDrawPos(ref handPos, ref behind, ref flip))
        {
            if (carriedThing is Pawn || carriedThing is Corpse)
            {
                handPos += new Vector3(0.44f, 0f, 0f);
            }
            else
            {
                handPos += new Vector3(0.18f, 0f, 0.05f);
            }
        }

        if (!behind && anim.CurrentRotation == Rot4.North)
        {
            drawLoc.y -= 2 * Offsets.YOffset_CarriedThing;
        }

        handPos.y += 2 * Offsets.YOffset_CarriedThing * (behind ? -1f : 1f);// + 2* Offsets.YOffset_HandsFeetOver;
		anim.DrawHands(0f, handPos, carriedThing, flip);

    }
}