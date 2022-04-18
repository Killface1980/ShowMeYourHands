using System;
using HarmonyLib;
using UnityEngine;
using Verse;
using yayoCombat;

namespace ShowMeYourHands;

[HarmonyPatch(typeof(PawnRenderer_override), nameof(PawnRenderer_override.DrawEquipmentAiming))]
public static class PawnRenderer_DrawEquipmentAiming_DrawEquipmentAimingOverride
{
    public static void Prefix(PawnRenderer __instance, Thing eq, ref Vector3 drawLoc, ref float aimAngle, Pawn pawn, Stance_Busy stance_Busy = null, bool pffhand = false)
    {
        PawnRenderer_DrawEquipmentAiming.SaveWeaponLocationsAndDoOffsets(pawn,  eq,ref drawLoc, ref aimAngle);
        //ShowMeYourHandsMain.LogMessage($"Saving from dual wield {eq.def.defName}, {drawLoc}, {aimAngle}");
        // ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(drawLoc, aimAngle);
    }
}