using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ShowMeYourHands;

/// <summary>
///     Definition of the settings for the mod
/// </summary>
[ShowMeYourHandsMod.HotSwappable]
internal class ShowMeYourHandsModSettings : ModSettings
{
    public static Dictionary<string, Vector2> offsets = new Dictionary<string, Vector2>();
    public static Dictionary<string, float> rotation = new Dictionary<string, float>();

    public static bool offsetTab = false, debugMode = false;

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
    public bool MatchArmorColor = true;
    public bool MatchArtificialLimbColor = true;
    public bool MatchHandAmounts = true;
    public bool RepositionHands = true;
    public bool ResizeHands = true;
    public bool ShowOtherTmes = true;
    public bool ShowWhenCarry = true;
    public bool VerboseLogging;

    // Hands and feet added as an extra option. Can be removed/included
    public bool UseHands = true;
    public bool UseFeet = true;
    public bool UsePaws = true;
    public bool CutHair = true;


    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref this.VerboseLogging, nameof(this.VerboseLogging));
        Scribe_Values.Look(ref this.MatchArmorColor, nameof(this.MatchArmorColor));
        Scribe_Values.Look(ref this.MatchArtificialLimbColor, nameof(this.MatchArtificialLimbColor));
        Scribe_Values.Look(ref this.MatchHandAmounts, nameof(this.MatchHandAmounts));
        Scribe_Values.Look(ref this.ResizeHands, nameof(this.ResizeHands), true);
        Scribe_Values.Look(ref this.RepositionHands, nameof(this.RepositionHands), true);
        Scribe_Values.Look(ref this.ShowWhenCarry, nameof(this.ShowWhenCarry));
        Scribe_Values.Look(ref this.ShowOtherTmes, nameof(this.ShowOtherTmes));
        Scribe_Collections.Look(ref this.ManualMainHandPositions, nameof(this.ManualMainHandPositions), LookMode.Value,
            LookMode.Value,
            ref this.manualMainHandPositionsKeys, ref this.manualMainHandPositionsValues);
        Scribe_Collections.Look(ref this.ManualOffHandPositions, nameof(this.ManualOffHandPositions), LookMode.Value,
            LookMode.Value,
            ref this.manualOffHandPositionsKeys, ref this.manualOffHandPositionsValues);
        Scribe_Collections.Look(ref this.ManualWeaponPositions, nameof(this.ManualWeaponPositions), LookMode.Value,
            LookMode.Value,
            ref this.manualWeaponPositionsKeys, ref this.manualWeaponPositionsValues);
        Scribe_Collections.Look(ref this.ManualAimedWeaponPositions, nameof(this.ManualAimedWeaponPositions), LookMode.Value,
            LookMode.Value,
            ref this.manualAimedWeaponPositionsKeys, ref this.manualAimedWeaponPositionsValues);

        Scribe_Values.Look(ref this.UseHands, nameof(this.UseHands), true);
        Scribe_Values.Look(ref this.UseFeet, nameof(this.UseFeet), true);
        Scribe_Values.Look(ref this.UsePaws, nameof(this.UsePaws), true);
        Scribe_Values.Look(ref this.CutHair, nameof(this.CutHair), true);

        Scribe_Collections.Look(ref offsets, "SMYHAnimations-animationOffsets");
        Scribe_Collections.Look(ref rotation, "RJWAnimations-rotationOffsets");

        Scribe_Values.Look(ref offsetTab, "SMYHAnimations-EnableOffsetTab", false);
      
        Scribe_Values.Look(ref debugMode, "SMYHAnimations-AnimsDebugMode", false);


    }

    public void ResetManualValues()
    {
        this.ManualMainHandPositions       = new Dictionary<string, SaveableVector3>();
        this.manualMainHandPositionsKeys = new List<string>();
        this.manualMainHandPositionsValues = new List<SaveableVector3>();

        this.ManualOffHandPositions        = new Dictionary<string, SaveableVector3>();
        this.manualOffHandPositionsKeys = new List<string>();
        this.manualOffHandPositionsValues  = new List<SaveableVector3>();

        this.ManualWeaponPositions          = new Dictionary<string, SaveableVector3>();
        this.manualWeaponPositionsKeys      = new List<string>();
        this.manualWeaponPositionsValues    = new List<SaveableVector3>();

        this.ManualAimedWeaponPositions       = new Dictionary<string, SaveableVector3>();
        this.manualAimedWeaponPositionsKeys   = new List<string>();
        this.manualAimedWeaponPositionsValues = new List<SaveableVector3>();


        RimWorld_MainMenuDrawer_MainMenuOnGUI.UpdateHandDefinitions();
    }
}