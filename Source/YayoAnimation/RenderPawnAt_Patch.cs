using PawnAnimator;
using UnityEngine;
using Verse;
using yayoAni;

namespace ShowMeYourHandsYayoAni;

[HotSwappable]
[HarmonyLib.HarmonyPatch(typeof(yayo), nameof(yayo.checkAni))]
public static class checkAni_Patch
{
    static pawnDrawData pdd;

    // ReSharper disable once UnusedParameter.Global
    public static void Postfix(Pawn pawn, ref Vector3 pos, Rot4 rot)
    {
        if (pawn == null || pawn.Dead)
        {
            return;
        }
        //CompFace compFace = pawn.GetCompFace();
        if (!pawn.GetCompAnim(out CompBodyAnimator compAnim))
        {
            return;
        }
        pdd = dataUtility.GetData(pawn);
        if (pdd == null) return;
        compAnim.CurrentRotation = pdd.fixed_rot ?? pawn.Rotation;
        compAnim.Offset_Angle = pdd.offset_angle;
        compAnim.Offset_Pos = pdd.offset_pos;


#pragma warning restore CS0162
    }

}