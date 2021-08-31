using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
namespace EdB.PrepareCarefully {
    public class TabViewPawns : TabViewBase {
        public PanelColonyPawnList PanelColonyPawns { get; set; }
        public PanelWorldPawnList PanelWorldPawns { get; set; }
        public PanelRandomize PanelRandomize { get; set; }
        public PanelName PanelName { get; set; }
        public PanelAge PanelAge { get; set; }
        public PanelAppearance PanelAppearance { get; set; }
        public PanelSkills PanelSkills { get; set; }
        public PanelIncapableOf PanelIncapable { get; set; }
        public PanelLoadSave PanelSaveLoad { get; set; }
        public PanelFavoriteColor PanelFavoriteColor { get; set; }
        public PanelScrollingContent PanelFlexible { get; set; }
        public PanelModuleBackstory PanelBackstory { get; set; }
        public PanelModuleTraits PanelTraits { get; set; }
        public PanelModuleHealth PanelHealth { get; set; }
        public PanelModuleFaction PanelFaction { get; set; }

        public TabViewPawns() {
            InitializePanels();
        }

        public override string Name {
            get {
                return "EdB.PC.TabView.Pawns.Title".Translate();
            }
        }

        protected void InitializePanels() {
            PanelColonyPawns = new PanelColonyPawnList();
            PanelWorldPawns = new PanelWorldPawnList();
            PanelRandomize = new PanelRandomize();
            PanelName = new PanelName();
            PanelAge = new PanelAge();
            PanelAppearance = new PanelAppearance();
            PanelSkills = new PanelSkills();
            PanelIncapable = new PanelIncapableOf();
            PanelSaveLoad = new PanelLoadSave();
            PanelFavoriteColor = new PanelFavoriteColor();
            PanelBackstory = new PanelModuleBackstory();
            PanelTraits = new PanelModuleTraits();
            PanelHealth = new PanelModuleHealth();
            PanelFaction = new PanelModuleFaction();
            PanelFlexible = new PanelScrollingContent() {
                Modules = new List<PanelModule>() {
                    PanelFaction, PanelBackstory, PanelTraits, PanelHealth
                }
            };
        }

        public override void Draw(State state, Rect rect) {
            base.Draw(state, rect);

            // Draw the panels.
            PanelColonyPawns.Draw(state);
            PanelWorldPawns.Draw(state);
            if (state.CurrentPawn != null) {
                PanelRandomize.Draw(state);
                PanelName.Draw(state);
                if (ModsConfig.IdeologyActive) {
                    PanelFavoriteColor.Draw(state);
                }
                PanelSaveLoad.Draw(state);
                PanelAge.Draw(state);
                PanelAppearance.Draw(state);
                PanelFlexible.Draw(state);
                PanelSkills.Draw(state);
                PanelIncapable.Draw(state);
            }
        }

        protected override void Resize(Rect rect) {
            base.Resize(rect);

            Vector2 panelMargin = Style.SizePanelMargin;
            
            // Pawn list
            PawnListMode pawnListMode = PrepareCarefully.Instance.State.PawnListMode;
            float pawnListWidth = 168;
            float minimizedHeight = 36;
            float maximizedHeight = rect.height - panelMargin.y - minimizedHeight;
            if (pawnListMode == PawnListMode.ColonyPawnsMaximized) {
                PanelColonyPawns.Resize(new Rect(rect.xMin, rect.yMin, pawnListWidth, maximizedHeight));
                PanelWorldPawns.Resize(new Rect(PanelColonyPawns.PanelRect.x, PanelColonyPawns.PanelRect.yMax + panelMargin.y, pawnListWidth, minimizedHeight));
            }
            else if (pawnListMode == PawnListMode.WorldPawnsMaximized) {
                PanelColonyPawns.Resize(new Rect(rect.xMin, rect.yMin, pawnListWidth, minimizedHeight));
                PanelWorldPawns.Resize(new Rect(PanelColonyPawns.PanelRect.x, PanelColonyPawns.PanelRect.yMax + panelMargin.y, pawnListWidth, maximizedHeight));
            }
            else {
                float listHeight = Mathf.Floor((rect.height - panelMargin.y) *0.5f);
                PanelColonyPawns.Resize(new Rect(rect.xMin, rect.yMin, pawnListWidth, listHeight));
                PanelWorldPawns.Resize(new Rect(PanelColonyPawns.PanelRect.x, PanelColonyPawns.PanelRect.yMax + panelMargin.y, pawnListWidth, listHeight));
            }

            // Randomize, Age and Save/Load
            PanelRandomize.Resize(new Rect(PanelColonyPawns.PanelRect.xMax + panelMargin.x,
                PanelColonyPawns.PanelRect.yMin, 64, 64));
            float namePanelWidth = 532;
            if (ModsConfig.IdeologyActive) {
                namePanelWidth -= 88;
            }
            PanelName.Resize(new Rect(PanelRandomize.PanelRect.xMax + panelMargin.x,
                PanelRandomize.PanelRect.yMin, namePanelWidth, 64));
            bool favoriteColor = ModsConfig.IdeologyActive;
            PanelFavoriteColor.Resize(new Rect(PanelName.PanelRect.xMax + panelMargin.x, PanelName.PanelRect.yMin, favoriteColor ? 64 : 0, favoriteColor ? 64 : 0));
            float panelSaveLoadLeft = favoriteColor ? PanelFavoriteColor.PanelRect.xMax : PanelName.PanelRect.xMax;
            PanelSaveLoad.Resize(new Rect(panelSaveLoadLeft + panelMargin.x, PanelName.PanelRect.yMin, 154, 64));

            // Age and Appearance
            float columnSize1 = 226;
            PanelAge.Resize(new Rect(PanelColonyPawns.PanelRect.xMax + panelMargin.x,
                PanelRandomize.PanelRect.yMax + panelMargin.y, columnSize1, 64));
            PanelAppearance.Resize(new Rect(PanelAge.PanelRect.xMin, PanelAge.PanelRect.yMax + panelMargin.y,
                columnSize1, 414));

            // Faction, Backstory, Traits and Health
            float columnSize2 = 304;
            PanelFlexible.Resize(new Rect(PanelAppearance.PanelRect.xMax + panelMargin.x, PanelRandomize.PanelRect.yMax + panelMargin.y, columnSize2, rect.height - PanelName.PanelRect.height - panelMargin.y));
            
            // Skills and Incapable Of
            float columnSize3 = 218;
            PanelSkills.Resize(new Rect(PanelFlexible.PanelRect.xMax + panelMargin.x, PanelFlexible.PanelRect.yMin,
                columnSize3, 362));
            PanelIncapable.Resize(new Rect(PanelSkills.PanelRect.xMin, PanelSkills.PanelRect.yMax + panelMargin.y,
                columnSize3, 116));
        }

        public void ResizeTabView() {
            Resize(TabViewRect);
        }
    }
}
