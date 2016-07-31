using System;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	public class ScrollView
	{
		public static readonly float ScrollbarSize = 15;
		private float contentHeight;
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
		}

		public bool ScrollbarsVisible {
			get {
				return ContentHeight > ViewHeight;
			}
		}

		public ScrollView() {

		}

		public ScrollView(bool consumeScrollEvents) {
			this.consumeScrollEvents = consumeScrollEvents;
		}

		public void Begin(Rect viewRect)
		{
			this.viewRect = viewRect;
			this.contentRect = new Rect(0, 0, viewRect.width - 16, contentHeight);
			if (consumeScrollEvents) {
				Widgets.BeginScrollView(viewRect, ref position, contentRect);
			}
			else {
				BeginScrollView(viewRect, ref position, contentRect);
			}
		}

		public void End(float yPosition)
		{
			if (Event.current.type == EventType.Layout) {
				contentHeight = yPosition;
			}
			Widgets.EndScrollView();
		}

		protected static void BeginScrollView(Rect outRect, ref Vector2 scrollPosition, Rect viewRect)
		{
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

