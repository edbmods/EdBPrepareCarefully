using System;
using UnityEngine;
namespace EdB.PrepareCarefully {
    public interface IPanel {
        Rect PanelRect {
            get;
        }
        void Resize(Rect rect);
        void Draw();
    }
}
