using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class PanelBackstory : PanelModule {
        protected Rect LabelRect { get; set; }
        protected Rect FieldRect { get; set; }
        protected Rect FavoriteColorLabelRect { get; set; }
        protected Vector2 FavoriteColorRectSize { get; set; }
        protected Rect FavoriteColorRect { get; set; }

        protected WidgetField FieldChildhood = new WidgetField();
        protected WidgetField FieldAdulthood = new WidgetField();
        protected List<Filter<BackstoryDef>> availableFilters = new List<Filter<BackstoryDef>>();
        protected List<Filter<BackstoryDef>> activeFilters = new List<Filter<BackstoryDef>>();

        public delegate void UpdateBackstoryHandler(BackstorySlot slot, BackstoryDef backstory);
        public delegate void RandomizeButtonClickedHandler();
        public delegate void UpdateFavoriteColorHandler(ColorDef colorDef);

        public event UpdateBackstoryHandler BackstoryUpdated;
        public event RandomizeButtonClickedHandler RandomizeButtonClicked;
        public event UpdateFavoriteColorHandler FavoriteColorUpdated;

        public ModState State { get; set; }
        public ViewState ViewState { get; set; }
        public ProviderBackstories ProviderBackstories { get; set; }

        public PanelBackstory() {

        }
        public void PostConstruct() {
            availableFilters.Add(new FilterBackstoryMatchesFaction() {
                ViewState = ViewState,
                ProviderBackstories = ProviderBackstories,
            });
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

            Vector2 favoriteColorSize = Text.CalcSize("EdB.PC.Panel.Backstory.FavoriteColorLabel".Translate());
            FavoriteColorRectSize = new Vector2(width - fieldPadding - fieldPadding - 23 - favoriteColorSize.x, favoriteColorSize.y - 4);
            FavoriteColorLabelRect = new Rect(panelPadding, 0, favoriteColorSize.x, Style.FieldHeight);
            FavoriteColorRect = new Rect(FavoriteColorLabelRect.xMax + fieldPadding, FavoriteColorLabelRect.HalfHeight() - FavoriteColorRectSize.HalfY(), FavoriteColorRectSize.x, FavoriteColorRectSize.y);
        }

        public float Measure() {
            return 0;
        }

        protected float DrawChildhood(CustomizedPawn customizedPawn, float y, float width) {
            // Draw the label
            Text.Font = GameFont.Small;
            GUI.color = Style.ColorText;
            Text.Anchor = TextAnchor.MiddleCenter;
            Rect labelRect = LabelRect.OffsetBy(0, y);
            Widgets.Label(labelRect, "Childhood".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            Pawn pawn = customizedPawn.Pawn;
            BackstoryDef backstory = pawn.story.Childhood;
            Gender gender = pawn.gender;

            // Draw the field
            FieldChildhood.Rect = FieldRect.OffsetBy(0, y);
            if (backstory != null) {
                FieldChildhood.Label = backstory.TitleCapFor(gender);
            }
            else {
                FieldChildhood.Label = null;
            }
            FieldChildhood.Tip = backstory.CheckedDescriptionFor(pawn);
            if (UtilityPawns.IsNewborn(pawn) || UtilityPawns.IsJuvenile(pawn)) {
                FieldChildhood.ClickAction = null;
                FieldChildhood.PreviousAction = null;
                FieldChildhood.NextAction = null;
                FieldChildhood.NextPreviousButtonsHidden = true;
            }
            else {
                FieldChildhood.NextPreviousButtonsHidden = false;
                FieldChildhood.ClickAction = () => {
                    ShowBackstoryDialog(customizedPawn, BackstorySlot.Childhood);
                };
                FieldChildhood.PreviousAction = () => {
                    NextBackstory(customizedPawn, BackstorySlot.Childhood, -1);
                };
                FieldChildhood.NextAction = () => {
                    NextBackstory(customizedPawn, BackstorySlot.Childhood, 1);
                };
            }

            FieldChildhood.Draw();

            return FieldRect.height;
        }

        protected float DrawAdulthood(CustomizedPawn customizedPawn, float y, float width) {
            Pawn pawn = customizedPawn.Pawn;
            Text.Font = GameFont.Small;
            GUI.color = Style.ColorText;
            Text.Anchor = TextAnchor.MiddleCenter;
            BackstoryDef backstory = pawn.story.Adulthood;
            bool hasAdulthoodBackstory = (pawn.story.Adulthood != null);
            if (!hasAdulthoodBackstory) {
                GUI.color = Style.ColorControlDisabled;
            }
            Rect labelRect = LabelRect.OffsetBy(0, y);
            Widgets.Label(labelRect, "Adulthood".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // Draw the field
            FieldAdulthood.Rect = FieldRect.OffsetBy(0, y);
            FieldAdulthood.Enabled = hasAdulthoodBackstory;
            if (FieldAdulthood.Enabled) {
                FieldAdulthood.Label = backstory.TitleCapFor(pawn.gender);
                FieldAdulthood.Tip = backstory.CheckedDescriptionFor(pawn);
                FieldAdulthood.ClickAction = () => {
                    ShowBackstoryDialog(customizedPawn, BackstorySlot.Adulthood);
                };
                FieldAdulthood.PreviousAction = () => {
                    NextBackstory(customizedPawn, BackstorySlot.Adulthood, -1);
                };
                FieldAdulthood.NextAction = () => {
                    NextBackstory(customizedPawn, BackstorySlot.Adulthood, 1);
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
                RandomizeButtonClicked?.Invoke();
            }
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public override float Draw(float y) {
            float top = y;
            y += Margin.y;

            CustomizedPawn customizedPawn = ViewState.CurrentPawn;
            if (customizedPawn.Pawn.DevelopmentalStage == DevelopmentalStage.Adult) {
                DrawRandomizeButton(y, Width);
            }
            y += DrawHeader(y, Width, "Backstory".Translate().Resolve());
            y += DrawChildhood(customizedPawn, y, Width);
            y += 6;
            y += DrawAdulthood(customizedPawn, y, Width);


            if (ModsConfig.IdeologyActive && customizedPawn.Pawn.story.favoriteColor != null && !UtilityPawns.IsBaby(customizedPawn.Pawn)) {
                y += 8;
                Text.Font = GameFont.Small;
                GUI.color = Style.ColorText;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(FavoriteColorLabelRect.OffsetBy(0, y), "EdB.PC.Panel.Backstory.FavoriteColorLabel".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;

                Rect favoriteColorRect = FavoriteColorRect.OffsetBy(0, y);
                ColorDef favoriteColor = customizedPawn.Pawn.story.favoriteColor;
                
                if (favoriteColorRect.Contains(Event.current.mousePosition)) {
                    Widgets.DrawAtlas(favoriteColorRect, Textures.TextureFieldAtlasWhite);
                    GUI.color = favoriteColor.color;
                    Widgets.DrawAtlas(favoriteColorRect.InsetBy(1), Textures.TextureFieldAtlasWhite);
                }
                else {
                    GUI.color = favoriteColor.color;
                    Widgets.DrawAtlas(favoriteColorRect, Textures.TextureFieldAtlasWhite);
                }
                GUI.color = Color.white;
                if (Widgets.ButtonInvisible(favoriteColorRect, false)) {
                    var dialog = new DialogFavoriteColor(favoriteColor) {
                        ConfirmAction = (ColorDef colorDef) => FavoriteColorUpdated(colorDef),
                        CurrentPawn = ViewState.CurrentPawn
                    };
                    Find.WindowStack.Add(dialog);
                }
                y += FavoriteColorRect.height - 4;
            }
            y += 6;

            y += Margin.y;

            return y - top;
        }

        protected void ShowBackstoryDialog(CustomizedPawn customizedPawn, BackstorySlot slot) {
            Pawn pawn = customizedPawn.Pawn;
            BackstoryDef originalBackstory = (slot == BackstorySlot.Childhood) ? pawn.story.Childhood : pawn.story.Adulthood;
            BackstoryDef selectedBackstory = originalBackstory;
            Filter<BackstoryDef> filterToRemove = null;
            bool filterListDirtyFlag = true;
            List<BackstoryDef> fullOptionsList = slot == BackstorySlot.Childhood ?
                    this.ProviderBackstories.AllChildhookBackstories : this.ProviderBackstories.AllAdulthookBackstories;
            List<BackstoryDef> filteredBackstories = new List<BackstoryDef>(fullOptionsList.Count);
            DialogOptions<BackstoryDef> dialog = new DialogOptions<BackstoryDef>(filteredBackstories) {
                NameFunc = (BackstoryDef backstory) => {
                    return backstory.TitleCapFor(pawn.gender);
                },
                DescriptionFunc = (BackstoryDef backstory) => {
                    return backstory.CheckedDescriptionFor(pawn);
                },
                SelectedFunc = (BackstoryDef backstory) => {
                    return selectedBackstory == backstory;
                },
                SelectAction = (BackstoryDef backstory) => {
                    selectedBackstory = backstory;
                },
                CloseAction = () => {
                    if (slot == BackstorySlot.Childhood) {
                        BackstoryUpdated?.Invoke(BackstorySlot.Childhood, selectedBackstory);
                    }
                    else {
                        BackstoryUpdated?.Invoke(BackstorySlot.Adulthood, selectedBackstory);
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

        protected void NextBackstory(CustomizedPawn pawn, BackstorySlot slot, int direction) {
            BackstoryDef backstory;
            List<BackstoryDef> backstories;
            PopulateBackstoriesFromSlot(pawn, slot, out backstories, out backstory);

            int currentIndex = FindBackstoryIndex(pawn, slot);
            currentIndex += direction;
            if (currentIndex >= backstories.Count) {
                currentIndex = 0;
            }
            else if (currentIndex < 0) {
                currentIndex = backstories.Count - 1;
            }
            BackstoryUpdated?.Invoke(slot, backstories[currentIndex]);
        }

        protected int FindBackstoryIndex(CustomizedPawn pawn, BackstorySlot slot) {
            BackstoryDef backstory;
            List<BackstoryDef> backstories;
            PopulateBackstoriesFromSlot(pawn, slot, out backstories, out backstory);
            return backstories.IndexOf(backstory);
        }

        protected void PopulateBackstoriesFromSlot(CustomizedPawn customizedPawn, BackstorySlot slot, out List<BackstoryDef> backstories,
                out BackstoryDef backstory) {
            Pawn pawn = customizedPawn.Pawn;
            backstory = (slot == BackstorySlot.Childhood) ? pawn.story.Childhood : pawn.story.Adulthood;
            backstories = (slot == BackstorySlot.Childhood) ? ProviderBackstories.GetChildhoodBackstoriesForPawn(pawn)
                : ProviderBackstories.GetAdulthoodBackstoriesForPawn(pawn);
        }
    }
}
