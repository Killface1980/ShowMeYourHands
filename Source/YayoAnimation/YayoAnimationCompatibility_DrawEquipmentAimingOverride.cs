using System;
using FacialStuff;
using UnityEngine;
using Verse;

namespace ShowMeYourHands;

public static class YayoAnimationCompatibility_DrawEquipmentAimingOverride
{
    public static void SaveWeaponLocation(PawnRenderer __instance, ref Thing eq, ref Vector3 drawLoc, ref float aimAngle)
    {
        PawnRenderer_DrawEquipmentAiming.SaveWeaponLocationsAndDoOffsets(__instance.graphics.pawn, eq, ref drawLoc, ref aimAngle);
        return;
        //ShowMeYourHandsMain.LogMessage($"Saving from dual wield {eq.def.defName}, {drawLoc}, {aimAngle}");
        ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(drawLoc, aimAngle);
        return;
        Pawn pawn = __instance.graphics.pawn;

        //ShowMeYourHandsMain.LogMessage($"Saving from vanilla {eq.def.defName}, {drawLoc}, {aimAngle}");
        if (pawn == null || !pawn.GetCompAnim(out CompBodyAnimator compAnim))
        {
            return;
        };
        WhandCompProps extensions = eq.def.GetCompProperties<WhandCompProps>();
        if (extensions == null)
        {
            return;
        }
        bool flipped = (compAnim.CurrentRotation == Rot4.West || compAnim.CurrentRotation == Rot4.North);

        float size = compAnim.GetBodysizeScaling();
        // TODO: Options?

        // ShowMeYourHandsMain.LogMessage($"Changing angle and position {eq.def.defName}, {drawLoc}, {aimAngle}");

        compAnim.CalculatePositionsWeapon(extensions, out Vector3 weaponOffset, flipped);
        drawLoc += weaponOffset * size;
        ShowMeYourHandsMain.LogMessage($"New angle and position {eq.def.defName}, {drawLoc}, {aimAngle}");

    }
}