using System;
using UnityEngine;
using Verse;
namespace EdB.PrepareCarefully {
    public interface ITabView {
        string Name { get; }
        TabRecord TabRecord { get; set; }
        void Draw(Rect rect);
    }
}
