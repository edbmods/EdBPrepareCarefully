using System;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class ScrollViewVertical {
        public static readonly float ScrollbarSize = 15;
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

        public ScrollViewVertical() {

        }

        public ScrollViewVertical(bool consumeScrollEvents) {
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
            if (Event.current.type == EventType.Layout) {
                contentHeight = yPosition;
            }
            Widgets.EndScrollView();
            if (scrollTo != null) {
                Position = scrollTo.Value;
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
            if (y < 0) {
                y = 0;
            }
            else if (y > ContentHeight - ViewHeight) {
                y = ContentHeight - ViewHeight;
            }
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

