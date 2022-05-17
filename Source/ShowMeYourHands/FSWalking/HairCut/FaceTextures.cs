using System.Collections.Generic;
using System.Linq;
using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;
using ShowMeYourHands;
using UnityEngine;
using Verse;
using static System.Byte;

namespace FacialStuff.GraphicsFS
{
    [StaticConstructorOnStartup]
    public static class FaceTextures
    {
        public static readonly Texture2D BlankTexture;

        public static readonly Texture2D MaskTexFullheadFrontBack;
        public static readonly Texture2D MaskTexFullheadFrontBack256;
        public static readonly Texture2D MaskTexFullheadFrontBack512;
        public static readonly Texture2D MaskTexFullheadSide;
        public static readonly Texture2D MaskTexFullheadSide256;
        public static readonly Texture2D MaskTexFullheadSide512;
        public static readonly Texture2D MaskTexUpperheadSide;
        public static readonly Texture2D MaskTexUpperheadSide256;
        public static readonly Texture2D MaskTexUpperheadSide512;
        public static readonly Texture2D MaskTexUppherheadFrontBack;
        public static readonly Texture2D MaskTexUppherheadFrontBack256;
        public static readonly Texture2D MaskTexUppherheadFrontBack512;
        public static readonly Texture2D RedTexture;
        public static readonly Color SkinRottingMultiplyColor = new(0.35f, 0.38f, 0.3f);

        /*
                private static Texture2D _maskTexAverageSide;
        */

        public static Color32 AverageColorFromTexture(Texture2D texture)
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
            Texture2D tex = new(texture.width, texture.height);
            tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            return AverageColorFromColors(tex.GetPixels32());
        }
        private static Color32 AverageColorFromColors(Color32[] colors)
        {
            Dictionary<Color32, int> shadeDictionary = new();
            foreach (Color32 texColor in colors)
            {
                if (texColor.a < 50)
                {
                    // Ignore low transparency
                    continue;
                }

                Rgb currentRgb = new() { B = texColor.b, G = texColor.b, R = texColor.r };

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


        public static bool IsWeaponLong(ThingDef weapon, out Vector3 mainHand, out Vector3 secHand)
        {
            Texture texture = weapon.graphicData.Graphic.MatSingle.mainTexture;

            // This is not allowed
            //var icon = (Texture2D) texture;

            // This is
            RenderTexture renderTexture = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, renderTexture);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Texture2D icon = new(texture.width, texture.height);
            icon.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            icon.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);


            Color32[] pixels = icon.GetPixels32();
            int width = icon.width;
            int startPixel = width;
            int endPixel = 0;

            for (int i = 0; i < icon.height; i++)
            {
                for (int j = 0; j < startPixel; j++)
                {
                    if (pixels[j + (i * width)].a < 5)
                    {
                        continue;
                    }

                    startPixel = j;
                    break;
                }

                for (int j = width - 1; j >= endPixel; j--)
                {
                    if (pixels[j + (i * width)].a < 5)
                    {
                        continue;
                    }

                    endPixel = j;
                    break;
                }
            }


            float percentWidth = (endPixel - startPixel) / (float)width;
            float percentStart = 0f;
            if (startPixel != 0)
            {
                percentStart = startPixel / (float)width;
            }

            float percentEnd = 0f;
            if (width - endPixel != 0)
            {
                percentEnd = (width - endPixel) / (float)width;
            }

            ShowMeYourHandsMain.LogMessage(
                $"{weapon.defName}: start {startPixel.ToString()}, percentstart {percentStart}, end {endPixel.ToString()}, percentend {percentEnd}, width {width}, percent {percentWidth}");

            if (percentWidth > 0.7f)
            {
                mainHand = new Vector3(-0.3f + percentStart, 0.3f, -0.05f);
                secHand = new Vector3(0.2f, -0.100f, -0.05f);
            }
            else
            {
                mainHand = new Vector3(-0.3f + percentStart, 0.3f, 0f);
                secHand = Vector3.zero;
            }

            return percentWidth > 0.7f;
        }


        static FaceTextures()
        {
            MaskTexUppherheadFrontBack = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Upperhead_south"));

            MaskTexUpperheadSide = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Upperhead_east"));

            MaskTexFullheadFrontBack = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Fullhead_south"));

            MaskTexFullheadSide = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Fullhead_east"));

            MaskTexUppherheadFrontBack256 = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Upperhead_256_south"));

            MaskTexUpperheadSide256 = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Upperhead_256_east"));

            MaskTexFullheadFrontBack256 = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Fullhead_256_south"));

            MaskTexFullheadSide256 = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Fullhead_256_east"));
         
            MaskTexUppherheadFrontBack512 = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Upperhead_512_south"));

            MaskTexUpperheadSide512 = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Upperhead_512_east"));

            MaskTexFullheadFrontBack512 = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Fullhead_512_south"));

            MaskTexFullheadSide512 = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Fullhead_512_east"));

            BlankTexture = new Texture2D(128, 128, TextureFormat.ARGB32, false);

            // The RedTexture is used as a mask texture, in case hair/eyes have no mask on their own
            RedTexture = new Texture2D(128, 128, TextureFormat.ARGB32, false);

            for (int x = 0; x < BlankTexture.width; x++)
            {
                for (int y = 0; y < BlankTexture.height; y++)
                {
                    BlankTexture.SetPixel(x, y, Color.clear);
                }
            }
            for (int x = 0; x < RedTexture.width; x++)
            {
                for (int y = 0; y < RedTexture.height; y++)
                {
                    RedTexture.SetPixel(x, y, Color.red);
                }
            }

            BlankTexture.name = "Blank";
            RedTexture.name = "Red";

            BlankTexture.Compress(false);
            BlankTexture.Apply(false, true);

            RedTexture.Compress(false);
            RedTexture.Apply(false, true);
        }

        public static Texture2D MakeReadable(Texture2D texture)
        {
            RenderTexture previous = RenderTexture.active;

            // Create a temporary RenderTexture of the same size as the texture
            RenderTexture tmp = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(texture, tmp);

            // Set the current RenderTexture to the temporary one we created
            RenderTexture.active = tmp;

            // Create a new readable Texture2D to copy the pixels to it
            Texture2D myTexture2D = new(texture.width, texture.width, TextureFormat.ARGB32, false);

            // Copy the pixels from the RenderTexture to the new Texture
            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            myTexture2D.Apply();

            // Reset the active RenderTexture
            RenderTexture.active = previous;

            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(tmp);

            return myTexture2D;

            // "myTexture2D" now has the same pixels from "texture" and it's readable.
        }
    }
}