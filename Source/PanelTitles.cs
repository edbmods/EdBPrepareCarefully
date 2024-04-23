using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class PanelTitles : PanelModule {
        public Rect FieldRect;
        public static readonly float FieldPadding = 6;

        protected WidgetScrollViewVertical scrollView = new WidgetScrollViewVertical();
        protected List<WidgetField> fields = new List<WidgetField>();
        protected List<RoyalTitle> itemsToRemove = new List<RoyalTitle>();
        protected HashSet<TraitDef> disallowedTraitDefs = new HashSet<TraitDef>();
        protected Dictionary<Trait, string> conflictingTraitList = new Dictionary<Trait, string>();
        public ModState State { get; set; }
        public ViewState ViewState { get; set; }
        public ProviderTitles ProviderTitles { get; set; }

        public override void Resize(float width) {
            base.Resize(width);
            FieldRect = new Rect(FieldPadding, 0, width - FieldPadding * 2, 28);
        }

        public float Measure() {
            return 0;
        }

        public override bool IsVisible() {
            return ModsConfig.RoyaltyActive;
        }

        public float DrawDialogGroupHeader(ProviderTitles.TitleSet group, float y, float width) {
            float factionIconSize = 24;
            float iconPadding = 4;
            float availableWidth = width - factionIconSize - iconPadding;

            string groupTitle = group.Faction.Name;
            float labelHeight = Text.CalcHeight(groupTitle, availableWidth);
            Rect labelRect = new Rect(factionIconSize + iconPadding, y, width, Mathf.Max(factionIconSize, labelHeight));
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label(labelRect, groupTitle);
            Text.Anchor = TextAnchor.UpperLeft;

            Rect iconRect = new Rect(0, labelRect.yMax - factionIconSize, factionIconSize, factionIconSize);
            GUI.DrawTexture(iconRect, group.Faction.def.FactionIcon);

            return labelRect.height;
        }

        public float DrawDialogGroupContent(Dictionary<Faction, int> favorLookup, ProviderTitles.TitleSet set, float y, float width) {
            if (!favorLookup.ContainsKey(set.Faction)) {
                //Logger.Debug("Drawing dialog group content, but faction not found: " + favorLookup.Count);
                return 0;
            }
            int favorValue = favorLookup[set.Faction];
            //Logger.Debug("Drawing dialog group content, and value was " + favorValue);

            float top = y;

            y += 8;
            GameFont savedFont = Text.Font;
            Text.Font = GameFont.Tiny;
            string favorLabel = set.Faction.def.royalFavorLabel.CapitalizeFirst();
            Vector2 sizeLabel = Text.CalcSize(favorLabel);
            string textToMeasure = new string('5', set.MaxFavor.ToString().Length);
            Vector2 sizeValue = Text.CalcSize(textToMeasure);
            Text.Font = savedFont;
            float labelHeight = Math.Max(sizeLabel.y, sizeValue.y);

            float fieldPadding = 16;
            Rect labelRect = new Rect(fieldPadding, y, sizeLabel.x, labelHeight);
            Rect valueRect = new Rect(width - sizeValue.x - fieldPadding, y + 1, sizeValue.x, labelHeight);

            float sliderHeight = 8f;
            float sliderPadding = 8;
            float sliderWidth = valueRect.xMin - labelRect.xMax - sliderPadding * 2f;
            Rect sliderRect = new Rect(labelRect.xMax + sliderPadding, labelRect.yMin + labelRect.height * 0.5f - sliderHeight * 0.5f - 1,
                sliderWidth, sliderHeight);

            // Draw the certainty slider
            Text.Font = GameFont.Tiny;
            GUI.color = Style.ColorText;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(labelRect, favorLabel);
            Text.Anchor = TextAnchor.MiddleRight;
            Text.Font = GameFont.Tiny;
            Widgets.Label(valueRect, favorLookup[set.Faction].ToString());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            float value = GUI.HorizontalSlider(sliderRect, favorValue, 0, set.MaxFavor);
            y += labelRect.height;

            favorLookup[set.Faction] = Mathf.FloorToInt(value);

            y += 12;

            return y - top;
        }

        protected IEnumerable<ProviderTitles.Title> FactionTitlesWithNoneOption(ProviderTitles.TitleSet titleSet) {
            yield return new ProviderTitles.Title() {
                Faction = titleSet.Faction,
                Def = null
            };
            foreach (var title in titleSet.Titles) {
                yield return title;
            }
        }

        public void OpenOptionsDialog(CustomizedPawn pawn) {
            Dictionary<Faction, RoyalTitleDef> selectedTitles = new Dictionary<Faction, RoyalTitleDef>();
            Dictionary<Faction, int> favorLookup = new Dictionary<Faction, int>();
            foreach (var title in pawn.Pawn.royalty.AllTitlesForReading) {
                if (!selectedTitles.ContainsKey(title.faction)) {
                    selectedTitles.Add(title.faction, title.def);
                }
            }
            foreach (var faction in Find.World.factionManager.AllFactionsInViewOrder) {
                int favor = pawn.Pawn.royalty.GetFavor(faction);
                favorLookup.Add(faction, favor);
            }
            Find.WindowStack.Add(new DialogTitles<ProviderTitles.TitleSet, ProviderTitles.Title>() {
                Header = "EdB.PC.Dialog.Titles.Header".Translate(),
                Groups = () => ProviderTitles.Titles,
                GroupTitle = (g) => g.Faction.Name,
                DrawGroupHeader = DrawDialogGroupHeader,
                DrawGroupContent = (g, y, width) => DrawDialogGroupContent(favorLookup, g, y, width),
                OptionsFromGroup = (g) => FactionTitlesWithNoneOption(g),
                OptionTitle = (o) => {
                    return o.Def != null ? o.Def.GetLabelCapFor(pawn.Pawn) : "None".Translate().CapitalizeFirst().Resolve();
                },
                IsSelected = (o) => {
                    if (o.Def != null) {
                        RoyalTitleDef def = selectedTitles.GetOrDefault(o.Faction);
                        if (def != null && def == o.Def) {
                            return true;
                        }
                        return false;
                    }
                    else {
                        return selectedTitles.GetOrDefault(o.Faction) == null;
                    }
                },
                Select = (o) => {
                    if (selectedTitles.ContainsKey(o.Faction)) {
                        selectedTitles.Remove(o.Faction);
                    }
                    if (o.Def != null) {
                        selectedTitles.Add(o.Faction, o.Def);
                    }
                },
                Confirm = () => {
                    ConfigureTitles(pawn, selectedTitles, favorLookup);
                }
            });
        }

        protected void ConfigureTitles(CustomizedPawn pawn, Dictionary<Faction, RoyalTitleDef> selectedTitles, Dictionary<Faction, int> favorLookup) {
            List<RoyalTitle> toRemove = new List<RoyalTitle>();
            foreach (var title in pawn.Pawn.royalty.AllTitlesForReading) {
                RoyalTitleDef def = selectedTitles.GetOrDefault(title.faction);
                if (def == null) {
                    toRemove.Add(title);
                }
            }
            foreach (var title in toRemove) {
                pawn.Pawn.royalty.SetTitle(title.faction, null, false, false, false);
            }
            foreach (var pair in selectedTitles) {
                pawn.Pawn.royalty.SetTitle(pair.Key, pair.Value, false, false, false);
            }
            foreach (var pair in favorLookup) {
                pawn.Pawn.royalty.SetFavor(pair.Key, pair.Value, false);
            }
        }

        public override float Draw(float y) {
            float top = y;
            y += Margin.y;

            y += DrawHeader(y, Width, "EdB.PC.Panel.Titles.Header".Translate().Resolve());

            CustomizedPawn currentPawn = ViewState.CurrentPawn;
            int index = 0;
            Action clickAction = null;
            foreach (RoyalTitle title in currentPawn.Pawn.royalty.AllTitlesForReading) {
                if (title == null) {
                    continue;
                }
                if (index > 0) {
                    y += FieldPadding;
                }
                if (index >= fields.Count) {
                    fields.Add(new WidgetField() {
                        IconSizeFunc = () => new Vector2(22, 22)
                    });
                }

                RoyalTitle localTitle = title;
                RoyalTitleDef localTitleDef = title.def;
                int localIndex = index;

                WidgetField field = fields[index];
                Rect fieldRect = FieldRect.OffsetBy(0, y);
                field.Rect = fieldRect;
                Rect fieldClickRect = fieldRect;
                fieldClickRect.width -= 36;
                field.ClickRect = fieldClickRect;
                field.DrawIconFunc = (Rect rect) => GUI.DrawTexture(rect, title.faction.def.FactionIcon);
                field.Label = title.def.GetLabelCapFor(currentPawn.Pawn);
                field.TipAction = (rect) => {
                    if (Mouse.IsOver(rect)) {
                        MethodInfo method = ReflectionUtil.Method(typeof(CharacterCardUtility), "GetTitleTipString");
                        string tipString = method.Invoke(null, new object[] { currentPawn.Pawn, localTitle.faction, localTitle, currentPawn.Pawn.royalty.GetFavor(localTitle.faction) }) as string;
                        tipString = tipString.Replace("\n\n" + "ClickToLearnMore".Translate(), "");
                        TipSignal tip = new TipSignal(() => tipString, (int)y * 37);
                        TooltipHandler.TipRegion(rect, tip);
                    }
                };

                field.ClickAction = () => {
                    OpenOptionsDialog(currentPawn);
                };
                field.Draw();

                // Remove trait button.
                //Rect deleteRect = new Rect(field.Rect.xMax - 32, field.Rect.y + field.Rect.HalfHeight() - 6, 12, 12);
                //if (deleteRect.Contains(Event.current.mousePosition)) {
                //    GUI.color = Style.ColorButtonHighlight;
                //}
                //else {
                //    GUI.color = Style.ColorButton;
                //}
                //GUI.DrawTexture(deleteRect, Textures.TextureButtonDelete);
                //if (Widgets.ButtonInvisible(deleteRect, false)) {
                //    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                //    traitsToRemove.Add(trait);
                //}

                index++;

                y += FieldRect.height;
            }

            // If the index is still zero, then the pawn has no titles.  Draw the "none" label.
            if (index == 0) {
                GUI.color = Style.ColorText;
                Widgets.Label(FieldRect.InsetBy(6, 0).OffsetBy(0, y - 4), "EdB.PC.Panel.Titles.None".Translate());
                y += FieldRect.height - 4;
            }

            GUI.color = Color.white;

            // Fire any action that was triggered
            if (clickAction != null) {
                clickAction();
                clickAction = null;
            }

            // Add button.
            Rect addRect = new Rect(Width - 24, top + 12, 16, 16);
            Style.SetGUIColorForButton(addRect);
            GUI.DrawTexture(addRect, Textures.TextureButtonAdd);
            if (Widgets.ButtonInvisible(addRect, false)) {
                OpenOptionsDialog(currentPawn);
            }

            // Remove any traits that were marked for deletion
            if (itemsToRemove.Count > 0) {
                foreach (var item in itemsToRemove) {
                    //TraitRemoved(item);
                }
                itemsToRemove.Clear();
            }

            y += Margin.y;
            return y - top;
        }

    }
}
