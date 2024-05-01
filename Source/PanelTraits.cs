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
        public delegate void SetTraitsHandler(IEnumerable<Trait> traits);

        public event RandomizeTraitsHandler TraitsRandomized;
        public event AddTraitHandler TraitAdded;
        public event UpdateTraitHandler TraitUpdated;
        public event RemoveTraitHandler TraitRemoved;
        public event SetTraitsHandler TraitsSet;

        protected WidgetScrollViewVertical scrollView = new WidgetScrollViewVertical();
        protected List<WidgetField> fields = new List<WidgetField>();
        protected List<Trait> traitsToRemove = new List<Trait>();
        protected HashSet<TraitDef> disallowedTraitDefs = new HashSet<TraitDef>();
        protected Dictionary<Trait, string> conflictingTraitList = new Dictionary<Trait, string>();
        protected TipCache tipCache = new TipCache();
        public ModState State { get; set; }
        public ViewState ViewState { get; set; }
        public ProviderTraits ProviderTraits { get; set; }
        public DialogManageTraits DialogManageTraits { get; set; }

        public override void Resize(float width) {
            base.Resize(width);
            FieldRect = new Rect(FieldPadding, 0, width - FieldPadding * 2, Style.FieldHeight);
        }

        public float Measure() {
            return 0;
        }

        public override float Draw(float y) {
            float top = y;
            y += Margin.y;

            y += DrawHeader(y, Width, "Traits".Translate().Resolve());

            CustomizedPawn customizedPawn = ViewState.CurrentPawn;
            Pawn pawn = customizedPawn.Pawn;
            int index = 0;
            Action clickAction = null;
            TraitSet traitSet = pawn.story.traits;
            Vector2 currentPosition = new Vector2(FieldRect.x, FieldRect.y + y);
            float maxPositionX = currentPosition.x + FieldRect.width;
            int drawnTraitCount = 0;
            foreach (Trait trait in traitSet.allTraits) {
                if (trait == null) {
                    continue;
                }
                bool canDelete = ManagerPawns.CanRemoveTrait(pawn, trait);
                drawnTraitCount++;
                if (index >= fields.Count) {
                    fields.Add(new WidgetField());
                }
                WidgetField field = fields[index];

                Vector2 labelSize = Text.CalcSize(trait.LabelCap);
                float fieldWidth = labelSize.x + 16 + (canDelete ? 16: 0);
                if (currentPosition.x + fieldWidth > maxPositionX) {
                    y += FieldRect.height + 6;
                    currentPosition = new Vector2(FieldRect.x, FieldRect.y + y);
                }

                Rect fieldRect = new Rect(currentPosition.x, currentPosition.y, fieldWidth, FieldRect.height);
                field.Rect = fieldRect;
                Rect fieldClickRect = fieldRect;

                field.Label = trait.LabelCap;
                field.LabelRect = new Rect(8, 0, labelSize.x, FieldRect.height);
                field.Tip = GetTraitTip(trait, customizedPawn);
                field.Color = Style.ColorText;
                if (trait.Suppressed) {
                    field.Color = ColoredText.SubtleGrayColor;
                }
                else if (trait.sourceGene != null) {
                    field.Color = ColoredText.GeneColor;
                }

                Trait localTrait = trait;
                int localIndex = index;
                field.ClickAction = () => {
                    Trait originalTrait = localTrait;
                    Trait selectedTrait = originalTrait;
                    ComputeDisallowedTraits(customizedPawn, originalTrait);
                    DialogOptions<Trait> dialog = new DialogOptions<Trait>(ProviderTraits.Traits) {
                        NameFunc = (Trait t) => {
                            return t.LabelCap;
                        },
                        DescriptionFunc = (Trait t) => {
                            return GetTraitTip(t, customizedPawn);
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
                            TraitUpdated?.Invoke(localIndex, selectedTrait);
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
                field.PreviousAction = null;
                field.NextAction = null;
                field.Draw();

                currentPosition = new Vector2(currentPosition.x + fieldWidth + 6, currentPosition.y);

                // Remove trait button.
                if (canDelete) {
                    Rect deleteRect = new Rect(field.Rect.xMax - 16, field.Rect.y + field.Rect.HalfHeight() - 6, 12, 12);
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
                }

                index++;
            }
            if (drawnTraitCount > 0) {
                y += FieldRect.height + 8;
            }

            tipCache.MakeReady();

            // If the index is still zero, then the pawn has no traits.  Draw the "none" label.
            if (index == 0) {
                GUI.color = Style.ColorText;
                string message = "EdB.PC.Panel.Traits.None".Translate();
                if (UtilityPawns.IsBaby(pawn)) {
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

            if (UtilityPawns.TraitsAllowed(pawn)) {

                // Manage traits button.
                Rect manageTraitsRect = new Rect(Width - 25, top + 14, 16, 16);
                Style.SetGUIColorForButton(manageTraitsRect);
                int traitCount = traitSet.allTraits.Count();
                GUI.DrawTexture(manageTraitsRect, Textures.TextureButtonManage);
                if (Widgets.ButtonInvisible(manageTraitsRect, false)) {
                    if (DialogManageTraits == null) {
                        DialogManageTraits = new DialogManageTraits(ProviderTraits);
                        DialogManageTraits.TraitAdded += (t) => { TraitAdded?.Invoke(t); };
                        DialogManageTraits.TraitRemoved += (t) => { TraitRemoved?.Invoke(t); };
                        DialogManageTraits.TraitsSet += (t) => { TraitsSet?.Invoke(t); };
                    }
                    DialogManageTraits.InitializeWithCustomizedPawn(customizedPawn);
                    Find.WindowStack.Add(DialogManageTraits);
                }
                
                // Randomize traits button.
                Rect randomizeRect = new Rect(manageTraitsRect.x - 29, top + 9, 22, 22);
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
                    TraitsRandomized?.Invoke();
                }

            }

            // Remove any traits that were marked for deletion
            if (traitsToRemove.Count > 0) {
                foreach (var trait in traitsToRemove) {
                    TraitRemoved?.Invoke(trait);
                }
                traitsToRemove.Clear();
                tipCache.Invalidate();
            }

            y += Margin.y;
            return y - top;
        }


        protected string GetTraitTip(Trait trait, CustomizedPawn customizedPawn) {
            if (!tipCache.Ready || !tipCache.Lookup.ContainsKey(trait)) {
                string value = GenerateTraitTip(trait, customizedPawn);
                tipCache.Lookup.Add(trait, value);
                return value;
            }
            else {
                return tipCache.Lookup[trait];
            }
        }

        protected string GenerateTraitTip(Trait trait, CustomizedPawn customizedPawn) {
            try {
                string baseTip = trait.TipString(customizedPawn.Pawn);
                string conflictingNames = null;
                if (!conflictingTraitList.TryGetValue(trait, out conflictingNames)) {
                    List<Trait> conflictingTraits = ProviderTraits.Traits.Where((Trait t) => {
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

        protected void ComputeDisallowedTraits(CustomizedPawn customizedPawn, Trait traitToReplace) {
            Pawn pawn = customizedPawn.Pawn;
            var allTraits = pawn.story.traits.allTraits;
            disallowedTraitDefs.Clear();
            foreach (Trait t in allTraits) {
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

        protected void SelectNextTrait(CustomizedPawn customizedPawn, int traitIndex) {
            Pawn pawn = customizedPawn.Pawn;
            var allTraits = pawn.story.traits.allTraits;
            Trait currentTrait = allTraits[traitIndex];
            ComputeDisallowedTraits(customizedPawn, currentTrait);
            int index = -1;
            if (currentTrait != null) {
                index = ProviderTraits.Traits.FindIndex((Trait t) => {
                    return t.Label.Equals(currentTrait.Label);
                });
            }
            int count = 0;
            do {
                index++;
                if (index >= ProviderTraits.Traits.Count) {
                    index = 0;
                }
                if (++count > ProviderTraits.Traits.Count + 1) {
                    index = -1;
                    break;
                }
            }
            while (index != -1 && (allTraits.Contains(ProviderTraits.Traits[index]) || disallowedTraitDefs.Contains(ProviderTraits.Traits[index].def)));

            Trait newTrait = null;
            if (index > -1) {
                newTrait = ProviderTraits.Traits[index];
            }
            TraitUpdated(traitIndex, newTrait);
        }

        protected void SelectPreviousTrait(CustomizedPawn customizedPawn, int traitIndex) {
            Pawn pawn = customizedPawn.Pawn;
            var allTraits = pawn.story.traits.allTraits;
            Trait currentTrait = allTraits[traitIndex];
            ComputeDisallowedTraits(customizedPawn, currentTrait);
            int index = -1;
            if (currentTrait != null) {
                index = ProviderTraits.Traits.FindIndex((Trait t) => {
                    return t.Label.Equals(currentTrait.Label);
                });
            }
            int count = 0;
            do {
                index--;
                if (index < 0) {
                    index = ProviderTraits.Traits.Count - 1;
                }
                if (++count > ProviderTraits.Traits.Count + 1) {
                    index = -1;
                    break;
                }
            }
            while (index != -1 && (allTraits.Contains(ProviderTraits.Traits[index]) || disallowedTraitDefs.Contains(ProviderTraits.Traits[index].def)));

            Trait newTrait = null;
            if (index > -1) {
                newTrait = ProviderTraits.Traits[index];
            }
            TraitUpdated(traitIndex, newTrait);
        }

        public class TipCache {
            public Dictionary<Trait, string> Lookup = new Dictionary<Trait, string>();
            private bool ready = false;
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
