using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class Dialog_SavePreset : Dialog_Preset {
        protected Action<string> action;
        protected const float NewPresetNameButtonSpace = 20;
        protected const float NewPresetHeight = 35;
        protected const float NewPresetNameWidth = 400;

        private bool focusedPresetNameArea;

        public Dialog_SavePreset(Action<string> action) {
            this.action = action;
            this.interactButLabel = "OverwriteButton".Translate();
            this.bottomAreaHeight = 85;
            if ("".Equals(PrepareCarefully.Instance.Filename)) {
                PrepareCarefully.Instance.Filename = PresetFiles.UnusedDefaultName();
            }
        }

        protected override void DoMapEntryInteraction(string presetName) {
            if (string.IsNullOrEmpty(presetName)) {
                return;
            }
            PrepareCarefully.Instance.Filename = presetName;
            if (action != null) {
                action(PrepareCarefully.Instance.Filename);
            }
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
            string text = Widgets.TextField(rect, PrepareCarefully.Instance.Filename);
            if (GenText.IsValidFilename(text)) {
                PrepareCarefully.Instance.Filename = text;
            }
            if (!this.focusedPresetNameArea) {
                GUI.FocusControl("PresetNameField");
                this.focusedPresetNameArea = true;
            }
            Rect butRect = new Rect(420, top, inRect.width - 400 - 20, 35);
            if (Widgets.ButtonText(butRect, "EdB.PC.Dialog.Preset.Button.Save".Translate(), true, false, true) || flag) {
                if (PrepareCarefully.Instance.Filename.Length == 0) {
                    Messages.Message("NeedAName".Translate(), MessageSound.RejectInput);
                }
                else {
                    if (action != null) {
                        action(PrepareCarefully.Instance.Filename);
                    }
                    Close(true);
                }
            }
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
        }
    }
}

