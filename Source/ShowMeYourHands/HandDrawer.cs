using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;
using RimWorld;
using UnityEngine;
using Verse;
using static System.Byte;

namespace ShowMeYourHands;

[StaticConstructorOnStartup]
public class HandDrawer : ThingComp
{
    private static Dictionary<Thing, Color> colorDictionary;


    public static readonly Dictionary<Pawn, Graphic> mainHandGraphics = new Dictionary<Pawn, Graphic>();
    public static readonly Dictionary<Pawn, Graphic> offHandGraphics = new Dictionary<Pawn, Graphic>();
    public static readonly Dictionary<Pawn, float> pawnBodySizes = new Dictionary<Pawn, float>();
    public static readonly Dictionary<Pawn, bool> pawnsMissingAHand = new Dictionary<Pawn, bool>();

    private Color handColor;
    private Mesh handMesh;
    private int LastDrawn;
    private Vector3 MainHand;
    private Vector3 OffHand;

    private Color HandColor
    {
        get
        {
            if (GenTicks.TicksAbs % 100 != 0 && handColor != default)
            {
                return handColor;
            }

            if (!(parent is Pawn pawn))
            {
                return Color.white;
            }

            handColor = getHandColor(pawn, out bool hasGloves, out Color secondColor);
            if (!mainHandGraphics.ContainsKey(pawn) || mainHandGraphics[pawn].color != handColor)
            {
                if (hasGloves)
                {
                    mainHandGraphics[pawn] = GraphicDatabase.Get<Graphic_Single>("HandClean", ShaderDatabase.Cutout,
                        new Vector2(1f, 1f),
                        handColor, handColor);
                }
                else
                {
                    mainHandGraphics[pawn] = GraphicDatabase.Get<Graphic_Single>("Hand", ShaderDatabase.Cutout,
                        new Vector2(1f, 1f),
                        handColor, handColor);
                }
            }

            if (offHandGraphics.ContainsKey(pawn) && offHandGraphics[pawn].color == handColor)
            {
                return handColor;
            }

            if (hasGloves)
            {
                offHandGraphics[pawn] = GraphicDatabase.Get<Graphic_Single>("OffHandClean",
                    ShaderDatabase.Cutout,
                    new Vector2(1f, 1f),
                    handColor, handColor);
            }
            else
            {
                if (secondColor != default)
                {
                    offHandGraphics[pawn] = GraphicDatabase.Get<Graphic_Single>("OffHand",
                        ShaderDatabase.Cutout,
                        new Vector2(1f, 1f),
                        secondColor, secondColor);
                }
                else
                {
                    offHandGraphics[pawn] = GraphicDatabase.Get<Graphic_Single>("OffHand", ShaderDatabase.Cutout,
                        new Vector2(1f, 1f),
                        handColor, handColor);
                }
            }

            return handColor;
        }
        set => handColor = value;
    }

    public void ReadXML()
    {
        WhandCompProps whandCompProps = (WhandCompProps)props;
        if (whandCompProps.MainHand != Vector3.zero)
        {
            MainHand = whandCompProps.MainHand;
        }

        if (whandCompProps.SecHand != Vector3.zero)
        {
            OffHand = whandCompProps.SecHand;
        }
    }


    private void AngleCalc()
    {
        if (!(parent is Pawn pawn) || pawn.Dead || !pawn.Spawned)
        {
            return;
        }

        if (pawn.equipment?.Primary == null)
        {
            return;
        }

        if (pawn.CurJob != null && pawn.CurJob.def.neverShowWeapon)
        {
            return;
        }

        ThingWithComps mainhandWeapon = pawn.equipment.Primary;
        WhandCompProps compProperties = mainhandWeapon.def.GetCompProperties<WhandCompProps>();
        if (compProperties != null)
        {
            MainHand = compProperties.MainHand;
            OffHand = compProperties.SecHand;
        }
        else
        {
            OffHand = Vector3.zero;
            MainHand = Vector3.zero;
        }

        ThingWithComps offhandWeapon = null;
        if (pawn.equipment.AllEquipmentListForReading.Count == 2)
        {
            offhandWeapon = (from weapon in pawn.equipment.AllEquipmentListForReading
                             where weapon != mainhandWeapon
                             select weapon).First();
            WhandCompProps offhandComp = offhandWeapon?.def.GetCompProperties<WhandCompProps>();
            if (offhandComp != null)
            {
                OffHand = offhandComp.MainHand;
            }
        }

        if (pawn.stances.curStance is Stance_Busy { neverAimWeapon: false, focusTarg.IsValid: true } stance_Busy)
        {
            Vector3 a = stance_Busy.focusTarg.HasThing
                ? stance_Busy.focusTarg.Thing.DrawPos
                : stance_Busy.focusTarg.Cell.ToVector3Shifted();

            float num = 0f;
            if ((a - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
            {
                num = (a - pawn.DrawPos).AngleFlat();
            }

            DrawHands(mainhandWeapon, num, offhandWeapon, false, true);
            return;
        }

        Type baseType = pawn.Drawer.renderer.GetType();
        MethodInfo methodInfo = baseType.GetMethod("CarryWeaponOpenly", BindingFlags.NonPublic | BindingFlags.Instance);
        object result = methodInfo?.Invoke(pawn.Drawer.renderer, null);
        if (result == null || !(bool)result)
        {
            return;
        }

        if (pawn.Rotation == Rot4.South || pawn.Rotation == Rot4.North)
        {
            DrawHands(mainhandWeapon, 143f, offhandWeapon, true);
            return;
        }

        if (pawn.Rotation == Rot4.East)
        {
            DrawHands(mainhandWeapon, 143f, offhandWeapon);
            return;
        }

        if (pawn.Rotation != Rot4.West)
        {
            return;
        }

        DrawHands(mainhandWeapon, 217f, offhandWeapon);
    }


    public void DrawHands()
    {
        if (!(parent is Pawn pawn) || pawn.DestroyedOrNull() || pawn.Map == null)
        {
            return;
        }

        if (!pawnBodySizes.ContainsKey(pawn) || GenTicks.TicksAbs % GenTicks.TickLongInterval == 0)
        {
            float bodySize = 1f;
            if (ShowMeYourHandsMod.instance.Settings.ResizeHands)
            {
                if (pawn.RaceProps != null)
                {
                    bodySize = pawn.RaceProps.baseBodySize;
                }

                if (ShowMeYourHandsMain.BabysAndChildrenLoaded && ShowMeYourHandsMain.GetBodySizeScaling != null)
                {
                    bodySize = (float)ShowMeYourHandsMain.GetBodySizeScaling.Invoke(null, new object[] { pawn });
                }
            }

            pawnBodySizes[pawn] = 0.8f * bodySize;
        }

        Color unused = HandColor;
        if (handMesh == null)
        {
            handMesh = MeshMakerPlanes.NewPlaneMesh(pawnBodySizes[pawn]);
        }

        Graphic mainHandTex = mainHandGraphics[pawn];
        Graphic offHandTex = offHandGraphics[pawn];

        if (mainHandTex == null || offHandTex == null)
        {
            return;
        }

        Material mainSingle = mainHandTex.MatSingle;
        Material offSingle = offHandTex.MatSingle;
        Vector3 heightOffset = new Vector3(0, 0, 0.7f * pawnBodySizes[pawn] / 2);
        Vector3 sideOffset = new Vector3(0.2f, 0, 0);
        Vector3 layerOffset = new Vector3(0, 0.0001f, 0);

        Vector3 basePosition = pawn.DrawPos - heightOffset;
        if (pawn.Rotation == Rot4.North)
        {
            Graphics.DrawMesh(handMesh,
                basePosition + sideOffset - layerOffset, new Quaternion(), mainSingle, 0);
        }

        if (pawn.Rotation == Rot4.South)
        {
            Graphics.DrawMesh(handMesh,
                basePosition - sideOffset + layerOffset, new Quaternion(), mainSingle, 0);
        }

        if (pawn.Rotation == Rot4.East)
        {
            Graphics.DrawMesh(handMesh,
                basePosition + layerOffset, new Quaternion(), mainSingle, 0);
            return;
        }

        if (pawnsMissingAHand.ContainsKey(pawn) && pawnsMissingAHand[pawn])
        {
            return;
        }

        if (pawn.Rotation == Rot4.North)
        {
            Graphics.DrawMesh(handMesh,
                basePosition - sideOffset - layerOffset, new Quaternion(), offSingle, 0);
            return;
        }

        if (pawn.Rotation == Rot4.South)
        {
            Graphics.DrawMesh(handMesh,
                basePosition + sideOffset + layerOffset, new Quaternion(), offSingle, 0);
            return;
        }

        Graphics.DrawMesh(handMesh,
            basePosition + layerOffset, new Quaternion(), offSingle, 0);
    }

    public void DrawHands(Thing carriedThing, Vector3 thingPosition)
    {
        if (!(parent is Pawn pawn))
        {
            return;
        }

        if (!pawnBodySizes.ContainsKey(pawn) || GenTicks.TicksAbs % GenTicks.TickLongInterval == 0)
        {
            float bodySize = 1f;
            if (ShowMeYourHandsMod.instance.Settings.ResizeHands)
            {
                if (pawn.RaceProps != null)
                {
                    bodySize = pawn.RaceProps.baseBodySize;
                }

                if (ShowMeYourHandsMain.BabysAndChildrenLoaded && ShowMeYourHandsMain.GetBodySizeScaling != null)
                {
                    bodySize = (float)ShowMeYourHandsMain.GetBodySizeScaling.Invoke(null, new object[] { pawn });
                }
            }

            pawnBodySizes[pawn] = 0.8f * bodySize;
        }

        Color unused = HandColor;
        Mesh mesh = MeshMakerPlanes.NewPlaneMesh(pawnBodySizes[pawn]);
        Graphic mainHandTex = mainHandGraphics[pawn];
        Graphic offHandTex = offHandGraphics[pawn];


        if (mainHandTex == null || offHandTex == null)
        {
            return;
        }

        LastDrawn = GenTicks.TicksAbs;
        Material matSingle = mainHandTex.MatSingle;
        Material offSingle = offHandTex.MatSingle;
        Vector3 height = new Vector3(0, 0, 0.1f);
        Vector3 width = new Vector3(-0.2f, 0, 0);
        if (pawn.Rotation == Rot4.West)
        {
            height.z *= -1;
        }

        Graphics.DrawMesh(mesh,
            thingPosition + height + width, new Quaternion(), matSingle, 0);

        if (pawnsMissingAHand.ContainsKey(pawn) && pawnsMissingAHand[pawn])
        {
            return;
        }

        Graphics.DrawMesh(mesh,
            thingPosition + (height * -1) + (width * -1), new Quaternion(), offSingle, 0);
    }

    private void DrawHands(Thing mainHandWeapon, float aimAngle, Thing offHandWeapon = null, bool idle = false,
        bool aiming = false)
    {
        bool flipped = false;
        if (!(parent is Pawn pawn))
        {
            return;
        }

        if (!ShowMeYourHandsMain.weaponLocations.ContainsKey(mainHandWeapon))
        {
            if (ShowMeYourHandsMod.instance.Settings.VerboseLogging)
            {
                Log.ErrorOnce(
                    $"[ShowMeYourHands]: Could not find the position for {mainHandWeapon.def.label} from the mod {mainHandWeapon.def.modContentPack.Name}, equipped by {pawn.Name}. Please report this issue to the author of Show Me Your Hands if possible.",
                    mainHandWeapon.def.GetHashCode());
            }

            return;
        }

        Vector3 mainWeaponLocation = ShowMeYourHandsMain.weaponLocations[mainHandWeapon].Item1;
        float mainHandAngle = ShowMeYourHandsMain.weaponLocations[mainHandWeapon].Item2;
        Vector3 offhandWeaponLocation = Vector3.zero;
        float offHandAngle = mainHandAngle;
        float mainMeleeExtra = 0f;
        float offMeleeExtra = 0f;
        bool mainMelee = false;
        bool offMelee = false;
        if (offHandWeapon != null)
        {
            if (!ShowMeYourHandsMain.weaponLocations.ContainsKey(offHandWeapon))
            {
                if (ShowMeYourHandsMod.instance.Settings.VerboseLogging)
                {
                    Log.ErrorOnce(
                        $"[ShowMeYourHands]: Could not find the position for {offHandWeapon.def.label} from the mod {offHandWeapon.def.modContentPack.Name}, equipped by {pawn.Name}. Please report this issue to the author of Show Me Your Hands if possible.",
                        offHandWeapon.def.GetHashCode());
                }
            }
            else
            {
                offhandWeaponLocation = ShowMeYourHandsMain.weaponLocations[offHandWeapon].Item1;
                offHandAngle = ShowMeYourHandsMain.weaponLocations[offHandWeapon].Item2;
            }
        }

        mainHandAngle -= 90f;
        offHandAngle -= 90f;
        if (pawn.Rotation == Rot4.West || aimAngle is > 200f and < 340f)
        {
            flipped = true;
        }

        if (mainHandWeapon.def.IsMeleeWeapon)
        {
            mainMelee = true;
            mainMeleeExtra = 0.0001f;
            if (idle && offHandWeapon != null) //Dual wield idle vertical
            {
                if (pawn.Rotation == Rot4.South)
                {
                    mainHandAngle -= mainHandWeapon.def.equippedAngleOffset;
                }
                else
                {
                    mainHandAngle += mainHandWeapon.def.equippedAngleOffset;
                }
            }
            else
            {
                if (flipped)
                {
                    mainHandAngle -= 180f;
                    mainHandAngle -= mainHandWeapon.def.equippedAngleOffset;
                }
                else
                {
                    mainHandAngle += mainHandWeapon.def.equippedAngleOffset;
                }
            }
        }
        else
        {
            if (flipped)
            {
                mainHandAngle -= 180f;
            }
        }

        if (offHandWeapon?.def.IsMeleeWeapon == true)
        {
            offMelee = true;
            offMeleeExtra = 0.0001f;
            if (idle && pawn.Rotation == Rot4.North) //Dual wield north
            {
                offHandAngle -= offHandWeapon.def.equippedAngleOffset;
            }
            else
            {
                if (flipped)
                {
                    offHandAngle -= 180f;
                    offHandAngle -= offHandWeapon.def.equippedAngleOffset;
                }
                else
                {
                    offHandAngle += offHandWeapon.def.equippedAngleOffset;
                }
            }
        }
        else
        {
            if (flipped)
            {
                offHandAngle -= 180f;
            }
        }

        mainHandAngle %= 360f;
        offHandAngle %= 360f;

        Color unused = HandColor;

        if (!mainHandGraphics.ContainsKey(pawn) || !offHandGraphics.ContainsKey(pawn))
        {
            return;
        }

        Graphic mainHandTex = mainHandGraphics[pawn];
        Graphic offHandTex = offHandGraphics[pawn];


        if (mainHandTex == null || offHandTex == null)
        {
            return;
        }

        Material matSingle = mainHandTex.MatSingle;
        Material offSingle = offHandTex.MatSingle;
        float drawSize = 1f;
        LastDrawn = GenTicks.TicksAbs;

        if (ShowMeYourHandsMod.instance.Settings.RepositionHands && mainHandWeapon.def.graphicData != null &&
            mainHandWeapon.def?.graphicData?.drawSize.x != 1f)
        {
            drawSize = mainHandWeapon.def.graphicData.drawSize.x;
        }

        if (!pawnBodySizes.ContainsKey(pawn) || GenTicks.TicksAbs % GenTicks.TickLongInterval == 0)
        {
            float bodySize = 1f;
            if (ShowMeYourHandsMod.instance.Settings.ResizeHands)
            {
                if (pawn.RaceProps != null)
                {
                    bodySize = pawn.RaceProps.baseBodySize;
                }

                if (ShowMeYourHandsMain.BabysAndChildrenLoaded && ShowMeYourHandsMain.GetBodySizeScaling != null)
                {
                    bodySize = (float)ShowMeYourHandsMain.GetBodySizeScaling.Invoke(null, new object[] { pawn });
                }
            }

            pawnBodySizes[pawn] = 0.8f * bodySize;
        }

        Mesh mesh = MeshMakerPlanes.NewPlaneMesh(pawnBodySizes[pawn], flipped);

        if (MainHand != Vector3.zero)
        {
            float x = MainHand.x * drawSize;
            float z = MainHand.z * drawSize;
            float y = MainHand.y < 0 ? -0.0001f : 0.0001f;

            if (flipped)
            {
                x *= -1;
            }

            if (pawn.Rotation == Rot4.North && !mainMelee && !aiming)
            {
                z += 0.1f;
            }

            mainWeaponLocation += AdjustRenderOffsetFromDir(pawn, mainHandWeapon as ThingWithComps);

            Graphics.DrawMesh(mesh,
                mainWeaponLocation + new Vector3(x, y + mainMeleeExtra, z).RotatedBy(mainHandAngle),
                Quaternion.AngleAxis(mainHandAngle, Vector3.up), y >= 0 ? matSingle : offSingle, 0);
        }

        if (OffHand == Vector3.zero || pawnsMissingAHand.ContainsKey(pawn) && pawnsMissingAHand[pawn])
        {
            return;
        }

        float x2 = OffHand.x * drawSize;
        float z2 = OffHand.z * drawSize;
        float y2 = OffHand.y < 0 ? -0.0001f : 0.0001f;


        if (offHandWeapon != null)
        {
            drawSize = 1f;

            if (ShowMeYourHandsMod.instance.Settings.RepositionHands && offHandWeapon.def.graphicData != null &&
                offHandWeapon.def?.graphicData?.drawSize.x != 1f)
            {
                drawSize = offHandWeapon.def.graphicData.drawSize.x;
            }

            x2 = OffHand.x * drawSize;
            z2 = OffHand.z * drawSize;

            if (flipped)
            {
                x2 *= -1;
            }

            if (idle && !offMelee)
            {
                if (pawn.Rotation == Rot4.South)
                {
                    z2 += 0.05f;
                }
                else
                {
                    z2 -= 0.05f;
                }
            }


            offhandWeaponLocation += AdjustRenderOffsetFromDir(pawn, offHandWeapon as ThingWithComps);

            Graphics.DrawMesh(mesh,
                offhandWeaponLocation + new Vector3(x2, y2 + offMeleeExtra, z2).RotatedBy(offHandAngle),
                Quaternion.AngleAxis(offHandAngle, Vector3.up),
                y2 >= 0 ? matSingle : offSingle, 0);
            return;
        }

        if (flipped)
        {
            x2 *= -1;
        }

        Graphics.DrawMesh(mesh,
            mainWeaponLocation + new Vector3(x2, y2 + offMeleeExtra, z2).RotatedBy(mainHandAngle),
            Quaternion.AngleAxis(mainHandAngle, Vector3.up), y2 >= 0 ? matSingle : offSingle, 0);
    }

    private Vector3 AdjustRenderOffsetFromDir(Pawn pawn, ThingWithComps weapon)
    {
        if (!ShowMeYourHandsMain.OversizedWeaponLoaded && !ShowMeYourHandsMain.EnableOversizedLoaded)
        {
            return Vector3.zero;
        }

        switch (pawn.Rotation.AsInt)
        {
            case 0:
                return ShowMeYourHandsMain.northOffsets.TryGetValue(weapon.def, out Vector3 northValue)
                    ? northValue
                    : Vector3.zero;
            case 1:
                return ShowMeYourHandsMain.eastOffsets.TryGetValue(weapon.def, out Vector3 eastValue)
                    ? eastValue
                    : Vector3.zero;
            case 2:
                return ShowMeYourHandsMain.southOffsets.TryGetValue(weapon.def, out Vector3 southValue)
                    ? southValue
                    : Vector3.zero;
            case 3:
                return ShowMeYourHandsMain.westOffsets.TryGetValue(weapon.def, out Vector3 westValue)
                    ? westValue
                    : Vector3.zero;
            default:
                return Vector3.zero;
        }
    }


    public override void PostDraw()
    {
        AngleCalc();
    }

    private Color getHandColor(Pawn pawn, out bool hasGloves, out Color secondColor)
    {
        hasGloves = false;
        secondColor = default;
        List<Hediff_AddedPart> addedHands = null;

        if (ShowMeYourHandsMod.instance.Settings.MatchHandAmounts ||
            ShowMeYourHandsMod.instance.Settings.MatchArtificialLimbColor)
        {
            addedHands = pawn.health?.hediffSet?.GetHediffs<Hediff_AddedPart>()
                .Where(x => x.Part.def == ShowMeYourHandsMain.HandDef ||
                            x.Part.parts.Any(record => record.def == ShowMeYourHandsMain.HandDef)).ToList();
        }

        if (ShowMeYourHandsMod.instance.Settings.MatchHandAmounts && pawn.health is { hediffSet: { } })
        {
            pawnsMissingAHand[pawn] = pawn.health
                    .hediffSet
                    .GetNotMissingParts().Count(record => record.def == ShowMeYourHandsMain.HandDef) +
                addedHands?.Count < 2;
        }

        if (!ShowMeYourHandsMod.instance.Settings.MatchArmorColor || !(from apparel in pawn.apparel.WornApparel
                                                                       where apparel.def.apparel.bodyPartGroups.Any(def => def.defName == "Hands")
                                                                       select apparel).Any())
        {
            if (!ShowMeYourHandsMod.instance.Settings.MatchArtificialLimbColor)
            {
                return pawn.story.SkinColor;
            }

            if (addedHands == null || !addedHands.Any())
            {
                return pawn.story.SkinColor;
            }

            Color mainColor = (Color)default;

            foreach (Hediff_AddedPart hediffAddedPart in addedHands)
            {
                if (!ShowMeYourHandsMain.HediffColors.ContainsKey(hediffAddedPart.def))
                {
                    continue;
                }

                if (mainColor == default)
                {
                    mainColor = ShowMeYourHandsMain.HediffColors[hediffAddedPart.def];
                    continue;
                }

                secondColor = ShowMeYourHandsMain.HediffColors[hediffAddedPart.def];
            }

            if (mainColor == default)
            {
                return pawn.story.SkinColor;
            }

            if (secondColor == default)
            {
                secondColor = pawn.story.SkinColor;
            }

            return mainColor;
        }

        IEnumerable<Apparel> handApparel = from apparel in pawn.apparel.WornApparel
                                           where apparel.def.apparel.bodyPartGroups.Any(def => def.defName == "Hands")
                                           select apparel;

        //ShowMeYourHandsMain.LogMessage($"Found gloves on {pawn.NameShortColored}: {string.Join(",", handApparel)}");

        Thing outerApparel = null;
        int highestDrawOrder = 0;
        foreach (Apparel thing in handApparel)
        {
            int thingOutmostLayer =
                thing.def.apparel.layers.OrderByDescending(def => def.drawOrder).First().drawOrder;
            if (outerApparel != null && highestDrawOrder >= thingOutmostLayer)
            {
                continue;
            }

            highestDrawOrder = thingOutmostLayer;
            outerApparel = thing;
        }

        if (outerApparel == null)
        {
            return pawn.story.SkinColor;
        }

        hasGloves = true;
        if (colorDictionary == null)
        {
            colorDictionary = new Dictionary<Thing, Color>();
        }

        if (ShowMeYourHandsMain.IsColorable.Contains(outerApparel.def))
        {
            CompColorable comp = outerApparel.TryGetComp<CompColorable>();
            if (comp.Active)
            {
                return comp.Color;
            }
        }

        if (colorDictionary.ContainsKey(outerApparel))
        {
            return colorDictionary[outerApparel];
        }

        if (outerApparel.Stuff != null && outerApparel.Graphic.Shader != ShaderDatabase.CutoutComplex)
        {
            colorDictionary[outerApparel] = outerApparel.def.GetColorForStuff(outerApparel.Stuff);
        }
        else
        {
            colorDictionary[outerApparel] =
                AverageColorFromTexture((Texture2D)outerApparel.Graphic.MatSingle.mainTexture);
        }

        return colorDictionary[outerApparel];
    }

    private Color32 AverageColorFromTexture(Texture2D texture)
    {
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            texture.width,
            texture.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);
        Graphics.Blit(texture, renderTexture);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        Texture2D tex = new Texture2D(texture.width, texture.height);
        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        tex.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);
        return AverageColorFromColors(tex.GetPixels32());
    }

    private Color32 AverageColorFromColors(Color32[] colors)
    {
        Dictionary<Color32, int> shadeDictionary = new Dictionary<Color32, int>();
        foreach (Color32 texColor in colors)
        {
            if (texColor.a < 50)
            {
                // Ignore low transparency
                continue;
            }

            Rgb currentRgb = new Rgb { B = texColor.b, G = texColor.b, R = texColor.r };

            if (currentRgb.Compare(new Rgb { B = 0, G = 0, R = 0 }, new Cie1976Comparison()) < 2)
            {
                // Ignore black pixels
                continue;
            }

            if (shadeDictionary.Count == 0)
            {
                shadeDictionary[texColor] = 1;
                continue;
            }


            bool added = false;
            foreach (Color32 rgb in shadeDictionary.Keys.Where(rgb =>
                         currentRgb.Compare(new Rgb { B = rgb.b, G = rgb.b, R = rgb.r }, new Cie1976Comparison()) < 2))
            {
                shadeDictionary[rgb]++;
                added = true;
                break;
            }

            if (!added)
            {
                shadeDictionary[texColor] = 1;
            }
        }

        if (shadeDictionary.Count == 0)
        {
            return new Color32(0, 0, 0, MaxValue);
        }

        Color32 greatestValue = shadeDictionary.Aggregate((rgb, max) => rgb.Value > max.Value ? rgb : max).Key;
        greatestValue.a = MaxValue;
        return greatestValue;
    }


    public override void CompTick()
    {
        base.CompTick();
        if (!ShowMeYourHandsMod.instance.Settings.ShowOtherTmes || LastDrawn >= GenTicks.TicksAbs - 1 ||
            GenTicks.TicksAbs == 0)
        {
            return;
        }

        DrawHands();
    }
}