using System.Collections.Generic;
using Verse;

namespace ShowMeYourHands;

/// <summary>
///     Definition of the settings for the mod
/// </summary>
[ShowMeYourHandsMod.HotSwappable]
internal class ShowMeYourHandsModSettings : ModSettings
{
    public Dictionary<string, SaveableVector3> ManualMainHandPositions = new();

    private List<string> manualMainHandPositionsKeys;

    private List<SaveableVector3> manualMainHandPositionsValues;

    private List<SaveableVector3> manualWeaponPositionsValues;
    private List<SaveableVector3> manualAimedWeaponPositionsValues;

    public Dictionary<string, SaveableVector3> ManualOffHandPositions = new();

    public Dictionary<string, SaveableVector3> ManualWeaponPositions = new();
    public Dictionary<string, SaveableVector3> ManualAimedWeaponPositions = new();
    
    private List<string> manualOffHandPositionsKeys;

    private List<string> manualWeaponPositionsKeys;
    private List<string> manualAimedWeaponPositionsKeys;

    private List<SaveableVector3> manualOffHandPositionsValues;
    public bool MatchArmorColor;
    public bool MatchArtificialLimbColor;
    public bool MatchHandAmounts;
    public bool RepositionHands = true;
    public bool ResizeHands = true;
    public bool ShowOtherTmes;
    public bool ShowWhenCarry;
    public bool VerboseLogging;

    // Hands and feet added as an extra option. Can be removed/included
    public bool UseHands = true;
    public bool UseFeet = true;
    public bool UsePaws = true;
    public bool CutHair = true;


    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref VerboseLogging, "VerboseLogging");
        Scribe_Values.Look(ref MatchArmorColor, "MatchArmorColor");
        Scribe_Values.Look(ref MatchArtificialLimbColor, "MatchArtificialLimbColor");
        Scribe_Values.Look(ref MatchHandAmounts, "MatchHandAmounts");
        Scribe_Values.Look(ref ResizeHands, "ResizeHands", true);
        Scribe_Values.Look(ref RepositionHands, "RepositionHands", true);
        Scribe_Values.Look(ref ShowWhenCarry, "ShowWhenCarry");
        Scribe_Values.Look(ref ShowOtherTmes, "ShowOtherTmes");
        Scribe_Collections.Look(ref ManualMainHandPositions, "ManualMainHandPositions", LookMode.Value,
            LookMode.Value,
            ref manualMainHandPositionsKeys, ref manualMainHandPositionsValues);
        Scribe_Collections.Look(ref ManualOffHandPositions, "ManualOffHandPositions", LookMode.Value,
            LookMode.Value,
            ref manualOffHandPositionsKeys, ref manualOffHandPositionsValues);
        Scribe_Collections.Look(ref ManualWeaponPositions, "ManualWeaponPositions", LookMode.Value,
            LookMode.Value,
            ref manualWeaponPositionsKeys, ref manualWeaponPositionsValues);
        Scribe_Collections.Look(ref ManualAimedWeaponPositions, "ManualAimedWeaponPositions", LookMode.Value,
            LookMode.Value,
            ref manualAimedWeaponPositionsKeys, ref manualAimedWeaponPositionsValues);

        Scribe_Values.Look(ref UseHands, "UseHands", true);
        Scribe_Values.Look(ref UseFeet, "UseFeet", true);
        Scribe_Values.Look(ref UsePaws, "UsePaws", true);
        Scribe_Values.Look(ref CutHair, "CutHair", true);

    }

    public void ResetManualValues()
    {
        ManualMainHandPositions       = new Dictionary<string, SaveableVector3>();
        manualMainHandPositionsKeys = new List<string>();
        manualMainHandPositionsValues = new List<SaveableVector3>();

        ManualOffHandPositions        = new Dictionary<string, SaveableVector3>();
        manualOffHandPositionsKeys = new List<string>();
        manualOffHandPositionsValues  = new List<SaveableVector3>();

        ManualWeaponPositions          = new Dictionary<string, SaveableVector3>();
        manualWeaponPositionsKeys      = new List<string>();
        manualWeaponPositionsValues    = new List<SaveableVector3>();

        ManualAimedWeaponPositions       = new Dictionary<string, SaveableVector3>();
        manualAimedWeaponPositionsKeys   = new List<string>();
        manualAimedWeaponPositionsValues = new List<SaveableVector3>();


        RimWorld_MainMenuDrawer_MainMenuOnGUI.UpdateHandDefinitions();
    }
}