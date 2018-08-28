using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class PanelFaction : PanelBase {
        private Field FieldFaction = new Field();
        private Rect RectFactionField;
        private ProviderFactions providerFactions = PrepareCarefully.Instance.Providers.Factions;
        private LabelTrimmer labelTrimmer = new LabelTrimmer();
        public PanelFaction() {
        }
        public override string PanelHeader {
            get {
                return "Faction".Translate();
            }
        }

        public override void Resize(Rect rect) {
            base.Resize(rect);
            float top = 36;
            float labelPadding = 12;
            float fieldWidth = rect.width - labelPadding * 2;
            float fieldHeight = 22;
            RectFactionField = new Rect(labelPadding, top, fieldWidth, fieldHeight);
            FieldFaction.Rect = RectFactionField;
            labelTrimmer.Rect = FieldFaction.Rect.InsetBy(8, 0);
        }

        protected override void DrawPanelContent(State state) {
            base.DrawPanelContent(state);

            CustomPawn pawn = state.CurrentPawn;

            GUI.color = Color.white;
            if (pawn.Type == CustomPawnType.Colonist) {
                FieldFaction.Label = "Colony".Translate();
                FieldFaction.Enabled = false;
                FieldFaction.ClickAction = () => { };
            }
            else {
                FieldFaction.Label = labelTrimmer.TrimLabelIfNeeded((pawn.Faction != null) ? (pawn.Faction.Name) : (providerFactions.RandomFaction.Name));
                FieldFaction.Enabled = true;
                FieldFaction.ClickAction = () => {
                    ShowFactionDialog(pawn);
                };
            }
            FieldFaction.Draw();
            
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        protected List<WidgetTable<CustomFaction>.RowGroup> rowGroups = new List<WidgetTable<CustomFaction>.RowGroup>();
        protected void ShowFactionDialog(CustomPawn pawn) {
            CustomFaction selectedFaction = pawn.Faction != null ? pawn.Faction : PrepareCarefully.Instance.Providers.Factions.RandomFaction;
            HashSet<CustomFaction> disabled = new HashSet<CustomFaction>();
            rowGroups.Clear();
            rowGroups.Add(new WidgetTable<CustomFaction>.RowGroup("<b>" + "EdB.PC.Dialog.Faction.SelectRandomFaction".Translate() + "</b>", providerFactions.RandomCustomFactions));
            rowGroups.Add(new WidgetTable<CustomFaction>.RowGroup("<b>" + "EdB.PC.Dialog.Faction.SelectSpecificFaction".Translate() + "</b>", providerFactions.SpecificCustomFactions));
            rowGroups.Add(new WidgetTable<CustomFaction>.RowGroup("<b>" + "EdB.PC.Dialog.Faction.SelectLeaderFaction".Translate() + "</b>", providerFactions.LeaderCustomFactions));
            DialogFactions factionDialog = new DialogFactions() {
                HeaderLabel = "EdB.PC.Dialog.Faction.ChooseFaction".Translate(),
                SelectAction = (CustomFaction faction) => { selectedFaction = faction; },
                RowGroups = rowGroups,
                DisabledFactions = disabled,
                CloseAction = () => {
                    pawn.Faction = selectedFaction;
                },
                SelectedFaction = selectedFaction
            };
            factionDialog.ScrollTo(selectedFaction);
            Find.WindowStack.Add(factionDialog);
        }

    }
}
