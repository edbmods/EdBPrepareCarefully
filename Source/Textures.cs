using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    [StaticConstructorOnStartup]
    public static class Textures {
        private static bool loaded = false;

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
        public static Texture2D TextureButtonEdit;
        public static Texture2D TextureButtonGenderFemale;
        public static Texture2D TextureButtonGenderMale;
        public static Texture2D TextureButtonReset;
        public static Texture2D TextureButtonClearSkills;
        public static Texture2D TextureDropdownIndicator;
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
        public static Texture2D TextureButtonBGAtlas;
        public static Texture2D TextureButtonBGAtlasMouseover;
        public static Texture2D TextureButtonBGAtlasClick;
        public static Texture2D TextureArrowLeft;
        public static Texture2D TextureArrowRight;
        public static Texture2D TextureArrowDown;
        public static Texture2D TextureGenderFemaleLarge;
        public static Texture2D TextureGenderMaleLarge;
        public static Texture2D TextureGenderlessLarge;
        public static Texture2D TextureCheckbox;
        public static Texture2D TextureCheckboxSelected;
        public static Texture2D TextureCheckboxPartiallySelected;
        public static Texture2D TextureDottedLine;
        public static Texture2D TextureMaximizeUp;
        public static Texture2D TextureMaximizeDown;
        public static Texture2D TextureButtonWorldPawn;
        public static Texture2D TextureButtonColonyPawn;
        public static Texture2D TextureFilterAtlas1;
        public static Texture2D TextureFilterAtlas2;
        public static Texture2D TextureButtonCloseSmall;
        public static Texture2D TextureBaby;
        public static Texture2D TextureChild;
        public static Texture2D TextureAdult;
        public static Texture2D TextureButtonManage;
        public static Texture2D TextureButtonRotateView;
        public static Texture2D TextureButtonHatHidden;
        public static Texture2D TextureButtonHatVisible;
        public static Texture2D TextureFieldAtlasWhite;
        public static Texture2D TextureIconWarning;
        public static Texture2D TextureFavoriteColor;
        public static Texture2D TextureIdeoColor;
        public static Texture2D TextureCheckmark;
        public static Texture2D TextureCheckmarkForcedSelection;

        public static Texture2D TextureWhite {
            get {
                return BaseContent.WhiteTex;
            }
        }
        public static Texture2D TextureButtonInfo {
            get {
                return TexButton.Info;
            }
        }

        static Textures() {
            LoadTextures();
        }

        public static bool Loaded {
            get {
                return loaded;
            }
        }

        public static void Reset() {
            LongEventHandler.ExecuteWhenFinished(() => {
                LoadTextures();
            });
        }

        private static void LoadTextures() {
            loaded = false;
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
            TextureButtonClearSkills = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonClear", true);
            TextureButtonCloseSmall = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonCloseSmall", true);
            TextureButtonDelete = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonDelete", true);
            TextureButtonDeleteTab = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonDeleteTab", true);
            TextureButtonDeleteTabHighlight = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonDeleteTabHighlight", true);
            TextureButtonEdit = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonEdit", true);
            TextureButtonGenderFemale = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonGenderFemale", true);
            TextureButtonGenderMale = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonGenderMale", true);
            TextureButtonReset = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonReset", true);
            TextureDropdownIndicator = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/DropdownIndicator", true);
            TextureAlert = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/Alert", true);
            TextureAlertSmall = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/AlertSmall", true);
            TextureDerivedRelationship = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/DerivedRelationship", true);
            TextureButtonAdd = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonAdd", true);
            TextureDeleteX = ContentFinder<Texture2D>.Get("UI/Buttons/Delete", true);
            TextureSortAscending = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/SortAscending", true);
            TextureSortDescending = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/SortDescending", true);
            TextureArrowLeft = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ArrowLeft", true);
            TextureArrowRight = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ArrowRight", true);
            TextureArrowDown = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ArrowDown", true);
            TextureGenderFemaleLarge = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/GenderFemaleLarge", true);
            TextureGenderMaleLarge = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/GenderMaleLarge", true);
            TextureGenderlessLarge = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/GenderlessLarge", true);
            TextureCheckbox = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/Checkbox", true);
            TextureCheckboxSelected = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/CheckboxSelected", true);
            TextureCheckboxPartiallySelected = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/CheckboxPartiallySelected", true);
            TextureDottedLine = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/DottedLine", true);
            TextureMaximizeUp = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/MaximizeUp", true);
            TextureMaximizeDown = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/MaximizeDown", true);
            TextureButtonWorldPawn = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonWorldPawn", true);
            TextureButtonColonyPawn = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonColonyPawn", true);
            TextureFilterAtlas1 = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/FilterAtlas1", true);
            TextureFilterAtlas2 = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/FilterAtlas2", true);
            TextureButtonManage = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonManage", false);
            TextureButtonRotateView = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonRotateView", false);
            TextureButtonHatHidden = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonHatHidden", false);
            TextureButtonHatVisible = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/ButtonHatVisible", false);
            TextureIconWarning = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/IconWarning", false);

            TextureTabAtlas = ContentFinder<Texture2D>.Get("UI/Widgets/TabAtlas", true);

            TextureButtonBGAtlas = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBG", true);
            TextureButtonBGAtlasMouseover = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGMouseover", true);
            TextureButtonBGAtlasClick = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGClick", true);

            TextureAlternateRow = SolidColorMaterials.NewSolidColorTexture(new Color(1, 1, 1, 0.05f));
            TextureSkillBarFill = SolidColorMaterials.NewSolidColorTexture(new Color(1f, 1f, 1f, 0.1f));

            TextureBaby = ContentFinder<Texture2D>.Get("UI/Icons/DevelopmentalStages/Baby", false);
            TextureChild = ContentFinder<Texture2D>.Get("UI/Icons/DevelopmentalStages/Child", false);
            TextureAdult = ContentFinder<Texture2D>.Get("UI/Icons/DevelopmentalStages/Adult", false);

            TextureFieldAtlasWhite = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/FieldAtlasWhite", true);

            TextureFavoriteColor = ContentFinder<Texture2D>.Get("UI/Icons/ColorSelector/ColorFavourite", true);
            TextureIdeoColor = ContentFinder<Texture2D>.Get("UI/Icons/ColorSelector/ColorIdeology", true);

            TextureCheckmark = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/Checkmark", true);
            TextureCheckmarkForcedSelection = ContentFinder<Texture2D>.Get("EdB/PrepareCarefully/CheckmarkForcedSelection", true);

            loaded = true;
        }
    }
}

