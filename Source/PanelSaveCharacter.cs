using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class PanelSaveCharacter : PanelBase {
        public delegate void SaveCharacterHandler(string name);

        public event SaveCharacterHandler CharacterSaved;

        protected Rect RectButtonSave;
        public ViewState ViewState { get; set; }

        public PanelSaveCharacter() {
        }

        public override void Resize(Rect rect) {
            base.Resize(rect);

            float panelPadding = 12;
            float buttonWidth = PanelRect.width - panelPadding * 2;
            float buttonHeight = 38;
            float top = PanelRect.height * 0.5f - buttonHeight * 0.5f;
            RectButtonSave = new Rect(panelPadding, top, buttonWidth, buttonHeight);
        }

        protected override void DrawPanelContent() {
            base.DrawPanelContent();
            CustomizedPawn currentPawn = ViewState.CurrentPawn;
            if (Widgets.ButtonText(RectButtonSave, "EdB.PC.Panel.LoadSave.Save".Translate(), true, false, true)) {
                Find.WindowStack.Add(new DialogSaveColonist(currentPawn.Pawn.LabelShort,
                    (string file) => {
                        CharacterSaved?.Invoke(file);
                    }
                ));
            }
        }
    }
}
