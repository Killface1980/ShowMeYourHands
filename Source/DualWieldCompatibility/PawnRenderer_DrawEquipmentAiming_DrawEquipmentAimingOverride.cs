﻿using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ShowMeYourHands;

[HarmonyPatch(typeof(DualWield.Harmony.PawnRenderer_DrawEquipmentAiming), "DrawEquipmentAimingOverride",
    typeof(Thing), typeof(Vector3),
    typeof(float))]
public static class PawnRenderer_DrawEquipmentAiming_DrawEquipmentAimingOverride
{
    [HarmonyPrefix]
    public static void SaveWeaponLocation(ref Thing eq, ref Vector3 drawLoc, ref float aimAngle)
    {
        //ShowMeYourHandsMain.LogMessage($"Saving from dual wield {eq.def.defName}, {drawLoc}, {aimAngle}");
        ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(drawLoc, aimAngle);
    }
}