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
        public delegate void LoadCharacterHandler(string name);
        public delegate void SaveCharacterHandler(CustomPawn pawn, string name);

        public event LoadCharacterHandler CharacterLoaded;
        public event SaveCharacterHandler CharacterSaved;

        protected Rect RectButtonSave;
        protected Rect RectButtonLoad;

        public PanelLoadSave() {
        }

        public override void Resize(Rect rect) {
            base.Resize(rect);

            float panelPadding = 12;
            float buttonSpacing = 6;
            float availableSpace = PanelRect.width - panelPadding * 2 - buttonSpacing;
            float buttonWidth = Mathf.Floor(availableSpace * 0.5f);
            float buttonHeight = 38;
            float top = PanelRect.height * 0.5f - buttonHeight * 0.5f;
            RectButtonLoad = new Rect(panelPadding, top, buttonWidth, buttonHeight);
            RectButtonSave = new Rect(PanelRect.width - panelPadding - buttonWidth, top, buttonWidth, buttonHeight);
        }

        protected override void DrawPanelContent(State state) {
            base.DrawPanelContent(state);
            CustomPawn currentPawn = state.CurrentPawn;

            if (Widgets.ButtonText(RectButtonLoad, "EdB.PC.Panel.LoadSave.Load".Translate(), true, false, true)) {
                Find.WindowStack.Add(new Dialog_LoadColonist(
                    (string name) => {
                        CharacterLoaded(name);
                    }
                ));
            }
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
