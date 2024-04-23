using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class DialogLoadPreset : DialogPreset {
        private Action<string> action;
        public DialogLoadPreset(Action<string> action) {
            this.action = action;
            this.interactButLabel = "EdB.PC.Dialog.Preset.Button.Load".Translate();
        }

        protected override void DoMapEntryInteraction(string mapName) {
            if (action != null) {
                action(mapName);
            }
            Close(true);
        }
    }
}

