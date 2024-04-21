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

        public static Vector2 SwatchSize = new Vector2(16, 16);
        public static Vector2 SwatchSpacing = new Vector2(4, 4);
        public static Color SwatchBorderColor = Color.white;
        public static Color SelectedSwatchBorderColor = Color.white;

        public static float DrawSwatches(float x, float y, float width, float swatchSize, Color currentColor, List<Color> swatches, SelectColorHandler selectAction) {
            Rect rect = new Rect(x + 1, y + 1, swatchSize, swatchSize);
            if (swatches != null && swatches.Count > 0) {
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

        public static float Draw(float x, float y, float width, Color currentColor, List<Color> swatches, SelectColorHandler selectAction) {
            Rect rect = new Rect(x, y, SwatchSize.x, SwatchSize.y);
            if (swatches != null && swatches.Count > 0) {
                foreach (Color color in swatches) {
                    bool selected = (color == currentColor);
                    if (selected) {
                        Rect selectionRect = new Rect(rect.x - 2, rect.y - 2, SwatchSize.x + 4, SwatchSize.y + 4);
                        GUI.color = SelectedSwatchBorderColor;
                        GUI.DrawTexture(selectionRect, BaseContent.WhiteTex);
                    }

                    Rect borderRect = new Rect(rect.x - 1, rect.y - 1, SwatchSize.x + 2, SwatchSize.y + 2);
                    GUI.color = SwatchBorderColor;
                    GUI.DrawTexture(borderRect, BaseContent.WhiteTex);

                    GUI.color = color;
                    GUI.DrawTexture(rect, BaseContent.WhiteTex);

                    if (!selected) {
                        if (Widgets.ButtonInvisible(rect, false)) {
                            selectAction?.Invoke(color);
                        }
                    }

                    rect.x += SwatchSize.x + SwatchSpacing.x;
                    if (rect.x >= width - (SwatchSize.x + SwatchSpacing.x)) {
                        rect.y += SwatchSize.y + SwatchSpacing.y;
                        rect.x = x;
                    }
                }
            }

            GUI.color = Color.white;

            if (rect.x != x) {
                rect.x = x;
                rect.y += SwatchSize.y + SwatchSpacing.y;
            }
            rect.y += 4;
            rect.width = 49;
            rect.height = 49;
            GUI.color = SwatchBorderColor;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = currentColor;
            GUI.DrawTexture(rect.ContractedBy(1), BaseContent.WhiteTex);

            GUI.color = Color.red;
            float originalR = currentColor.r;
            float originalG = currentColor.g;
            float originalB = currentColor.b;
            float r = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y - 1, 136, 16), currentColor.r, 0, 1);
            GUI.color = Color.green;
            float g = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y + 19, 136, 16), currentColor.g, 0, 1);
            GUI.color = Color.blue;
            float b = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y + 39, 136, 16), currentColor.b, 0, 1);
            if (!CloseEnough(r, originalR) || !CloseEnough(g, originalG) || !CloseEnough(b, originalB)) {
                selectAction?.Invoke(new Color(r, g, b));
            }

            GUI.color = Color.white;

            return rect.yMax - y;
        }

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
