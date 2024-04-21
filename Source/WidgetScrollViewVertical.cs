using System;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class WidgetScrollViewVertical {
        public static readonly float ScrollbarSize = 16;
        private float contentHeight;
        private Vector2 position = Vector2.zero;
        private Rect viewRect;
        private Rect contentRect;
        private bool consumeScrollEvents = true;
        private Vector2? scrollTo = null;

        public float ViewHeight {
            get {
                return viewRect.height;
            }
        }

        public float ViewWidth {
            get {
                return viewRect.width;
            }
        }

        // The current width of the view, adjusted based on whether or not the scrollbars are visible
        public float CurrentViewWidth {
            get {
                return !ScrollbarsVisible ? viewRect.width : viewRect.width - ScrollbarSize;
            }
        }

        public float ContentWidth {
            get {
                return contentRect.width;
            }
        }

        public float ContentHeight {
            get {
                return contentHeight;
            }
        }

        public Vector2 Position {
            get {
                return position;
            }
            set {
                position = value;
            }
        }

        public bool ScrollbarsVisible {
            get {
                return ContentHeight > ViewHeight;
            }
        }

        public WidgetScrollViewVertical() {

        }

        public WidgetScrollViewVertical(bool consumeScrollEvents) {
            this.consumeScrollEvents = consumeScrollEvents;
        }

        public void Begin(Rect viewRect) {
            this.viewRect = viewRect;
            this.contentRect = new Rect(0, 0, viewRect.width - 16, contentHeight);
            if (consumeScrollEvents) {
                Widgets.BeginScrollView(viewRect, ref position, contentRect);
            }
            else {
                BeginScrollView(viewRect, ref position, contentRect);
            }
        }

        public void End(float yPosition) {
            contentHeight = yPosition;
            Widgets.EndScrollView();
            if (scrollTo != null) {
                Vector2 newPosition = scrollTo.Value;
                if (newPosition.y < 0) {
                    newPosition.y = 0;
                }
                else if (newPosition.y > ContentHeight - ViewHeight - 1) {
                    newPosition.y = ContentHeight - ViewHeight - 1;
                }
                Position = newPosition;
                scrollTo = null;
            }
        }

        public void ScrollToTop() {
            scrollTo = new Vector2(0, 0);
        }

        public void ScrollToBottom() {
            scrollTo = new Vector2(0, float.MaxValue);
        }

        public void ScrollTo(float y) {
            scrollTo = new Vector2(0, y);
        }

        protected static void BeginScrollView(Rect outRect, ref Vector2 scrollPosition, Rect viewRect) {
            Vector2 vector = scrollPosition;
            Vector2 vector2 = GUI.BeginScrollView(outRect, scrollPosition, viewRect);
            Vector2 vector3;
            if (Event.current.type == EventType.MouseDown) {
                vector3 = vector;
            }
            else {
                vector3 = vector2;
            }
            if (Event.current.type == EventType.ScrollWheel && Mouse.IsOver(outRect)) {
                vector3 += Event.current.delta * 40;
            }
            scrollPosition = vector3;
        }
    }
}

