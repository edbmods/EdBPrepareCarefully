using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class PanelHealth : PanelModule {
        protected static readonly string HediffTypeInjury = "HediffTypeInjury";
        protected static readonly string HediffTypeImplant = "HediffTypeImplant";

        public delegate void AddInjuryHandler(Injury injury);
        public delegate void AddImplantHandler(Implant implant);
        public delegate void RemoveHediffsHandler(IEnumerable<Hediff> hediffs);
        public delegate void UpdateImplantsHandler(IEnumerable<Implant> implants);

        public event AddInjuryHandler InjuryAdded;
        public event AddImplantHandler ImplantAdded;
        public event RemoveHediffsHandler HediffsRemoved;
        public event UpdateImplantsHandler ImplantsUpdated;

        protected Rect FieldRect;
        protected Rect RectButtonDelete;
        protected float FieldPadding = 6;
        protected string selectedHediffType = HediffTypeImplant;
        protected List<WidgetField> fields = new List<WidgetField>();
        protected List<Hediff> hediffRemovalList = new List<Hediff>();
        protected HashSet<BodyPartRecord> disabledBodyParts = new HashSet<BodyPartRecord>();
        protected HashSet<InjuryOption> disabledInjuryOptions = new HashSet<InjuryOption>();
        protected HashSet<RecipeDef> disabledImplantRecipes = new HashSet<RecipeDef>();
        protected List<InjurySeverity> severityOptions = new List<InjurySeverity>();
        protected List<InjurySeverity> permanentInjurySeverities = new List<InjurySeverity>();
        protected LabelTrimmer labelTrimmer = new LabelTrimmer();
        public ModState State { get; set; }
        public ViewState ViewState { get; set; }
        public ProviderHealthOptions ProviderHealth { get; set; }

        public PanelHealth() {
            permanentInjurySeverities.Add(new InjurySeverity(2));
            permanentInjurySeverities.Add(new InjurySeverity(3));
            permanentInjurySeverities.Add(new InjurySeverity(4));
            permanentInjurySeverities.Add(new InjurySeverity(5));
            permanentInjurySeverities.Add(new InjurySeverity(6));
        }

        public override void Resize(float width) {
            base.Resize(width);
            Vector2 buttonSize = new Vector2(12, 12);
            float buttonPadding = 8;
            FieldRect = new Rect(FieldPadding, 0, width - FieldPadding * 2, Style.FieldHeight);

            RectButtonDelete = new Rect(FieldRect.xMax - buttonPadding - buttonSize.x,
                FieldRect.height * 0.5f - buttonSize.y * 0.5f,
                buttonSize.x, buttonSize.y);

            labelTrimmer.Width = FieldRect.width - (FieldRect.xMax - RectButtonDelete.xMin) * 2 - 10;
        }

        public override float Draw(float y) {
            float top = y;
            y += Margin.y;

            y += DrawHeader(y, Width, "Health".Translate().Resolve());

            CustomizedPawn currentPawn = ViewState.CurrentPawn;
            int index = 0;
            IEnumerable<IGrouping<BodyPartRecord, Hediff>> groupedHediffs = ReflectionUtil.Method(typeof(HealthCardUtility), "VisibleHediffGroupsInOrder")
                .Invoke(null, new object[] { currentPawn.Pawn, false }) as IEnumerable<IGrouping<BodyPartRecord, Hediff>>;
            foreach (var group in groupedHediffs) {
                foreach (var hediff in group) {
                    if (index >= fields.Count) {
                        fields.Add(new WidgetField());
                    }

                    if (index != 0) {
                        y += FieldPadding;
                    }
                    y += DrawHediff(currentPawn, hediff, fields[index], y, Width);

                    index++;
                }
            }

            // If the index is still zero, then the pawn has no hediffs.  Draw the "none" label.
            if (index == 0) {
                GUI.color = Style.ColorText;
                Widgets.Label(FieldRect.InsetBy(6, 0).OffsetBy(0, y - 4), "EdB.PC.Panel.Health.None".Translate());
                y += FieldRect.height - 4;
            }

            if (hediffRemovalList.Count > 0) {
                HediffsRemoved?.Invoke(hediffRemovalList);
                hediffRemovalList.Clear();
            }

            DrawAddButton(top, Width);

            return y - top;
        }

        protected string GetTooltipForPart(Pawn pawn, BodyPartRecord part) {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(part.LabelCap + ": ");
            stringBuilder.AppendLine(" " + pawn.health.hediffSet.GetPartHealth(part) + " / " + part.def.GetMaxHealth(pawn));
            float num = PawnCapacityUtility.CalculatePartEfficiency(pawn.health.hediffSet, part);
            if (num != 1f) {
                stringBuilder.AppendLine("Efficiency".Translate() + ": " + num.ToStringPercent());
            }
            return stringBuilder.ToString();
        }

        public float DrawHediff(CustomizedPawn currentPawn, Hediff hediff, WidgetField field, float y, float width) {
            float top = y;
            Pawn pawn = currentPawn.Pawn;
            BodyPartRecord part = hediff.Part;
            float labelWidth = ((part != null) ? Text.CalcHeight(part.LabelCap, width) : Text.CalcHeight("WholeBody".Translate(), width));
            Color partColor = HealthUtility.RedColor;
            if (part != null) {
                partColor = HealthUtility.GetPartConditionLabel(pawn, part).Second;
            }

            string partLabel = part != null ? part.LabelCap : "WholeBody".Translate().Resolve();
            string hediffLabel = hediff.LabelCap;
            Color changeColor = hediff.LabelColor;

            Rect fieldRect = FieldRect.OffsetBy(0, y);
            field.Rect = fieldRect;
            Rect fieldClickRect = fieldRect;
            fieldClickRect.width -= 36;
            field.ClickRect = fieldClickRect;

            if (part != null) {
                field.Label = labelTrimmer.TrimLabelIfNeeded(new HealthPanelLabelProvider(partLabel, hediffLabel, partColor, changeColor));
            }
            else {
                string trimmedLabel = labelTrimmer.TrimLabelIfNeeded(hediffLabel);
                field.Label = "<color=#" + ColorUtility.ToHtmlStringRGBA(changeColor) + ">" + trimmedLabel + "</color>";
            }
            field.TipAction = (rect) => {
                TooltipHandler.TipRegion(rect, new TipSignal(() => hediff.GetTooltip(pawn, false), (int)y + 127857, TooltipPriority.Default));
                if (part != null) {
                    TooltipHandler.TipRegion(rect, new TipSignal(() => GetTooltipForPart(pawn, part), (int)y + 127858, TooltipPriority.Pawn));
                }
            };

            field.Draw();

            if (CanDelete(currentPawn, hediff)) {
                Rect deleteRect = RectButtonDelete.OffsetBy(0, y);
                if (deleteRect.Contains(Event.current.mousePosition)) {
                    GUI.color = Style.ColorButtonHighlight;
                }
                else {
                    GUI.color = Style.ColorButton;
                }
                GUI.DrawTexture(deleteRect, Textures.TextureButtonDelete);
                if (Widgets.ButtonInvisible(deleteRect, false)) {
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                    hediffRemovalList.Add(hediff);
                }
                GUI.color = Color.white;
            }

            y += FieldRect.height;

            return y - top;
        }

        public bool CanDelete(CustomizedPawn currentPawn, Hediff hediff) {
            Pawn pawn = currentPawn?.Pawn;
            if (pawn == null || hediff == null) {
                return false;
            }
            if (pawn.mutant != null) {
                if (pawn.mutant.Hediff.def == hediff.def) {
                    return false;
                }
            }
            return true;
        }

        public void DrawAddButton(float y, float width) {
            Rect rect = new Rect(width - 27, y + 12, 16, 16);
            if (rect.Contains(Event.current.mousePosition)) {
                GUI.color = Style.ColorButtonHighlight;
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(rect, Textures.TextureButtonAdd);

            // Add button.
            if (Widgets.ButtonInvisible(rect, false)) {
                CustomizedPawn customizedPawn = ViewState.CurrentPawn;

                Action addEntryAction = () => { };

                OptionsHealth healthOptions = ProviderHealth.GetOptions(customizedPawn);
                string selectedHediffType = this.selectedHediffType;
                RecipeDef selectedRecipe = null;
                InjuryOption selectedInjury = null;
                BodyPartRecord selectedBodyPart = null;
                bool bodyPartSelectionRequired = true;
                InjurySeverity selectedSeverity = null;

                DialogOptions<InjurySeverity> severityDialog;
                DialogOptions<BodyPartRecord> bodyPartDialog;
                DialogOptions<InjuryOption> injuryOptionDialog;
                DialogManageImplants manageImplantsDialog;
                DialogOptions<string> hediffTypeDialog;

                ResetDisabledInjuryOptions(customizedPawn);

                Action addInjuryAction = () => {
                    if (bodyPartSelectionRequired) {
                        AddInjuryToPawn(selectedInjury, selectedSeverity, selectedBodyPart);
                    }
                    else {
                        if (selectedInjury.ValidParts != null && selectedInjury.ValidParts.Count > 0) {
                            foreach (var p in selectedInjury.ValidParts) {
                                var part = healthOptions.FindBodyPartsForDef(p).FirstOrDefault();
                                if (part != null) {
                                    AddInjuryToPawn(selectedInjury, selectedSeverity, part.Record);
                                }
                                else {
                                    Logger.Warning("Could not find body part record for definition: " + p.defName);
                                }
                            }
                        }
                        else {
                            AddInjuryToPawn(selectedInjury, selectedSeverity, null);
                        }
                    }
                };

                severityDialog = new DialogOptions<InjurySeverity>(severityOptions) {
                    ConfirmButtonLabel = "EdB.PC.Common.Add".Translate(),
                    CancelButtonLabel = "EdB.PC.Common.Cancel".Translate(),
                    HeaderLabel = "EdB.PC.Panel.Health.SelectSeverity".Translate(),
                    NameFunc = (InjurySeverity option) => {
                        if (!string.IsNullOrEmpty(option.Label)) {
                            return option.Label;
                        }
                        else {
                            return selectedInjury.HediffDef.LabelCap;
                        }
                    },
                    SelectedFunc = (InjurySeverity option) => {
                        return option == selectedSeverity;
                    },
                    SelectAction = (InjurySeverity option) => {
                        selectedSeverity = option;
                    },
                    ConfirmValidation = () => {
                        if (selectedSeverity == null) {
                            return "EdB.PC.Panel.Health.Error.MustSelectSeverity";
                        }
                        else {
                            return null;
                        }
                    },
                    CloseAction = () => {
                        addInjuryAction();
                    }
                };

                bodyPartDialog = new DialogOptions<BodyPartRecord>(null) {
                    ConfirmButtonLabel = "EdB.PC.Common.Add".Translate(),
                    CancelButtonLabel = "EdB.PC.Common.Cancel".Translate(),
                    HeaderLabel = "EdB.PC.Dialog.BodyPart.Header".Translate(),
                    NameFunc = (BodyPartRecord option) => {
                        return option.LabelCap;
                    },
                    SelectedFunc = (BodyPartRecord option) => {
                        return option == selectedBodyPart;
                    },
                    SelectAction = (BodyPartRecord option) => {
                        selectedBodyPart = option;
                    },
                    EnabledFunc = (BodyPartRecord option) => {
                        return !disabledBodyParts.Contains(option);
                    },
                    ConfirmValidation = () => {
                        if (selectedBodyPart == null) {
                            return "EdB.PC.Dialog.BodyPart.Error.Required";
                        }
                        else {
                            return null;
                        }
                    },
                    CloseAction = () => {
                        if (selectedHediffType == HediffTypeInjury) {
                            if (this.severityOptions.Count > 1) {
                                Find.WindowStack.Add(severityDialog);
                            }
                            else {
                                if (severityOptions.Count > 0) {
                                    selectedSeverity = this.severityOptions[0];
                                }
                                addInjuryAction();
                            }
                        }
                        else if (selectedHediffType == HediffTypeImplant) {
                            ImplantAdded(new Implant(selectedBodyPart, selectedRecipe));
                        }
                    }
                };

                injuryOptionDialog = new DialogOptions<InjuryOption>(healthOptions.SelectableInjuryOptions) {
                    ConfirmButtonLabel = "EdB.PC.Common.Next".Translate(),
                    CancelButtonLabel = "EdB.PC.Common.Cancel".Translate(),
                    HeaderLabel = "EdB.PC.Dialog.Injury.Header".Translate(),
                    NameFunc = (InjuryOption option) => {
                        //return option.HediffDef.defName;
                        return option.Label;
                    },
                    DescriptionFunc = (InjuryOption option) => {
                        return option.HediffDef?.description;
                    },
                    SelectedFunc = (InjuryOption option) => {
                        return selectedInjury == option;
                    },
                    SelectAction = (InjuryOption option) => {
                        selectedInjury = option;
                        if (option.ValidParts == null && !option.WholeBody) {
                            bodyPartSelectionRequired = true;
                        }
                        else if (option.ValidParts != null && option.ValidParts.Count > 0) {
                            bodyPartSelectionRequired = true;
                        }
                        else {
                            bodyPartSelectionRequired = false;
                        }
                    },
                    EnabledFunc = (InjuryOption option) => {
                        return !disabledInjuryOptions.Contains(option);
                    },
                    ConfirmValidation = () => {
                        if (selectedInjury == null) {
                            return "EdB.PC.Dialog.Injury.Error.Required";
                        }
                        else {
                            return null;
                        }
                    },
                    CloseAction = () => {
                        ResetSeverityOptions(selectedInjury);
                        if (bodyPartSelectionRequired) {
                            bodyPartDialog.Options = healthOptions.BodyPartsForInjury(selectedInjury);
                            int count = bodyPartDialog.Options.Count();
                            if (count > 1) {
                                ResetDisabledBodyParts(bodyPartDialog.Options, customizedPawn);
                                Find.WindowStack.Add(bodyPartDialog);
                                return;
                            }
                            else if (count == 1) {
                                selectedBodyPart = bodyPartDialog.Options.First();
                            }
                        }

                        if (severityOptions.Count > 1) {
                            Find.WindowStack.Add(severityDialog);
                        }
                        else {
                            if (severityOptions.Count > 0) {
                                selectedSeverity = this.severityOptions[0];
                            }
                            addInjuryAction();
                        }
                    }
                };

                hediffTypeDialog = new DialogOptions<string>(new string[] { HediffTypeInjury, HediffTypeImplant }) {
                    ConfirmButtonLabel = "EdB.PC.Common.Next".Translate(),
                    CancelButtonLabel = "EdB.PC.Common.Cancel".Translate(),
                    NameFunc = (string type) => {
                        return ("EdB.PC.Panel.Health." + type).Translate();
                    },
                    SelectedFunc = (string type) => {
                        return selectedHediffType == type;
                    },
                    SelectAction = (string type) => {
                        selectedHediffType = type;
                    },
                    ConfirmValidation = () => {
                        if (selectedHediffType == null) {
                            return "EdB.PC.Panel.Health.Error.MustSelectOption";
                        }
                        else {
                            return null;
                        }
                    },
                    CloseAction = () => {
                        this.selectedHediffType = selectedHediffType;
                        if (selectedHediffType == HediffTypeInjury) {
                            Find.WindowStack.Add(injuryOptionDialog);
                        }
                        else {
                            ResetDisabledImplantRecipes(customizedPawn);
                            manageImplantsDialog = new DialogManageImplants(customizedPawn, ProviderHealth) {
                                HeaderLabel = "EdB.PC.Dialog.Implant.Header".Translate(),
                                CloseAction = (List<Implant> implants) => {
                                    ImplantsUpdated?.Invoke(implants);
                                }
                            };
                            Find.WindowStack.Add(manageImplantsDialog);
                        }
                    }
                };
                Find.WindowStack.Add(hediffTypeDialog);
            }
        }

        protected void ApplyImplantsToPawn(CustomizedPawn pawn, List<Implant> implants) {
            //Logger.Debug("Updated implants");
            //foreach (var i in implants) {
            //    Logger.Debug("  " + i.recipe.LabelCap + ", " + i.PartName + (i.ReplacesPart ? ", replaces part" : ""));
            //}
            // TODO
            //pawn.UpdateImplants(implants);
        }

        protected void AddInjuryToPawn(InjuryOption option, InjurySeverity severity, BodyPartRecord bodyPart) {
            Injury injury = new Injury();
            injury.BodyPartRecord = bodyPart;
            injury.Option = option;
            if (severity != null) {
                injury.Severity = severity.Value;
            }
            else {
                injury.Severity = option.HediffDef.initialSeverity;
            }
            InjuryAdded?.Invoke(injury);
        }

        protected void ResetDisabledInjuryOptions(CustomizedPawn pawn) {
            // TODO
            //disabledInjuryOptions.Clear();
            //OptionsHealth optionsHealth = PrepareCarefully.Instance.Providers.Health.GetOptions(pawn);
            //foreach (var injuryOption in optionsHealth.InjuryOptions) {
            //    InjuryOption option = injuryOption;
            //    if (option.IsOldInjury) {
            //        continue;
            //    }

            //    if (option.ValidParts != null && option.ValidParts.Count > 0) {
            //        HashSet<BodyPartRecord> records = new HashSet<BodyPartRecord>(optionsHealth.BodyPartsForInjury(injuryOption));
            //        int recordCount = records.Count;
            //        int injuryCountForThatPart = pawn.Injuries.Where((Injury i) => {
            //            return (i.Option == option);
            //        }).Count();
            //        if (injuryCountForThatPart >= recordCount) {
            //            disabledInjuryOptions.Add(injuryOption);
            //        }
            //        else {
            //            continue;
            //        }
            //    }
            //    else {
            //        Injury injury = pawn.Injuries.FirstOrDefault((Injury i) => {
            //            return (i.Option == option);
            //        });
            //        if (injury != null) {
            //            disabledInjuryOptions.Add(injuryOption);
            //        }
            //    }
            //}
        }

        protected void ResetDisabledBodyParts(IEnumerable<BodyPartRecord> parts, CustomizedPawn pawn) {
            // TODO
            //disabledBodyParts.Clear();
            //OptionsHealth healthOptions = PrepareCarefully.Instance.Providers.Health.GetOptions(pawn);
            //foreach (var part in parts) {
            //    UniqueBodyPart uniquePart = healthOptions.FindBodyPartsForRecord(part);
            //    if (pawn.HasPartBeenReplaced(part) || pawn.HasAtLeastOnePartBeenReplaced(uniquePart.Ancestors.Select((UniqueBodyPart p) => { return p.Record; }))) {
            //        disabledBodyParts.Add(part);
            //    }
            //    else {
            //        Injury injury = pawn.Injuries.FirstOrDefault((Injury i) => {
            //            return i.BodyPartRecord == part;
            //        });
            //        if (injury != null) {
            //            disabledBodyParts.Add(part);
            //        }
            //    }
            //}
        }

        protected void ResetDisabledImplantRecipes(CustomizedPawn pawn) {
            // TODO
            //disabledImplantRecipes.Clear();
            //OptionsHealth healthOptions = PrepareCarefully.Instance.Providers.Health.GetOptions(pawn);
            //foreach (RecipeDef recipeDef in healthOptions.ImplantRecipes) {
            //    if (recipeDef.appliedOnFixedBodyParts != null) {
            //        if (recipeDef.appliedOnFixedBodyParts.Count == 1) {
            //            foreach (var uniquePart in healthOptions.FindBodyPartsForDef(recipeDef.appliedOnFixedBodyParts[0])) {
            //                if (pawn.HasSameImplant(uniquePart.Record, recipeDef)) {
            //                    disabledImplantRecipes.Add(recipeDef);
            //                    break;
            //                }
            //                if (pawn.HasPartBeenReplaced(uniquePart.Record)) {
            //                    disabledImplantRecipes.Add(recipeDef);
            //                    break;
            //                }
            //            }
            //        }
            //    }
            //}
        }

        protected void ResetSeverityOptions(InjuryOption injuryOption) {
            severityOptions.Clear();
            if (injuryOption.SeverityOptions().Any(s => s != null)) {
                severityOptions.AddRange(injuryOption.SeverityOptions());
                //Logger.Debug("{" + injuryOption.Label + "} has severity options: " + string.Join(", ", severityOptions.Select(o => o.Label)));
            }
            else {
                //Logger.Debug("{" + injuryOption.Label + "} has no severity options");
            }
        }

        // Custom label provider for health diffs that properly maintains the rich text/html tags while trimming.
        public struct HealthPanelLabelProvider : LabelTrimmer.LabelProvider {
            private static readonly int PART_NAME = 0;
            private static readonly int CHANGE_NAME = 1;
            private int elementToTrim;
            private string partName;
            private string changeName;
            private readonly string partColor;
            private readonly string changeColor;
            public HealthPanelLabelProvider(string partName, string changeName, Color partColor, Color changeColor) {
                this.partName = partName;
                this.changeName = changeName;
                this.partColor = ColorUtility.ToHtmlStringRGBA(partColor);
                this.changeColor = ColorUtility.ToHtmlStringRGBA(changeColor);
                this.elementToTrim = CHANGE_NAME;
            }
            public string Current {
                get {
                     return "<color=#" + partColor + ">"+ partName + "</color>: <color=#" + changeColor + ">" + changeName + "</color>";
                }
            }
            public string CurrentWithSuffix(string suffix) {
                return "<color=#" + partColor + ">" + partName + "</color>: <color=#" + changeColor + ">" + changeName + suffix + "</color>";
            }
            public string Trim() {
                if (elementToTrim == CHANGE_NAME) {
                    if (!TrimChangeName()) {
                        elementToTrim = PART_NAME;
                    }
                }
                else {
                    TrimPartName();
                }
                return Current;
            }
            private bool TrimString(ref string value) {
                int length = value.Length;
                if (length == 0) {
                    return false;
                }
                value = value.Substring(0, length - 1).TrimEnd();
                if (length == 0) {
                    return false;
                }
                return true;
            }
            private bool TrimChangeName() {
                return TrimString(ref changeName);
            }
            private bool TrimPartName() {
                return TrimString(ref partName);
            }
        }
    }
}
