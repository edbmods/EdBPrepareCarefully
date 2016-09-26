using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	public class PawnColorUtils
	{
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
		// detour the color methods in that class--as long as they detour the GetSkinDataLeftIndexByWhiteness() method.
		public static void InitializeColors()
		{
			List<float> values = new List<float>();

			// Get the private GetSkinDataLeftIndexByWhiteness() method from the PawnSkinColors class.
			MethodInfo getSkinDataLeftIndexByWhitenessMethod = typeof(PawnSkinColors)
					.GetMethod("GetSkinDataLeftIndexByWhiteness",
				    BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(float) }, null);

			// Iterate all values from 0.0f to 1.0f, using increments of 0.01f, to get the left index for each value.
			// Use this technique to construct a list of all of the indexes and their values.  Once we have the list
			// of indexes and their values, we can use the GetSkinColor() method to get the actual colors.
			int currentIndex = 0;
			values.Add(0.0f);
			float f = 0.01f;
			int counter = 1;
			while (f < 1.0f) {
				int result = (int) getSkinDataLeftIndexByWhitenessMethod.Invoke(null, new object[] { f });
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
			}
		}

		public static Color GetSkinColor(float value)
		{
			int leftIndex = GetColorLeftIndex(value);
			if (leftIndex == Colors.Length - 1) {
				return Colors[leftIndex];
			}
			float t = Mathf.InverseLerp(ColorValues[leftIndex], ColorValues[leftIndex + 1], value);
			return Color.Lerp(Colors[leftIndex], Colors[leftIndex + 1], t);
		}

		public static float GetSkinValue(Color color)
		{
			return GetSkinValue(color, Colors);
		}

		private static float GetRoundedSkinValue(Color color)
		{
			return GetSkinValue(color, RoundedColors);
		}

		private static float GetSkinValue(Color color, Color[] colors)
		{
			int leftIndex = GetColorLeftIndex(color, colors);
			if (leftIndex == colors.Length - 1) {
				return 1.0f;
			}

			int rightIndex = leftIndex + 1;
			float t = (color.b - colors[leftIndex].b) / (colors[rightIndex].b - colors[leftIndex].b);

			float value = Mathf.Lerp(ColorValues[leftIndex], ColorValues[rightIndex], t);
			return value;
		}

		public static float GetSkinLerpValue(Color color)
		{
			int leftIndex = GetColorLeftIndex(color);
			if (leftIndex == Colors.Length - 1) {
				return 0.0f;
			}

			int rightIndex = leftIndex + 1;
			float t = (color.b - Colors[leftIndex].b) / (Colors[rightIndex].b - Colors[leftIndex].b);

			return t;
		}

		public static int GetColorLeftIndex(float value)
		{
			int result = 0;
			for (int i = 0; i < Colors.Length; i++) {
				if (value < ColorValues[i]) {
					break;
				}
				result = i;
			}
			return result;
		}

		public static int GetColorLeftIndex(Color color)
		{
			return GetColorLeftIndex(color, Colors);
		}

		public static int GetRoundedColorLeftIndex(Color color)
		{
			return GetColorLeftIndex(color, RoundedColors);
		}

		private static int GetColorLeftIndex(Color color, Color[] colors)
		{
			int result = colors.Length - 1;
			for (int i = 0; i < colors.Length - 1; i++) {
				Color color1 = colors[i];
				Color color2 = colors[i + 1];
				if (color.r >= color1.r && color.r <= color2.r
				    && color.g >= color1.g && color.g <= color2.g
				    && color.b >= color1.b && color.b <= color2.b)
				{
					result = i;
					break;
				}
			}
			if (result == colors.Length - 1) {
				result = colors.Length - 2;
			}
			return result;
		}

		public static Color FindColor(int colorIndex, float lerpValue)
		{
			Color color1 = Colors[colorIndex];
			Color color2 = Colors[colorIndex + 1];
			return Color.Lerp(color1, color2, lerpValue);
		}

		public static Color FromRoundedColor(Color color)
		{
			float value = GetRoundedSkinValue(color);
			Color result = GetSkinColor(value);
			return result;
		}
	}
}

