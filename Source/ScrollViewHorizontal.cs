using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    class ScrollViewHorizontal {
        public static readonly float ScrollbarSize = 15;
        private float contentWidth;
        private Vector2 position = Vector2.zero;
        private Rect viewRect;
        private Rect contentRect;
        private bool consumeScrollEvents = true;

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
                return contentWidth;
            }
        }

        public float ContentHeight {
            get {
                return contentRect.height;
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
                return ContentWidth > ViewWidth;
            }
        }

        public ScrollViewHorizontal() {

        }

        public ScrollViewHorizontal(bool consumeScrollEvents) {
            this.consumeScrollEvents = consumeScrollEvents;
        }

        public void Begin(Rect viewRect) {
            this.viewRect = viewRect;
            this.contentRect = new Rect(0, 0, contentWidth, viewRect.height - 16);
            if (consumeScrollEvents) {
                Widgets.BeginScrollView(viewRect, ref position, contentRect);
            }
            else {
                BeginScrollView(viewRect, ref position, contentRect);
            }
        }

        public void End(float xPosition) {
            if (Event.current.type == EventType.Layout) {
                contentWidth = xPosition;
            }
            Widgets.EndScrollView();
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
