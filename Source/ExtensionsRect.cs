using System;
using UnityEngine;
namespace EdB.PrepareCarefully {
    public static class ExtensionsRect {
        public static float MiddleX(this Rect rect) {
            return rect.x + rect.width * 0.5f;
        }
        public static float MiddleY(this Rect rect) {
            return rect.y + rect.height * 0.5f;
        }
        public static Rect GrowBy(this Rect rect, float xAmount, float yAmount) {
            return new Rect(rect.x, rect.y, rect.width + xAmount, rect.height + yAmount);
        }
        public static float HalfWidth(this Rect rect) {
            return rect.width * 0.5f;
        }
        public static float HalfHeight(this Rect rect) {
            return rect.height * 0.5f;
        }
        public static Rect OffsetBy(this Rect rect, Vector2 offset) {
            return new Rect(rect.position + offset, rect.size);
        }
        public static Rect OffsetBy(this Rect rect, float x, float y) {
            return new Rect(rect.position + new Vector2(x, y), rect.size);
        }
        public static Rect MoveTo(this Rect rect, Vector2 position) {
            return new Rect(position, rect.size);
        }
        public static Rect MoveTo(this Rect rect, float x, float y) {
            return new Rect(new Vector2(x, y), rect.size);
        }
        public static Rect InsetBy(this Rect rect, float left, float top, float right, float bottom) {
	        return new Rect(rect.x + left, rect.y + top, rect.width - left - right, rect.height - top - bottom);
        }
        public static Rect InsetBy(this Rect rect, Vector2 topLeft, Vector2 bottomRight) {
	        return new Rect(rect.x + topLeft.x, rect.y + topLeft.y, rect.width - topLeft.x - bottomRight.x, rect.height - topLeft.y - bottomRight.y);
        }
        public static Rect InsetBy(this Rect rect, float amount) {
            return rect.InsetBy(amount, amount, amount, amount);
        }
        public static Rect InsetBy(this Rect rect, Vector2 amount) {
            return rect.InsetBy(amount, amount);
        }
        public static Rect InsetBy(this Rect rect, float xAmount, float yAmount) {
            return rect.InsetBy(new Vector2(xAmount, yAmount), new Vector2(xAmount, yAmount));
        }
        public static Rect OutsetBy(this Rect rect, float x, float y) {
            return new Rect(rect.x - x * 0.5f, rect.y - y * 0.5f, rect.width + x, rect.height + y);
        }
        public static Rect ShrinkBy(this Rect rect, float amount) {
            return new Rect(rect.x, rect.y, rect.width - amount, rect.height - amount);
        }
        public static Rect ShrinkBy(this Rect rect, float xAmount, float yAmount) {
            return new Rect(rect.x, rect.y, rect.width - xAmount, rect.height - yAmount);
        }
        public static Rect InsetToRight(this Rect rect, float amount) {
            return new Rect(rect.xMax - amount, rect.y, rect.width - amount, rect.height);
        }
        public static Rect InsetToBottom(this Rect rect, float amount) {
            return new Rect(rect.x, rect.yMax - amount, rect.width, rect.height - amount);
        }
        public static Rect Combined(this Rect rect, Rect other) {
            Vector2 min = new Vector2(Mathf.Min(rect.xMin, other.xMin), Mathf.Min(rect.yMin, other.yMin));
            Vector2 max = new Vector2(Mathf.Max(rect.xMax, other.xMax), Mathf.Max(rect.yMax, other.yMax));
            return new Rect(min, max - min);
        }
        public static Rect ResizeTo(this Rect rect, float width, float height) {
            return new Rect(rect.x, rect.y, width, height);
        }
        public static bool Mouseover(this Rect rect) {
            return rect.Contains(Event.current.mousePosition);
        }
    }
}
