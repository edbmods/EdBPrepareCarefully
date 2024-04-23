using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public static class WidgetEquipmentField {

        public static float DrawSelectedEquipment(float x, float y, float width, Thing thing, Action labelClickAction = null, Action deleteAction = null) {
            float top = y;
            Rect rowRect = new Rect(x, y, width, 36f);
            Rect iconRect = new Rect(rowRect.x + 4f, rowRect.MiddleY() - 14f, 28f, 28f);
            float labelX = iconRect.xMax + 8;
            Rect labelRect = new Rect(labelX, y, rowRect.width - labelX, 24);
            Rect deleteRect = new Rect(rowRect.xMax - 18, rowRect.y + rowRect.HalfHeight() - 6, 12, 12);
            Rect infoButtonRect = new Rect(rowRect.xMax - 30, rowRect.y + rowRect.HalfHeight() - 11, 24, 24);

            var guiState = UtilityGUIState.Save();
            try {
                GUI.color = Color.white;

                Widgets.DrawAtlas(rowRect, Textures.TextureFieldAtlas);

                if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null) {
                    Widgets.ThingIcon(iconRect, thing);
                }
                Text.Anchor = TextAnchor.MiddleLeft;
                string text = thing.def.LabelCap;
                GUI.color = Style.ColorText;
                Text.WordWrap = false;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                if (labelClickAction != null && rowRect.Contains(Event.current.mousePosition)) {
                    if (deleteAction == null || !deleteRect.Contains(Event.current.mousePosition)) {
                        GUI.color = Style.ColorTextPanelHeader;
                    }
                }
                Widgets.Label(labelRect, text.Truncate(labelRect.width - 48));
                Rect subtitleRect = new Rect(labelX, labelRect.y + 17, rowRect.width - 36f, 16);
                bool hasQuality = thing.TryGetQuality(out QualityCategory quality);
                int percent = 100;
                if (thing.def.useHitPoints) {
                    int hitPoints = thing.HitPoints;
                    int maxHitPoints = thing.MaxHitPoints;
                    if (hitPoints < maxHitPoints) {
                        percent = (int)((float)hitPoints / (float)maxHitPoints * 100f);
                    }
                }
                if (thing.Stuff != null && hasQuality) {
                    Text.Font = GameFont.Tiny;
                    string subtitleText = thing.Stuff.LabelCap + ", " + quality.GetLabel();
                    if (percent != 100) {
                        subtitleText += " (" + percent + "%)";
                    }
                    Widgets.Label(subtitleRect, subtitleText.Truncate(subtitleRect.width - 48));
                }
                else if (thing.Stuff != null) {
                    Text.Font = GameFont.Tiny;
                    Widgets.Label(subtitleRect, thing.Stuff.LabelCap.Truncate(subtitleRect.width - 48));
                }
                else if (hasQuality) {
                    Text.Font = GameFont.Tiny;
                    string subtitleText = quality.GetLabel().CapitalizeFirst();
                    if (percent != 100) {
                        subtitleText += " (" + percent + "%)";
                    }
                    Widgets.Label(subtitleRect, subtitleText);
                }
                Text.WordWrap = true;

                if (deleteAction != null) {
                    if (deleteRect.Contains(Event.current.mousePosition)) {
                        GUI.color = Style.ColorButtonHighlight;
                    }
                    else {
                        GUI.color = Style.ColorButton;
                    }
                    GUI.DrawTexture(deleteRect, Textures.TextureButtonDelete);
                    if (Widgets.ButtonInvisible(deleteRect, false)) {
                        deleteAction?.Invoke();
                    }
                }
                if (deleteAction == null && rowRect.Contains(Event.current.mousePosition)) {
                    WidgetInfoButton.Draw(infoButtonRect, thing);
                }
                if (labelClickAction != null) {
                    if (Widgets.ButtonInvisible(rowRect, false)) {
                        labelClickAction?.Invoke();
                    }
                }
            }
            finally {
                guiState.Restore();
            }



            //if (Mouse.IsOver(rect)) {
            //    string text2 = thing.LabelNoParenthesisCap.AsTipTitle() + GenLabel.LabelExtras(thing, includeHp: true, includeQuality: true) + "\n\n" + thing.DescriptionDetailed;
            //    if (thing.def.useHitPoints) {
            //        text2 = text2 + "\n" + thing.HitPoints + " / " + thing.MaxHitPoints;
            //    }
            //    TooltipHandler.TipRegion(rect, text2);
            //}
            y += rowRect.height;
            return y - top;

        }

    }
}
