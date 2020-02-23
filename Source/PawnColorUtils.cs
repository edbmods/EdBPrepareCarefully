using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class PawnColorUtils {
        // The values will match the color values in the PawnSkinColors class.  We use a round-about way of getting at
        // those color values in the InitializeColors() method.
        public static Color[] Colors;

        // The same colors, but rounded to 3 fractional digits.  When colors are saved to a preset, they are
        // automatically rounded, so we need to use these rounded values when loading them.  We could avoid this if
        // we didn't store the color directly in the preset, and instead used the same value that the game stores in
        // save files.  Since we didn't do this from the beginning, we'll keeping using colors for now to keep presets
        // backwards-compatibile.
        public static Color[] RoundedColors;

        // The values will match the color values in the PawnSkinColors class.
        public static float[] ColorValues;

        // Populates color arrays from PawnSkinColors.SkinColors.  Uses an indirect method of getting the color values
        // instead of just copying the color list via reflection.  This will make it work better with mods that
        // detour the color methods in that class--as long as they detour the GetSkinDataIndexOfMelanin() method.
        public static void InitializeColors() {
            List<float> values = new List<float>();

            // Iterate all values from 0.0f to 1.0f, using increments of 0.01f, to get the left index for each value.
            // Use this technique to construct a list of all of the indexes and their values.  Once we have the list
            // of indexes and their values, we can use the GetSkinColor() method to get the actual colors.
            int currentIndex = 0;
            values.Add(0.0f);
            float f = 0.01f;
            int counter = 1;
            while (f < 1.0f) {
                int result = Reflection.PawnSkinColors.GetSkinDataIndexOfMelanin(f);
                if (result != currentIndex) {
                    currentIndex = result;
                    values.Add(f);
                }
                counter++;
                double d = (double)counter / 100.0;
                f = (float)d;
            }
            values.Add(1.0f);

            // Allocate the arrays and fill them with the correct values.
            int length = values.Count;
            Colors = new Color[length];
            ColorValues = new float[length];
            RoundedColors = new Color[length];
            for (int i = 0; i < length; i++) {
                float v = values[i];
                Color color = PawnSkinColors.GetSkinColor(v);
                Colors[i] = color;
                RoundedColors[i] = color;
                RoundedColors[i].r = (float)Math.Round(color.r, 3);
                RoundedColors[i].g = (float)Math.Round(color.g, 3);
                RoundedColors[i].b = (float)Math.Round(color.b, 3);
                RoundedColors[i].a = (float)Math.Round(color.a, 3);
                ColorValues[i] = v;
                //Log.Message("Color added: (" + color.r + ", " + color.g + ", " + color.b + ")");
            }
        }

        public static float GetRelativeLerpValue(float value) {
            int leftIndex = GetLeftIndexForValue(value);
            if (leftIndex == Colors.Length - 1) {
                return 0.0f;
            }

            int rightIndex = leftIndex + 1;
            float t = Mathf.InverseLerp(ColorValues[leftIndex], ColorValues[rightIndex], value);
            return t;
        }

        public static float GetValueFromRelativeLerp(int leftIndex, float lerp) {
            if (leftIndex >= Colors.Length - 1) {
                return 1.0f;
            }
            else if (leftIndex < 0) {
                return 0.0f;
            }

            int rightIndex = leftIndex + 1;
            float result = Mathf.Lerp(ColorValues[leftIndex], ColorValues[rightIndex], lerp);

            return result;
        }

        public static int GetLeftIndexForValue(float value) {
            int result = 0;
            for (int i = 0; i < Colors.Length; i++) {
                if (value < ColorValues[i]) {
                    break;
                }
                result = i;
            }
            return result;
        }

        private static bool Between(Color color, Color a, Color b) {
            if (color.r >= a.r && color.r <= b.r || color.r <= a.r && color.r >= b.r) {
                if (color.g >= a.g && color.g <= b.g || color.g <= a.g && color.g >= b.g) {
                    if (color.b >= a.b && color.b <= b.b || color.b <= a.b && color.b >= b.b) {
                        return true;
                    }
                }
            }
            return false;
        }

        public static float FindMelaninValueFromColor(Color color) {
            int colorCount = Colors.Length;
            for (int i = 0; i < colorCount - 1; i++) {
                Color a = RoundedColors[i];
                Color b = RoundedColors[i + 1];
                bool between = Between(color, a, b);
                if (!between) {
                    continue;
                }
                Color c = a;
                Color d = b;
                bool ignorer = false;
                bool ignoreg = false;
                bool ignoreb = false;
                if (c.r == d.r) {
                    ignorer = true;
                }
                if (c.g == d.g) {
                    ignoreg = true;
                }
                if (c.b == d.b) {
                    ignoreb = true;
                }
                float tr = (color.r - c.r) / (d.r - c.r);
                float tg = (color.g - c.g) / (d.g - c.g);
                float tb = (color.b - c.b) / (d.b - c.b);
                bool invalid = false;
                float count = 0.0f;
                float total = 0.0f;
                if (!ignorer) {
                    if (tr < 0.0f && tr > 1.0f) {
                        invalid = true;
                    }
                    else {
                        count += 1.0f;
                        total += tr;
                    }
                }
                if (!ignoreg) {
                    if (tg < 0.0f && tg > 1.0f) {
                        invalid = true;
                    }
                    else {
                        count += 1.0f;
                        total += tg;
                    }
                }
                if (!ignoreb) {
                    if (tb < 0.0f && tb > 1.0f) {
                        invalid = true;
                    }
                    else {
                        count += 1.0f;
                        total += tb;
                    }
                }
                if (invalid) {
                    continue;
                }
                float t = count > 0 ? total / count : 0;
                float result = GetValueFromRelativeLerp(i, t);

                return result;
            }
            Log.Warning("Prepare Carefully could not find a valid matching value for the saved Color");
            return 0.5f;
        }
    }
}

