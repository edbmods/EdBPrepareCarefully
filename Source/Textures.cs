using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	[StaticConstructorOnStartup]
	public static class Textures
	{
		public static Texture2D TexturePassionMajor;
		public static Texture2D TexturePassionMinor;
		public static Texture2D TextureFieldAtlas;
		public static Texture2D TexturePortraitBackground;
		public static Texture2D TextureButtonPrevious;
		public static Texture2D TextureButtonNext;
		public static Texture2D TextureButtonRandom;
		public static Texture2D TextureButtonRandomLarge;
		public static Texture2D TexturePassionNone;
		public static Texture2D TextureButtonDelete;
		public static Texture2D TextureButtonDeleteTab;
		public static Texture2D TextureButtonDeleteTabHighlight;
		public static Texture2D TextureButtonReset;
		public static Texture2D TextureButtonClearSkills;
		public static Texture2D TextureAlert;
		public static Texture2D TextureAlertSmall;
		public static Texture2D TextureDerivedRelationship;
		public static Texture2D TextureButtonAdd;
		public static Texture2D TextureRadioButtonOff;
		public static Texture2D TextureDeleteX;
		public static Texture2D TextureAlternateRow;
		public static Texture2D TextureSkillBarFill;
		public static Texture2D TextureSortAscending;
		public static Texture2D TextureSortDescending;
		public static Texture2D TextureTabAtlas;

		static Textures() {
			LoadTextures();
		}

		public static void Reset()
		{
			LongEventHandler.ExecuteWhenFinished(() => {
				LoadTextures();
			});
		}

		private static void LoadTextures()
		{
			TexturePassionMajor = ContentFinder<Texture2D>.Get("UI/Icons/PassionMajor", true);
			TexturePassionMinor = ContentFinder<Texture2D>.Get("UI/Icons/PassionMinor", true);
			TextureRadioButtonOff = ContentFinder<Texture2D>.Get("UI/Widgets/RadioButOff", true);
			TexturePortraitBackground = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/CharMakerPortraitBG", true);
			TextureFieldAtlas = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/FieldAtlas", true);
			TextureButtonPrevious = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonPrevious", true);
			TextureButtonNext = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonNext", true);
			TextureButtonRandom = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonRandom", true);
			TextureButtonRandomLarge = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonRandomLarge", true);
			TexturePassionNone = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/NoPassion", true);
			TextureButtonDelete = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonDelete", true);
			TextureButtonDeleteTab = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonDeleteTab", true);
			TextureButtonDeleteTabHighlight = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonDeleteTabHighlight", true);
			TextureButtonReset = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonReset", true);
			TextureButtonClearSkills = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonClear", true);
			TextureAlert = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/Alert", true);
			TextureAlertSmall = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/AlertSmall", true);
			TextureDerivedRelationship = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/DerivedRelationship", true);
			TextureButtonAdd = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonAdd", true);
			TextureDeleteX = ContentFinder<Texture2D>.Get("UI/Buttons/Delete", true);
			TextureSortAscending = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/SortAscending", true);
			TextureSortDescending = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/SortDescending", true);
			TextureTabAtlas = ContentFinder<Texture2D>.Get("UI/Widgets/TabAtlas", true);

			TextureAlternateRow = SolidColorMaterials.NewSolidColorTexture(new Color(1, 1, 1, 0.05f));
			TextureSkillBarFill = SolidColorMaterials.NewSolidColorTexture(new Color(1f, 1f, 1f, 0.1f));


		}
	}
}

