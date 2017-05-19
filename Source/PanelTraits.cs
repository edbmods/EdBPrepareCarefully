using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class PanelTraits : PanelBase {
        public delegate void RandomizeTraitsHandler();
        public delegate void AddTraitHandler(Trait trait);
        public delegate void UpdateTraitHandler(int index, Trait trait);
        public delegate void RemoveTraitHandler(Trait trait);

        public event RandomizeTraitsHandler TraitsRandomized;
        public event AddTraitHandler TraitAdded;
        public event UpdateTraitHandler TraitUpdated;
        public event RemoveTraitHandler TraitRemoved;

        private ProviderTraits providerTraits = new ProviderTraits();
        protected ScrollViewVertical scrollView = new ScrollViewVertical();
        protected List<Field> fields = new List<Field>();
        protected List<Trait> traitsToRemove = new List<Trait>();

        protected Vector2 SizeField;
        protected Vector2 SizeTrait;
        protected Vector2 SizeFieldPadding = new Vector2(5, 6);
        protected Vector2 SizeTraitMargin = new Vector2(4, -6);
        protected Rect RectScrollFrame;
        protected Rect RectScrollView;

        public PanelTraits() {
        }
        public override string PanelHeader {
            get {
                return "EdB.PC.Panel.Traits.Title".Translate();
            }
        }

        public override void Resize(Rect rect) {
            base.Resize(rect);

            float panelPadding = 10;
            float fieldHeight = 28;
            SizeTrait = new Vector2(PanelRect.width - panelPadding * 2, fieldHeight + SizeFieldPadding.y * 2);
            SizeField = new Vector2(SizeTrait.x - SizeFieldPadding.x * 2, SizeTrait.y - SizeFieldPadding.y * 2);

            RectScrollFrame = new Rect(panelPadding, BodyRect.y,
                PanelRect.width - panelPadding * 2, BodyRect.height - panelPadding);
            RectScrollView = new Rect(0, 0, RectScrollFrame.width, RectScrollFrame.height);
        }

        protected override void DrawPanelContent(State state) {
            base.DrawPanelContent(state);
            CustomPawn customPawn = state.CurrentPawn;

            float cursor = 0;
            GUI.color = Color.white;
            GUI.BeginGroup(RectScrollFrame);
            try {
                if (customPawn.Traits.Count() == 0) {
                    GUI.color = Style.ColorText;
                    Widgets.Label(RectScrollView.InsetBy(6, 0, 0, 0), "EdB.PC.Panel.Traits.None".Translate());
                }
                GUI.color = Color.white;

                scrollView.Begin(RectScrollView);

                int index = 0;
                foreach (Trait trait in customPawn.Traits) {
                    if (index >= fields.Count) {
                        fields.Add(new Field());
                    }
                    Field field = fields[index];

                    GUI.color = Style.ColorPanelBackgroundItem;
                    Rect traitRect = new Rect(0, cursor, SizeTrait.x - (scrollView.ScrollbarsVisible ? 16 : 0), SizeTrait.y);
                    GUI.DrawTexture(traitRect, BaseContent.WhiteTex);
                    GUI.color = Color.white;

                    Rect fieldRect = new Rect(SizeFieldPadding.x, cursor + SizeFieldPadding.y, SizeField.x, SizeField.y);
                    if (scrollView.ScrollbarsVisible) {
                        fieldRect.width = fieldRect.width - 16;
                    }
                    field.Rect = fieldRect;
                    Rect fieldClickRect = fieldRect;
                    fieldClickRect.width = fieldClickRect.width - 36;
                    field.ClickRect = fieldClickRect;

                    if (trait != null) {
                        field.Label = trait.LabelCap;
                        field.Tip = trait.TipString(customPawn.Pawn);
                    }
                    else {
                        field.Label = null;
                        field.Tip = null;
                    }
                    Trait localTrait = trait;
                    int localIndex = index;
                    field.ClickAction = () => {
                        Trait originalTrait = localTrait;
                        Trait selectedTrait = originalTrait;
                        Dialog_Options<Trait> dialog = new Dialog_Options<Trait>(providerTraits.Traits) {
                            NameFunc = (Trait t) => {
                                return t.LabelCap;
                            },
                            DescriptionFunc = (Trait t) => {
                                return t.TipString(customPawn.Pawn);
                            },
                            SelectedFunc = (Trait t) => {
                                if ((selectedTrait == null || t == null) && selectedTrait != t) {
                                    return false;
                                }
                                return selectedTrait.def == t.def && selectedTrait.Label == t.Label;
                            },
                            SelectAction = (Trait t) => {
                                selectedTrait = t;
                            },
                            EnabledFunc = (Trait t) => {
                                if (t == null) {
                                    return originalTrait != null;
                                }
                                else if ((originalTrait == null || !originalTrait.Label.Equals(t.Label)) && customPawn.HasTrait(t)) {
                                    return false;
                                }
                                else {
                                    return true;
                                }
                            },
                            CloseAction = () => {
                                TraitUpdated(localIndex, selectedTrait);
                            },
                            NoneSelectedFunc = () => {
                                return selectedTrait == null;
                            },
                            SelectNoneAction = () => {
                                selectedTrait = null;
                            }
                        };
                        Find.WindowStack.Add(dialog);
                    };
                    field.PreviousAction = () => {
                        SelectPreviousTrait(customPawn, index);
                    };
                    field.NextAction = () => {
                        SelectNextTrait(customPawn, index);
                    };
                    field.Draw();

                    // Remove trait button.
                    Rect deleteRect = new Rect(field.Rect.xMax - 32, field.Rect.y + field.Rect.HalfHeight() - 6, 12, 12);
                    if (deleteRect.Contains(Event.current.mousePosition)) {
                        GUI.color = Style.ColorButtonHighlight;
                    }
                    else {
                        GUI.color = Style.ColorButton;
                    }
                    GUI.DrawTexture(deleteRect, Textures.TextureButtonDelete);
                    if (Widgets.ButtonInvisible(deleteRect, false)) {
                        SoundDefOf.TickTiny.PlayOneShotOnCamera();
                        traitsToRemove.Add(trait);
                    }

                    index++;

                    cursor += SizeTrait.y + SizeTraitMargin.y;
                }
                cursor -= SizeTraitMargin.y;
            }
            finally {
                scrollView.End(cursor);
                GUI.EndGroup();
            }

            GUI.color = Color.white;

            // Randomize traits button.
            Rect randomizeRect = new Rect(PanelRect.width - 32, 9, 22, 22);
            if (randomizeRect.Contains(Event.current.mousePosition)) {
                GUI.color = Style.ColorButtonHighlight;
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(randomizeRect, Textures.TextureButtonRandom);
            if (Widgets.ButtonInvisible(randomizeRect, false)) {
                SoundDefOf.TickLow.PlayOneShotOnCamera();
                TraitsRandomized();
            }

            // Add trait button.
            Rect addRect = new Rect(randomizeRect.x - 24, 12, 16, 16);
            Style.SetGUIColorForButton(addRect);
            bool addButtonEnabled = (state.CurrentPawn != null && state.CurrentPawn.Traits.Count() < 4);
            if (!addButtonEnabled) {
                GUI.color = Style.ColorButtonDisabled;
            }
            GUI.DrawTexture(addRect, Textures.TextureButtonAdd);
            if (addButtonEnabled && Widgets.ButtonInvisible(addRect, false)) {
                SoundDefOf.TickLow.PlayOneShotOnCamera();
                Trait selectedTrait = null;
                Dialog_Options<Trait> dialog = new Dialog_Options<Trait>(providerTraits.Traits) {
                    NameFunc = (Trait t) => {
                        return t.LabelCap;
                    },
                    DescriptionFunc = (Trait t) => {
                        return t.TipString(customPawn.Pawn);
                    },
                    SelectedFunc = (Trait t) => {
                        return selectedTrait == t;
                    },
                    SelectAction = (Trait t) => {
                        selectedTrait = t;
                    },
                    EnabledFunc = (Trait t) => {
                        if (customPawn.HasTrait(t)) {
                            return false;
                        }
                        else {
                            return true;
                        }
                    },
                    CloseAction = () => {
                        if (selectedTrait != null) {
                            TraitAdded(selectedTrait);
                        }
                    }
                };
                Find.WindowStack.Add(dialog);
            }

            if (traitsToRemove.Count > 0) {
                foreach (var trait in traitsToRemove) {
                    TraitRemoved(trait);
                }
                traitsToRemove.Clear();
            }
        }

        protected void SelectNextTrait(CustomPawn customPawn, int traitIndex) {
            Trait currentTrait = customPawn.GetTrait(traitIndex);
            int index = -1;
            if (currentTrait != null) {
                index = providerTraits.Traits.FindIndex((Trait t) => {
                    return t.Label.Equals(currentTrait.Label);
                });
            }
            int count = 0;
            do {
                index++;
                if (index >= providerTraits.Traits.Count) {
                    index = -1;
                }
                if (++count > providerTraits.Traits.Count + 1) {
                    index = -1;
                    break;
                }
            }
            while (index != -1 && customPawn.HasTrait(providerTraits.Traits[index]));

            Trait newTrait = null;
            if (index > -1) {
                newTrait = providerTraits.Traits[index];
            }
            TraitUpdated(traitIndex, newTrait);
        }

        protected void SelectPreviousTrait(CustomPawn customPawn, int traitIndex) {
            Trait currentTrait = customPawn.GetTrait(traitIndex);
            int index = -1;
            if (currentTrait != null) {
                index = providerTraits.Traits.FindIndex((Trait t) => {
                    return t.Label.Equals(currentTrait.Label);
                });
            }
            int count = 0;
            do {
                index--;
                if (index < -1) {
                    index = providerTraits.Traits.Count - 1;
                }
                if (++count > providerTraits.Traits.Count + 1) {
                    index = -1;
                    break;
                }
            }
            while (index != -1 && customPawn.HasTrait(providerTraits.Traits[index]));

            Trait newTrait = null;
            if (index > -1) {
                newTrait = providerTraits.Traits[index];
            }
            TraitUpdated(traitIndex, newTrait);
        }

        protected void ClearTrait(CustomPawn customPawn, int traitIndex) {
            TraitUpdated(traitIndex, null);
        }

        public void ScrollToTop() {
            scrollView.ScrollToTop();
        }
        public void ScrollToBottom() {
            scrollView.ScrollToBottom();
        }
    }
}
