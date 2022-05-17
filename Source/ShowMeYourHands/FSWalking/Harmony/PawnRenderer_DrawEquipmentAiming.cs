using FacialStuff;
using RimWorld;
using System;
using System.Linq;
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

        if (compAnim.CurrentRotation == Rot4.North && !pawn.Aiming())
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

        // Log.ErrorOnce(aimAngle.ToString("N0"), Mathf.RoundToInt(aimAngle)*1000);

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

        bool isFirstHandWeapon = eq != pawn.equipment.Primary;
        bool flipped = aimAngle is > 200f and < 340f;

        // ShowMeYourHandsMain.LogMessage($"Changing angle and position {eq.def.defName}, {drawLoc}, {aimAngle}");

        compAnim.CalculatePositionsWeapon(extensions, out Vector3 weaponOffset, flipped);
        // Log.Message(eq.def + " - " + weaponOffset.x.ToString("N3") + "-" + weaponOffset.y.ToString("N3") + "-" +
        //             weaponOffset.z.ToString("N3"));

        if (pawn?.equipment?.AllEquipmentListForReading != null && pawn.equipment.AllEquipmentListForReading.Count == 2)
        {
            drawLoc.z += weaponOffset.z;
            drawLoc.x += weaponOffset.x * 0.5f * (isFirstHandWeapon ? -1f : 1f);
        }
        else
        {
            drawLoc += weaponOffset;
        }

        int equipment = isFirstHandWeapon ? (int)TweenThing.Equipment1 : (int)TweenThing.Equipment2;

        bool noTween = false;// pawn.Drafted;
                             //if (pawn.pather != null && pawn.pather.MovedRecently(5))
                             //{
                             //    noTween = true;
                             //}
#if includeTweens
        switch (compAnim.Vector3Tweens[equipment].State)
        {
            case TweenState.Running:
                if (noTween || compAnim.IsMoving)
                {
                    compAnim.Vector3Tweens[equipment].Stop(StopBehavior.ForceComplete);
                }

                drawLoc = compAnim.Vector3Tweens[equipment].CurrentValue;
                // Log.Message("running");
                break;

            case TweenState.Paused:
                break;

            case TweenState.Stopped:
                if (noTween || (compAnim.IsMoving))
                {
                    break;
                }

                ScaleFunc scaleFunc = ScaleFuncs.SineEaseOut;

                Vector3 start = compAnim.LastPosition[equipment];
                float distance = Vector3.Distance(start, drawLoc);
                if (distance > 0.05f && distance < 0.5f)
                {
                     float duration = Mathf.Abs(distance * 50f);
                     if (start != Vector3.zero && duration > 12f)
                     {
                        // Log.Message("Distance: " +distance.ToString("N2") + " - duration: " + duration);
                         start.y = drawLoc.y;
                         compAnim.Vector3Tweens[equipment].Start(start, drawLoc, Mathf.Min(duration, 15f), scaleFunc);
                         drawLoc = start;
                     }
                }

                break;
        }

        switch (compAnim.FloatTweens[equipment].State)
        {
            case TweenState.Running:
                if (noTween || compAnim.IsMoving)
                {
                    compAnim.FloatTweens[equipment].Stop(StopBehavior.ForceComplete);
                }

                aimAngle = compAnim.FloatTweens[equipment].CurrentValue;
                // Log.Message("running");
                break;

            case TweenState.Paused:
                break;

            case TweenState.Stopped:
                if (noTween || (compAnim.IsMoving))
                {
                    break;
                }
                break; // Todo: currently broken with gun twirling
                ScaleFunc scaleFunc = ScaleFuncs.SineEaseOut;

                float start = compAnim.LastAimAngle[equipment];
                float angleDiff = Mathf.Abs(start)- Mathf.Abs(aimAngle);

                if (Mathf.Abs(angleDiff) < 145f)
                {
                     float duration = Mathf.Abs(angleDiff * 0.35f);
                     if (angleDiff > 10f)
                     {
                         // Log.Message("Distance: " +distance.ToString("N2") + " - duration: " + duration);
                         compAnim.FloatTweens[equipment].Start(start, aimAngle, Mathf.Min(duration, 15f), scaleFunc);
                         aimAngle = start;
                     }
                }

                if (angleDiff > 0f)
                {
                    ;
                }

            {
                   // Log.Message("Start: " + start + " - Ende: " + drawLoc);
                }
                break;
        }
#endif

        Vector3 newDrawLoc = drawLoc;
        float newAimAngle  = aimAngle;
        float currentY     = newDrawLoc.y;

        if (ShowMeYourHandsMain.rightHandLocations.ContainsKey(eq) && ShowMeYourHandsMain.rightHandLocations[eq].Item1 != Vector3.zero)
        {
            drawLoc += ShowMeYourHandsMain.rightHandLocations[eq].Item1;
            if (compAnim.CurrentRotation.IsHorizontal)
            {
                if (compAnim.CurrentRotation == Rot4.West)
                {
                    drawLoc.y = Mathf.Max(drawLoc.y, currentY);
                }
                else
                {
                    drawLoc.y = Mathf.Min(drawLoc.y, currentY);

                }
            }
             // aimAngle += ShowMeYourHandsMain.rightHandLocations[eq].Item2;
        }
        else
        {
            if (ShowMeYourHandsMain.leftHandLocations.ContainsKey(eq) && ShowMeYourHandsMain.leftHandLocations[eq].Item1 != Vector3.zero)
            {
                drawLoc += ShowMeYourHandsMain.leftHandLocations[eq].Item1;
                if (compAnim.CurrentRotation.IsHorizontal)
                {
                    if (compAnim.CurrentRotation == Rot4.West)
                    {
                        drawLoc.y = Mathf.Max(drawLoc.y, currentY);
                    }
                    else
                    {
                        drawLoc.y = Mathf.Min(drawLoc.y, currentY);

                    }
                }

                 //  aimAngle += ShowMeYourHandsMain.leftHandLocations[eq].Item2 ;
            }
        }

        CompEquippable compEquippable = eq.TryGetComp<CompEquippable>();
        if (compEquippable != null)
        {
            EquipmentUtility.Recoil(eq.def, EquipmentUtility.GetRecoilVerb(compEquippable.AllVerbs), out Vector3 drawOffset, out float angleOffset, aimAngle);
            newDrawLoc += drawOffset;
            newAimAngle += angleOffset;
        }
#if includeTweens

        compAnim.LastPosition[(int)equipment] = drawLoc;
        compAnim.LastAimAngle[(int)equipment] = aimAngle;
#endif
        // ShowMeYourHandsMain.LogMessage($"New angle and position {eq.def.defName}, {drawLoc}, {aimAngle}");
        if (ShowMeYourHandsMain.OversizedWeaponLoaded || ShowMeYourHandsMain.EnableOversizedLoaded)
        {
            // CompProperties thingComp =
            //     eq.def.comps.FirstOrDefault(y => y.GetType().ToString().Contains("CompOversizedWeapon"));
            if (!eq.def.IsMeleeWeapon) // oversized weapon

              //  if (eq.def.graphicData.drawSize.x != 1f)
            {
                newAimAngle -= eq.def.equippedAngleOffset * (flipped ? -1f : 1f);
            }
        }

        ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(newDrawLoc, newAimAngle);

        //  compAnim.DrawHands(0f, newDrawLoc, eq, flipped);
    }
}