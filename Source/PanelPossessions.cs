using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;

namespace EdB.PrepareCarefully {
    public class PanelPossessions : PanelModule {
        public ModState State { get; set; }
        public ViewState ViewState { get; set; }

        public override void Resize(float width) {
            base.Resize(width);
        }

        public override float Draw(float y) {
            CustomizedPawn customizedPawn = ViewState.CurrentPawn;
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (customizations == null) {
                return y;
            }

            float top = y;
            y += Margin.y;
            y += DrawHeader(y, Width, "Possessions".Translate().Resolve());

            if (customizations.Possessions.Count > 0) {
                foreach (CustomizedPossession item in customizations.Possessions) {
                    y += DrawRow(y, Width, item);
                }
            }
            else {
                GUI.color = Style.ColorText;
                Rect rectText = new Rect(Margin.x, y, Width - Margin.x * 2, 20);
                Widgets.Label(rectText, "EdB.PC.Panel.Incapable.None".Translate());
                y += rectText.height;
                GUI.color = Color.white;
            }

            y += Margin.y;

            return y - top;
        }

        private float DrawRow(float y, float width, CustomizedPossession possession) {
            float top = y;
            Rect rowRect = new Rect(6f, y, width - 12f, 36f);
            Rect iconRect = new Rect(rowRect.x + 4f, y + 4, 28f, 28f);
            Rect labelRect = new Rect(iconRect.xMax + 8f, y, rowRect.width - iconRect.xMax - 8f, rowRect.height);
            Rect infoButtonRect = new Rect(rowRect.xMax - 30, rowRect.y + rowRect.HalfHeight() - 11, 24, 24);
            Widgets.DrawAtlas(rowRect, Textures.TextureFieldAtlas);
            //Widgets.InfoCardButton(rect.width - 24f, y, thing);
            //rect.width -= 24f;
            if (possession.ThingDef.DrawMatSingle != null && possession.ThingDef.DrawMatSingle.mainTexture != null) {
                Widgets.DefIcon(iconRect, possession.ThingDef);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            ThingDefCount thingDefCount = new ThingDefCount(possession.ThingDef, possession.Count);
            string text = thingDefCount.LabelCap;
            Text.WordWrap = false;
            Widgets.Label(labelRect, text.Truncate(labelRect.width));
            Text.WordWrap = true;
            if (rowRect.Contains(Event.current.mousePosition)) {
                WidgetInfoButton.Draw(infoButtonRect, thingDefCount);
            }
            y += 40f;
            return y - top;
            
        }
    }
}
