using System;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	public static class ColorValidator
	{
		public static Color ColorEmpty = new Color(-1f, -1f, -1f, -1f);

		public static bool Validate(ColorGenerator generator, Color color) {
			if (typeof(ColorGenerator_Options).Equals(generator.GetType())) {
				return Validate((ColorGenerator_Options)generator, color);
			}
			else if (typeof(ColorGenerator_Single).Equals(generator.GetType())) {
				return Validate((ColorGenerator_Single)generator, color);
			}
			else if (typeof(ColorGenerator_StandardApparel).Equals(generator.GetType())) {
				return Validate((ColorGenerator_StandardApparel)generator, color);
			}
			else if (typeof(ColorGenerator_White).Equals(generator.GetType())) {
				return Validate((ColorGenerator_White)generator, color);
			}
			else {
				return true;
			}
		}

		public static bool Validate(ColorGenerator_Single generator, Color color) {
			return color == generator.color;
		}

		public static bool Validate(ColorGenerator_Options generator, Color color) {
			foreach (ColorOption allowedColor in generator.options) {
				if (allowedColor.only != ColorEmpty) {
					if (color == allowedColor.only) {
						return true;
					}
				}
				else if (color.r >= allowedColor.min.r && color.g >= allowedColor.min.g && color.b >= allowedColor.min.b && color.a >= allowedColor.min.a
					&& color.r <= allowedColor.max.r && color.g <= allowedColor.max.g && color.b <= allowedColor.max.b && color.a <= allowedColor.max.a)
				{
					return true;
				}
			}
			return false;
		}

		public static bool Validate(ColorGenerator_StandardApparel generator, Color color) {
			return true;
		}

		public static bool Validate(ColorGenerator_White generator, Color color) {
			return (color == Color.white);
		}
	}
}

