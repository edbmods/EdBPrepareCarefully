using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
namespace EdB.PrepareCarefully {
    public static class WidgetEquipmentIcon {
        public static void Draw(Rect rect, EquipmentRecord entry) {
            Draw(rect, entry.def, entry.color);
        }
        public static void Draw(Rect rect, AnimalRecord animal) {
            Draw(rect, animal.Thing, Color.white);
        }
        public static void Draw(Rect rect, ThingDef thingDef, Color color) {
            rect = new Rect(rect.MiddleX() - 17, rect.MiddleY() - 17, 34, 34);
            GUI.color = color;
            // EdB: Inline copy of static Widgets.ThingIcon(Rect, ThingDef) with the selected
            // color based on the stuff.
            // EdB: Inline copy of static private method with modifications to keep scaled icons within the
            // bounds of the specified Rect and to draw them using the stuff color.
            //Widgets.ThingIconWorker(rect, thing.def, thingDef.uiIcon);
            float num = GenUI.IconDrawScale(thingDef);
            Rect resizedRect = rect;
            if (num != 1f) {
                // For items that are going to scale out of the bounds of the icon rect, we need to shrink
                // the bounds a little.
                if (num > 1) {
                    resizedRect = rect.ContractedBy(4);
                }
                resizedRect.width *= num;
                resizedRect.height *= num;
                resizedRect.center = rect.center;
            }
            GUI.DrawTexture(resizedRect, thingDef.uiIcon);
            GUI.color = Color.white;
        }

        public static void Draw(Rect rect, Thing thing, Color color) {
            rect = new Rect(rect.center.x - 17, rect.center.y - 17, 38, 38);
            GUI.color = color;
            // EdB: Inline copy of static Widgets.ThingIcon(Rect, Thing) with graphics switched to show a side view
            // instead of a front view.
            GUI.color = thing.DrawColor;
            Texture resolvedIcon;
            if (!thing.def.uiIconPath.NullOrEmpty()) {
                resolvedIcon = thing.def.uiIcon;
            }
            else if (thing is Pawn) {
                Pawn pawn = (Pawn)thing;
                if (!pawn.Drawer.renderer.graphics.AllResolved) {
                    pawn.Drawer.renderer.graphics.ResolveAllGraphics();
                }
                Material matSingle = pawn.Drawer.renderer.graphics.nakedGraphic.MatEast;
                resolvedIcon = matSingle.mainTexture;
                GUI.color = matSingle.color;
            }
            else {
                resolvedIcon = thing.Graphic.ExtractInnerGraphicFor(thing).MatEast.mainTexture;
            }
            // EdB: Inline copy of static private method.
            //Widgets.ThingIconWorker(rect, thing.def, resolvedIcon);
            float num = GenUI.IconDrawScale(thing.def);
            if (num != 1f) {
                Vector2 center = rect.center;
                rect.width *= num;
                rect.height *= num;
                rect.center = center;
            }
            GUI.DrawTexture(rect, resolvedIcon);
            GUI.color = Color.white;
        }
    }
}
