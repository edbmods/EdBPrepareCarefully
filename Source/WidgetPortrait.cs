using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public static class WidgetPortrait {

        public static void Draw(Pawn pawn, Rect clipRect, Rect portraitRect, Rot4? orientation = null) {
            if (pawn == null) {
                return;
            }
            RenderTexture pawnTexture = PortraitsCache.Get(pawn, portraitRect.size, orientation ?? Rot4.South);
            try {
                GUI.BeginClip(clipRect);
                GUI.DrawTexture(portraitRect, (Texture)pawnTexture);
            }
            catch (Exception e) {
                Logger.Error("Failed to draw pawn", e);
            }
            finally {
                GUI.EndClip();
            }
        }
    }
}
