using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class PanelLoadSave : PanelBase {
        public delegate void SaveCharacterHandler(CustomPawn pawn, string name);

        public event SaveCharacterHandler CharacterSaved;

        protected Rect RectButtonSave;

        public PanelLoadSave() {
        }

        public override void Resize(Rect rect) {
            base.Resize(rect);

            float panelPadding = 12;
            float buttonWidth = PanelRect.width - panelPadding * 2;
            float buttonHeight = 38;
            float top = PanelRect.height * 0.5f - buttonHeight * 0.5f;
            RectButtonSave = new Rect(panelPadding, top, buttonWidth, buttonHeight);
        }

        protected override void DrawPanelContent(State state) {
            base.DrawPanelContent(state);
            CustomPawn currentPawn = state.CurrentPawn;
            if (Widgets.ButtonText(RectButtonSave, "EdB.PC.Panel.LoadSave.Save".Translate(), true, false, true)) {
                Find.WindowStack.Add(new Dialog_SaveColonist(state.CurrentPawn.Pawn.LabelShort,
                    (string file) => {
                        CharacterSaved(currentPawn, file);
                    }
                ));
            }
        }
    }
}
