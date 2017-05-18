using System;
using UnityEngine;
using Verse;
namespace EdB.PrepareCarefully {
    public class TabViewRelationships : TabViewBase {
        public TabViewRelationships() {
            PanelRelationshipsParentChild = new PanelRelationshipsParentChild();
            PanelRelationshipsOther = new PanelRelationshipsOther();
        }
        
        public PanelRelationshipsParentChild PanelRelationshipsParentChild { get; set; }
        public PanelRelationshipsOther PanelRelationshipsOther { get; set; }

        public override string Name {
            get {
                return "EdB.PC.TabView.Relationships.Title".Translate();
            }
        }

        protected override void Resize(Rect rect) {
            base.Resize(rect);

            Vector2 panelMargin = Style.SizePanelMargin;

            Rect parentChildRect = new Rect(rect.x, rect.y, rect.width, 316);
            Rect otherRect = new Rect(rect.x, rect.y + parentChildRect.height + panelMargin.y, rect.width, rect.height - parentChildRect.height - panelMargin.y);

            PanelRelationshipsParentChild.Resize(parentChildRect);
            PanelRelationshipsOther.Resize(otherRect);
        }

        public override void Draw(State state, Rect rect) {
            base.Draw(state, rect);

            // Draw the panels.
            PanelRelationshipsParentChild.Draw(state);
            PanelRelationshipsOther.Draw(state);
        }
    }
}
