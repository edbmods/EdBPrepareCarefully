using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public static class WidgetInfoButton {
        public static readonly Vector2 ButtonSize = new Vector2(24, 24);
        public static void Draw(Rect rect, Thing thing) {
            Color savedColor = GUI.color;
            try {
                Style.SetGUIColorForButton(rect);
                GUI.DrawTexture(rect, Textures.TextureButtonInfo);
                if (Widgets.ButtonInvisible(rect)) {
                    Find.WindowStack.Add((Window)new Dialog_InfoCard(thing));
                }
            }
            finally {
                GUI.color = savedColor;
            }
        }
        public static void Draw(Rect rect, ThingDef thingDef, ThingDef stuff = null) {
            Color savedColor = GUI.color;
            try {
                Style.SetGUIColorForButton(rect);
                GUI.DrawTexture(rect, Textures.TextureButtonInfo);
                if (Widgets.ButtonInvisible(rect)) {
                    if (stuff != null) {
                        Find.WindowStack.Add((Window)new Dialog_InfoCard(thingDef, stuff));
                    }
                    else {
                        Find.WindowStack.Add((Window)new Dialog_InfoCard(thingDef));
                    }
                }
            }
            finally {
                GUI.color = savedColor;
            }
        }
        public static void Draw(Rect rect, ThingDefCount thingDefCount) {
            Draw(rect, thingDefCount.ThingDef);
        }
    }
}
