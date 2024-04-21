using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class TabViewEquipment : TabViewBase {
        public override string Name => "EdB.PC.TabView.Equipment.Title".Translate();
        public PanelEquipmentAvailable PanelAvailable { get; set; }
        public PanelEquipmentSelected PanelSelected { get; set; }

        protected override void Resize(Rect rect) {
            base.Resize(rect);

            Vector2 panelMargin = Style.SizePanelMargin;

            float availableWidth = rect.width - panelMargin.x;
            float availableHeight = rect.height;
            float panelWidth = Mathf.Floor(availableWidth / 2);

            PanelAvailable.Resize(new Rect(rect.x, rect.y, panelWidth, availableHeight));
            PanelSelected.Resize(new Rect(PanelAvailable.PanelRect.xMax + panelMargin.x, rect.y,
                panelWidth, availableHeight));
        }

        public override void Draw(Rect rect) {
            base.Draw(rect);

            // Draw the panels.
            PanelAvailable.Draw();
            PanelSelected.Draw();
        }
    }
}
