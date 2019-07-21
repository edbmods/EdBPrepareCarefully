using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
namespace EdB.PrepareCarefully {
    public class LabelTrimmer {

        public interface LabelProvider {
            string Trim();
            string Current {
                get;
            }
            string CurrentWithSuffix(string suffix);
        }

        public struct DefaultLabelProvider : LabelProvider {
            private string label;
            private bool trimmed;
            public DefaultLabelProvider(string label) {
                this.label = label;
                this.trimmed = false;
            }
            public string Trim() {
                int length = label.Length;
                if (length == 0) {
                    return "";
                }
                label = label.Substring(0, length - 1).TrimEnd();
                trimmed = true;
                return label;
            }
            public string Current {
                get {
                    return label;
                }
            }
            public string CurrentWithSuffix(string suffix) {
                if (trimmed) {
                    return label + suffix;
                }
                else {
                    return label;
                }
            }
        }

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
                if (width != value) {
                    cache.Clear();
                }
                width = value;
            }
        }
        public string TrimLabelIfNeeded(string name) {
            return TrimLabelIfNeeded(new DefaultLabelProvider(name));
        }
        public string TrimLabelIfNeeded(LabelProvider provider) {
            string label = provider.Current;
            if (Text.CalcSize(label).x <= width) {
                return label;
            }
            if (cache.TryGetValue(label, out string shorter)) {
                return shorter;
            }
            return TrimLabel(provider);
        }
        public string TrimLabel(LabelProvider provider) {
            string original = provider.Current;
            string shorter = original;
            while (!shorter.NullOrEmpty()) {
                int length = shorter.Length;
                shorter = provider.Trim();
                // The trimmer should always return a shorter length.  If it doesn't we bail--it's a bad implementation.
                if (shorter.Length >= length) {
                    break;
                }
                string withSuffix = provider.CurrentWithSuffix(suffix);
                Vector2 size = Text.CalcSize(withSuffix);
                if (size.x <= width) {
                    cache.Add(original, withSuffix);
                    return shorter;
                }
            }
            return original;
        }
    }
}
