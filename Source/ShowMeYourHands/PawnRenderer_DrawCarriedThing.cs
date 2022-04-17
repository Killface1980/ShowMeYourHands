using FacialStuff;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ShowMeYourHands;

[HarmonyPatch(typeof(PawnRenderer), "DrawCarriedThing")]
public static class PawnRenderer_DrawCarriedThing
{
    public static void Postfix(Vector3 drawLoc, Pawn ___pawn)
    {
        Thing carriedThing = ___pawn.carryTracker?.CarriedThing;
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


        Vector3 drawPos = drawLoc;
        bool behind = false;
        bool flip = false;
        if (___pawn.CurJob == null || !___pawn.jobs.curDriver.ModifyCarriedThingDrawPos(ref drawPos, ref behind, ref flip))
        {
            if (carriedThing is Pawn || carriedThing is Corpse)
            {
                drawPos += new Vector3(0.44f, 0f, 0f);
            }
            else
            {
                drawPos += new Vector3(0.18f, 0f, 0.05f);
            }
        }


        if (behind || ___pawn.Rotation == Rot4.North)
        {
            drawPos.y -=  - Offsets.YOffset_CarriedThing;
        }

		anim.DrawHands(Quaternion.identity, drawPos, carriedThing, flip);

    }
}