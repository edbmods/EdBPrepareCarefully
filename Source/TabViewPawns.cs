using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
namespace EdB.PrepareCarefully {
    public class TabViewPawns : TabViewBase {
        public bool LargeUI { get; set; }
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
        public PanelBackstory PanelBackstory { get; set; }
        public PanelTraits PanelTraits { get; set; }
        public PanelHealth PanelHealth { get; set; }
        public PanelFaction PanelFaction { get; set; }
        public PanelIdeo PanelIdeo { get; set; }
        public PanelAbilities PanelAbilities { get; set; }
        public PanelScrollingContent PanelColumn1 { get; set; }
        public PanelScrollingContent PanelColumn2 { get; set; }
        //public PanelModuleAge PanelAge { get; set; }
        public PanelTitles PanelTitles { get; set; }

        public TabViewPawns(bool largeUI) {
            this.LargeUI = largeUI;
            InitializePanels(largeUI);
        }

        public override string Name {
            get {
                return "EdB.PC.TabView.Pawns.Title".Translate();
            }
        }

        protected void InitializePanels(bool largeUI) {
            PanelColonyPawns = new PanelColonyPawnList();
            PanelWorldPawns = new PanelWorldPawnList();
            PanelRandomize = new PanelRandomize();
            PanelName = new PanelName();
            PanelAppearance = new PanelAppearance();
            PanelSkills = new PanelSkills();
            PanelIncapable = new PanelIncapableOf();
            PanelSaveLoad = new PanelLoadSave();
            PanelFavoriteColor = new PanelFavoriteColor();
            PanelBackstory = new PanelBackstory();
            PanelTraits = new PanelTraits();
            PanelHealth = new PanelHealth();
            PanelFaction = new PanelFaction();
            PanelIdeo = new PanelIdeo();
            PanelAbilities = new PanelAbilities();
            PanelAge = new PanelAge();
            //PanelAge = new PanelModuleAge();
            PanelTitles = new PanelTitles();
            if (largeUI) {
                TwoColumnLayout();
            }
            else {
                OneColumnLayout();
            }
        }

        public void OneColumnLayout() {
            PanelColumn1 = new PanelScrollingContent() {
                Modules = new List<PanelModule>()
            };
            //PanelColumn1.Modules.Add(PanelAge);
            PanelColumn1.Modules.Add(PanelFaction);
            if (ModsConfig.IdeologyActive) {
                PanelColumn1.Modules.Add(PanelIdeo);
            }
            PanelColumn1.Modules.Add(PanelBackstory);
            PanelColumn1.Modules.Add(PanelTraits);
            //PanelColumn1.Modules.Add(PanelTitles);
            PanelColumn1.Modules.Add(PanelHealth);
            //PanelColumn1.Modules.Add(PanelAbilities);
        }

        public void TwoColumnLayout() {
            PanelColumn1 = new PanelScrollingContent() {
                Modules = new List<PanelModule>() {
                        /*PanelAge,*/ PanelFaction, PanelIdeo, PanelAbilities
                    }
            };
            PanelColumn2 = new PanelScrollingContent() {
                Modules = new List<PanelModule>() {
                        PanelBackstory, PanelTraits, PanelTitles, PanelHealth
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
                PanelColumn1.Draw(state);
                PanelColumn2?.Draw(state);
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

            // Randomize, Name and Save/Load
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

            float x = PanelColonyPawns.PanelRect.xMax + panelMargin.x;
            float top = PanelRandomize.PanelRect.yMax + panelMargin.y;

            // Age and Appearance
            float columnSize1 = 226;
            PanelAge.Resize(new Rect(PanelColonyPawns.PanelRect.xMax + panelMargin.x,  PanelRandomize.PanelRect.yMax + panelMargin.y, columnSize1, 64));
            PanelAppearance.Resize(new Rect(PanelAge.PanelRect.xMin, PanelAge.PanelRect.yMax + panelMargin.y, columnSize1, 414));
            //PanelAppearance.Resize(new Rect(x, top, columnSize1, 490));
            x += columnSize1 + panelMargin.x;

            float columnSize2 = 304;
            // Faction, Backstory, Traits and Health
            PanelColumn1.Resize(new Rect(x, top, columnSize2, rect.height - PanelName.PanelRect.height - panelMargin.y));
            x += columnSize2 + panelMargin.x;
            if (LargeUI && PanelColumn2 != null) {
                PanelColumn2.Resize(new Rect(x, top, columnSize2, rect.height - PanelName.PanelRect.height - panelMargin.y));
                x += columnSize2 + panelMargin.x;
            }

            // Skills and Incapable Of
            float columnSize3 = 218;
            PanelSkills.Resize(new Rect(x, top, columnSize3, 362));
            PanelIncapable.Resize(new Rect(PanelSkills.PanelRect.xMin, PanelSkills.PanelRect.yMax + panelMargin.y,
                columnSize3, 116));
        }

        public void ResizeTabView() {
            Resize(TabViewRect);
        }
    }
}
