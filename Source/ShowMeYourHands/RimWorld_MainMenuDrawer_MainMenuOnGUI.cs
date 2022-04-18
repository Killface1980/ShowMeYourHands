using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FacialStuff.GraphicsFS;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using WHands;

namespace ShowMeYourHands;

[HarmonyPatch(typeof(MainMenuDrawer), "MainMenuOnGUI")]
public static class RimWorld_MainMenuDrawer_MainMenuOnGUI
{
    public static bool alreadyRun;

    private static List<ThingDef> doneWeapons = new();

    [HarmonyPostfix]
    public static void MainMenuOnGUI()
    {
        if (alreadyRun)
        {
            return;
        }

        alreadyRun = true;

        LongEventHandler.ExecuteWhenFinished(UpdateHandDefinitions);

        MethodInfo original                 = typeof(PawnRenderer).GetMethod("DrawEquipmentAiming");
        Patches drawEquipmentAimingPatches  = Harmony.GetPatchInfo(original);
        MethodInfo saveWeaponLocationMethod = typeof(PawnRenderer_DrawEquipmentAiming).GetMethod(nameof(PawnRenderer_DrawEquipmentAiming.DrawEquipmentAimingPrefix));

        if (drawEquipmentAimingPatches is null)
        {
            ShowMeYourHandsMain.LogMessage("There seem to be no patches for DrawEquipmentAiming");
                ShowMeYourHandsMain.harmony.Patch(original, new HarmonyMethod(saveWeaponLocationMethod, Priority.High));
            return;
        }

        List<string> modifyingPatches = new()
        {
            "com.ogliss.rimworld.mod.VanillaWeaponsExpandedLaser",
            "com.github.automatic1111.gunplay",
            "com.o21toolbox.rimworld.mod",
            "com.github.automatic1111.rimlaser"
        };

        ShowMeYourHandsMain.harmony.Patch(original, new HarmonyMethod(saveWeaponLocationMethod, Priority.First));

        foreach (HarmonyLib.Patch patch in drawEquipmentAimingPatches.Prefixes.Where(patch => modifyingPatches.Contains(patch.owner)))
        {
            ShowMeYourHandsMain.harmony.Patch(original, new HarmonyMethod(saveWeaponLocationMethod, -1, null, new[] { patch.owner }));
        }

        ShowMeYourHandsMain.harmony.Patch(original, new HarmonyMethod(saveWeaponLocationMethod, Priority.Last));

        drawEquipmentAimingPatches = Harmony.GetPatchInfo(original);

        if (drawEquipmentAimingPatches.Prefixes.Count > 0)
        {
            ShowMeYourHandsMain.LogMessage($"{drawEquipmentAimingPatches.Prefixes.Count} current active prefixes");
            foreach (HarmonyLib.Patch patch in drawEquipmentAimingPatches.Prefixes.OrderByDescending(patch => patch.priority))
            {
                if (ShowMeYourHandsMain.knownPatches.Contains(patch.owner))
                {
                    ShowMeYourHandsMain.LogMessage(
                        $"Prefix - Owner: {patch.owner}, Method: {patch.PatchMethod.Name}, Prio: {patch.priority}");
                }
                else
                {
                    ShowMeYourHandsMain.LogMessage(
                        $"There is an unexpected patch of the weapon-rendering function. This may affect hand-positions. Please report the following information to the author of the 'Show Me Your Hands'-mod\nPrefix - Owner: {patch.owner}, Method: {patch.PatchMethod.Name}, Prio: {patch.priority}",
                        false, true);
                }
            }
        }

        if (drawEquipmentAimingPatches.Transpilers.Count <= 0)
        {
            return;
        }

        ShowMeYourHandsMain.LogMessage($"{drawEquipmentAimingPatches.Transpilers.Count} current active transpilers");
        foreach (HarmonyLib.Patch patch in drawEquipmentAimingPatches.Transpilers.OrderByDescending(patch => patch.priority))
        {
            if (ShowMeYourHandsMain.knownPatches.Contains(patch.owner))
            {
                ShowMeYourHandsMain.LogMessage(
                    $"Transpiler - Owner: {patch.owner}, Method: {patch.PatchMethod}, Prio: {patch.priority}");
            }
            else
            {
                ShowMeYourHandsMain.LogMessage(
                    $"There is an unexpected patch of the weapon-rendering function. This may affect hand-positions. Please report the following information to the author of the 'Show Me Your Hands'-mod\nTranspiler {patch.index}. Owner: {patch.owner}, Method: {patch.PatchMethod}, Prio: {patch.priority}",
                    false, true);
            }
        }


    }

    public static void UpdateHandDefinitions()
    {
        doneWeapons = new List<ThingDef>();
        string currentStage = "LoadFromSettings";

        try
        {
            LoadFromSettings();
            currentStage = "LoadFromDefs";
            LoadFromDefs();
            currentStage = "FigureOutTheRest";
            FigureOutTheRest();
        }
        catch (Exception exception)
        {
            ShowMeYourHandsMain.LogMessage(
                $"Failed to save some settings, if debugging the stage it failed was {currentStage}.\n{exception}");
        }

        ShowMeYourHandsMain.LogMessage($"Defined hand definitions of {doneWeapons.Count} weapons", true);
    }

    public static void FigureOutSpecific(ThingDef weapon)
    {
        WhandCompProps compProps = weapon.GetCompProperties<WhandCompProps>();
        if (compProps == null)
        {
            compProps = new WhandCompProps
            {
                compClass = typeof(WhandComp)
            };
            if (weapon.IsMeleeWeapon)
            {
                compProps.MainHand = new Vector3(-0.25f, 0.3f, 0);
            }
            else
            {
                compProps.SecHand = FaceTextures.IsWeaponLong(weapon, out Vector3 mainHand, out Vector3 secHand)
                    ? secHand
                    : Vector3.zero;
                compProps.MainHand = mainHand;
            }

            weapon.comps.Add(compProps);
        }
        else
        {
            if (weapon.IsMeleeWeapon)
            {
                compProps.MainHand = new Vector3(-0.25f, 0.3f, 0);
            }
            else
            {
                compProps.SecHand = FaceTextures.IsWeaponLong(weapon, out Vector3 mainHand, out Vector3 secHand)
                    ? secHand
                    : Vector3.zero;
                compProps.MainHand = mainHand;
            }
        }
    }

    private static void FigureOutTheRest()
    {
        foreach (ThingDef weapon in from weapon in DefDatabase<ThingDef>.AllDefsListForReading
                 where weapon.IsWeapon && !weapon.destroyOnDrop &&
                       !doneWeapons.Contains(weapon)
                 select weapon)
        {
            if (ShowMeYourHandsMod.IsShield(weapon))
            {
                ShowMeYourHandsMain.LogMessage($"Ignoring {weapon.defName} is probably a shield");
                continue;
            }

            WhandCompProps compProps = weapon.GetCompProperties<WhandCompProps>();
            if (compProps == null)
            {
                compProps = new WhandCompProps
                {
                    compClass = typeof(WhandComp)
                };
                if (weapon.IsMeleeWeapon)
                {
                    compProps.MainHand = new Vector3(-0.25f, 0.3f, 0);
                }
                else
                {
                    compProps.SecHand = FaceTextures.IsWeaponLong(weapon, out Vector3 mainHand, out Vector3 secHand)
                        ? secHand
                        : Vector3.zero;
                    compProps.MainHand = mainHand;
                }

                weapon.comps.Add(compProps);
            }
            else
            {
                if (weapon.IsMeleeWeapon)
                {
                    compProps.MainHand = new Vector3(-0.25f, 0.3f, 0);
                }
                else
                {
                    compProps.SecHand = FaceTextures.IsWeaponLong(weapon, out Vector3 mainHand, out Vector3 secHand)
                        ? secHand
                        : Vector3.zero;
                    compProps.MainHand = mainHand;
                }
            }

            doneWeapons.Add(weapon);
        }
    }

    private static void LoadFromSettings()
    {
        if (ShowMeYourHandsMod.instance.Settings.ManualMainHandPositions == null)
        {
            return;
        }

        foreach (KeyValuePair<string, SaveableVector3> keyValuePair in ShowMeYourHandsMod.instance?.Settings?.ManualMainHandPositions)
        {
            ThingDef weapon = DefDatabase<ThingDef>.GetNamedSilentFail(keyValuePair.Key);
            if (weapon == null)
            {
                continue;
            }

            WhandCompProps compProps = weapon.GetCompProperties<WhandCompProps>();
            if (compProps == null)
            {
                compProps = new WhandCompProps

                {
                    compClass = typeof(WhandComp),
                    MainHand = keyValuePair.Value.ToVector3(),
                    SecHand =
                        ShowMeYourHandsMod.instance?.Settings?.ManualOffHandPositions
                            .ContainsKey(keyValuePair.Key) == true
                            ? ShowMeYourHandsMod.instance.Settings.ManualOffHandPositions[keyValuePair.Key]
                                .ToVector3()
                            : Vector3.zero,
                    MainHandAngle = keyValuePair.Value.ToAngleFloat(),
                    SecHandAngle =
                        ShowMeYourHandsMod.instance?.Settings?.ManualOffHandPositions
                            .ContainsKey(keyValuePair.Key) == true
                            ? ShowMeYourHandsMod.instance.Settings.ManualOffHandPositions[keyValuePair.Key]
                                .ToAngleFloat()
                            : 0f,

                };
                weapon.comps.Add(compProps);
            }
            else
            {
                compProps.MainHand = keyValuePair.Value.ToVector3();
                compProps.MainHandAngle = keyValuePair.Value.ToAngleFloat();

                if (ShowMeYourHandsMod.instance?.Settings?.ManualOffHandPositions.ContainsKey(keyValuePair.Key) == true)
                {
                    compProps.SecHand = ShowMeYourHandsMod.instance.Settings.ManualOffHandPositions[keyValuePair.Key]
                        .ToVector3();
                    compProps.SecHandAngle = ShowMeYourHandsMod.instance.Settings.ManualOffHandPositions[keyValuePair.Key]
                        .ToAngleFloat();
                }
                else
                {
                    compProps.SecHand = Vector3.zero;
                    compProps.SecHandAngle = 0f;
                }
            }

            doneWeapons.Add(weapon);
        }
    }

    public static void LoadFromDefs(ThingDef specificDef = null)
    {
        List<ClutterHandsTDef> defs = DefDatabase<ClutterHandsTDef>.AllDefsListForReading;
        if (specificDef == null)
        {
            ShowMeYourHandsMod.DefinedByDef = new HashSet<string>();
        }

        foreach (ClutterHandsTDef handsTDef in defs)
        {
            if (handsTDef.WeaponCompLoader.Count <= 0)
            {
                return;
            }

            foreach (ClutterHandsTDef.CompTargets weaponSets in handsTDef.WeaponCompLoader)
            {
                if (weaponSets.ThingTargets.Count <= 0)
                {
                    continue;
                }

                foreach (string weaponDefName in weaponSets.ThingTargets)
                {
                    if (specificDef != null && weaponDefName != specificDef.defName)
                    {
                        continue;
                    }

                    ThingDef weapon = DefDatabase<ThingDef>.GetNamedSilentFail(weaponDefName);
                    if (weapon == null)
                    {
                        continue;
                    }

                    if (specificDef == null && doneWeapons.Contains(weapon))
                    {
                        continue;
                    }

                    WhandCompProps compProps = weapon.GetCompProperties<WhandCompProps>();
                    if (compProps == null)
                    {
                        compProps = new WhandCompProps
                        {
                            compClass = typeof(WhandComp),
                            MainHand = weaponSets.MainHand,
                            SecHand = weaponSets.SecHand,

                            WeaponPositionOffset = weaponSets.WeaponPositionOffset,
                            AimedWeaponPositionOffset = weaponSets.AimedWeaponPositionOffset,
                            MainHandAngle = weaponSets.MainHandAngle,
                            SecHandAngle = weaponSets.SecHandAngle

                    };
                        weapon.comps.Add(compProps);
                    }
                    else
                    {
                        compProps.MainHand = weaponSets.MainHand;
                        compProps.SecHand = weaponSets.SecHand;
                        
                        compProps.WeaponPositionOffset = weaponSets.WeaponPositionOffset;
                        compProps.AimedWeaponPositionOffset = weaponSets.AimedWeaponPositionOffset;

                        compProps.MainHandAngle = weaponSets.MainHandAngle;
                        compProps.SecHandAngle = weaponSets.SecHandAngle;

                    }

                    ShowMeYourHandsMod.DefinedByDef.Add(weapon.defName);
                    if (specificDef != null)
                    {
                        return;
                    }

                    doneWeapons.Add(weapon);
                }
            }
        }
    }
}