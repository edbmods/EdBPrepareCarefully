using System;
using UnityEngine;
using Verse;
namespace EdB.PrepareCarefully {
    abstract public class TabViewBase : ITabView {
        public TabRecord TabRecord {
            get;
            set;
        }
        public abstract string Name {
            get;
        }
        public Rect TabViewRect = new Rect(float.MinValue, float.MinValue, float.MinValue, float.MinValue);
        public TabViewBase() {
        }
        public virtual void Draw(State state, Rect rect) {
            if (rect != TabViewRect) {
                Resize(rect);
            }
        }
        protected virtual void Resize(Rect rect) {
            TabViewRect = rect;
        }
    }
}
