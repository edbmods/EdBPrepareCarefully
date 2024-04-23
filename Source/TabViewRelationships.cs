using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static UnityEngine.Random;

namespace EdB.PrepareCarefully {
    public class TabViewRelationships : TabViewBase {
        public override string Name => "EdB.PC.TabView.Relationships.Title".Translate();
        public PanelRelationshipsParentChild PanelRelationshipsParentChild { get; set; }
        public PanelRelationshipsOther PanelRelationshipsOther { get; set; }


        protected override void Resize(Rect rect) {
            base.Resize(rect);

            Vector2 panelMargin = Style.SizePanelMargin;

            Rect parentChildRect = new Rect(rect.x, rect.y, rect.width, 316);
            Rect otherRect = new Rect(rect.x, rect.y + parentChildRect.height + panelMargin.y, rect.width, rect.height - parentChildRect.height - panelMargin.y);

            PanelRelationshipsParentChild.Resize(parentChildRect);
            PanelRelationshipsOther.Resize(otherRect);
        }

        public override void Draw(Rect rect) {
            base.Draw(rect);

            // Draw the panels.
            PanelRelationshipsParentChild.Draw();
            PanelRelationshipsOther.Draw();
        }
    }
}
