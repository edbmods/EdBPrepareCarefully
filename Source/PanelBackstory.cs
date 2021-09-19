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
    public class PanelBackstory : PanelModule {
        protected Rect LabelRect { get; set; }
        protected Rect FieldRect { get; set; }
        protected Field FieldChildhood = new Field();
        protected Field FieldAdulthood = new Field();
        protected ProviderBackstories providerBackstories = PrepareCarefully.Instance.Providers.Backstories;
        protected List<Filter<Backstory>> availableFilters = new List<Filter<Backstory>>();
        protected List<Filter<Backstory>> activeFilters = new List<Filter<Backstory>>();

        public delegate void UpdateBackstoryHandler(BackstorySlot slot, Backstory backstory);
        public delegate void RandomizeBackstoriesHandler();

        public event UpdateBackstoryHandler BackstoryUpdated;
        public event RandomizeBackstoriesHandler BackstoriesRandomized;

        public PanelBackstory() {
            availableFilters.Add(new FilterBackstoryMatchesFaction());
            availableFilters.Add(new FilterBackstoryNoDisabledWorkTypes());
            availableFilters.Add(new FilterBackstoryNoPenalties());
            foreach (var s in DefDatabase<SkillDef>.AllDefs) {
                availableFilters.Add(new FilterBackstorySkillAdjustment(s, 1));
                availableFilters.Add(new FilterBackstorySkillAdjustment(s, 3));
                availableFilters.Add(new FilterBackstorySkillAdjustment(s, 5));
            }
        }
        public override void Resize(float width) {
            base.Resize(width);
            float panelPadding = 12;
            float fieldPadding = 8;

            // The width of the label is the widest of the childhood/adulthood text
            GameFont savedFont = Text.Font;
            Text.Font = GameFont.Small;
            Vector2 sizeChildhood = Text.CalcSize("Childhood".Translate());
            Vector2 sizeAdulthood = Text.CalcSize("Adulthood".Translate());
            Text.Font = savedFont;
            float labelWidth = Mathf.Max(sizeChildhood.x, sizeAdulthood.x);

            LabelRect = new Rect(panelPadding, 0, labelWidth, Style.FieldHeight);
            FieldRect = new Rect(LabelRect.xMax + fieldPadding, 0, width - LabelRect.xMax - fieldPadding * 2, Style.FieldHeight);
        }

        public float Measure() {
            return 0;
        }

        protected float DrawChildhood(CustomPawn pawn, float y, float width) {
            // Draw the label
            Text.Font = GameFont.Small;
            GUI.color = Style.ColorText;
            Text.Anchor = TextAnchor.MiddleCenter;
            Rect labelRect = LabelRect.OffsetBy(0, y);
            Widgets.Label(labelRect, "Childhood".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // Draw the field
            FieldChildhood.Rect = FieldRect.OffsetBy(0, y);
            if (pawn.Childhood != null) {
                FieldChildhood.Label = pawn.Childhood.TitleCapFor(pawn.Gender);
            }
            else {
                FieldChildhood.Label = null;
            }
            FieldChildhood.Tip = pawn.Childhood.CheckedDescriptionFor(pawn.Pawn);
            FieldChildhood.ClickAction = () => {
                ShowBackstoryDialog(pawn, BackstorySlot.Childhood);
            };
            FieldChildhood.PreviousAction = () => {
                NextBackstory(pawn, BackstorySlot.Childhood, -1);
            };
            FieldChildhood.NextAction = () => {
                NextBackstory(pawn, BackstorySlot.Childhood, 1);
            };
            FieldChildhood.Draw();

            return FieldRect.height;
        }

        protected float DrawAdulthood(CustomPawn pawn, float y, float width) {
            Text.Font = GameFont.Small;
            GUI.color = Style.ColorText;
            Text.Anchor = TextAnchor.MiddleCenter;
            if (!pawn.HasAdulthoodBackstory) {
                GUI.color = Style.ColorControlDisabled;
            }
            Rect labelRect = LabelRect.OffsetBy(0, y);
            Widgets.Label(labelRect, "Adulthood".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // Draw the field
            FieldAdulthood.Rect = FieldRect.OffsetBy(0, y);
            FieldAdulthood.Enabled = pawn.HasAdulthoodBackstory;
            if (FieldAdulthood.Enabled) {
                FieldAdulthood.Label = pawn.Adulthood.TitleCapFor(pawn.Gender);
                FieldAdulthood.Tip = pawn.Adulthood.CheckedDescriptionFor(pawn.Pawn);
                FieldAdulthood.ClickAction = () => {
                    ShowBackstoryDialog(pawn, BackstorySlot.Adulthood);
                };
                FieldAdulthood.PreviousAction = () => {
                    NextBackstory(pawn, BackstorySlot.Adulthood, -1);
                };
                FieldAdulthood.NextAction = () => {
                    NextBackstory(pawn, BackstorySlot.Adulthood, 1);
                };
            }
            else {
                FieldAdulthood.Label = null;
                FieldAdulthood.Tip = null;
                FieldAdulthood.ClickAction = null;
                FieldAdulthood.PreviousAction = () => { };
                FieldAdulthood.NextAction = () => { };
            }
            FieldAdulthood.Draw();

            return FieldRect.height;
        }

        public void DrawRandomizeButton(float y, float width) {
            // Randomize button.
            Rect randomizeRect = new Rect(width - 32, y + 9, 22, 22);
            if (randomizeRect.Contains(Event.current.mousePosition)) {
                GUI.color = Style.ColorButtonHighlight;
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(randomizeRect, Textures.TextureButtonRandom);
            if (Widgets.ButtonInvisible(randomizeRect, false)) {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                BackstoriesRandomized();
            }
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        // Deprecated
        // Leave here for compatibility with any patches that used the old method for drawing
        protected override void DrawPanelContent(State state) {
        }

        public override float Draw(State state, float y) {
            float top = y;
            y += Margin.y;

            CustomPawn pawn = state.CurrentPawn;
            DrawRandomizeButton(y, Width);
            y += DrawHeader(y, Width, "Backstory".Translate().Resolve());
            y += DrawChildhood(pawn, y, Width);
            y += 6;
            y += DrawAdulthood(pawn, y, Width);

            y += Margin.y;

            // For backwards compatibility with any patches that used the old method for drawing
            GUI.BeginGroup(new Rect(0, top, Width, y - top));
            try {
                DrawPanelContent(state);
            }
            finally {
                GUI.EndGroup();
            }

            return y - top;
        }

        protected void ShowBackstoryDialog(CustomPawn customPawn, BackstorySlot slot) {
            Backstory originalBackstory = (slot == BackstorySlot.Childhood) ? customPawn.Childhood : customPawn.Adulthood;
            Backstory selectedBackstory = originalBackstory;
            Filter<Backstory> filterToRemove = null;
            bool filterListDirtyFlag = true;
            List<Backstory> fullOptionsList = slot == BackstorySlot.Childhood ?
                    this.providerBackstories.AllChildhookBackstories : this.providerBackstories.AllAdulthookBackstories;
            List<Backstory> filteredBackstories = new List<Backstory>(fullOptionsList.Count);
            Dialog_Options<Backstory> dialog = new Dialog_Options<Backstory>(filteredBackstories) {
                NameFunc = (Backstory backstory) => {
                    return backstory.TitleCapFor(customPawn.Gender);
                },
                DescriptionFunc = (Backstory backstory) => {
                    return backstory.CheckedDescriptionFor(customPawn.Pawn);
                },
                SelectedFunc = (Backstory backstory) => {
                    return selectedBackstory == backstory;
                },
                SelectAction = (Backstory backstory) => {
                    selectedBackstory = backstory;
                },
                CloseAction = () => {
                    if (slot == BackstorySlot.Childhood) {
                        BackstoryUpdated(BackstorySlot.Childhood, selectedBackstory);
                    }
                    else {
                        BackstoryUpdated(BackstorySlot.Adulthood, selectedBackstory);
                    }
                }
            };
            dialog.DrawHeader = (Rect rect) => {
                if (filterToRemove != null) {
                    activeFilters.Remove(filterToRemove);
                    filterToRemove = null;
                    filterListDirtyFlag = true;
                }
                if (filterListDirtyFlag) {
                    filteredBackstories.Clear();
                    filteredBackstories.AddRange(fullOptionsList.Where(p => { foreach (var f in activeFilters) if (f.FilterFunction(p) == false) return (false); return (true); }));
                    filterListDirtyFlag = false;
                    dialog.ScrollToTop();
                }

                float filterHeight = 18;
                float filterPadding = 4;
                float maxWidth = rect.width - 32;
                Vector2 cursor = new Vector2(0, 0);

                string addFilterLabel = "EdB.PC.Dialog.Backstory.Filter.Add".Translate();
                float width = Text.CalcSize(addFilterLabel).x;
                Rect addFilterRect = new Rect(rect.x, rect.y, width + 30, filterHeight);
                Widgets.DrawAtlas(addFilterRect, Textures.TextureFilterAtlas1);
                Text.Font = GameFont.Tiny;
                if (addFilterRect.Contains(Event.current.mousePosition)) {
                    GUI.color = Style.ColorButtonHighlight;
                }
                else {
                    GUI.color = Style.ColorText;
                }
                Widgets.Label(addFilterRect.InsetBy(10, 0, 20, 0).OffsetBy(0, 1), addFilterLabel);
                GUI.DrawTexture(new Rect(addFilterRect.xMax - 20, addFilterRect.y + 6, 11, 8), Textures.TextureDropdownIndicator);

                if (Widgets.ButtonInvisible(addFilterRect, true)) {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    foreach (var filter in availableFilters) {
                        if (activeFilters.FirstOrDefault((f) => {
                            if (f == filter || f.ConflictsWith(filter)) {
                                return true;
                            }
                            return false;
                        }) == null) {
                            list.Add(new FloatMenuOption(filter.LabelFull, () => {
                                activeFilters.Add(filter);
                                filterListDirtyFlag = true;
                            }, MenuOptionPriority.Default, null, null, 0, null, null));
                        }
                    }
                    Find.WindowStack.Add(new FloatMenu(list, null, false));
                }

                cursor.x += addFilterRect.width + filterPadding;
                Text.Font = GameFont.Tiny;
                foreach (var filter in activeFilters) {
                    GUI.color = Style.ColorText;
                    float labelWidth = Text.CalcSize(filter.LabelShort).x;
                    if (cursor.x + labelWidth > maxWidth) {
                        cursor.x = 0;
                        cursor.y += filterHeight + filterPadding;
                    }
                    Rect filterRect = new Rect(cursor.x, cursor.y, labelWidth + 30, filterHeight);
                    Widgets.DrawAtlas(filterRect, Textures.TextureFilterAtlas2);
                    Rect closeButtonRect = new Rect(filterRect.xMax - 15, filterRect.y + 5, 9, 9);
                    if (filterRect.Contains(Event.current.mousePosition)) {
                        GUI.color = Style.ColorButtonHighlight;
                    }
                    else {
                        GUI.color = Style.ColorText;
                    }
                    Widgets.Label(filterRect.InsetBy(10, 0, 20, 0).OffsetBy(0, 1), filter.LabelShort);
                    GUI.DrawTexture(closeButtonRect, Textures.TextureButtonCloseSmall);
                    if (Widgets.ButtonInvisible(filterRect)) {
                        filterToRemove = filter;
                        filterListDirtyFlag = true;
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    }
                    cursor.x += filterRect.width + filterPadding;
                }

                Text.Font = GameFont.Small;
                GUI.color = Color.white;

                return cursor.y + filterHeight + 4;
            };
            Find.WindowStack.Add(dialog);
        }

        protected void NextBackstory(CustomPawn pawn, BackstorySlot slot, int direction) {
            Backstory backstory;
            List<Backstory> backstories;
            PopulateBackstoriesFromSlot(pawn, slot, out backstories, out backstory);

            int currentIndex = FindBackstoryIndex(pawn, slot);
            currentIndex += direction;
            if (currentIndex >= backstories.Count) {
                currentIndex = 0;
            }
            else if (currentIndex < 0) {
                currentIndex = backstories.Count - 1;
            }
            BackstoryUpdated(slot, backstories[currentIndex]);
        }

        protected int FindBackstoryIndex(CustomPawn pawn, BackstorySlot slot) {
            Backstory backstory;
            List<Backstory> backstories;
            PopulateBackstoriesFromSlot(pawn, slot, out backstories, out backstory);
            return backstories.IndexOf(backstory);
        }

        protected void PopulateBackstoriesFromSlot(CustomPawn pawn, BackstorySlot slot, out List<Backstory> backstories,
                out Backstory backstory) {
            backstory = (slot == BackstorySlot.Childhood) ? pawn.Childhood : pawn.Adulthood;
            backstories = (slot == BackstorySlot.Childhood) ? providerBackstories.GetChildhoodBackstoriesForPawn(pawn)
                : providerBackstories.GetAdulthoodBackstoriesForPawn(pawn);
        }
    }
}
