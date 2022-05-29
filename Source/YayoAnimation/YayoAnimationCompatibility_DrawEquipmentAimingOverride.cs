using System;
using FacialStuff;
using HarmonyLib;
using ShowMeYourHands.Harmony;
using UnityEngine;
using Verse;
using yayoAni;

namespace ShowMeYourHands;
[HotSwappable]
[HarmonyPatch( "yayoAni.patch_DrawEquipmentAiming", "Prefix")]
public static class YayoAnimationCompatibility_DrawEquipmentAimingOverride
{
    public static void Prefix(PawnRenderer __instance, ref Thing eq, ref Vector3 drawLoc, ref float aimAngle)
    {
        if (!core.val_combat)
        {
            return;
        }
        if (aimAngle > 200f && aimAngle < 340f)
        {
            drawLoc.y = -0.01f;

        }
        Pawn pawn = __instance?.graphics?.pawn;
        // pawn is null, __instance is null ...
        PawnRenderer_DrawEquipmentAiming.SaveWeaponLocationsAndDoOffsets(pawn, eq, ref drawLoc, ref aimAngle);

        return;
        //ShowMeYourHandsMain.LogMessage($"Saving from dual wield {eq.def.defName}, {drawLoc}, {aimAngle}");
        ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(drawLoc, aimAngle);
        return;

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
