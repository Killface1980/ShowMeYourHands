using HarmonyLib;
using PawnAnimator.Harmony;
using UnityEngine;
using Verse;
using yayoCombat;

namespace ShowMeYourHandsYayoAdapted;

[HotSwappable]
[HarmonyPatch(typeof(PawnRenderer_override), nameof(PawnRenderer_override.DrawEquipmentAiming))]
public static class PawnRenderer_override_DrawEquipmentAiming
{
    public static void Prefix(PawnRenderer instance, Thing eq, ref Vector3 drawLoc, ref float aimAngle, Pawn pawn, Stance_Busy stance_Busy = null, bool pffhand = false)
    {

        if (!yayoCombat.yayoCombat.advAni)
        {
            return;
        }
        PawnRenderer_DrawEquipmentAiming.SaveWeaponLocationsAndDoOffsets(pawn,  eq,ref drawLoc, ref aimAngle);
        //ShowMeYourHandsMain.LogMessage($"Saving from dual wield {eq.def.defName}, {drawLoc}, {aimAngle}");
        // ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(drawLoc, aimAngle);
    }
}