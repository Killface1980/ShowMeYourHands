using FacialStuff;
using UnityEngine;
using Verse;
using yayoAni;

namespace ShowMeYourHands;

[HotSwappable]
[HarmonyLib.HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
public static class RenderPawnAt_Patch
{
    static pawnDrawData pdd;

    // ReSharper disable once UnusedParameter.Global
    public static void Prefix(PawnRenderer __instance, Vector3 drawLoc, Rot4? rotOverride = null, bool neverAimWeapon = false)
    {
        Pawn pawn = __instance.graphics.pawn;

        //CompFace compFace = pawn.GetCompFace();
        if (pawn != null)
        {

        };
        if (!pawn.GetCompAnim(out CompBodyAnimator compAnim))
        {
            return;
        }
        pdd = dataUtility.GetData(pawn);
        compAnim.CurrentRotation = pdd.fixed_rot ?? pawn.Rotation;
        compAnim.Offset_Angle = pdd.offset_angle;
        compAnim.Offset_Pos = pdd.offset_pos;


#pragma warning restore CS0162
    }

}