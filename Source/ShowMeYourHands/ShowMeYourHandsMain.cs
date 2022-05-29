using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace ShowMeYourHands;

public static class ShowMeYourHandsMain
{
    public static readonly Dictionary<Thing, Tuple<Vector3, float>> weaponLocations = new();
    public static readonly Dictionary<Thing, Tuple<Vector3, float>> rightHandLocations = new();
    public static readonly Dictionary<Thing, Tuple<Vector3, float>> leftHandLocations = new();

    public static readonly Dictionary<Thing, Rot4?> pawnRotation = new();
    public static readonly Dictionary<Thing, float> pawnAngle = new();
    public static readonly Dictionary<Thing, Vector3> pawnOffset = new();



    public static readonly Dictionary<ThingDef, Vector3> southOffsets = new();
    public static readonly Dictionary<ThingDef, Vector3> northOffsets = new();
    public static readonly Dictionary<ThingDef, Vector3> eastOffsets = new();
    public static readonly Dictionary<ThingDef, Vector3> westOffsets = new();

    [NotNull] public static readonly List<ThingDef> IsColorable;

    public static readonly HarmonyLib.Harmony harmony;

    public static readonly bool BabysAndChildrenLoaded;
    public static readonly bool BabysAndChildrenLoaded2;
    public static readonly bool BabysAndChildrenLoaded3;

    public static readonly MethodInfo GetBodySizeScaling;

    public static readonly bool OversizedWeaponLoaded;

    public static readonly bool EnableOversizedLoaded;

    public static bool DualWieldLoaded;

    public static bool YayoAdoptedLoaded;

    public static readonly BodyPartDef HandDef;
    public static readonly BodyPartDef FootDef;

    public static readonly Dictionary<HediffDef, Color> HediffColors;

    public static readonly List<string> knownPatches = new()
    {
        // This mod
        "Killface.ShowMeYourHands",
        // Yayos Combat 3
        // Replaces weapon drawer
        "com.yayo.combat",
        "com.yayo.combat3",
        "com.yayo.yayoAni",
        // Dual Wield
        // Replaces weapon drawer if dual wielding
        "Roolo.DualWield",
        // Vanilla Expanded Framework
        "OskarPotocki.VFECore",
        // Vanilla Weapons Expanded - Laser
        // Modifies weapon position for lasers
        "com.ogliss.rimworld.mod.VanillaWeaponsExpandedLaser",
        // JecsTools
        "jecstools.jecrell.comps.oversized",
        "rimworld.androitiers-jecrell.comps.oversized",
        "jecstools.jecrell.comps.installedpart",
        "rimworld.Ogliss.comps.oversized",
        "rimworld.jecrellpelador.comps.oversizedbigchoppa",
        // Adeptus Mechanicus, not sure what
        "com.ogliss.rimworld.mod.AdeptusMechanicus",
        // Faction Colors, not sure what
        "rimworld.ohu.factionColors.main",
        // Enable oversized weapons
        "rimworld.carnysenpai.enableoversizedweapons",
        // Modifies weapon position
        "com.github.automatic1111.gunplay",
        // Red Scare Framework
        // Modifies weapon position, not sure why
        "Chismar.RedScare",
        // [O21] Toolbox
        // Modifies weapon position for lasers
        "com.o21toolbox.rimworld.mod",
        // Rimlaser
        // Modifies weapon position for lasers
        "com.github.automatic1111.rimlaser",
        // Combat extended
        "CombatExtended.HarmonyCE",
        // Performance optimizer
        "PerformanceOptimizer.Main",
        // RIMMSqol
        "RIMMSqol"
    };



    static ShowMeYourHandsMain()
    {
        DualWieldLoaded         = ModLister.GetActiveModWithIdentifier("Roolo.DualWield") != null;
        YayoAdoptedLoaded       = ModLister.GetActiveModWithIdentifier("com.yayo.combat3") != null;
        BabysAndChildrenLoaded  = ModLister.GetActiveModWithIdentifier("babies.and.children.continued") != null;
        BabysAndChildrenLoaded2 = ModLister.GetActiveModWithIdentifier("Babies_and_Children") != null;
        BabysAndChildrenLoaded3 = ModLister.GetActiveModWithIdentifier("babies.and.children.continued.13") != null;
        if (BabysAndChildrenLoaded || BabysAndChildrenLoaded2 || BabysAndChildrenLoaded3)
        {
            Type type = AccessTools.TypeByName("BabiesAndChildren.GraphicTools");
            if (type != null)
            {
                GetBodySizeScaling = type.GetMethod("GetBodySizeScaling");
            }

            LogMessage("BabiesAndChildren loaded, will compensate for children hand size");
        }

        OversizedWeaponLoaded = AccessTools.TypeByName("CompOversizedWeapon") != null;
        EnableOversizedLoaded = ModLister.GetActiveModWithIdentifier("CarnySenpai.EnableOversizedWeapons") != null;

        if (OversizedWeaponLoaded || EnableOversizedLoaded)
        {
            List<ThingDef> allWeapons = DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.IsWeapon).ToList();
            foreach (ThingDef weapon in allWeapons)
            {
                saveWeaponOffsets(weapon);
            }

            LogMessage(
                $"OversizedWeapon loaded, will compensate positioning. Cached offsets for {allWeapons.Count} weapons");
        }
        /*
        CompProperties compProperties = new() { compClass = typeof(HandDrawer) };
        foreach (ThingDef thingDef in from race in DefDatabase<ThingDef>.AllDefsListForReading
                 where race.race?.Humanlike == true
                 select race)
        {
            thingDef.comps?.Add(compProperties);
        }
        */
        HandDef = DefDatabase<BodyPartDef>.GetNamedSilentFail("Hand");
        FootDef = DefDatabase<BodyPartDef>.GetNamedSilentFail("Foot");

        IEnumerable<HediffDef> partsHediffs =
            DefDatabase<HediffDef>.AllDefsListForReading.Where(def =>
                def.hediffClass == typeof(Hediff_AddedPart) && def.spawnThingOnRemoved != null);
        HediffColors = new Dictionary<HediffDef, Color>();
        foreach (HediffDef partsHediff in partsHediffs)
        {
            TechLevel techLevel = partsHediff.spawnThingOnRemoved.techLevel;
            HediffColors[partsHediff] = GetColorFromTechLevel(techLevel);
        }

        LogMessage($"Cached {HediffColors.Count} hediffs colors");

        IsColorable = DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.HasComp(typeof(CompColorable)))
            .ToList();

        harmony = new HarmonyLib.Harmony("Killface.ShowMeYourHands");

        harmony.PatchAll(Assembly.GetExecutingAssembly());

        if (ModLister.GetActiveModWithIdentifier("MalteSchulze.RIMMSqol") == null)
        {
            return;
        }

        LogMessage(
            "RIMMSqol loaded, will remove their destructive Prefixes for the rotation-methods");
        MethodInfo original = typeof(Pawn_RotationTracker).GetMethod("FaceCell");
        harmony.Unpatch(original, HarmonyPatchType.Prefix, "RIMMSqol");
        original = typeof(Pawn_RotationTracker).GetMethod("Face");
        harmony.Unpatch(original, HarmonyPatchType.Prefix, "RIMMSqol");

        // not reliable
        /*
        harmony.Patch(
                         AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.DrawEquipmentAiming)),
                         new HarmonyMethod(typeof(DrawEquipmentAiming_Patch), nameof(DrawEquipmentAiming_Patch.DrawEquipmentAiming_Prefix)),
                         new HarmonyMethod(typeof(DrawEquipmentAiming_Patch), nameof(DrawEquipmentAiming_Patch.DrawEquipmentAiming_Postfix)),
                         null);
        */
        // FS Hands on Weapons
        // more skin colors -- obsolete, new mod adds 20+
        /*
        harmony.Patch(
                         AccessTools.Method(typeof(PawnSkinColors), "GetSkinDataIndexOfMelanin"),
                         new HarmonyMethod(
                                           typeof(PawnSkinColors_FS),
                                           nameof(PawnSkinColors_FS.GetSkinDataIndexOfMelanin_Prefix)),
                         null);

        harmony.Patch(
                         AccessTools.Method(typeof(PawnSkinColors), nameof(PawnSkinColors.GetSkinColor)),
                         new HarmonyMethod(typeof(PawnSkinColors_FS), nameof(PawnSkinColors_FS.GetSkinColor_Prefix)),
                         null);

        harmony.Patch(
                         AccessTools.Method(typeof(PawnSkinColors), nameof(PawnSkinColors.RandomMelanin)),
                         new HarmonyMethod(typeof(PawnSkinColors_FS), nameof(PawnSkinColors_FS.RandomMelanin_Prefix)),
                         null);

        harmony.Patch(
                         AccessTools.Method(typeof(PawnSkinColors),
                                            nameof(PawnSkinColors.GetMelaninCommonalityFactor)),
                         new HarmonyMethod(
                                           typeof(PawnSkinColors_FS),
                                           nameof(PawnSkinColors_FS.GetMelaninCommonalityFactor_Prefix)),
                         null);
        
        */
    }

    public static void LogMessage(string message, bool forced = false, bool warning = false)
    {
        if (warning)
        {
            Log.Warning($"[ShowMeYourHands]: {message}");
            return;
        }

        if (!forced && !ShowMeYourHandsMod.instance.Settings.VerboseLogging)
        {
            return;
        }

        Log.Message($"[ShowMeYourHands]: {message}");
    }


    private static Color GetColorFromTechLevel(TechLevel techLevel)
    {
        switch (techLevel)
        {
            case TechLevel.Neolithic:
                return ThingDefOf.WoodLog.stuffProps.color;
            case TechLevel.Industrial:
                return ThingDefOf.Steel.stuffProps.color;
            case TechLevel.Spacer:
                return ThingDefOf.Silver.stuffProps.color;
            case TechLevel.Ultra:
                return ThingDefOf.Gold.stuffProps.color;
            case TechLevel.Archotech:
                return ThingDefOf.Plasteel.stuffProps.color;
            default:
                return ThingDefOf.Steel.stuffProps.color;
        }
    }


    private static void saveWeaponOffsets(ThingDef weapon)
    {
        if (OversizedWeaponLoaded)
        {
            CompProperties thingComp =
                weapon.comps.FirstOrDefault(y => y.GetType().ToString().Contains("CompOversizedWeapon"));
            if (thingComp == null)
            {
                return;
            }

            Type oversizedType = thingComp.GetType();
            IEnumerable<FieldInfo> fields = oversizedType.GetFields().Where(info => info.Name.Contains("Offset"));

            foreach (FieldInfo fieldInfo in fields)
            {
                switch (fieldInfo.Name)
                {
                    case "northOffset":
                        northOffsets[weapon] = fieldInfo.GetValue(thingComp) is Vector3
                            ? (Vector3)fieldInfo.GetValue(thingComp)
                            : Vector3.zero;
                        break;
                    case "southOffset":
                        southOffsets[weapon] = fieldInfo.GetValue(thingComp) is Vector3
                            ? (Vector3)fieldInfo.GetValue(thingComp)
                            : Vector3.zero;
                        break;
                    case "westOffset":
                        westOffsets[weapon] = fieldInfo.GetValue(thingComp) is Vector3
                            ? (Vector3)fieldInfo.GetValue(thingComp)
                            : Vector3.zero;
                        break;
                    case "eastOffset":
                        eastOffsets[weapon] = fieldInfo.GetValue(thingComp) is Vector3
                            ? (Vector3)fieldInfo.GetValue(thingComp)
                            : Vector3.zero;
                        break;
                }
            }

            return;
        }

        if (!EnableOversizedLoaded)
        {
            return;
        }

        if (weapon.graphicData == null)
        {
            return;
        }

        GraphicData graphicData = weapon.graphicData;

        Vector3 baseOffset = graphicData.drawOffset;

        northOffsets[weapon] = graphicData.drawOffsetNorth ?? baseOffset;
        southOffsets[weapon] = graphicData.drawOffsetSouth ?? baseOffset;
        eastOffsets[weapon]  = graphicData.drawOffsetEast ?? baseOffset;
        westOffsets[weapon]  = graphicData.drawOffsetWest ?? baseOffset;
    }
}