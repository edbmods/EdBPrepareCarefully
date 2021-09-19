using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class PanelFaction : PanelModule {
        public static readonly float FieldPadding = 6;

        public Rect FieldRect;
        protected Field FieldFaction = new Field();
        protected ProviderFactions providerFactions = PrepareCarefully.Instance.Providers.Factions;
        protected LabelTrimmer labelTrimmer = new LabelTrimmer();

        public override void Resize(float width) {
            base.Resize(width);
            FieldRect = new Rect(FieldPadding, 0, width - FieldPadding * 2, Style.FieldHeight);
        }

        public float Measure() {
            return 0;
        }

        public override bool IsVisible(State state) {
            return state.CurrentPawn.Type != CustomPawnType.Colonist;
        }

        public override float Draw(State state, float y) {
            float top = y;
            y += Margin.y;

            y += DrawHeader(y, Width, "Faction".Translate().Resolve());

            CustomPawn pawn = state.CurrentPawn;
            FieldFaction.Rect = FieldRect.OffsetBy(0, y);
            labelTrimmer.Rect = FieldFaction.Rect.InsetBy(8, 0);
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
            y += FieldRect.height;

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;


            y += Margin.y;
            return y - top;
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
