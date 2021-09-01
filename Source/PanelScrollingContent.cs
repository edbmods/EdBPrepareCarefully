using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class PanelScrollingContent : PanelBase {
        protected ScrollViewVertical scrollView = new ScrollViewVertical();

        protected Rect RectScrollFrame;
        protected Rect RectScrollView;

        public List<PanelModule> Modules { get; set; } = new List<PanelModule>();

        public override string PanelHeader {
            get {
                return null;
            }
        }

        public void ScrollToTop() {
            scrollView.ScrollToTop();
        }

        public void ScrollToBottom() {
            scrollView.ScrollToBottom();
        }

        public override void Resize(Rect rect) {
            base.Resize(rect);

            Vector2 contentSize = new Vector2(PanelRect.width, BodyRect.height);

            RectScrollFrame = new Rect(0, BodyRect.y, contentSize.x, contentSize.y);
            RectScrollView = new Rect(0, 0, RectScrollFrame.width, RectScrollFrame.height);

            Modules.ForEach(m => { m.Resize(rect.width); });
        }

        protected override void DrawPanelContent(State state) {
            base.DrawPanelContent(state);
            CustomPawn currentPawn = state.CurrentPawn;

            bool wasScrolling = scrollView.ScrollbarsVisible;

            float y = 0;
            GUI.BeginGroup(RectScrollFrame);

            try {
                scrollView.Begin(RectScrollView);
                try {
                    int visibleModules = 0;
                    foreach (var module in Modules) {
                        if (module.IsVisible(state)) {
                            if (visibleModules > 0) {
                                y += 6;
                                GUI.color = Style.ColorTabViewBackground;
                                GUI.DrawTexture(new Rect(0, y, PanelRect.width, 2), BaseContent.WhiteTex);
                                GUI.color = Color.white;
                                y += 2;
                            }
                            y += module.Draw(state, y);
                            visibleModules++;
                        }
                    }
                }
                finally {
                    scrollView.End(y);
                }
            }
            finally {
                GUI.EndGroup();
            }

            float? newWidth = null;
            if (wasScrolling && !scrollView.ScrollbarsVisible) {
                newWidth = PanelRect.width;
            }
            else if (!wasScrolling && scrollView.ScrollbarsVisible) {
                newWidth = PanelRect.width - ScrollViewVertical.ScrollbarSize;
            }
            if (newWidth.HasValue) {
                Modules.ForEach(m => { m.Resize(newWidth.Value); });
            }
        }
    }
}
