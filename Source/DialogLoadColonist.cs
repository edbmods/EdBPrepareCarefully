using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class DialogLoadColonist : DialogColonist {
        protected Action<string> action;

        public DialogLoadColonist(Action<string> action) {
            this.action = action;
            this.interactButLabel = "EdB.PC.Dialog.PawnPreset.Button.Load".Translate();
        }

        protected override void DoMapEntryInteraction(string colonistName) {
            action(colonistName);
            Close(true);
        }
    }
}

