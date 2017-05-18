using System;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class Dialog_Confirm : Window {
        private Vector2 scrollPos = new Vector2();
        private const float TitleHeight = 40f;
        private string text;
        private Action confirmedAction;
        private bool destructiveAction;
        private string title;
        public string confirmLabel;
        public string goBackLabel;
        public bool showGoBack;
        public float interactionDelay;
        private float scrollViewHeight;
        private float createRealTime;

        public override Vector2 InitialSize {
            get {
                float y = 300f;
                if (this.title != null)
                    y += 40f;
                return new Vector2(500f, y);
            }
        }

        private float TimeUntilInteractive {
            get {
                return this.interactionDelay - (Time.realtimeSinceStartup - this.createRealTime);
            }
        }

        private bool InteractionDelayExpired {
            get {
                return (double)this.TimeUntilInteractive <= 0.0;
            }
        }

        public Dialog_Confirm(string text, Action confirmedAction) {
            this.text = text;
            this.confirmedAction = confirmedAction;
            this.destructiveAction = false;
            this.title = null;
            this.showGoBack = true;
            this.confirmLabel = "EdB.PC.Common.Confirm".Translate();
            this.goBackLabel = "EdB.PC.Common.Cancel".Translate();
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnEscapeKey = showGoBack;
            this.createRealTime = Time.realtimeSinceStartup;
        }


        public Dialog_Confirm(string text, Action confirmedAction, bool destructive) {
            this.text = text;
            this.confirmedAction = confirmedAction;
            this.destructiveAction = destructive;
            this.title = null;
            this.showGoBack = true;
            this.confirmLabel = "EdB.PC.Common.Confirm".Translate();
            this.goBackLabel = "EdB.PC.Common.Cancel".Translate();
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnEscapeKey = showGoBack;
            this.createRealTime = Time.realtimeSinceStartup;
        }

        public Dialog_Confirm(string text, Action confirmedAction, bool destructive, string title) {
            this.text = text;
            this.confirmedAction = confirmedAction;
            this.destructiveAction = destructive;
            this.title = title;
            this.showGoBack = true;
            this.confirmLabel = "EdB.PC.Common.Confirm".Translate();
            this.goBackLabel = "EdB.PC.Common.Cancel".Translate();
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnEscapeKey = showGoBack;
            this.createRealTime = Time.realtimeSinceStartup;
        }
        public Dialog_Confirm(string text, Action confirmedAction, bool destructive, string title, bool showGoBack) {
            this.text = text;
            this.confirmedAction = confirmedAction;
            this.destructiveAction = destructive;
            this.title = title;
            this.showGoBack = showGoBack;
            this.confirmLabel = "EdB.PC.Common.Confirm".Translate();
            this.goBackLabel = "EdB.PC.Common.Cancel".Translate();
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnEscapeKey = showGoBack;
            this.createRealTime = Time.realtimeSinceStartup;
        }

        public override void DoWindowContents(Rect inRect) {
            float y = inRect.y;
            if (!this.title.NullOrEmpty()) {
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(0.0f, y, inRect.width, 40f), this.title);
                y += 40f;
            }
            Text.Font = GameFont.Small;
            Rect outRect = new Rect(0.0f, y, inRect.width, inRect.height - 45f - y);
            Rect viewRect = new Rect(0.0f, 0.0f, inRect.width - 16f, this.scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref this.scrollPos, viewRect);
            Widgets.Label(new Rect(0.0f, 0.0f, viewRect.width, this.scrollViewHeight), this.text);
            if (Event.current.type == EventType.Layout)
                this.scrollViewHeight = Text.CalcHeight(this.text, viewRect.width);
            Widgets.EndScrollView();
            if (this.destructiveAction)
                GUI.color = new Color(1f, 0.3f, 0.35f);
            string label = !this.InteractionDelayExpired ? this.confirmLabel + "(" + Mathf.Ceil(this.TimeUntilInteractive).ToString("F0") + ")" : this.confirmLabel;
            if (Widgets.ButtonText(new Rect((float)((double)inRect.width / 2.0 + 20.0), inRect.height - 35f, (float)((double)inRect.width / 2.0 - 20.0), 35f), label, true, false, true) && this.InteractionDelayExpired) {
                this.confirmedAction();
                this.Close(true);
            }
            GUI.color = Color.white;
            if (!this.showGoBack || !Widgets.ButtonText(new Rect(0.0f, inRect.height - 35f, (float)((double)inRect.width / 2.0 - 20.0), 35f), this.goBackLabel, true, false, true))
                return;
            this.Close(true);
        }
    }
}
