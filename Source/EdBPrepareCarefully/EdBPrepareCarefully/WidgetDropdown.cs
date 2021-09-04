﻿using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public static class WidgetDropdown {
        public static bool Button(Rect rect, string label) {
            return Button(rect, label, true, false, true);
        }
        public static bool Button(Rect rect, string label, bool drawBackground, bool doMouseoverSound, bool active) {
            TextAnchor anchor = Text.Anchor;
            Color color = GUI.color;
            if (drawBackground) {
                Texture2D atlas = Textures.TextureButtonBGAtlas;
                if (Mouse.IsOver(rect)) {
                    atlas = Textures.TextureButtonBGAtlasMouseover;
                    if (Input.GetMouseButton(0)) {
                        atlas = Textures.TextureButtonBGAtlasClick;
                    }
                }
                Widgets.DrawAtlas(rect, atlas);
                Rect indicator = new Rect(rect.xMax - 21, rect.MiddleY() - 4, 11, 8);
                GUI.DrawTexture(indicator, Textures.TextureDropdownIndicator);
            }
            if (doMouseoverSound) {
                MouseoverSounds.DoRegion(rect);
            }
            if (!drawBackground) {
                GUI.color = new Color(0.8f, 0.85f, 1f);
                if (Mouse.IsOver(rect)) {
                    GUI.color = Widgets.MouseoverOptionColor;
                }
            }
            if (drawBackground) {
                Text.Anchor = TextAnchor.MiddleCenter;
            }
            else {
                Text.Anchor = TextAnchor.MiddleLeft;
            }
            Rect textRect = new Rect(rect.x, rect.y, rect.width - 12, rect.height);
            Widgets.Label(textRect, label);
            Text.Anchor = anchor;
            GUI.color = color;
            return active && Widgets.ButtonInvisible(rect, false);
        }
    }
}
