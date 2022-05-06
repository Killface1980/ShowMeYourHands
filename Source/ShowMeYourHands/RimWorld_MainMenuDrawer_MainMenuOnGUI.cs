using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FacialStuff;
using FacialStuff.Defs;
using FacialStuff.GraphicsFS;
using HarmonyLib;
using RimWorld;
using ShowMeYourHands.FSWalking;
using UnityEngine;
using Verse;
using WHands;
using static ShowMeYourHands.ShowMeYourHandsMain;

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

        AnimalPawnCompsBodyDefImport();

        AnimalPawnCompsImportFromAnimationTargetDefs();


        MethodInfo original                 = typeof(PawnRenderer).GetMethod("DrawEquipmentAiming");
        Patches drawEquipmentAimingPatches  = Harmony.GetPatchInfo(original);
        MethodInfo saveWeaponLocationMethod = typeof(PawnRenderer_DrawEquipmentAiming).GetMethod(nameof(PawnRenderer_DrawEquipmentAiming.DrawEquipmentAimingPrefix));

        if (drawEquipmentAimingPatches is null)
        {
            LogMessage("There seem to be no patches for DrawEquipmentAiming");
                harmony.Patch(original, new HarmonyMethod(saveWeaponLocationMethod, Priority.High));
            return;
        }

        List<string> modifyingPatches = new()
        {
            "com.ogliss.rimworld.mod.VanillaWeaponsExpandedLaser",
            "com.github.automatic1111.gunplay",
            "com.o21toolbox.rimworld.mod",
            "com.github.automatic1111.rimlaser"
        };

        harmony.Patch(original, new HarmonyMethod(saveWeaponLocationMethod, Priority.First));

        foreach (Patch patch in drawEquipmentAimingPatches.Prefixes.Where(patch => modifyingPatches.Contains(patch.owner)))
        {
            harmony.Patch(original, new HarmonyMethod(saveWeaponLocationMethod, -1, null, new[] { patch.owner }));
        }

        // harmony.Patch(AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming"), null, null, new HarmonyMethod(typeof(DrawEquipmentAiming_Patch).GetMethod("Transpiler_DrawEquipmentAiming")));


        harmony.Patch(original, new HarmonyMethod(saveWeaponLocationMethod, Priority.Last));

        drawEquipmentAimingPatches = Harmony.GetPatchInfo(original);

        if (drawEquipmentAimingPatches.Prefixes.Count > 0)
        {
            LogMessage($"{drawEquipmentAimingPatches.Prefixes.Count} current active prefixes");
            foreach (Patch patch in drawEquipmentAimingPatches.Prefixes.OrderByDescending(patch => patch.priority))
            {
                if (knownPatches.Contains(patch.owner))
                {
                    LogMessage(
                        $"Prefix - Owner: {patch.owner}, Method: {patch.PatchMethod.Name}, Prio: {patch.priority}");
                }
                else
                {
                    LogMessage(
                        $"There is an unexpected patch of the weapon-rendering function. This may affect hand-positions. Please report the following information to the author of the 'Show Me Your Hands'-mod\nPrefix - Owner: {patch.owner}, Method: {patch.PatchMethod.Name}, Prio: {patch.priority}",
                        false, true);
                }
            }
        }

        if (drawEquipmentAimingPatches.Transpilers.Count <= 0)
        {
            return;
        }

        LogMessage($"{drawEquipmentAimingPatches.Transpilers.Count} current active transpilers");
        foreach (Patch patch in drawEquipmentAimingPatches.Transpilers.OrderByDescending(patch => patch.priority))
        {
            if (knownPatches.Contains(patch.owner))
            {
                LogMessage(
                    $"Transpiler - Owner: {patch.owner}, Method: {patch.PatchMethod}, Prio: {patch.priority}");
            }
            else
            {
                LogMessage(
                    $"There is an unexpected patch of the weapon-rendering function. This may affect hand-positions. Please report the following information to the author of the 'Show Me Your Hands'-mod\nTranspiler {patch.index}. Owner: {patch.owner}, Method: {patch.PatchMethod}, Prio: {patch.priority}",
                    false, true);
            }
        }


    }
    private static void AnimalPawnCompsImportFromAnimationTargetDefs()
    {
        // ReSharper disable once PossibleNullReferenceException
        foreach (AnimationTargetDef def in DefDatabase<AnimationTargetDef>.AllDefsListForReading)
        {
            if (def.CompLoaderTargets.NullOrEmpty())
            {
                continue;
            }

            foreach (CompLoaderTargets pawnSets in def.CompLoaderTargets)
            {
                if (pawnSets == null)
                {
                    continue;
                }

                if (pawnSets.thingTargets.NullOrEmpty())
                {
                    continue;
                }

                foreach (string target in pawnSets.thingTargets)
                {
                    ThingDef thingDef = ThingDef.Named(target);
                    if (thingDef == null)
                    {
                        continue;
                    }
                    //if (DefDatabase<BodyAnimDef>
                    //   .AllDefsListForReading.Any(x => x.defName.Contains(thingDef.defName))) continue;
                    if (thingDef.HasComp(typeof(CompBodyAnimator)))
                    {
                        continue;
                    }

                    CompProperties_BodyAnimator bodyAnimator = new()
                    {
                        compClass = typeof(CompBodyAnimator),
                        handTexPath = pawnSets.handTexPath,
                        footTexPath = pawnSets.footTexPath,
                        hipOffsets = pawnSets.hipOffsets,
                        shoulderOffsets = pawnSets.shoulderOffsets,
                        armLength = pawnSets.armLength,
                        extraLegLength = pawnSets.extraLegLength,
                        extremitySize = pawnSets.extremitySize,
                        quadruped = pawnSets.quadruped,
                        bipedWithHands = pawnSets.bipedWithHands,
                        offCenterX = pawnSets.offCenterX
                    };
                    thingDef.comps?.Add(bodyAnimator);

                }
            }
        }
    }

    private static void AnimalPawnCompsBodyDefImport()
    {
        // ReSharper disable once PossibleNullReferenceException
        foreach (BodyAnimDef def in DefDatabase<BodyAnimDef>.AllDefsListForReading)
        {
            AddCompBaToThingDef(def);
        }
    }

    public static void AddCompBaToThingDef(BodyAnimDef def)
    {
        string target = def.thingTarget;
        if (target.NullOrEmpty())
        {
            return;
        }

        ThingDef thingDef = ThingDef.Named(target);
        if (thingDef == null)
        {
            return;
        }

        //if (DefDatabase<BodyAnimDef>
        //   .AllDefsListForReading.Any(x => x.defName.Contains(thingDef.defName))) continue;
        if (thingDef.HasComp(typeof(CompBodyAnimator)))
        {
            return;
        }

        CompProperties_BodyAnimator bodyAnimator = new()
        {
            compClass       = typeof(CompBodyAnimator),
            handTexPath     = def.handTexPath,
            footTexPath     = def.footTexPath,
            extremitySize   = def.extremitySize,
            quadruped       = def.quadruped,
            bipedWithHands  = def.bipedWithHands,
            shoulderOffsets = def.shoulderOffsets,
            hipOffsets      = def.hipOffsets,
            armLength       = def.armLength,
            extraLegLength  = def.extraLegLength,
            offCenterX      = def.offCenterX
        };

        thingDef.comps?.Add(bodyAnimator);
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
            LogMessage(
                $"Failed to save some settings, if debugging the stage it failed was {currentStage}.\n{exception}");
        }

        LogMessage($"Defined hand definitions of {doneWeapons.Count} weapons", true);
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
                LogMessage($"Ignoring {weapon.defName} is probably a shield");
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
        //List<Dictionary<string, SaveableVector3>> list = new()
        //{
        //    ShowMeYourHandsMod.instance?.Settings?.ManualMainHandPositions,
        //    ShowMeYourHandsMod.instance?.Settings?.ManualWeaponPositions,
        //    ShowMeYourHandsMod.instance?.Settings?.ManualAimedPositions,
        //};
        //foreach (Dictionary<string, SaveableVector3> kvp in list)
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
                    WeaponPositionOffset = ShowMeYourHandsMod.instance?.Settings?.ManualWeaponPositions
                        .ContainsKey(keyValuePair.Key) == true
                        ? ShowMeYourHandsMod.instance.Settings.ManualWeaponPositions[keyValuePair.Key]
                            .ToVector3()
                        : Vector3.zero,
                    AimedWeaponPositionOffset = ShowMeYourHandsMod.instance?.Settings?.ManualAimedWeaponPositions
                    .ContainsKey(keyValuePair.Key) == true
                    ? ShowMeYourHandsMod.instance.Settings.ManualAimedWeaponPositions[keyValuePair.Key]
                    .ToVector3()
                    : Vector3.zero,

                };
                weapon.comps.Add(compProps);
            }
            else
            {
                compProps.MainHand      = keyValuePair.Value.ToVector3();
                compProps.MainHandAngle = keyValuePair.Value.ToAngleFloat();

                // compProps.WeaponPositionOffset = keyValuePair.Value[1].ToVector3();
                // compProps.AimedWeaponPositionOffset = keyValuePair.Value[2].ToVector3();

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
                if (ShowMeYourHandsMod.instance?.Settings?.ManualWeaponPositions.ContainsKey(keyValuePair.Key) == true)
                {
                    compProps.WeaponPositionOffset = ShowMeYourHandsMod.instance.Settings.ManualWeaponPositions[keyValuePair.Key]
                        .ToVector3();
                }
                else
                {
                    compProps.WeaponPositionOffset = Vector3.zero;
                }
                if (ShowMeYourHandsMod.instance?.Settings?.ManualAimedWeaponPositions.ContainsKey(keyValuePair.Key) == true)
                {
                    compProps.AimedWeaponPositionOffset = ShowMeYourHandsMod.instance.Settings.ManualAimedWeaponPositions[keyValuePair.Key]
                        .ToVector3();
                }
                else
                {
                    compProps.AimedWeaponPositionOffset = Vector3.zero;
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
                            MainHand  = weaponSets.MainHand,
                            SecHand   = weaponSets.SecHand,

                            WeaponPositionOffset      = weaponSets.WeaponPositionOffset,
                            AimedWeaponPositionOffset = weaponSets.AimedWeaponPositionOffset,
                            MainHandAngle             = weaponSets.MainHandAngle,
                            SecHandAngle              = weaponSets.SecHandAngle

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