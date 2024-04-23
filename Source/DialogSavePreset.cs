using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class DialogSavePreset : DialogPreset {
        public ViewState ViewState { get; set; }
        public Action<string> Action { get; set; }
        protected const float NewPresetNameButtonSpace = 20;
        protected const float NewPresetHeight = 35;
        protected const float NewPresetNameWidth = 400;

        private bool focusedPresetNameArea;

        public DialogSavePreset() {
            this.interactButLabel = "OverwriteButton".Translate();
            this.bottomAreaHeight = 85;
        }

        public override void PreOpen() {
            base.PreOpen();
            if ("".Equals(ViewState.Filename)) {
                ViewState.Filename = PresetFiles.UnusedDefaultName();
            }
        }

        protected override void DoMapEntryInteraction(string presetName) {
            if (string.IsNullOrEmpty(presetName)) {
                return;
            }
            ViewState.Filename = presetName;
            Action?.Invoke(ViewState.Filename);
            Close(true);
        }

        protected override void DoSpecialSaveLoadGUI(Rect inRect) {
            GUI.BeginGroup(inRect);
            bool flag = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return;
            float top = inRect.height - 52;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.SetNextControlName("PresetNameField");
            Rect rect = new Rect(5, top, 400, 35);
            string text = Widgets.TextField(rect, ViewState.Filename);
            if (GenText.IsValidFilename(text)) {
                ViewState.Filename = text;
            }
            if (!this.focusedPresetNameArea) {
                GUI.FocusControl("PresetNameField");
                this.focusedPresetNameArea = true;
            }
            Rect butRect = new Rect(420, top, inRect.width - 400 - 20, 35);
            if (Widgets.ButtonText(butRect, "EdB.PC.Dialog.Preset.Button.Save".Translate(), true, false, true) || flag) {
                if (ViewState.Filename.Length == 0) {
                    Messages.Message("NeedAName".Translate(), MessageTypeDefOf.RejectInput);
                }
                else {
                    Action?.Invoke(ViewState.Filename);
                    Close(true);
                }
            }
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
        }
    }
}

