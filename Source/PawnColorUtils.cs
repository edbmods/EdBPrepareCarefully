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
		public static Color[] Colors;
		// The values are copied from the PawnSkinColors class, but after being copied they will look something like this.
		//public static Color[] Colors = new Color[] {
		//	new Color(0.3882353f, 0.2745098f, 0.1411765f),
		//	new Color(0.509804f, 0.3568628f, 0.1882353f),
		//	new Color(0.8941177f, 0.6196079f, 0.3529412f),
		//	new Color(1f, 0.9372549f, 0.7411765f),
		//	new Color(1f, 0.9372549f, 0.8352941f),
		//	new Color(0.9490196f, 0.9294118f, 0.8784314f),
		//};

		// The same colors, but rounded to the 3 fractional digits.  When colors are saved to a preset, they are
		// automatically rounded, so we need to use these rounded values when loading them.  Could avoid this if
		// we didn't store the color directly in the preset, but used the same value that the game stores in save
		// files.  Should have done this from the beginning but will keeping using colors for now to keep preset
		// backwards compatibility.
		public static Color[] RoundedColors;

		// The values are copied from the PawnSkinColors class, but after being copied they will look something like this.
		public static float[] ColorValues; // = new float[] { 0f, 0.1f, 0.25f, 0.5f, 0.75f, 1f };

		// The values are copied from the PawnSkinColors class, but after being copied they will look something like this.
		public static float[] ColorSelectors; // = new float[] { 0f, 0.05f, 0.2f, 0.285f, 0.785f, 1f };

		// Populates color arrays from PawnSkinColors.SkinColors.  Uses reflection because the field that stores the
		// colors and the SkinColorData class are both private.
		public static void InitializeColors()
		{
			FieldInfo skinColorsField = typeof(PawnSkinColors).GetField("SkinColors", BindingFlags.Static | BindingFlags.NonPublic);
			Array skinColors = (Array)skinColorsField.GetValue(null);
			int length = skinColors.Length;
			Colors = new Color[length];
			RoundedColors = new Color[length];
			ColorValues = new float[length];
			ColorSelectors = new float[length];
			for (int i = 0; i < length; i++) {
				object colorData = skinColors.GetValue(i);
				FieldInfo whitenessField = colorData.GetType().GetField("whiteness", BindingFlags.Instance | BindingFlags.Public);
				FieldInfo selectorField = colorData.GetType().GetField("selector", BindingFlags.Instance | BindingFlags.Public);
				FieldInfo colorField = colorData.GetType().GetField("color", BindingFlags.Instance | BindingFlags.Public);
				Colors[i] = (Color)colorField.GetValue(colorData);
				RoundedColors[i] = (Color)colorField.GetValue(colorData);
				RoundedColors[i].r = (float)Math.Round(RoundedColors[i].r, 3);
				RoundedColors[i].g = (float)Math.Round(RoundedColors[i].g, 3);
				RoundedColors[i].b = (float)Math.Round(RoundedColors[i].b, 3);
				RoundedColors[i].a = (float)Math.Round(RoundedColors[i].a, 3);
				ColorValues[i] = (float)whitenessField.GetValue(colorData);
				ColorSelectors[i] = (float)selectorField.GetValue(colorData);
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

