using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class TabViewPawns : TabViewBase {
        public override string Name => "EdB.PC.TabView.Pawns.Title".Translate();
        public PanelColonyPawnListMinimized PanelColonyPawnsMinimized { get; set; }
        public PanelColonyPawnListRefactored PanelColonyPawns { get; set; }
        public PanelWorldPawnList PanelWorldPawns { get; set; }
        public PanelWorldPawnListMinimized PanelWorldPawnsMinimized { get; set; }
        public PanelRandomize PanelRandomize { get; set; }
        public PanelName PanelName { get; set; }
        public PanelSaveCharacter PanelSaveCharacter { get; set; }
        public PanelAppearance PanelAppearance { get; set; }
        public PanelApparel PanelApparel { get; set; }
        public PanelPossessions PanelPossessions { get; set; }
        public PanelAbilitiesRefactored PanelAbilities { get; set; }
        public PanelIdeo PanelIdeo { get; set; }
        public PanelXenotype PanelXenotype { get; set; }
        public PanelAgeRefactored PanelAge { get; set; }
        public PanelBackstory PanelBackstory { get; set; }
        public PanelTitles PanelTitles { get; set; }
        public PanelTraits PanelTraits { get; set; }
        public PanelHealth PanelHealth { get; set; }
        public PanelSkills PanelSkills { get; set; }
        public PanelIncapableOf PanelIncapableOf { get; set; }
        public PanelScrollingContent AppearanceColumn { get; set; }
        public PanelScrollingContent PanelColumn1 { get; set; }
        public PanelScrollingContent PanelColumn2 { get; set; }
        public ModState State { get; set; }
        public ViewState ViewState { get; set; }
        public bool LargeUI { get; set; } = false;

        public void PostConstruction() {
            if (LargeUI) {
                InitializeTwoColumnLayout();
            }
            else {
                InitializeOneColumnLayout();
            }
        }

        public void InitializeOneColumnLayout() {
            AppearanceColumn = new PanelScrollingContent() {
                Modules = new List<PanelModule>() {
                    PanelAppearance, PanelApparel
                }
            };
            PanelColumn1 = new PanelScrollingContent() {
                Modules = new List<PanelModule>()
            };
            PanelColumn1.Modules.Add(PanelAge);
            PanelColumn1.Modules.Add(PanelBackstory);
            PanelColumn1.Modules.Add(PanelTraits);
            PanelColumn1.Modules.Add(PanelHealth);
            if (ModsConfig.BiotechActive) {
                PanelColumn1.Modules.Add(PanelXenotype);
            }
            if (ModsConfig.IdeologyActive && !Find.IdeoManager.classicMode) {
                PanelColumn1.Modules.Add(PanelIdeo);
            }
            if (ModsConfig.RoyaltyActive) {
                PanelColumn1.Modules.Add(PanelTitles);
            }
            PanelColumn1.Modules.Add(PanelAbilities);
            //PanelColumn1.Modules.Add(PanelFaction);
        }

        public void InitializeTwoColumnLayout() {
            AppearanceColumn = new PanelScrollingContent() {
                Modules = new List<PanelModule>() {
                    PanelAppearance,
                    PanelXenotype,
                }
            };
            List<PanelModule> column1Modules = new List<PanelModule> {
                PanelApparel,
                PanelPossessions,
                PanelTitles,
                PanelAbilities,
                PanelIdeo,
            };
            //column1Modules.Add(PanelFaction);
            //if (ModsConfig.IdeologyActive && !Find.IdeoManager.classicMode) {
            //    column1Modules.Add(PanelIdeo);
            //}
            //if (ModsConfig.BiotechActive) {
            //    column1Modules.Add(PanelXenotype);
            //}
            //column1Modules.Add(PanelAbilities);
            PanelColumn1 = new PanelScrollingContent() {
                Modules = column1Modules
            };

            PanelColumn2 = new PanelScrollingContent() {
                Modules = new List<PanelModule>() {
                    PanelAge, PanelBackstory,
                    PanelTraits, PanelHealth
                }
            };
        }

        public override void Draw(Rect rect) {
            base.Draw(rect);
            if (ViewState.PawnListMode == PawnListMode.ColonyPawnsMaximized) {
                PanelColonyPawns?.Draw();
                PanelWorldPawnsMinimized?.Draw();
            }
            else {
                PanelColonyPawnsMinimized?.Draw();
                PanelWorldPawns?.Draw();
            }
            if (ViewState.CurrentPawn != null) {
                PanelRandomize?.Draw();
                PanelName?.Draw();
                PanelSaveCharacter?.Draw();
                AppearanceColumn.Draw();
                PanelSkills?.Draw();
                PanelIncapableOf?.Draw();
                PanelColumn1?.Draw();
                if (LargeUI) {
                    PanelColumn2?.Draw();
                }
            }
        }
        protected override void Resize(Rect rect) {
            base.Resize(rect);

            Vector2 panelMargin = Style.SizePanelMargin;

            // Pawn list
            float pawnListWidth = 168;
            float minimizedHeight = 36;
            float maximizedHeight = rect.height - panelMargin.y - minimizedHeight;
            PanelColonyPawns.Resize(new Rect(rect.xMin, rect.yMin, pawnListWidth, maximizedHeight));
            PanelWorldPawnsMinimized.Resize(new Rect(PanelColonyPawns.PanelRect.x, PanelColonyPawns.PanelRect.yMax + panelMargin.y, pawnListWidth, minimizedHeight));
            PanelColonyPawnsMinimized.Resize(new Rect(rect.xMin, rect.yMin, pawnListWidth, minimizedHeight));
            PanelWorldPawns.Resize(new Rect(PanelColonyPawns.PanelRect.x, PanelColonyPawnsMinimized.PanelRect.yMax + panelMargin.y, pawnListWidth, maximizedHeight));

            // Randomize, Name and Save/Load
            float randomizeWidth = ModsConfig.BiotechActive ? 110 : 64;
            float saveButtonWidth = 170;

            float availableWidth = rect.width - panelMargin.x * 2.0f - pawnListWidth;
            availableWidth -= randomizeWidth;
            availableWidth -= saveButtonWidth + panelMargin.x;
            float namePanelWidth = availableWidth;

            Rect randomizeRect = new Rect(PanelColonyPawns.PanelRect.xMax + panelMargin.x,
                PanelColonyPawns.PanelRect.yMin, randomizeWidth, 64);
            PanelRandomize.Resize(randomizeRect);
            PanelName.Resize(new Rect(randomizeRect.xMax + panelMargin.x,
                randomizeRect.yMin, namePanelWidth, 64));

            float panelSaveCharacterLeft = PanelName.PanelRect.xMax;
            PanelSaveCharacter.Resize(new Rect(panelSaveCharacterLeft + panelMargin.x, PanelName.PanelRect.yMin, 154, 64));

            float x = PanelColonyPawns.PanelRect.xMax + panelMargin.x;
            float top = randomizeRect.yMax + panelMargin.y;

            // Appearance
            float columnSize1 = 226;
            AppearanceColumn.Resize(new Rect(PanelColonyPawns.PanelRect.xMax + panelMargin.x, randomizeRect.yMax + panelMargin.y, columnSize1, 526));
            x += columnSize1 + panelMargin.x;

            float columnSize2 = 304;
            // Faction, Backstory, Traits and Health
            PanelColumn1.Resize(new Rect(x, top, columnSize2, rect.height - PanelName.PanelRect.height - panelMargin.y));
            x += columnSize2 + panelMargin.x;
            if (LargeUI && PanelColumn2 != null) {
                PanelColumn2.Resize(new Rect(x, top, columnSize2, rect.height - PanelName.PanelRect.height - panelMargin.y));
                x += (columnSize2 + panelMargin.x);
            }

            // Skills and Incapable Of
            float columnSize3 = 218;
            PanelSkills.Resize(new Rect(x, top, columnSize3, 362));
            PanelIncapableOf.Resize(new Rect(PanelSkills.PanelRect.xMin, PanelSkills.PanelRect.yMax + panelMargin.y,
                columnSize3, 154));

        }
    }
}
