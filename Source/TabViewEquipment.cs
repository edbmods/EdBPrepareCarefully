using System;
using UnityEngine;
using Verse;
namespace EdB.PrepareCarefully {
    public class TabViewEquipment : TabViewBase {

        public PanelEquipmentAvailable PanelAvailable { get; set; }
        public PanelEquipmentSelected PanelSelected { get; set; }

        public TabViewEquipment() {
            PanelAvailable = new PanelEquipmentAvailable();
            PanelSelected = new PanelEquipmentSelected();
        }

        public override string Name {
            get {
                return "EdB.PC.TabView.Equipment.Title".Translate();
            }
        }

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

        public override void Draw(State state, Rect rect) {
            base.Draw(state, rect);

            // Draw the panels.
            PanelAvailable.Draw(PrepareCarefully.Instance.State);
            PanelSelected.Draw(PrepareCarefully.Instance.State);
        }

    }
}
