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
		public static readonly Color[] Colors = new Color[] {
			new Color(0.3882353f, 0.2745098f, 0.1411765f),
			new Color(0.509804f, 0.3568628f, 0.1882353f),
			new Color(0.8941177f, 0.6196079f, 0.3529412f),
			new Color(1f, 0.9372549f, 0.7411765f),
			new Color(1f, 0.9372549f, 0.8352941f),
			new Color(0.9490196f, 0.9294118f, 0.8784314f),
		};

		public static readonly float[] ColorValues = new float[] { 0f, 0.1f, 0.25f, 0.5f, 0.75f, 1f };

		public static readonly float[] ColorSelectors = new float[] { 0f, 0.05f, 0.2f, 0.285f, 0.785f, 1f };


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
			int leftIndex = GetColorLeftIndex(color);
			if (leftIndex == Colors.Length - 1) {
				return 1.0f;
			}

			int rightIndex = leftIndex + 1;
			float t = (color.b - Colors[leftIndex].b) / (Colors[rightIndex].b - Colors[leftIndex].b);

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
			int result = Colors.Length - 1;
			for (int i = 0; i < Colors.Length - 1; i++) {
				Color color1 = Colors[i];
				Color color2 = Colors[i + 1];
				if (color.r >= color1.r && color.r <= color2.r
				    && color.g >= color1.g && color.g <= color2.g
				    && color.b >= color1.b && color.b <= color2.b)
				{
					result = i;
					break;
				}
			}
			if (result == Colors.Length - 1) {
				result = Colors.Length - 2;
			}
			return result;
		}

		public static Color FindColor(int colorIndex, float lerpValue)
		{
			Color color1 = Colors[colorIndex];
			Color color2 = Colors[colorIndex + 1];
			return Color.Lerp(color1, color2, lerpValue);
		}
	}
}

