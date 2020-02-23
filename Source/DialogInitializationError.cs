using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class DialogInitializationError : Window {

        private Exception exception;

        public override Vector2 InitialSize {
            get {
                return new Vector2(500f, 400f);
            }
        }

        public DialogInitializationError(Exception exception) {
            this.exception = exception;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = false;
            this.closeOnAccept = true;
            this.doCloseButton = true;
        }

        public override void DoWindowContents(Rect inRect) {
            Text.Font = GameFont.Small;
            Widgets.Label(inRect, "EdB.PC.Error.Initialization".Translate());
        }
    }
}
