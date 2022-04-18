using System;
using FacialStuff;
using UnityEngine;
using Verse;

namespace ShowMeYourHands;

// [HarmonyPatch(typeof(PawnRenderer), "DrawEquipmentAiming", typeof(Thing), typeof(Vector3), typeof(float))]
[ShowMeYourHandsMod.HotSwappable]
public class PawnRenderer_DrawEquipmentAiming
{
    //[HarmonyPrefix]
    //[HarmonyPriority(Priority.High)]
    //// JecsTools Oversized overrides always
    //// Gunplay overrides if animations is turned on
    //// [O21] Toolbox overrides if animations is turned on
    //// Yayo Combat 3 overrides if animations is turned on
    //[HarmonyBefore("jecstools.jecrell.comps.oversized",
    //    "rimworld.androitiers-jecrell.comps.oversized",
    //    "com.github.automatic1111.gunplay",
    //    "com.o21toolbox.rimworld.mod",
    //    "com.yayo.combat",
    //    "com.yayo.combat3")]
    public static void DrawEquipmentAimingPrefix(PawnRenderer __instance, Thing eq, ref Vector3 drawLoc, ref float aimAngle)
    {
        SaveWeaponLocationsAndDoOffsets(__instance.graphics.pawn, eq, ref drawLoc, ref aimAngle);
    }

    public static void SaveWeaponLocationsAndDoOffsets(Pawn pawn, Thing eq, ref Vector3 drawLoc,
        ref float aimAngle)
    {
        //ShowMeYourHandsMain.LogMessage($"Saving from vanilla {eq.def.defName}, {drawLoc}, {aimAngle}");
        ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(drawLoc, aimAngle);


        //ShowMeYourHandsMain.LogMessage($"Saving from vanilla {eq.def.defName}, {drawLoc}, {aimAngle}");
        if (pawn == null || !pawn.GetCompAnim(out CompBodyAnimator compAnim))
        {
            return;
        }

        // flips the weapon display
        if (compAnim.CurrentRotation == Rot4.North && !(pawn.stances.curStance is Stance_Busy stance_Busy &&
                                                       !stance_Busy.neverAimWeapon && stance_Busy.focusTarg.IsValid))
        {
            if (aimAngle > 20f && aimAngle < 160f)
            {
                aimAngle +=90f;
            }
            else if (aimAngle > 200f && aimAngle < 340f)
            {
                aimAngle -= 90f;
            }
        }

        // Log.ErrorOnce(aimAngle.ToString("N0"), Mathf.RoundToInt(aimAngle));

        // Log.ErrorOnce(num.ToString(), 3);
        ;
        /*
        if (compAnim.CurrentRotation == Rot4.North)
        {
            if (aimAngle == 143f)
            {
                aimAngle = 217f;
            }
            else if (aimAngle == 217f)
            {
                aimAngle = 143f;
            }
        }
        */

        WhandCompProps extensions = eq.def.GetCompProperties<WhandCompProps>();
        if (extensions == null)
        {
            return;
        }

        bool flipped = (compAnim.CurrentRotation == Rot4.West || compAnim.CurrentRotation == Rot4.North);

        // ShowMeYourHandsMain.LogMessage($"Changing angle and position {eq.def.defName}, {drawLoc}, {aimAngle}");

        compAnim.CalculatePositionsWeapon(extensions, out Vector3 weaponOffset, flipped);
        drawLoc += weaponOffset;
        
        // ShowMeYourHandsMain.LogMessage($"New angle and position {eq.def.defName}, {drawLoc}, {aimAngle}");

        ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(drawLoc, aimAngle);


    }
}