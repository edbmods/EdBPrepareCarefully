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
    public abstract class PanelPawnListMinimized : PanelBase {
        public delegate void MaximizeHandler();

        public event MaximizeHandler Maximizing;

        protected Rect RectMaximize;
        protected Rect RectHeader;
        public ModState State { get; set; }
        public ViewState ViewState { get; set; }

        public override void Resize(Rect rect) {
            base.Resize(rect);

            float headerHeight = 36;
            Vector2 resizeButtonSize = new Vector2(18, 18);
            RectMaximize = new Rect(rect.width - 25, 9, resizeButtonSize.x, resizeButtonSize.y);
            RectHeader = new Rect(0, 0, rect.width, headerHeight);
        }

        protected override void DrawPanelContent() {

            var pawns = GetPawns();
            if (pawns == null) {
                Logger.Debug("pawns was null");
            }
            int pawnCount = GetPawns().Count();

            // Count label.
            Text.Font = GameFont.Medium;
            float headerWidth = Text.CalcSize(PanelHeader).x;
            Rect countRect = new Rect(10 + headerWidth + 3, 3, 50, 27);
            GUI.color = Style.ColorTextPanelHeader;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label(countRect, "EdB.PC.Panel.PawnList.PawnCount".Translate(pawnCount));
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            // Maximize button.
            if (RectHeader.Contains(Event.current.mousePosition)) {
                GUI.color = Style.ColorButtonHighlight;
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(RectMaximize, IsTopPanel() ? Textures.TextureMaximizeDown : Textures.TextureMaximizeUp);
            if (Widgets.ButtonInvisible(RectHeader, false)) {
                SoundDefOf.ThingSelected.PlayOneShotOnCamera();
                if (Maximizing != null) {
                    Maximizing();
                }
            }
            return;
        }

        protected abstract IEnumerable<CustomizedPawn> GetPawns();

        protected abstract bool IsTopPanel();

    }
}
