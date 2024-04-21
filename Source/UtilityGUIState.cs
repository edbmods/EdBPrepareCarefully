using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public struct UtilityGUIState {
        public Color color;
        public GameFont font;
        public TextAnchor anchor;
        public bool wordWrap;

        public static UtilityGUIState Save() {
            return new UtilityGUIState() {
                color = GUI.color,
                font = Text.Font,
                anchor = Text.Anchor,
                wordWrap = Text.WordWrap
            };
        }

        public void Restore() {
            GUI.color = color;
            Text.Font = font;
            Text.Anchor = anchor;
            Text.WordWrap = wordWrap;
        }
    }
}
