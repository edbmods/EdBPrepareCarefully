using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class WidgetColorSelector {
        public delegate void SelectColorHandler(Color color);
        public delegate void SelectColorDefHandler(ColorDef colorDef);

        public static Vector2 SwatchSize = new Vector2(16, 16);
        public static Vector2 SwatchSpacing = new Vector2(4, 4);
        public static Color SwatchBorderColor = Color.white;
        public static Color SelectedSwatchBorderColor = Color.white;
        public static Color SwatchOverlayColor = Color.white.ToTransparent(0.8f);

        public static float DrawSwatches(float x, float y, float width, float swatchSize, Color currentColor, List<Color> swatches, SelectColorHandler selectAction, CustomizedPawn pawn = null) {
            if (swatches.CountAllowNull() == 0) {
                return 0;
            }
            Rect rect = new Rect(x + 1, y + 1, swatchSize, swatchSize);
            foreach (Color color in swatches) {
                bool selected = color.IndistinguishableFrom(currentColor);
                if (selected) {
                    Rect selectionRect = new Rect(rect.x - 2, rect.y - 2, swatchSize + 4, swatchSize + 4);
                    GUI.color = SelectedSwatchBorderColor;
                    GUI.DrawTexture(selectionRect, BaseContent.WhiteTex);
                }

                Rect borderRect = new Rect(rect.x - 1, rect.y - 1, swatchSize + 2, swatchSize + 2);
                GUI.color = SwatchBorderColor;
                GUI.DrawTexture(borderRect, BaseContent.WhiteTex);

                GUI.color = color;
                GUI.DrawTexture(rect, BaseContent.WhiteTex);

                GUI.color = SwatchOverlayColor;
                Rect overlayRect = rect.InsetBy(1, 1);
                bool iconDisplayed = false;
                if (pawn?.Pawn?.story.favoriteColor != null) {
                    if (color.IndistinguishableFrom(pawn.Pawn.story.favoriteColor.color)) {
                        GUI.DrawTexture(overlayRect, Textures.TextureFavoriteColor);
                        TooltipHandler.TipRegion(overlayRect, "FavoriteColorPickerTip".Translate(pawn.Pawn.Named("PAWN")));
                        iconDisplayed = true;
                    }
                }
                if (!iconDisplayed && UtilityIdeo.IdeoEnabledForPawn(pawn) && pawn?.Pawn?.Ideo != null) {
                    if (color.IndistinguishableFrom(pawn.Pawn.Ideo.ApparelColor)) {
                        GUI.DrawTexture(overlayRect, Textures.TextureIdeoColor);
                        TooltipHandler.TipRegion(overlayRect, "IdeoColorPickerTip".Translate(pawn.Pawn.Named("PAWN")));
                    }
                }

                if (!selected) {
                    if (Widgets.ButtonInvisible(rect, false)) {
                        selectAction?.Invoke(color);
                    }
                }

                rect.x += swatchSize + SwatchSpacing.x;
                if (rect.xMax >= width) {
                    rect.y += swatchSize + SwatchSpacing.y;
                    rect.x = x + 1;
                }
            }

            GUI.color = Color.white;
            return rect.yMax - y;
        }
        public static float DrawColorDefSwatches(float x, float y, float width, float swatchSize, ColorDef currentColor, List<ColorDef> swatches, SelectColorDefHandler selectAction, CustomizedPawn pawn = null) {
            if (swatches.CountAllowNull() == 0) {
                return 0;
            }
            Rect rect = new Rect(x + 1, y + 1, swatchSize, swatchSize);
            foreach (ColorDef colorDef in swatches) {
                bool selected = currentColor == colorDef;
                if (selected) {
                    Rect selectionRect = new Rect(rect.x - 2, rect.y - 2, swatchSize + 4, swatchSize + 4);
                    GUI.color = SelectedSwatchBorderColor;
                    GUI.DrawTexture(selectionRect, BaseContent.WhiteTex);
                }

                Rect borderRect = new Rect(rect.x - 1, rect.y - 1, swatchSize + 2, swatchSize + 2);
                GUI.color = SwatchBorderColor;
                GUI.DrawTexture(borderRect, BaseContent.WhiteTex);

                GUI.color = colorDef.color;
                GUI.DrawTexture(rect, BaseContent.WhiteTex);

                GUI.color = SwatchOverlayColor;
                Rect overlayRect = rect.InsetBy(1, 1);
                bool iconDisplayed = false;
                if (pawn?.Pawn?.story.favoriteColor != null) {
                    if (colorDef == pawn.Pawn.story.favoriteColor) {
                        GUI.DrawTexture(overlayRect, Textures.TextureFavoriteColor);
                        TooltipHandler.TipRegion(overlayRect, "FavoriteColorPickerTip".Translate(pawn.Pawn.Named("PAWN")));
                        iconDisplayed = true;
                    }
                }
                if (!iconDisplayed && UtilityIdeo.IdeoEnabledForPawn(pawn) && pawn?.Pawn?.Ideo != null) {
                    if (colorDef == pawn.Pawn.Ideo.colorDef) {
                        GUI.DrawTexture(overlayRect, Textures.TextureIdeoColor);
                        TooltipHandler.TipRegion(overlayRect, "IdeoColorPickerTip".Translate(pawn.Pawn.Named("PAWN")));
                    }
                }

                if (!selected) {
                    if (Widgets.ButtonInvisible(rect, false)) {
                        selectAction?.Invoke(colorDef);
                    }
                }

                rect.x += swatchSize + SwatchSpacing.x;
                if (rect.xMax >= width) {
                    rect.y += swatchSize + SwatchSpacing.y;
                    rect.x = x + 1;
                }
            }

            GUI.color = Color.white;
            return rect.yMax - y;
        }

        public static void DrawSelector(Rect rect, Color currentColor, SelectColorHandler selectAction) {
            GUI.color = Color.white;

            Rect colorRect = new Rect(rect.x, rect.y, rect.height, rect.height);
            Rect slidersRect = new Rect(colorRect.xMax + 6, rect.y, rect.width - colorRect.width - 6, rect.height);
            float sliderHeight = 10;
            float spaceForSliderPadding = rect.height - sliderHeight * 3;
            float sliderPadding = spaceForSliderPadding / 2;
            Rect redRect = new Rect(slidersRect.x, slidersRect.y, slidersRect.width, 10);
            Rect greenRect = new Rect(slidersRect.x, redRect.yMax + sliderPadding, slidersRect.width, 10);
            Rect blueRect = new Rect(slidersRect.x, greenRect.yMax + sliderPadding, slidersRect.width, 10);

            GUI.color = SwatchBorderColor;
            GUI.DrawTexture(colorRect, BaseContent.WhiteTex);
            GUI.color = currentColor;
            GUI.DrawTexture(colorRect.ContractedBy(1), BaseContent.WhiteTex);

            GUI.color = Color.red;
            float originalR = currentColor.r;
            float originalG = currentColor.g;
            float originalB = currentColor.b;
            float r = GUI.HorizontalSlider(redRect, currentColor.r, 0, 1);
            GUI.color = Color.green;
            float g = GUI.HorizontalSlider(greenRect, currentColor.g, 0, 1);
            GUI.color = Color.blue;
            float b = GUI.HorizontalSlider(blueRect, currentColor.b, 0, 1);
            if (!CloseEnough(r, originalR) || !CloseEnough(g, originalG) || !CloseEnough(b, originalB)) {
                selectAction?.Invoke(new Color(r, g, b));
            }

            GUI.color = Color.white;
        }

        //public static float Draw(float x, float y, float width, float swatchSize, float selectorHeight, Color currentColor, List<Color> swatches, SelectColorHandler selectAction, CustomizedPawn pawn) {
        //    float top = y;
        //    if (swatches.CountAllowNull() > 0) {
        //        y += DrawSwatches(x, y, width, swatchSize, currentColor, swatches, selectAction, pawn);
        //    }
        //    y += 8f;
        //    DrawSelector(new Rect(x, y, width, selectorHeight), currentColor, selectAction);
        //    y += selectorHeight;
        //    return y - top;
        //}

        public static float Measure(float width, List<Color> swatches) {
            float height = 0;
            if (!swatches.NullOrEmpty()) {
                float swatchWidth = width + SwatchSpacing.x;
                float swatchesPerRow = swatchWidth / SwatchSize.x;
                float rows = Mathf.Ceil(swatches.Count / swatchesPerRow);
                float swatchesHeight = rows * (SwatchSize.y + SwatchSpacing.y);
                height += swatchesHeight;
            }
            return height + 50;
        }

        private static bool CloseEnough(float a, float b) {
            if (a > b - 0.0001f && a < b + 0.0001f) {
                return true;
            }
            else {
                return false;
            }
        }
    }
}
