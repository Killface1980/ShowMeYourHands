using System;
using System.Linq;
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

        if (!ShowMeYourHandsMod.instance.Settings.UseHands)
        {
            return;

        }

        //ShowMeYourHandsMain.LogMessage($"Saving from vanilla {eq.def.defName}, {drawLoc}, {aimAngle}");
        if (pawn == null || !pawn.GetCompAnim(out CompBodyAnimator compAnim))
        {
            return;
        }

        // flips the weapon display
        if (compAnim.CurrentRotation == Rot4.North && !(pawn.stances.curStance is Stance_Busy stance_Busy &&
                                                       !stance_Busy.neverAimWeapon && stance_Busy.focusTarg.IsValid))
        {
            float baseAngle = aimAngle - 180f;
            aimAngle = 180f - baseAngle;

            //if (aimAngle > 20f && aimAngle < 160f) // right
            //{
            //    float baseAngle = aimAngle - 180f;
            //    aimAngle = 180f - baseAngle;
            //
            //    // aimAngle +=90f;
            //}
            //else if (aimAngle > 200f && aimAngle < 340f) // left
            //{
            //    float baseAngle = aimAngle - 180f;
            //    aimAngle = 180f - baseAngle;
            //
            //    // aimAngle -= 90f;
            //}
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
        // Log.Message(eq.def + " - " + weaponOffset.x.ToString("N3") + "-" + weaponOffset.y.ToString("N3") + "-" +
        //             weaponOffset.z.ToString("N3"));

        if (pawn?.equipment?.AllEquipmentListForReading != null && pawn.equipment.AllEquipmentListForReading.Count == 2)
        {
                drawLoc.z += weaponOffset.z;
        
/*                ThingWithComps offHandWeapon = (from weapon in pawn.equipment.AllEquipmentListForReading
                where weapon != pawn?.equipment?.Primary as ThingWithComps
                select weapon).First();
            WhandCompProps offhandComp = offHandWeapon?.def?.GetCompProperties<WhandCompProps>();
            if (offhandComp != null)
            {
            //    drawLoc.z += weaponOffset.z;
            }
            else
            {
            //    drawLoc += weaponOffset;
            }
*/
        }
        else
        {
            drawLoc += weaponOffset;
        }


        // ShowMeYourHandsMain.LogMessage($"New angle and position {eq.def.defName}, {drawLoc}, {aimAngle}");

        ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(drawLoc, aimAngle);


    }
}