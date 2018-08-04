using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
namespace EdB.PrepareCarefully {
    public class LabelTrimmer {
        private Dictionary<string, string> cache = new Dictionary<string, string>();
        private string suffix = "...";
        private float width;
        public Rect Rect {
            set {
                if (width != value.width) {
                    cache.Clear();
                }
                width = value.width;
            }
        }
        public float Width {
            get {
                return width;
            }
            set {
                width = value;
            }
        }
        public string TrimLabelIfNeeded(string name) {
            if (Text.CalcSize(name).x <= width) {
                return name;
            }
            string shorter;
            if (cache.TryGetValue(name, out shorter)) {
                return shorter + suffix;
            }
            shorter = name;
            while (!shorter.NullOrEmpty()) {
                shorter = shorter.Substring(0, shorter.Length - 1);
                if (shorter.EndsWith(" ")) {
                    continue;
                }
                Vector2 size = Text.CalcSize(shorter + suffix);
                if (size.x <= width) {
                    cache.Add(name, shorter);
                    return shorter + suffix;
                }
            }
            return name;
        }
    }
}
