﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FacialStuff.FaceEditor;
using JetBrains.Annotations;
using Mlie;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ShowMeYourHands;

[HotSwappable]
[StaticConstructorOnStartup]
internal class ShowMeYourHandsMod : Mod
{
    /// <summary>
    ///     The instance of the settings to be read by the mod
    /// </summary>
    [NotNull] public static ShowMeYourHandsMod instance;

    private static readonly Vector2 buttonSize = new(120f, 25f);

    private static readonly Vector2 weaponSize = new(200f, 200f);

    private static readonly Vector2 iconSize = new(24f, 24f);


    private static readonly int buttonSpacer = 200;

    private static readonly float columnSpacer = 0.1f;

    private static float leftSideWidth;

    private static Listing_Standard listing_Standard;

    private static Listing_Standard listing_WeaponSettings;

    private static Vector3 currentMainHand;

    private static Vector3 currentOffHand;

    private static Vector3 currentWeaponPositionOffset;

    private static Vector3 currentAimedWeaponPositionOffset;

    public static float currentMainHandAngle;

    public static float currentOffHandAngle;
   
    public static float currentAimAngle = 143f;

    private static bool currentHasOffHand;

    private static bool currentMainBehind;

    private static bool currentOffBehind;

    private static bool currentShowAiming;

    private static Vector2 tabsScrollPosition;

    private static Vector2 summaryScrollPosition;

    private static Vector2 generalScrollPosition;

    private static List<ThingDef> allWeapons;

    private static List<string> selectedHasManualDefs;

    private static string currentVersion;

    private static Graphic handTex;
    private static Graphic footTex;
    private static Graphic bodyTex;
    private static Graphic headTex;

    private static Dictionary<string, int> totalWeaponsByMod = new();

    private static Dictionary<string, int> fixedWeaponsByMod = new();

    [NotNull] public static HashSet<string> DefinedByDef = new HashSet<string>();

    private static string selectedDef = "Settings";

    private static string selectedSubDef;


    /// <summary>
    ///     The private settings
    /// </summary>
    private ShowMeYourHandsModSettings settings;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="content"></param>
    public ShowMeYourHandsMod(ModContentPack content)
        : base(content)
    {
        instance = this;
        ParseHelper.Parsers<SaveableVector3>.Register(SaveableVector3.FromString);
        if (instance.Settings.ManualMainHandPositions == null)
        {
            instance.Settings.ManualMainHandPositions = new Dictionary<string, SaveableVector3>();
            instance.Settings.ManualOffHandPositions = new Dictionary<string, SaveableVector3>();
        }

        currentVersion =
            VersionFromManifest.GetVersionFromModMetaData(
                ModLister.GetActiveModWithIdentifier("Mlie.ShowMeYourHands"));
    }

    private static string SelectedDef
    {
        get => selectedDef;
        set
        {
            if (value == "Settings")
            {
                UpdateWeaponStatistics();
            }

            selectedDef = value;
        }
    }

    /// <summary>
    ///     The instance-settings for the mod
    /// </summary>
    [NotNull]
    internal ShowMeYourHandsModSettings Settings
    {
        get
        {
            if (settings == null)
            {
                settings = GetSettings<ShowMeYourHandsModSettings>();
            }

            return settings;
        }

        set => settings = value;
    }

    private static List<ThingDef> AllWeapons
    {
        get
        {
            if (allWeapons == null || allWeapons.Count == 0)
            {
                List<ThingDef> allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
                if (!allDefsListForReading.NullOrEmpty())
                {
                    allWeapons = (from weapon in allDefsListForReading
                        where weapon.IsWeapon && !weapon.destroyOnDrop && !IsShield(weapon)
                        orderby weapon.label
                        select weapon).ToList();
                }
            }

            return allWeapons;
        }
        set => allWeapons = value;
    }

    private Graphic HandTex
    {
        get
        {
            if (handTex == null)
            {
                handTex = GraphicDatabase.Get<Graphic_Multi>("HandIcon", ShaderDatabase.CutoutSkin,
                    new Vector2(1.25f, 1.25f),
                    PawnSkinColors.GetSkinColor(0.5f), PawnSkinColors.GetSkinColor(0.5f));
            }

            return handTex;
        }
        set => handTex = value;
    }
    private Graphic FootTex
    {
        get
        {
            if (footTex == null)
            {
                footTex = GraphicDatabase.Get<Graphic_Multi>("FootIcon", ShaderDatabase.CutoutSkin,
                    new Vector2(1.25f, 1.25f),
                    PawnSkinColors.GetSkinColor(0.5f), PawnSkinColors.GetSkinColor(0.5f));
            }

            return footTex;
        }
        set => footTex = value;
    }
    private Graphic BodyTex
    {
        get
        {
            if (bodyTex == null)
            {
                Color skinColor = PawnSkinColors.GetSkinColor(0.5f);
                bodyTex = GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Humanlike/Bodies/Naked_Male", ShaderDatabase.CutoutSkin,
                    new Vector2(1.5f, 1.5f),
                    skinColor, skinColor);
            }

            return bodyTex;
        }
        set => bodyTex = value;
    }
    private Graphic HeadTex
    {
        get
        {
            if (headTex == null)
            {
                Color skinColor = PawnSkinColors.GetSkinColor(0.5f);
                headTex = GraphicDatabaseHeadRecords.GetHeadRandom(Gender.Male, skinColor, CrownType.Average, false);
            }

            return headTex;
        }
        set => headTex = value;
    }

    /// <summary>
    ///     The settings-window
    /// </summary>
    /// <param name="rect"></param>
    public override void DoSettingsWindowContents(Rect rect)
    {
        base.DoSettingsWindowContents(rect);

        Rect rect2 = rect.ContractedBy(1);
        leftSideWidth = rect2.ContractedBy(10).width / 5 * 2;

        listing_Standard = new Listing_Standard();

        DrawOptions(rect2);
        DrawTabsList(rect2);
        Settings.Write();
    }

    /// <summary>
    ///     The title for the mod-settings
    /// </summary>
    /// <returns></returns>
    public override string SettingsCategory()
    {
        return "Show Me Your Hands";
    }

    public override void WriteSettings()
    {
        base.WriteSettings();
        RimWorld_MainMenuDrawer_MainMenuOnGUI.UpdateHandDefinitions();
    }

    public static bool IsShield(ThingDef weapon)
    {
        bool isShield = false;
        if (weapon.weaponTags == null)
        {
            return false;
        }

        foreach (string tag in weapon.weaponTags)
        {
            switch (tag)
            {
                case "Shield_Sidearm":
                case "Shield_NoSidearm":
                    continue;
            }

            if (tag.Contains("_ValidSidearm"))
            {
                continue;
            }

            if (tag.Contains("ShieldSafe"))
            {
                continue;
            }

            if (!tag.ToLower().Contains("shield"))
            {
                continue;
            }

            isShield = true;
        }

        return isShield;
    }

    private static void DrawButton(Action action, string text, Vector2 pos)
    {
        Rect rect = new(pos.x, pos.y, buttonSize.x, buttonSize.y);
        if (!Widgets.ButtonText(rect, text, true, false, Color.white))
        {
            return;
        }

        SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera();
        action();
    }

    private static void UpdateWeaponStatistics()
    {
        totalWeaponsByMod = new Dictionary<string, int>();
        fixedWeaponsByMod = new Dictionary<string, int>();
        if (AllWeapons.NullOrEmpty()) return;

        foreach (ThingDef currentWeapon in AllWeapons)
        {
            string weaponModName = currentWeapon?.modContentPack?.Name;
            if (string.IsNullOrEmpty(weaponModName))
            {
                weaponModName = "SMYH.unknown".Translate();
            }

            if (totalWeaponsByMod.ContainsKey(weaponModName))
            {
                totalWeaponsByMod[weaponModName]++;
            }
            else
            {
                totalWeaponsByMod[weaponModName] = 1;
            }

            if (!DefinedByDef.Contains(currentWeapon?.defName) &&
                !instance.Settings.ManualMainHandPositions.ContainsKey(currentWeapon.defName))
            {
                continue;
            }

            if (fixedWeaponsByMod.ContainsKey(weaponModName))
            {
                fixedWeaponsByMod[weaponModName]++;
            }
            else
            {
                fixedWeaponsByMod[weaponModName] = 1;
            }
        }
    }

    private bool DrawIcon(ThingDef thing, Rect rect, Vector3 mainHandPosition, Vector3 offHandPosition, float mainHandAngle, float offHandAngle)
    {
        if (thing == null)
        {
            return false;
        }

        Texture texture = thing.graphicData?.Graphic?.MatSingle?.mainTexture;
        if (thing.graphicData?.graphicClass == typeof(Graphic_Random))
        {
            texture = ((Graphic_Random)thing.graphicData.Graphic)?.FirstSubgraphic().MatSingle.mainTexture;
        }

        if (thing.graphicData?.graphicClass == typeof(Graphic_StackCount))
        {
            texture = ((Graphic_StackCount)thing.graphicData.Graphic)?.SubGraphicForStackCount(1, thing).MatSingle
                .mainTexture;
        }

        if (texture == null)
        {
            return false;
        }

        Rect rectOuter = rect.ExpandedBy(5);
        Rect rectLine = rect.ExpandedBy(2);
        Widgets.DrawBoxSolid(rectOuter, Color.grey);
        Widgets.DrawBoxSolid(rectLine, new ColorInt(42, 43, 44).ToColor);
        float weaponAngle = 0f;
        if (thing.IsRangedWeapon)
        {
            weaponAngle = thing.equippedAngleOffset;
        }
        DrawWeaponWithHands(thing, mainHandAngle, offHandAngle, mainHandPosition, offHandPosition, rect, texture, weaponAngle);

        return true;
    }

    private bool DrawIconPawnWeapon(ThingDef thing, Rect rect, Vector3 mainHandPosition, Vector3 offHandPosition, float mainHandAngle, float offHandAngle, Rot4 drawRotation)
    {
        if (thing == null)
        {
            return false;
        }

        Texture texture = thing.graphicData?.Graphic?.MatSingle?.mainTexture;
        if (thing.graphicData?.graphicClass == typeof(Graphic_Random))
        {
            texture = ((Graphic_Random)thing.graphicData.Graphic)?.FirstSubgraphic().MatSingle.mainTexture;
        }

        if (thing.graphicData?.graphicClass == typeof(Graphic_StackCount))
        {
            texture = ((Graphic_StackCount)thing.graphicData.Graphic)?.SubGraphicForStackCount(1, thing).MatSingle
                .mainTexture;
        }

        if (texture == null)
        {
            return false;
        }

        Rect rectOuter = rect.ExpandedBy(5);
        Rect rectLine = rect.ExpandedBy(2);
        Widgets.DrawBoxSolid(rectOuter, Color.grey);
        Widgets.DrawBoxSolid(rectLine, new ColorInt(42, 43, 44).ToColor);
        
        rect.y -= 5f;

        Rect bodyRect = new(rect.x, rect.y, rect.width, rect.height);
        GUI.DrawTexture(bodyRect, BodyTex.MatAt(drawRotation).mainTexture);
    
        Rect headRect = new(rect.x , rect.y- (0.34f/1.5f* rect.width), rect.width, rect.height);

        Rect footRect1 = new(rect.x + rect.width /2 - rect.width/8, rect.y + rect.height /2 + (0.34f * rect.width), rect.width/4, rect.height/4);
        Rect footRect2 = footRect1;
        footRect1.x -= footRect1.width/3;
        footRect2.x += footRect1.width / 3;
        // rect.x - (0.04f * rect.width), rect.y+ (0.34f* rect.width), rect.width, rect.height);
        GUI.DrawTexture(headRect, HeadTex.MatAt(drawRotation).mainTexture);
        GUI.DrawTexture(footRect1, FootTex.MatAt(drawRotation).mainTexture);
        GUI.DrawTexture(footRect2, FootTex.MatAt(drawRotation).mainTexture);

        float weaponRectSize = bodyRect.width / 1.5f;
        float middlePosWeapon = ((rect.width - weaponRectSize) / 2);
        float posXmodifier = 0f;
        float posYmodifier = 0f;

        // y is the posX modifier when turning east

        Vector3 positionOffset = currentShowAiming ? currentAimedWeaponPositionOffset : currentWeaponPositionOffset;
        if (drawRotation == Rot4.South)
        {
            posXmodifier += positionOffset.x;
            if (currentShowAiming) posXmodifier += 0.2f;
            posYmodifier -= 0.22f;
        }
        else if (drawRotation == Rot4.North)
        {
            posXmodifier -= positionOffset.x;
            posYmodifier -= 0.11f;
        }
        else if (drawRotation == Rot4.East)
        {
            posXmodifier += positionOffset.y + 0.2f;
            posYmodifier -= 0.22f;
        }
        else if (drawRotation == Rot4.West)
        {
            posXmodifier -= positionOffset.y + 0.2f;
            posXmodifier -= 0.2f;
            posYmodifier -= 0.22f;
        }
        
        posYmodifier += positionOffset.z;

        if (currentShowAiming)
        {
            posYmodifier += 0.4f / weaponRectSize;
        }


        Rect weaponRect = new(rect.x  + middlePosWeapon + (posXmodifier * weaponRectSize),
            rect.y + middlePosWeapon - (posYmodifier * weaponRectSize), weaponRectSize, weaponRectSize);

        float weaponAngle = 0f;
        if (drawRotation == Rot4.West) // TODO: + angle for aime position offsets
        {
            weaponAngle = 217f - 90f;
        }
        else
        {
            weaponAngle = 143f - 90f;
        }

        weaponAngle = currentAimAngle - 90;
        weaponAngle += thing.equippedAngleOffset;

        if (thing.IsMeleeWeapon)
        {
            // weaponAngle -= 135f;
        }
        mainHandAngle   += weaponAngle;
        offHandAngle    += weaponAngle;
        mainHandPosition = mainHandPosition.RotatedBy(weaponAngle);
        offHandPosition  = offHandPosition.RotatedBy(weaponAngle);
        DrawWeaponWithHands(thing, mainHandAngle, offHandAngle, mainHandPosition, offHandPosition, weaponRect, texture, weaponAngle);


        return true;
    }

    private void DrawWeaponWithHands(ThingDef thing, float mainHandAngle, float offHandAngle, Vector3 mainHandPosition, Vector3 offHandPosition, Rect rect,
        Texture texture, float weaponAngle)
    {
        float scaling = rect.width / weaponSize.x;
        float handSize = rect.width / 4;

        float weaponMiddle = rect.width / 2;

        Vector2 mainHandCoords = new(
            weaponMiddle + (mainHandPosition.x * rect.width) - (handSize / 2),
            weaponMiddle - (mainHandPosition.z * rect.width) - (handSize / 2));
        Vector2 offHandCoords = new(
            weaponMiddle + (offHandPosition.x * rect.width) - (handSize / 2),
            weaponMiddle - (offHandPosition.z * rect.width) - (handSize / 2));

        Rect mainHandRect = new(rect.x + mainHandCoords.x, (rect.y + mainHandCoords.y),
            handSize,
            handSize);
        Rect offHandRect = new(rect.x + offHandCoords.x, rect.y + offHandCoords.y,
            handSize,
            handSize);



        if (currentMainBehind)
        {
            DrawTextureRotatedLocal(mainHandRect, HandTex.MatEast.mainTexture, mainHandAngle);
        }

        if (currentHasOffHand && currentOffBehind)
        {
            DrawTextureRotatedLocal(offHandRect, HandTex.MatEast.mainTexture, offHandAngle);
        }


        DrawTextureRotatedLocal(rect, texture, weaponAngle);

        if (!currentMainBehind)
        {
            DrawTextureRotatedLocal(mainHandRect, HandTex.MatSouth.mainTexture, mainHandAngle);
        }

        if (currentHasOffHand && !currentOffBehind)
        {
            DrawTextureRotatedLocal(offHandRect, HandTex.MatSouth.mainTexture, offHandAngle);
        }
    }


    public void DrawTextureRotatedLocal(Rect rect, Texture texture, float angle)
    {
        if (angle == 0f)
        {
            GUI.DrawTexture(rect, texture);
            return;
        }

        Matrix4x4 matrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, rect.center);
        GUI.DrawTexture(rect, texture);
        GUI.matrix = matrix;
    }

    private void DrawWeapon(ThingDef thing, Rect rect)
    {
        if (thing?.graphicData?.Graphic?.MatSingle?.mainTexture == null)
        {
            return;
        }

        Texture texture2D = thing.graphicData.Graphic.MatSingle.mainTexture;
        if (thing.graphicData.graphicClass == typeof(Graphic_Random))
        {
            texture2D = ((Graphic_Random)thing.graphicData.Graphic).FirstSubgraphic().MatSingle.mainTexture;
        }

        if (thing.graphicData.graphicClass == typeof(Graphic_StackCount))
        {
            texture2D = ((Graphic_StackCount)thing.graphicData.Graphic).SubGraphicForStackCount(1, thing).MatSingle
                .mainTexture;
        }

        if (texture2D.width != texture2D.height)
        {
            float ratio = (float)texture2D.width / texture2D.height;

            if (ratio < 1)
            {
                rect.x += (rect.width - (rect.width * ratio)) / 2;
                rect.width *= ratio;
            }
            else
            {
                rect.y += (rect.height - (rect.height / ratio)) / 2;
                rect.height /= ratio;
            }
        }

        GUI.DrawTexture(rect, texture2D);
    }
    private float scrollViewHeight;
    private void DrawOptions(Rect rect)
    {
        Rect optionsOuterContainer   = rect.ContractedBy(10);
        optionsOuterContainer.x     += leftSideWidth + columnSpacer;
        optionsOuterContainer.width -= leftSideWidth + columnSpacer;

        Widgets.DrawBoxSolid(optionsOuterContainer, Color.grey);
        Rect optionsInnerContainer = optionsOuterContainer.ContractedBy(1);
        Widgets.DrawBoxSolid(optionsInnerContainer, new ColorInt(42, 43, 44).ToColor);
        Rect frameRect = optionsInnerContainer.ContractedBy(10);
        frameRect.x = leftSideWidth + columnSpacer + 25;
        frameRect.y += 15;
        frameRect.height -= 15;
        
        int tabHeadRectHeight = 80;
        float extraweaponsHeight = (totalWeaponsByMod.Count * 24f) + 15;
        
        Rect settingsRect = frameRect;
        settingsRect.height -= extraweaponsHeight +tabHeadRectHeight;
      
        Rect contentRect = frameRect;
        contentRect.x = 0;
        contentRect.y = 0;
        contentRect.width -= 24;
    
        contentRect.height = scrollViewHeight;

        float lineSpacing = Text.LineHeight;
        float curY = 0f;

        switch (SelectedDef)
        {
            case null:
                return;
            case "Settings":
            {
                Widgets.BeginScrollView(settingsRect, ref generalScrollPosition, contentRect);
                listing_Standard.Begin(contentRect);

                Text.Font = GameFont.Medium;
                lineSpacing = Text.LineHeight;
                listing_Standard.Label("SMYH.settings".Translate());
                curY += lineSpacing;
                Text.Font = GameFont.Small;
                lineSpacing = Text.LineHeight;
                listing_Standard.Gap();
                curY += lineSpacing;

                    //  fs
                    listing_Standard.CheckboxLabeled("usehands.label".Translate(), ref Settings.UseHands,
                        "usehands.tooltip".Translate());
                curY += lineSpacing;
                    listing_Standard.CheckboxLabeled("usefeet.label".Translate(), ref Settings.UseFeet,
                        "usefeet.tooltip".Translate());
                curY += lineSpacing;
                    listing_Standard.CheckboxLabeled("usepaws.label".Translate(), ref Settings.UsePaws,
                        "usepaws.tooltip".Translate());
                curY += lineSpacing;
                    listing_Standard.CheckboxLabeled("cuthair.label".Translate(), ref Settings.CutHair,
                        "cuthair.tooltip".Translate());
                    curY += lineSpacing;

                    // fs end

                    if (Prefs.UIScale != 1f)
                {
                    GUI.color = Color.yellow;
                    listing_Standard.Label(
                        "SMYH.uiscale.label".Translate(),
                        -1F,
                        "SMYH.uiscale.tooltip".Translate());
                curY += lineSpacing;
                    listing_Standard.Gap();
                curY += lineSpacing;
                    GUI.color = Color.white;
                }

                if (instance.Settings.ManualMainHandPositions?.Count > 0)
                {
                    Rect copyPoint = listing_Standard.Label("SMYH.copy.label".Translate(), -1F,
                        "SMYH.copy.tooltip".Translate());
                    DrawButton(() => { CopyChangedWeapons(); }, "SMYH.copy.button".Translate(),
                        new Vector2(copyPoint.position.x + buttonSpacer, copyPoint.position.y));
                curY += lineSpacing;
                    listing_Standard.Gap();
                curY += lineSpacing;
                
                Rect labelPoint = listing_Standard.Label("SMYH.resetall.label".Translate(), -1F,
                        "SMYH.resetall.tooltip".Translate());
                    DrawButton(() =>
                        {
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                "SMYH.resetall.confirm".Translate(),
                                delegate
                                {
                                    instance.Settings.ResetManualValues();
                                    UpdateWeaponStatistics();
                                }));
                        }, "SMYH.resetall.button".Translate(),
                        new Vector2(labelPoint.position.x + buttonSpacer, labelPoint.position.y));
                    curY += lineSpacing;

                        if (!string.IsNullOrEmpty(selectedSubDef) && selectedHasManualDefs.Count > 0)
                    {
                        DrawButton(() => { CopyChangedWeapons(true); }, "SMYH.copyselected.button".Translate(),
                            new Vector2(copyPoint.position.x + buttonSpacer + buttonSize.x + 10,
                                copyPoint.position.y));
                curY += lineSpacing;
                        DrawButton(() =>
                            {
                                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                    "SMYH.resetselected.confirm".Translate(selectedSubDef),
                                    delegate
                                    {
                                        foreach (ThingDef weaponDef in from ThingDef weapon in AllWeapons
                                                 where
                                                     weapon.modContentPack == null &&
                                                     selectedSubDef == "SMYH.unknown".Translate() ||
                                                     weapon.modContentPack?.Name == selectedSubDef
                                                 select weapon)
                                        {
                                            WhandCompProps whandCompProps = null;
                                            ResetOneWeapon(weaponDef, ref whandCompProps);
                                        }

                                        selectedHasManualDefs = new List<string>();
                                        UpdateWeaponStatistics();
                                    }));
                            }, "SMYH.resetselected.button".Translate(),
                            new Vector2(labelPoint.position.x + buttonSpacer + buttonSize.x + 10,
                                labelPoint.position.y));
                        curY += lineSpacing;

                    }
                    }
                else
                {
                    listing_Standard.Gap((buttonSize.y * 2) + 12);
                curY += lineSpacing;
                }

                listing_Standard.CheckboxLabeled("SMYH.logging.label".Translate(), ref Settings.VerboseLogging,
                    "SMYH.logging.tooltip".Translate());
                curY += lineSpacing;
                listing_Standard.CheckboxLabeled("SMYH.matcharmor.label".Translate(), ref Settings.MatchArmorColor,
                    "SMYH.matcharmor.tooltip".Translate());
                curY += lineSpacing;
                listing_Standard.CheckboxLabeled("SMYH.matchartificiallimb.label".Translate(),
                    ref Settings.MatchArtificialLimbColor,
                    "SMYH.matchartificiallimb.tooltip".Translate());
                curY += lineSpacing;
                listing_Standard.CheckboxLabeled("SMYH.matchhandamounts.label".Translate(),
                    ref Settings.MatchHandAmounts,
                    "SMYH.matchhandamounts.tooltip".Translate());
                curY += lineSpacing;
                listing_Standard.CheckboxLabeled("SMYH.resizehands.label".Translate(), ref Settings.ResizeHands,
                    "SMYH.resizehands.tooltip".Translate());
                curY += lineSpacing;
                listing_Standard.CheckboxLabeled("SMYH.repositionhands.label".Translate(),
                    ref Settings.RepositionHands,
                    "SMYH.repositionhands.tooltip".Translate());
                curY += lineSpacing;
                listing_Standard.CheckboxLabeled("SMYH.showwhencarry.label".Translate(),
                    ref Settings.ShowWhenCarry,
                    "SMYH.showwhencarry.tooltip".Translate());
                curY += lineSpacing;
                listing_Standard.CheckboxLabeled("SMYH.showothertimes.label".Translate(),
                    ref Settings.ShowOtherTmes,
                    "SMYH.showothertimes.tooltip".Translate());
                curY += lineSpacing;
                listing_Standard.End();

                if (Event.current.type == EventType.Layout)
                {
                    scrollViewHeight = curY + 15f;
                }
                Widgets.EndScrollView();


                Rect tabFrameRect    = frameRect;
                tabFrameRect.y       = settingsRect.yMax;
                tabFrameRect.height -= settingsRect.height;
                
                Rect tabHeadRect = tabFrameRect;
                tabHeadRect.height = tabHeadRectHeight;

                tabFrameRect.y += 80;
                tabFrameRect.height -= 80;
                
                Rect tabContentRect   = tabFrameRect;
                tabContentRect.x      = 0;
                tabContentRect.y      = 0;
                tabContentRect.width -= 24;
                tabContentRect.height -= 24;
                if (totalWeaponsByMod.Count == 0)
                {
                    UpdateWeaponStatistics();
                }


                listing_Standard.Begin(tabHeadRect);

                    if (currentVersion != null)
                {
                    listing_Standard.Gap();
                    GUI.contentColor = Color.gray;
                    listing_Standard.Label("SMYH.version.label".Translate(currentVersion));
                    GUI.contentColor = Color.white;
                }
                listing_Standard.GapLine();
                Text.Font = GameFont.Medium;
                listing_Standard.Label("SMYH.summary".Translate(), -1F, "SMYH.summary.tooltip".Translate());
                Text.Font = GameFont.Small;
                listing_Standard.Gap();
                listing_Standard.End();

                tabContentRect.height = extraweaponsHeight-15;
                Widgets.BeginScrollView(tabFrameRect, ref summaryScrollPosition, tabContentRect);
            
                listing_Standard.Begin(tabContentRect);
                

                foreach (KeyValuePair<string, int> keyValuePair in totalWeaponsByMod)
                {
                    int fixedWeapons = 0;
                    if (fixedWeaponsByMod.ContainsKey(keyValuePair.Key))
                    {
                        fixedWeapons = fixedWeaponsByMod[keyValuePair.Key];
                    }

                    decimal percent = fixedWeapons / (decimal)keyValuePair.Value * 100;

                    GUI.color = GetColorFromPercent(percent);

                    if (listing_Standard.ListItemSelectable(
                            $"{keyValuePair.Key} {Math.Round(percent)}% ({fixedWeapons}/{keyValuePair.Value})",
                            Color.yellow,
                            out _,
                            selectedSubDef == keyValuePair.Key))
                    {
                        selectedSubDef = selectedSubDef == keyValuePair.Key ? null : keyValuePair.Key;
                    }

                    GUI.color = Color.white;
                }

                listing_Standard.End();

                Widgets.EndScrollView();
                break;
            }

            default:
            {

                List<TabRecord> list = new();

                TabRecord item = new(
                    (string)"HandPosition".Translate(), SetTabFaceStyle(WeaponeStyleTab.HandPositionOnWeapon),
                    (bool)(Tab == WeaponeStyleTab.HandPositionOnWeapon));
                list.Add(item);
                TabRecord item2 = new(
                    (string)"WeaponPosition".Translate(), SetTabFaceStyle(WeaponeStyleTab.WeaponPositionOnPawn),
                    (bool)(Tab == WeaponeStyleTab.WeaponPositionOnPawn));
                list.Add(item2);
                Rect tabRect = new Rect(frameRect.x, frameRect.y+10, frameRect.width, TabDrawer.TabHeight); 
                TabDrawer.DrawTabs(tabRect, list, (tabRect.width )/2);

                frameRect.y += tabRect.height;
                frameRect.height -= tabRect.height;

                if (Tab == WeaponeStyleTab.HandPositionOnWeapon)
                {
                    DoSettingsWindowHandOnWeapon(frameRect);
                }

                if (Tab == WeaponeStyleTab.WeaponPositionOnPawn)
                {
                    DoSettingsWindowWeaponOnPawn(frameRect);

                }
                break;
            }
        }
    }

    public enum WeaponeStyleTab : byte
    {
        HandPositionOnWeapon,

        WeaponPositionOnPawn,
    }

    private void DoSettingsWindowHandOnWeapon(Rect frameRect)
    {

        ThingDef currentDef = DefDatabase<ThingDef>.GetNamedSilentFail(SelectedDef);
        listing_Standard.Begin(frameRect);
        if (currentDef == null)
        {
            listing_Standard.Label("SMYH.error.weapon".Translate(SelectedDef));
            listing_Standard.End();
            return;
        }

        WhandCompProps compProperties = currentDef.GetCompProperties<WhandCompProps>();
        if (compProperties == null)
        {
            listing_Standard.Label("SMYH.error.hands".Translate(SelectedDef));
            listing_Standard.End();
            return;
        }

        Text.Font = GameFont.Medium;
        Rect labelPoint = listing_Standard.Label(currentDef.label.CapitalizeFirst(), -1F,
            currentDef.defName);
        Text.Font = GameFont.Small;
        string modName = currentDef.modContentPack?.Name;
        string modId = currentDef.modContentPack?.PackageId;
        if (currentDef.modContentPack != null)
        {
            listing_Standard.Label($"{modName}", -1F, modId);
        }
        else
        {
            listing_Standard.Gap();
        }

        string description = currentDef.description;
        if (!string.IsNullOrEmpty(description))
        {
            if (description.Length > 250)
            {
                description = description.Substring(0, 250) + "...";
            }

            Widgets.Label(new Rect(labelPoint.x, labelPoint.y + 50, 250, 150), description);
        }

        listing_Standard.Gap(150);

        Rect weaponRect = new(labelPoint.x + 270, labelPoint.y + 5, weaponSize.x,
            weaponSize.y);

        if (currentMainHand == Vector3.zero)
        {
            currentMainHand      = compProperties.MainHand;
            currentOffHand       = compProperties.SecHand;
            currentHasOffHand    = currentOffHand != Vector3.zero;
            currentMainBehind    = compProperties.MainHand.y < 0;
            currentOffBehind     = compProperties.SecHand.y < 0 || currentOffHand == Vector3.zero;
            currentMainHandAngle = compProperties.MainHandAngle;
            currentOffHandAngle  = compProperties.SecHandAngle;
        }

        if (!DrawIcon(currentDef, weaponRect, currentMainHand, currentOffHand, currentMainHandAngle, currentOffHandAngle))
        {
            listing_Standard.Label("SMYH.error.texture".Translate(SelectedDef));
            listing_Standard.End();
            return;
        }



        listing_Standard.Gap(20);
        listing_Standard.CheckboxLabeled("SMYH.twohands.label".Translate(), ref currentHasOffHand);
        listing_Standard.GapLine();
        listing_Standard.ColumnWidth = 230;
        listing_Standard.Label("SMYH.mainhandhorizontal.label".Translate());
        currentMainHand.x = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
            currentMainHand.x, -0.5f, 0.5f, false,
            currentMainHand.x.ToString(), null, null, 0.001f);

        listing_Standard.Label("SMYH.mainhandvertical.label".Translate());
        currentMainHand.z = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
            currentMainHand.z, -0.5f, 0.5f, false,
            currentMainHand.z.ToString(), null, null, 0.001f);

        listing_Standard.Label("SMYH.mainhandAngle.label".Translate());
        currentMainHandAngle = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
            currentMainHandAngle, -180f, 180f, false,
            currentMainHandAngle.ToString(), null, null, 0.001f);

        listing_Standard.Gap();
        listing_Standard.CheckboxLabeled("SMYH.renderbehind.label".Translate(), ref currentMainBehind);

        if (currentHasOffHand)
        {
            listing_Standard.NewColumn();
            listing_Standard.Gap(262);
            listing_Standard.Label("SMYH.offhandhorizontal.label".Translate());
            currentOffHand.x = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
                currentOffHand.x, -0.5f, 0.5f, false,
                currentOffHand.x.ToString(), null, null, 0.001f);
            listing_Standard.Label("SMYH.offhandvertical.label".Translate());
            currentOffHand.z = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
                currentOffHand.z, -0.5f, 0.5f, false,
                currentOffHand.z.ToString(), null, null, 0.001f);

            listing_Standard.Label("SMYH.offhandAngle.label".Translate());
            currentOffHandAngle = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
                currentOffHandAngle, -180f, 180f, false,
                currentOffHandAngle.ToString(), null, null, 0.001f);

            listing_Standard.Gap(12f);
            listing_Standard.CheckboxLabeled("SMYH.renderbehind.label".Translate(), ref currentOffBehind);
        }

        float yPosButtons = frameRect.height - Text.LineHeight * 1.5f;
        DrawAllButtons(currentDef, yPosButtons);

        listing_Standard.End();
        //listing_Standard.ColumnWidth = frameRect.width;

    }

    private void DrawAllButtons(ThingDef currentDef, float yPosButtons)
    {
        WhandCompProps compProperties = currentDef?.GetCompProperties<WhandCompProps>();
        if (compProperties == null)
        {
            return;
        }
        if (instance.Settings.ManualMainHandPositions.ContainsKey(currentDef.defName))
        {
            DrawButton(() =>
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "SMYH.resetsingle.confirm".Translate(), delegate
                    {
                        ResetOneWeapon(currentDef, ref compProperties);
                        currentMainHand                  = compProperties.MainHand;
                        currentOffHand                   = compProperties.SecHand;
                        currentHasOffHand                = currentOffHand != Vector3.zero;
                        currentMainBehind                = compProperties.MainHand.y < 0;
                        currentOffBehind                 = compProperties.SecHand.y < 0;
                        currentMainHandAngle             = compProperties.MainHandAngle;
                        currentOffHandAngle              = compProperties.SecHandAngle;
                        currentWeaponPositionOffset      = compProperties.WeaponPositionOffset;
                        currentAimedWeaponPositionOffset = compProperties.AimedWeaponPositionOffset;
                        currentAimAngle                  = 143f;
                    }));
            }, "SMYH.reset.button".Translate(), new Vector2(350, yPosButtons));
        }

        if (currentMainHand                  != compProperties.MainHand ||
            currentOffHand                   != compProperties.SecHand ||
            currentHasOffHand                != (currentOffHand != Vector3.zero) ||
            currentMainBehind                != compProperties.MainHand.y < 0 ||
            currentOffBehind                 != compProperties.SecHand.y < 0 ||
            currentMainHandAngle             != compProperties.MainHandAngle ||
            currentOffHandAngle              != compProperties.SecHandAngle ||
            currentWeaponPositionOffset      != compProperties.WeaponPositionOffset ||
            currentAimedWeaponPositionOffset != compProperties.AimedWeaponPositionOffset)
        {
            DrawButton(() =>
            {
                currentMainHand   = compProperties.MainHand;
                currentOffHand    = compProperties.SecHand;
                currentHasOffHand = currentOffHand != Vector3.zero;
                currentMainBehind = compProperties.MainHand.y < 0;
                currentOffBehind  = compProperties.SecHand.y < 0;

                currentMainHandAngle             = compProperties.MainHandAngle;
                currentOffHandAngle              = compProperties.SecHandAngle;
                currentWeaponPositionOffset      = compProperties.WeaponPositionOffset;
                currentAimedWeaponPositionOffset = compProperties.AimedWeaponPositionOffset;
            }, "SMYH.undo.button".Translate(), new Vector2(190, yPosButtons));
            DrawButton(() =>
            {
                currentMainHand.y = currentMainBehind ? -0.1f : 0.1f;
                currentOffHand.y = currentOffBehind ? -0.1f : 0.1f;
                if (!currentHasOffHand)
                {
                    currentOffHand = Vector3.zero;
                }

                compProperties.MainHand      = currentMainHand;
                compProperties.SecHand       = currentOffHand;
                compProperties.MainHandAngle = currentMainHandAngle;
                compProperties.SecHandAngle  = currentOffHandAngle;

                compProperties.WeaponPositionOffset      = currentWeaponPositionOffset;
                compProperties.AimedWeaponPositionOffset = currentAimedWeaponPositionOffset;

                instance.Settings.ManualMainHandPositions[currentDef.defName] =
                    new SaveableVector3(compProperties.MainHand, compProperties.MainHandAngle);
                instance.Settings.ManualOffHandPositions[currentDef.defName] =
                    new SaveableVector3(compProperties.SecHand, compProperties.SecHandAngle);
            }, "SMYH.save.button".Translate(), new Vector2(25, yPosButtons));
        }
    }

    private void DoSettingsWindowWeaponOnPawn(Rect frameRect)
    {
        ThingDef currentDef = DefDatabase<ThingDef>.GetNamedSilentFail(SelectedDef);
        listing_Standard.Begin(frameRect);
        if (currentDef == null)
        {
            listing_Standard.Label("SMYH.error.weapon".Translate(SelectedDef));
            listing_Standard.End();
            return;
        }

        WhandCompProps compProperties = currentDef.GetCompProperties<WhandCompProps>();
        if (compProperties == null)
        {
            listing_Standard.Label("SMYH.error.hands".Translate(SelectedDef));
            listing_Standard.End();
            return;
        }

        Rect labelPoint = listing_Standard.Label("");
        Rect weaponRectSouth = new(labelPoint.x+10, labelPoint.y + 5, weaponSize.x,
            weaponSize.y);
        Rect weaponRectEast = new(weaponRectSouth.xMax + 50, weaponRectSouth.y, weaponRectSouth.width,
            weaponRectSouth.height);

        if (currentMainHand == Vector3.zero)
        {
            currentMainHand      = compProperties.MainHand;
            currentOffHand       = compProperties.SecHand;
            currentHasOffHand    = currentOffHand != Vector3.zero;
            currentMainBehind    = compProperties.MainHand.y < 0;
            currentOffBehind     = compProperties.SecHand.y < 0 || currentOffHand == Vector3.zero;
            currentMainHandAngle = compProperties.MainHandAngle;
            currentOffHandAngle  = compProperties.SecHandAngle;

            currentWeaponPositionOffset      = compProperties.WeaponPositionOffset;
            currentAimedWeaponPositionOffset = compProperties.AimedWeaponPositionOffset;
        }

        if (!DrawIconPawnWeapon(currentDef, weaponRectSouth, currentMainHand, currentOffHand, currentMainHandAngle, currentOffHandAngle, Rot4.South))
        {
            listing_Standard.Label("SMYH.error.texture".Translate(SelectedDef));
            listing_Standard.End();
            return;
        }

        DrawIconPawnWeapon(currentDef, weaponRectEast, currentMainHand, currentOffHand, currentMainHandAngle,
            currentOffHandAngle, Rot4.East);
        listing_Standard.End();

        Rect settingsRect = new(frameRect.x, frameRect.y + weaponRectSouth.height + 24f, frameRect.width,
            frameRect.height - weaponRectSouth.height - 24f);
        listing_Standard.Begin(settingsRect);

        listing_Standard.ColumnWidth = 230;

        listing_Standard.CheckboxLabeled("SMYH.aimingDesc.label".Translate(), ref currentShowAiming);

        listing_Standard.Label("SMYH.weaponPositionHorizontal.label".Translate());
        currentWeaponPositionOffset.x = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
            currentWeaponPositionOffset.x, -0.5f, 0.5f, false,
            currentWeaponPositionOffset.x.ToString(), null, null, 0.001f);

        listing_Standard.Label("SMYH.aimedPositionHorizontal.label".Translate());
        currentAimedWeaponPositionOffset.x = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
            currentAimedWeaponPositionOffset.x, -0.5f, 0.5f, false,
            currentAimedWeaponPositionOffset.x.ToString(), null, null, 0.001f);

        listing_Standard.Label("SMYH.weaponPositionVertical.label".Translate());
        currentWeaponPositionOffset.z = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
            currentWeaponPositionOffset.z, -0.5f, 0.5f, false,
            currentWeaponPositionOffset.z.ToString(), null, null, 0.001f);


        listing_Standard.Gap();

      //  if (currentHasOffHand)
        {
            listing_Standard.NewColumn();

            listing_Standard.Label("SMYH.aimAngle.label".Translate());
            currentAimAngle = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
                currentAimAngle, 20, 160, false,
                currentAimAngle.ToString(), null, null, 0.001f);


            listing_Standard.Label("SMYH.weaponPositionHorizontalEast.label".Translate());
            currentWeaponPositionOffset.y = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
                currentWeaponPositionOffset.y, -0.5f, 0.5f, false,
                currentWeaponPositionOffset.y.ToString(), null, null, 0.001f);


            listing_Standard.Label("SMYH.aimedPositionHorizontalEast.label".Translate());
            currentAimedWeaponPositionOffset.y = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
                currentAimedWeaponPositionOffset.y, -0.5f, 0.5f, false,
                currentAimedWeaponPositionOffset.y.ToString(), null, null, 0.001f);

            listing_Standard.Label("SMYH.aimedPositionVertical.label".Translate());
            currentAimedWeaponPositionOffset.z = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
                currentAimedWeaponPositionOffset.z, -0.5f, 0.5f, false,
                currentAimedWeaponPositionOffset.z.ToString(), null, null, 0.001f);


        }

        float yPosButtons = frameRect.height - Text.LineHeight * 1.5f;
      
        DrawAllButtons(currentDef, yPosButtons);

        listing_Standard.End();

    }

    private void DrawResetButton(ThingDef currentDef, float yPosButtons)
    {
        DrawButton(() =>
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "SMYH.resetsingle.confirm".Translate(), delegate
                {
                    WhandCompProps compProperties = currentDef?.GetCompProperties<WhandCompProps>();
                    if (compProperties == null)
                    {
                        return;
                    }
                    ResetOneWeapon(currentDef, ref compProperties);
                    currentMainHand                  = compProperties.MainHand;
                    currentOffHand                   = compProperties.SecHand;
                    currentHasOffHand                = currentOffHand != Vector3.zero;
                    currentMainBehind                = compProperties.MainHand.y < 0;
                    currentOffBehind                 = compProperties.SecHand.y < 0;
                    currentMainHandAngle             = compProperties.MainHandAngle;
                    currentOffHandAngle              = compProperties.SecHandAngle;
                    currentWeaponPositionOffset      = compProperties.WeaponPositionOffset;
                    currentAimedWeaponPositionOffset = compProperties.AimedWeaponPositionOffset;
                }));
        }, "SMYH.reset.button".Translate(), new Vector2(350, yPosButtons));
    }


    public WeaponeStyleTab Tab;

    public Action SetTabFaceStyle(WeaponeStyleTab tab)
    {
        return delegate { Tab = tab; };
    }

    private void CopyChangedWeapons(bool onlySelected = false)
    {
        if (onlySelected && string.IsNullOrEmpty(selectedSubDef))
        {
            return;
        }

        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        stringBuilder.AppendLine("<Defs>");
        stringBuilder.AppendLine("  <WHands.ClutterHandsTDef>");
        stringBuilder.AppendLine(
            onlySelected
                ? $"     <defName>ClutterHandsSettings_{Regex.Replace(selectedSubDef, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled)}_{SystemInfo.deviceName.GetHashCode()}</defName>"
                : $"     <defName>ClutterHandsSettings_All_{SystemInfo.deviceName.GetHashCode()}</defName>");

        stringBuilder.AppendLine("      <label>Weapon hand settings</label>");
        stringBuilder.AppendLine("      <thingClass>Thing</thingClass>");
        stringBuilder.AppendLine("      <WeaponCompLoader>");
        Dictionary<string, SaveableVector3> handPositionsToIterate = instance.Settings.ManualMainHandPositions;
        if (onlySelected)
        {
            List<string> weaponsDefsToSelectFrom = (from ThingDef weapon in AllWeapons
                where weapon.modContentPack == null &&
                      selectedSubDef == "SMYH.unknown".Translate() ||
                      weapon.modContentPack?.Name == selectedSubDef
                select weapon.defName).ToList();
            handPositionsToIterate = new Dictionary<string, SaveableVector3>(
                from position in instance.Settings.ManualMainHandPositions
                where weaponsDefsToSelectFrom.Contains(position.Key)
                select position);
        }

        foreach (KeyValuePair<string, SaveableVector3> settingsManualMainHandPosition in handPositionsToIterate)
        {
            stringBuilder.AppendLine("          <li>");
            stringBuilder.AppendLine($"              <MainHand>{settingsManualMainHandPosition.Value}</MainHand>");
            if (instance.Settings.ManualOffHandPositions.ContainsKey(settingsManualMainHandPosition.Key))
            {
                SaveableVector3 secHand = instance.Settings.ManualOffHandPositions[settingsManualMainHandPosition.Key];
                if (secHand.ToVector3() != Vector3.zero || secHand.ToAngleFloat() != 0f)
                {
                    stringBuilder.AppendLine($"              <SecHand>{secHand}</SecHand>");
                }
            }

            stringBuilder.AppendLine("              <ThingTargets>");
            stringBuilder.AppendLine($"                 <li>{settingsManualMainHandPosition.Key}</li> <!-- {ThingDef.Named(settingsManualMainHandPosition.Key).label} -->");
            stringBuilder.AppendLine("              </ThingTargets>");
            stringBuilder.AppendLine("          </li>");
        }

        stringBuilder.AppendLine("      </WeaponCompLoader>");
        stringBuilder.AppendLine("  </WHands.ClutterHandsTDef>");
        stringBuilder.AppendLine("</Defs>");

        GUIUtility.systemCopyBuffer = stringBuilder.ToString();
        Messages.Message("Modified data copied to clipboard.", MessageTypeDefOf.SituationResolved, false);
    }

    private void DrawTabsList(Rect rect)
    {

        Rect scrollContainer = rect.ContractedBy(10);
        scrollContainer.width = leftSideWidth;
        Widgets.DrawBoxSolid(scrollContainer, Color.grey);
        Rect innerContainer = scrollContainer.ContractedBy(1);
        Widgets.DrawBoxSolid(innerContainer, new ColorInt(42, 43, 44).ToColor);
        Rect tabFrameRect = innerContainer.ContractedBy(5);
        tabFrameRect.y += 15;
        tabFrameRect.height -= 15;
        Rect tabContentRect = tabFrameRect;
        tabContentRect.x = 0;
        tabContentRect.y = 0;
        tabContentRect.width -= 24;
        List<ThingDef> weaponsToShow = AllWeapons;
        int listAddition = 24;
        if (!string.IsNullOrEmpty(selectedSubDef))
        {
            weaponsToShow = (from ThingDef weapon in AllWeapons
                where weapon.modContentPack == null &&
                      selectedSubDef == "SMYH.unknown".Translate() ||
                      weapon.modContentPack?.Name == selectedSubDef
                select weapon).ToList();
            listAddition = 60;
        }

        tabContentRect.height = (weaponsToShow.Count * 25f) + listAddition;
        Widgets.BeginScrollView(tabFrameRect, ref tabsScrollPosition, tabContentRect);
        listing_Standard.ColumnWidth = tabContentRect.width;
        listing_Standard.Begin(tabContentRect);
        //Text.Font = GameFont.Tiny;
        if (listing_Standard.ListItemSelectable("SMYH.settings".Translate(), Color.yellow,
                out _, SelectedDef == "Settings"))
        {
            SelectedDef = SelectedDef == "Settings" ? null : "Settings";
        }

        listing_Standard.ListItemSelectable(null, Color.yellow, out _);
        selectedHasManualDefs = new List<string>();
        foreach (ThingDef thingDef in weaponsToShow)
        {
            string toolTip = "SMYH.weaponrow.red";
            if (!DefinedByDef.Contains(thingDef.defName) &&
                !instance.Settings.ManualMainHandPositions.ContainsKey(thingDef.defName))
            {
                GUI.color = Color.red;
            }
            else
            {
                if (instance.Settings.ManualMainHandPositions.ContainsKey(thingDef.defName))
                {
                    toolTip = "SMYH.weaponrow.green";
                    GUI.color = Color.green;
                    selectedHasManualDefs.Add(thingDef.defName);
                }
                else
                {
                    toolTip = "SMYH.weaponrow.cyan";
                    GUI.color = Color.cyan;
                }
            }

            if (listing_Standard.ListItemSelectable(thingDef.label.CapitalizeFirst(), Color.yellow,
                    out Vector2 position,
                    SelectedDef == thingDef.defName, false, toolTip.Translate()))
            {
                SelectedDef          = SelectedDef == thingDef.defName ? null : thingDef.defName;
                currentMainHand      = Vector3.zero;
                currentOffHand       = Vector3.zero;
                currentMainHandAngle = 0f;
                currentOffHandAngle  = 0f;
            }

            GUI.color = Color.white;
            position.x = position.x + tabContentRect.width - iconSize.x;
            DrawWeapon(thingDef, new Rect(position, iconSize));
        }

        if (!string.IsNullOrEmpty(selectedSubDef))
        {
            listing_Standard.ListItemSelectable(null, Color.yellow, out _);
            if (listing_Standard.ListItemSelectable(
                    "SMYH.showhidden".Translate(AllWeapons.Count - weaponsToShow.Count), Color.yellow,
                    out _))
            {
                selectedSubDef = string.Empty;
            }
        }

        listing_Standard.End();
        //Text.Font = GameFont.Small;
        Widgets.EndScrollView();
    }

    private void ResetOneWeapon(ThingDef currentDef, ref WhandCompProps compProperties)
    {
        instance.Settings.ManualMainHandPositions.Remove(currentDef.defName);
        instance.Settings.ManualOffHandPositions.Remove(currentDef.defName);
        if (compProperties == null)
        {
            compProperties = currentDef.GetCompProperties<WhandCompProps>();
        }

        compProperties.MainHand = Vector3.zero;
        compProperties.SecHand = Vector3.zero;
        compProperties.WeaponPositionOffset = Vector3.zero;
        compProperties.AimedWeaponPositionOffset = Vector3.zero;
        compProperties.MainHandAngle = 0f;
        compProperties.SecHandAngle = 0f;

        RimWorld_MainMenuDrawer_MainMenuOnGUI.LoadFromDefs(currentDef);
        if (compProperties.MainHand == Vector3.zero)
        {
            RimWorld_MainMenuDrawer_MainMenuOnGUI.FigureOutSpecific(currentDef);
        }
    }

    private Color GetColorFromPercent(decimal percent)
    {
        switch (percent)
        {
            case < 25:
                return Color.red;
            case < 50:
                return Color.yellow;
            case < 75:
                return Color.white;
            case >= 75:
                return Color.green;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class HotSwappableAttribute : Attribute
    {
    }
}