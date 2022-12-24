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
    public class PanelTraits : PanelModule {
        public Rect FieldRect;
        public static readonly float FieldPadding = 6;

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
        protected HashSet<TraitDef> disallowedTraitDefs = new HashSet<TraitDef>();
        protected Dictionary<Trait, string> conflictingTraitList = new Dictionary<Trait, string>();
        protected TipCache tipCache = new TipCache();

        public override void Resize(float width) {
            base.Resize(width);
            FieldRect = new Rect(FieldPadding, 0, width - FieldPadding * 2, Style.FieldHeight);
        }

        public float Measure() {
            return 0;
        }

        public override float Draw(State state, float y) {
            float top = y;
            y += Margin.y;

            y += DrawHeader(y, Width, "Traits".Translate().Resolve());

            CustomPawn currentPawn = state.CurrentPawn;
            int index = 0;
            Action clickAction = null;
            foreach (Trait trait in currentPawn.Traits) {
                if (index > 0) {
                    y += FieldPadding;
                }
                if (index >= fields.Count) {
                    fields.Add(new Field());
                }
                Field field = fields[index];

                Rect fieldRect = FieldRect.OffsetBy(0, y);
                field.Rect = fieldRect;
                Rect fieldClickRect = fieldRect;
                fieldClickRect.width -= 36;
                field.ClickRect = fieldClickRect;

                if (trait != null) {
                    field.Label = trait.LabelCap;
                    field.Tip = GetTraitTip(trait, currentPawn);
                    field.Color = Style.ColorText;
                    if (trait.Suppressed) {
                        field.Color = ColoredText.SubtleGrayColor;
                    }
                    else if (trait.sourceGene != null) {
                        field.Color = ColoredText.GeneColor;
                    }
                }
                else {
                    field.Label = null;
                    field.Tip = null;
                    field.Color = Style.ColorText;
                }
                Trait localTrait = trait;
                int localIndex = index;
                field.ClickAction = () => {
                    Trait originalTrait = localTrait;
                    Trait selectedTrait = originalTrait;
                    ComputeDisallowedTraits(currentPawn, originalTrait);
                    Dialog_Options<Trait> dialog = new Dialog_Options<Trait>(providerTraits.Traits) {
                        NameFunc = (Trait t) => {
                            return t.LabelCap;
                        },
                        DescriptionFunc = (Trait t) => {
                            return GetTraitTip(t, currentPawn);
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
                            return !disallowedTraitDefs.Contains(t.def);
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
                    var capturedIndex = index;
                    clickAction = () => {
                        SelectPreviousTrait(currentPawn, capturedIndex);
                    };
                };
                field.NextAction = () => {
                    var capturedIndex = index;
                    clickAction = () => {
                        SelectNextTrait(currentPawn, capturedIndex);
                    };
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
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                    traitsToRemove.Add(trait);
                }

                index++;

                y += FieldRect.height;
            }

            tipCache.MakeReady();

            // If the index is still zero, then the pawn has no traits.  Draw the "none" label.
            if (index == 0) {
                GUI.color = Style.ColorText;
                string message = "EdB.PC.Panel.Traits.None".Translate();
                if (state.CurrentPawn.Pawn.DevelopmentalStage.Baby()) {
                    message = "TraitsDevelopLaterBaby".Translate();
                }
                Widgets.Label(FieldRect.InsetBy(6, 0).OffsetBy(0, y - 4), message);
                y += FieldRect.height - 4;
            }

            GUI.color = Color.white;

            // Fire any action that was triggered
            if (clickAction != null) {
                clickAction();
                clickAction = null;
            }

            // Don't show add or randomize buttons if the pawn is a baby
            if (!state.CurrentPawn.Pawn.DevelopmentalStage.Baby()) {
                // Randomize traits button.
                Rect randomizeRect = new Rect(Width - 32, top + 9, 22, 22);
                if (randomizeRect.Contains(Event.current.mousePosition)) {
                    GUI.color = Style.ColorButtonHighlight;
                }
                else {
                    GUI.color = Style.ColorButton;
                }
                GUI.DrawTexture(randomizeRect, Textures.TextureButtonRandom);
                if (Widgets.ButtonInvisible(randomizeRect, false)) {
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                    tipCache.Invalidate();
                    TraitsRandomized();
                }

                // Add trait button.
                Rect addRect = new Rect(randomizeRect.x - 24, top + 12, 16, 16);
                Style.SetGUIColorForButton(addRect);
                int traitCount = state.CurrentPawn.Traits.Count();
                bool addButtonEnabled = (state.CurrentPawn != null && traitCount < Constraints.MaxTraits);
                if (!addButtonEnabled) {
                    GUI.color = Style.ColorButtonDisabled;
                }
                GUI.DrawTexture(addRect, Textures.TextureButtonAdd);
                if (addButtonEnabled && Widgets.ButtonInvisible(addRect, false)) {
                    ComputeDisallowedTraits(currentPawn, null);
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                    Trait selectedTrait = null;
                    Dialog_Options<Trait> dialog = new Dialog_Options<Trait>(providerTraits.Traits) {
                        ConfirmButtonLabel = "EdB.PC.Common.Add".Translate(),
                        NameFunc = (Trait t) => {
                            return t.LabelCap;
                        },
                        DescriptionFunc = (Trait t) => {
                            return GetTraitTip(t, state.CurrentPawn);
                        },
                        SelectedFunc = (Trait t) => {
                            return selectedTrait == t;
                        },
                        SelectAction = (Trait t) => {
                            selectedTrait = t;
                        },
                        EnabledFunc = (Trait t) => {
                            return !disallowedTraitDefs.Contains(t.def);
                        },
                        CloseAction = () => {
                            if (selectedTrait != null) {
                                TraitAdded(selectedTrait);
                                tipCache.Invalidate();
                            }
                        }
                    };
                    Find.WindowStack.Add(dialog);
                }
            }

            // Remove any traits that were marked for deletion
            if (traitsToRemove.Count > 0) {
                foreach (var trait in traitsToRemove) {
                    TraitRemoved(trait);
                }
                traitsToRemove.Clear();
                tipCache.Invalidate();
            }

            y += Margin.y;
            return y - top;
        }


        protected string GetTraitTip(Trait trait, CustomPawn pawn) {
            if (!tipCache.Ready || !tipCache.Lookup.ContainsKey(trait)) {
                string value = GenerateTraitTip(trait, pawn);
                tipCache.Lookup.Add(trait, value);
                return value;
            }
            else {
                return tipCache.Lookup[trait];
            }
        }

        protected string GenerateTraitTip(Trait trait, CustomPawn pawn) {
            try {
                string baseTip = trait.TipString(pawn.Pawn);
                string conflictingNames = null;
                if (!conflictingTraitList.TryGetValue(trait, out conflictingNames)) {
                    List<Trait> conflictingTraits = providerTraits.Traits.Where((Trait t) => {
                        return trait.def.conflictingTraits.Contains(t.def) || (t.def == trait.def && t.Label != trait.Label);
                    }).ToList();
                    if (conflictingTraits.Count == 0) {
                        conflictingTraitList.Add(trait, null);
                    }
                    else {
                        conflictingNames = "";
                        if (conflictingTraits.Count == 1) {
                            conflictingNames = "EdB.PC.Panel.Traits.Tip.Conflict.List.1".Translate(conflictingTraits[0].LabelCap);
                        }
                        else if (conflictingTraits.Count == 2) {
                            conflictingNames = "EdB.PC.Panel.Traits.Tip.Conflict.List.2".Translate(conflictingTraits[0].LabelCap, conflictingTraits[1].LabelCap);
                        }
                        else {
                            int c = conflictingTraits.Count;
                            conflictingNames = "EdB.PC.Panel.Traits.Tip.Conflict.List.Last".Translate(conflictingTraits[c - 2].LabelCap, conflictingTraits[c - 1].LabelCap);
                            for (int i = c - 3; i >= 0; i--) {
                                conflictingNames = "EdB.PC.Panel.Traits.Tip.Conflict.List.Many".Translate(conflictingTraits[i].LabelCap, conflictingNames);
                            }
                        }
                        conflictingTraitList.Add(trait, conflictingNames);
                    }
                }
                if (conflictingNames == null) {
                    return baseTip;
                }
                else {
                    return "EdB.PC.Panel.Traits.Tip.Conflict".Translate(baseTip, conflictingNames).Resolve();
                }
            }
            catch (Exception e) {
                Logger.Warning("There was an error when trying to generate a mouseover tip for trait {" + (trait?.LabelCap ?? "null") + "}\n" + e);
                return null;
            }
        }

        protected void ComputeDisallowedTraits(CustomPawn customPawn, Trait traitToReplace) {
            disallowedTraitDefs.Clear();
            foreach (Trait t in customPawn.Traits) {
                if (t == traitToReplace) {
                    continue;
                }
                disallowedTraitDefs.Add(t.def);
                if (t.def.conflictingTraits != null) {
                    foreach (var c in t.def.conflictingTraits) {
                        disallowedTraitDefs.Add(c);
                    }
                }
            }
        }

        protected void SelectNextTrait(CustomPawn customPawn, int traitIndex) {
            Trait currentTrait = customPawn.GetTrait(traitIndex);
            ComputeDisallowedTraits(customPawn, currentTrait);
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
                    index = 0;
                }
                if (++count > providerTraits.Traits.Count + 1) {
                    index = -1;
                    break;
                }
            }
            while (index != -1 && (customPawn.HasTrait(providerTraits.Traits[index]) || disallowedTraitDefs.Contains(providerTraits.Traits[index].def)));

            Trait newTrait = null;
            if (index > -1) {
                newTrait = providerTraits.Traits[index];
            }
            TraitUpdated(traitIndex, newTrait);
        }

        protected void SelectPreviousTrait(CustomPawn customPawn, int traitIndex) {
            Trait currentTrait = customPawn.GetTrait(traitIndex);
            ComputeDisallowedTraits(customPawn, currentTrait);
            int index = -1;
            if (currentTrait != null) {
                index = providerTraits.Traits.FindIndex((Trait t) => {
                    return t.Label.Equals(currentTrait.Label);
                });
            }
            int count = 0;
            do {
                index--;
                if (index < 0) {
                    index = providerTraits.Traits.Count - 1;
                }
                if (++count > providerTraits.Traits.Count + 1) {
                    index = -1;
                    break;
                }
            }
            while (index != -1 && (customPawn.HasTrait(providerTraits.Traits[index]) || disallowedTraitDefs.Contains(providerTraits.Traits[index].def)));

            Trait newTrait = null;
            if (index > -1) {
                newTrait = providerTraits.Traits[index];
            }
            TraitUpdated(traitIndex, newTrait);
        }

        protected void ClearTrait(CustomPawn customPawn, int traitIndex) {
            TraitUpdated(traitIndex, null);
            tipCache.Invalidate();
        }

        public class TipCache {
            public Dictionary<Trait, string> Lookup = new Dictionary<Trait, string>();
            private CustomPawn pawn = null;
            private bool ready = false;
            public void CheckPawn(CustomPawn pawn) {
                if (this.pawn != pawn) {
                    this.pawn = pawn;
                    Invalidate();
                }
            }
            public void Invalidate() {
                this.ready = false;
                Lookup.Clear();
            }
            public void MakeReady() {
                this.ready = true;
            }
            public bool Ready {
                get {
                    return ready;
                }
            }
        }
    }
}
