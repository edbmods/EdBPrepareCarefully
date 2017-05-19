using System;
using UnityEngine;
using Verse;
namespace EdB.PrepareCarefully {
    public class TabViewPawns : TabViewBase {
        public PanelPawnList PanelPawnList { get; set; }
        public PanelRandomize PanelRandomize { get; set; }
        public PanelName PanelName { get; set; }
        public PanelAge PanelAge { get; set; }
        public PanelAppearance PanelAppearance { get; set; }
        public PanelBackstory PanelBackstory { get; set; }
        public PanelTraits PanelTraits { get; set; }
        public PanelHealth PanelHealth { get; set; }
        public PanelSkills PanelSkills { get; set; }
        public PanelIncapableOf PanelIncapable { get; set; }
        public PanelLoadSave PanelSaveLoad { get; set; }

        public TabViewPawns() {
            PanelPawnList = new PanelPawnList();
            PanelRandomize = new PanelRandomize();
            PanelName = new PanelName();
            PanelAge = new PanelAge();
            PanelAppearance = new PanelAppearance();
            PanelBackstory = new PanelBackstory();
            PanelTraits = new PanelTraits();
            PanelHealth = new PanelHealth();
            PanelSkills = new PanelSkills();
            PanelIncapable = new PanelIncapableOf();
            PanelSaveLoad = new PanelLoadSave();
        }

        public override string Name {
            get {
                return "EdB.PC.TabView.Pawns.Title".Translate();
            }
        }

        public override void Draw(State state, Rect rect) {
            base.Draw(state, rect);

            // Draw the panels.
            PanelPawnList.Draw(state);
            PanelRandomize.Draw(state);
            PanelName.Draw(state);
            PanelSaveLoad.Draw(state);
            PanelAge.Draw(state);
            PanelAppearance.Draw(state);
            PanelBackstory.Draw(state);
            PanelTraits.Draw(state);
            PanelHealth.Draw(state);
            PanelSkills.Draw(state);
            PanelIncapable.Draw(state);
        }

        protected override void Resize(Rect rect) {
            base.Resize(rect);

            Vector2 panelMargin = Style.SizePanelMargin;

            PanelPawnList.Resize(new Rect(rect.xMin, rect.yMin, 110, 560));
            PanelRandomize.Resize(new Rect(PanelPawnList.PanelRect.xMax + panelMargin.x,
                PanelPawnList.PanelRect.yMin, 64, 64));
            PanelName.Resize(new Rect(PanelRandomize.PanelRect.xMax + panelMargin.x,
                PanelRandomize.PanelRect.yMin, 460, 64));
            PanelSaveLoad.Resize(new Rect(PanelName.PanelRect.xMax + panelMargin.x,
                PanelName.PanelRect.yMin, 284, 64));

            PanelAge.Resize(new Rect(PanelPawnList.PanelRect.xMax + panelMargin.x,
                PanelRandomize.PanelRect.yMax + panelMargin.y, 226, 64));
            PanelAppearance.Resize(new Rect(PanelAge.PanelRect.xMin, PanelAge.PanelRect.yMax + panelMargin.y,
                226, 405));

            PanelBackstory.Resize(new Rect(PanelAge.PanelRect.xMax + panelMargin.x, PanelAge.PanelRect.yMin,
                320, 120));
            PanelTraits.Resize(new Rect(PanelBackstory.PanelRect.xMin, PanelBackstory.PanelRect.yMax + panelMargin.y,
                320, 156));
            PanelHealth.Resize(new Rect(PanelBackstory.PanelRect.xMin, PanelTraits.PanelRect.yMax + panelMargin.y,
                320, 180));

            PanelSkills.Resize(new Rect(PanelBackstory.PanelRect.xMax + panelMargin.x, PanelBackstory.PanelRect.yMin,
                262, 362));
            PanelIncapable.Resize(new Rect(PanelSkills.PanelRect.xMin, PanelSkills.PanelRect.yMax + panelMargin.y,
                260, 103));
        }
    }
}
