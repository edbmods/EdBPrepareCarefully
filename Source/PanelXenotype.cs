using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class PanelXenotype : PanelModule {
        public static readonly Vector2 FieldPadding = new Vector2(6, 6);

        public Rect FieldRect;
        protected Field FieldXenotype = new Field();
        protected LabelTrimmer labelTrimmer = new LabelTrimmer();

        public override void Resize(float width) {
            base.Resize(width);
            FieldRect = new Rect(FieldPadding.x, 0, width - FieldPadding.x * 2, 30);
        }

        public float Measure() {
            return 0;
        }

        public override bool IsVisible(State state) {
            return ModsConfig.BiotechActive;
        }

        public override float Draw(State state, float y) {
            float top = y;
            y += Margin.y;

            y += DrawHeader(y, Width, "Xenotype".Translate().CapitalizeFirst().Resolve());

            CustomPawn pawn = state.CurrentPawn;
            Pawn_GeneTracker geneTracker = pawn.Pawn.genes;
            XenotypeDef xenotypeDef = geneTracker?.Xenotype;
            CustomXenotype customXenotype = geneTracker?.CustomXenotype;

            FieldXenotype.Rect = FieldRect.OffsetBy(0, y);
            labelTrimmer.Rect = FieldXenotype.Rect.InsetBy(8, 0);

            if (customXenotype != null) {
                FieldXenotype.Label = labelTrimmer.TrimLabelIfNeeded(customXenotype.name);
            }
            else if (xenotypeDef != null) {
                FieldXenotype.Label = labelTrimmer.TrimLabelIfNeeded(xenotypeDef.LabelCap);
            }
            FieldXenotype.Enabled = true;
            FieldXenotype.ClickAction = () => {
                Find.WindowStack.Add(new Dialog_ViewGenes(pawn.Pawn));
            };
            FieldXenotype.DrawIconFunc = (Rect rect) => {
                if (xenotypeDef != null) {
                    GUI.DrawTexture(rect, xenotypeDef.Icon);
                }
                else if (customXenotype != null) {
                    GUI.DrawTexture(rect, customXenotype.IconDef?.Icon);
                }
            };
            FieldXenotype.IconSizeFunc = () => new Vector2(24, 24);

            FieldXenotype.Draw();

            y += FieldRect.height;

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            y += Margin.y;

            return y - top;
        }

    }
}
