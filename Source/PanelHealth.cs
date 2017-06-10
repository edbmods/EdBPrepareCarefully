using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class PanelHealth : PanelBase {
        public delegate void AddInjuryHandler(Injury injury);
        public delegate void AddImplantHandler(Implant implant);

        public event AddInjuryHandler InjuryAdded;
        public event AddImplantHandler ImplantAdded;

        protected static readonly string HediffTypeInjury = "HediffTypeInjury";
        protected static readonly string HediffTypeImplant = "HediffTypeImplant";
        protected string selectedHediffType = HediffTypeImplant;

        protected float HeightEntrySpacing = 4;
        protected Vector2 SizeEntry;
        protected Vector2 SizeField;
        protected Rect RectScrollFrame;
        protected Rect RectScrollView;
        protected Rect RectItem;
        protected Rect RectLabel;
        protected Rect RectField;
        protected Rect RectButtonDelete;
        protected Rect RectButtonAdd;

        protected ScrollViewVertical scrollView = new ScrollViewVertical();
        protected List<Field> fields = new List<Field>();

        public List<CustomBodyPart> partRemovalList = new List<CustomBodyPart>();
        protected HashSet<BodyPartRecord> disabledBodyParts = new HashSet<BodyPartRecord>();
        protected HashSet<InjuryOption> disabledInjuryOptions = new HashSet<InjuryOption>();

        protected List<InjurySeverity> severityOptions = new List<InjurySeverity>();
        protected List<InjurySeverity> oldInjurySeverities = new List<InjurySeverity>();
        protected Dictionary<RecipeDef, string> recipeToolips = new Dictionary<RecipeDef, string>();

        public PanelHealth() {
            oldInjurySeverities.Add(new InjurySeverity(2));
            oldInjurySeverities.Add(new InjurySeverity(3));
            oldInjurySeverities.Add(new InjurySeverity(4));
            oldInjurySeverities.Add(new InjurySeverity(5));
            oldInjurySeverities.Add(new InjurySeverity(6));
        }
        public override string PanelHeader {
            get {
                return "Health".Translate();
            }
        }

        public void ScrollToTop() {
            scrollView.ScrollToTop();
        }
        public void ScrollToBottom() {
            scrollView.ScrollToBottom();
        }

        public override void Resize(Rect rect) {
            base.Resize(rect);

            float panelPadding = 10;
            float fieldHeight = 28;
            float labelHeight = 18;
            float labelPadding = -1;
            float buttonPadding = 8;
            Vector2 buttonSize = new Vector2(12, 12);
            Vector2 itemPadding = new Vector2(8, 6);
            Vector2 contentSize = new Vector2(PanelRect.width - panelPadding * 2, BodyRect.height - panelPadding);

            SizeEntry = new Vector2(contentSize.x, labelHeight + fieldHeight + itemPadding.y * 2 + labelPadding);
            RectItem = new Rect(0, 0, SizeEntry.x, SizeEntry.y);
            RectLabel = new Rect(itemPadding.x, itemPadding.y, contentSize.x - itemPadding.x * 2, labelHeight);
            RectField = new Rect(itemPadding.x, RectLabel.yMax + labelPadding, RectLabel.width, fieldHeight);
            RectButtonDelete = new Rect(RectField.xMax - buttonPadding - buttonSize.x,
                RectField.y + RectField.height * 0.5f - buttonSize.y * 0.5f,
                buttonSize.x, buttonSize.y);

            RectScrollFrame = new Rect(panelPadding, BodyRect.y, contentSize.x, contentSize.y);
            RectScrollView = new Rect(0, 0, RectScrollFrame.width, RectScrollFrame.height);

            RectButtonAdd = new Rect(PanelRect.width - 27, 12, 16, 16);
        }

        protected class InjurySeverity {
            public InjurySeverity(float value) {
                this.value = value;
            }
            public InjurySeverity(float value, HediffStage stage) {
                this.value = value;
                this.stage = stage;
            }
            protected float value = 0;
            protected HediffStage stage = null;
            protected int? variant = null;
            public float Value {
                get {
                    return this.value;
                }
            }
            public HediffStage Stage {
                get {
                    return this.stage;
                }
            }
            public int? Variant {
                get {
                    return this.variant;
                }
                set {
                    this.variant = value;
                }
            }
            public string Label {
                get {
                    if (stage != null) {
                        if (variant == null) {
                            return stage.label.CapitalizeFirst();
                        }
                        else {
                            return "EdB.PC.Dialog.Severity.Stage.Label.".Translate(new object[] {
                                        stage.label.CapitalizeFirst(), variant
                                    });
                        }
                    }
                    else {
                        return ("EdB.PC.Dialog.Severity.OldInjury.Label." + value).Translate();
                    }
                }
            }
        }

        protected override void DrawPanelContent(State state) {
            base.DrawPanelContent(state);
            CustomPawn customPawn = state.CurrentPawn;

            float cursor = 0;
            GUI.BeginGroup(RectScrollFrame);

            if (customPawn.BodyParts.Count == 0) {
                GUI.color = Style.ColorText;
                Widgets.Label(RectScrollView.InsetBy(6, 0, 0, 0), "EdB.PC.Panel.Health.None".Translate());
            }
            GUI.color = Color.white;

            scrollView.Begin(RectScrollView);

            cursor = DrawCustomBodyParts(cursor);

            scrollView.End(cursor);
            GUI.EndGroup();

            DrawAddButton();

            if (partRemovalList.Count > 0) {
                foreach (var x in partRemovalList) {
                    customPawn.RemoveCustomBodyParts(x);
                }
                partRemovalList.Clear();
            }
        }

        public void DrawAddButton() {
            if (RectButtonAdd.Contains(Event.current.mousePosition)) {
                GUI.color = Style.ColorButtonHighlight;
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(RectButtonAdd, Textures.TextureButtonAdd);

            // Add button.
            if (Widgets.ButtonInvisible(RectButtonAdd, false)) {
                CustomPawn customPawn = PrepareCarefully.Instance.State.CurrentPawn;

                Action addEntryAction = () => { };

                OptionsHealth healthOptions = PrepareCarefully.Instance.Providers.Health.GetOptions(customPawn);
                string selectedHediffType = this.selectedHediffType;
                RecipeDef selectedRecipe = null;
                InjuryOption selectedInjury = null;
                BodyPartRecord selectedBodyPart = null;
                bool bodyPartSelectionRequired = true;
                InjurySeverity selectedSeverity = null;

                Dialog_Options<InjurySeverity> severityDialog;
                Dialog_Options<BodyPartRecord> bodyPartDialog;
                Dialog_Options<InjuryOption> injuryOptionDialog;
                Dialog_Options<RecipeDef> implantRecipeDialog;
                Dialog_Options<string> hediffTypeDialog;

                ResetInjuryOptionEnabledState(customPawn);

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
                                    Log.Warning("Could not find body part record for definition: " + p.defName);
                                }
                            }
                        }
                        else {
                            AddInjuryToPawn(selectedInjury, selectedSeverity, null);
                        }
                    }
                };

                severityDialog = new Dialog_Options<InjurySeverity>(severityOptions) {
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
                            return "EdB.PC.Error.MustSelectSeverity";
                        }
                        else {
                            return null;
                        }
                    },
                    CloseAction = () => {
                        addInjuryAction();
                    }
                };

                bodyPartDialog = new Dialog_Options<BodyPartRecord>(null) {
                    ConfirmButtonLabel = "EdB.PC.Common.Add".Translate(),
                    CancelButtonLabel = "EdB.PC.Common.Cancel".Translate(),
                    HeaderLabel = "EdB.PC.Dialog.BodyPart.Header".Translate(),
                    NameFunc = (BodyPartRecord option) => {
                        return option.def.LabelCap;
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

                injuryOptionDialog = new Dialog_Options<InjuryOption>(healthOptions.InjuryOptions) {
                    ConfirmButtonLabel = "EdB.PC.Common.Next".Translate(),
                    CancelButtonLabel = "EdB.PC.Common.Cancel".Translate(),
                    HeaderLabel = "EdB.PC.Dialog.Injury.Header".Translate(),
                    NameFunc = (InjuryOption option) => {
                        return option.Label;
                    },
                    SelectedFunc = (InjuryOption option) => {
                        return selectedInjury == option;
                    },
                    SelectAction = (InjuryOption option) => {
                        selectedInjury = option;
                        if (option.ValidParts == null && !option.WholeBody) {
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
                            ResetBodyPartEnabledState(bodyPartDialog.Options, customPawn);
                            Find.WindowStack.Add(bodyPartDialog);
                        }
                        else if (severityOptions.Count > 1) {
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

                implantRecipeDialog = new Dialog_Options<RecipeDef>(healthOptions.ImplantRecipes) {
                    ConfirmButtonLabel = "EdB.PC.Common.Next".Translate(),
                    CancelButtonLabel = "EdB.PC.Common.Cancel".Translate(),
                    HeaderLabel = "EdB.PC.Dialog.Implant.Header".Translate(),
                    NameFunc = (RecipeDef recipe) => {
                        return recipe.LabelCap;
                    },
                    SelectedFunc = (RecipeDef recipe) => {
                        return selectedRecipe == recipe;
                    },
                    SelectAction = (RecipeDef recipe) => {
                        selectedRecipe = recipe;
                        IEnumerable<BodyPartRecord> bodyParts = healthOptions.FindBodyPartsForImplantRecipe(recipe).Select((UniqueBodyPart p) => { return p.Record; });
                        int bodyPartCount = bodyParts.Count();
                        if (bodyParts != null && bodyPartCount > 0) {
                            if (bodyPartCount > 1) {
                                selectedBodyPart = null;
                                bodyPartDialog.Options = bodyParts;
                                bodyPartSelectionRequired = true;
                                ResetBodyPartEnabledState(bodyParts, customPawn);
                            }
                            else {
                                selectedBodyPart = bodyParts.First();
                                bodyPartSelectionRequired = false;
                            }
                        }
                        else {
                            selectedBodyPart = null;
                            bodyPartSelectionRequired = false;
                        }
                    },
                    ConfirmValidation = () => {
                        if (selectedRecipe == null) {
                            return "EdB.PC.Dialog.Implant.Error.Required";
                        }
                        else {
                            return null;
                        }
                    },
                    CloseAction = () => {
                        if (bodyPartSelectionRequired) {
                            Find.WindowStack.Add(bodyPartDialog);
                        }
                        else {
                            customPawn.AddImplant(new Implant(selectedBodyPart, selectedRecipe));
                        }
                    }
                };

                hediffTypeDialog = new Dialog_Options<string>(new string[] { HediffTypeInjury, HediffTypeImplant }) {
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
                            Find.WindowStack.Add(implantRecipeDialog);
                        }
                    }
                };

                Find.WindowStack.Add(hediffTypeDialog);
            }
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
            InjuryAdded(injury);
        }

        protected void ResetInjuryOptionEnabledState(CustomPawn pawn) {
            disabledInjuryOptions.Clear();
            foreach (var injuryOption in PrepareCarefully.Instance.Providers.Health.GetOptions(pawn).InjuryOptions) {
                InjuryOption option = injuryOption;
                if (option.IsOldInjury) {
                    continue;
                }
                Injury injury = pawn.Injuries.FirstOrDefault((Injury i) => {
                    return (i.Option == option);
                });
                if (injury != null) {
                    disabledInjuryOptions.Add(injuryOption);
                }
            }
        }

        protected void ResetBodyPartEnabledState(IEnumerable<BodyPartRecord> parts, CustomPawn pawn) {
            disabledBodyParts.Clear();
            OptionsHealth healthOptions = PrepareCarefully.Instance.Providers.Health.GetOptions(pawn);
            foreach (var part in parts) {
                UniqueBodyPart uniquePart = healthOptions.FindBodyPartsForRecord(part);
                if (pawn.IsReplacedPart(part) || pawn.AtLeastOneReplacedPart(uniquePart.Ancestors.Select((UniqueBodyPart p) => { return p.Record; }))) {
                    disabledBodyParts.Add(part);
                }
            }
        }

        protected void ResetSeverityOptions(InjuryOption injuryOption) {
            severityOptions.Clear();

            // Don't add stages for addictions since they are handled sort of differently.
            if (injuryOption.HediffDef.hediffClass != null && typeof(Hediff_Addiction).IsAssignableFrom(injuryOption.HediffDef.hediffClass)) {
                return;
            }

            // If the injury has no stages, add the old injury severity options.
            // TODO: Is this right?
            if (injuryOption.HediffDef.stages == null || injuryOption.HediffDef.stages.Count == 0) {
                severityOptions.AddRange(oldInjurySeverities);
                return;
            }

            int variant = 1;
            InjurySeverity previous = null;

            foreach (var stage in injuryOption.HediffDef.stages) {
                // Filter out a stage if it will definitely kill the pawn.
                if (injuryOption.HediffDef.DoesStageDefinitelyKillPawn(stage)) {
                    continue;
                }
                // Filter out hidden stages.
                if (!stage.everVisible) {
                    continue;
                }
                InjurySeverity value = null;
                if (stage.minSeverity == 0) {
                    float severity = injuryOption.HediffDef.initialSeverity > 0 ? injuryOption.HediffDef.initialSeverity : 0.001f;
                    value = new InjurySeverity(severity, stage);
                }
                else {
                    value = new InjurySeverity(stage.minSeverity, stage);
                }
                if (previous == null) {
                    previous = value;
                    variant = 1;
                }
                else {
                    if (previous.Stage.label == stage.label) {
                        previous.Variant = variant;
                        variant++;
                        value.Variant = variant;
                    }
                    else {
                        previous = value;
                        variant = 1;
                    }
                }
                severityOptions.Add(value);
            }
        }

        public float DrawCustomBodyParts(float cursor) {
            CustomPawn customPawn = PrepareCarefully.Instance.State.CurrentPawn;
            int index = 0;
            foreach (var i in customPawn.BodyParts) {
                if (index >= fields.Count) {
                    fields.Add(new Field());
                }
                cursor = DrawCustomBodyPart(cursor, i, fields[index]);
                index++;
            }
            cursor -= HeightEntrySpacing;
            return cursor;
        }

        public float DrawCustomBodyPart(float cursor, CustomBodyPart customPart, Field field) {
            bool willScroll = scrollView.ScrollbarsVisible;
            Rect entryRect = RectItem;
            entryRect.y = entryRect.y + cursor;
            entryRect.width = entryRect.width - (willScroll ? 16 : 0);
            GUI.color = Style.ColorPanelBackgroundItem;
            GUI.DrawTexture(entryRect, BaseContent.WhiteTex);

            // Draw background box.
            GUI.BeginGroup(entryRect);

            // Draw body part name.
            GUI.color = Style.ColorText;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.LowerCenter;
            Rect labelRect = RectLabel;
            labelRect.width = labelRect.width - (willScroll ? 16 : 0);
            Widgets.Label(labelRect, customPart.PartName);
            Text.Font = GameFont.Small;

            // Draw field
            Rect fieldRect = RectField;
            fieldRect.width = fieldRect.width - (willScroll ? 16 : 0);
            field.Rect = fieldRect;
            field.Label = customPart.ChangeName;
            field.Color = customPart.LabelColor;
            if (customPart.HasTooltip) {
                field.Tip = customPart.Tooltip;
            }
            else {
                field.Tip = null;
            }
            field.Draw();

            // Delete the option.
            Rect deleteRect = RectButtonDelete;
            if (willScroll) {
                deleteRect.x = deleteRect.x - 16;
            }
            Style.SetGUIColorForButton(deleteRect);
            GUI.DrawTexture(deleteRect, Textures.TextureButtonDelete);
            if (Widgets.ButtonInvisible(deleteRect, false)) {
                SoundDefOf.TickTiny.PlayOneShotOnCamera();
                partRemovalList.Add(customPart);
            }

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            GUI.EndGroup();

            return cursor + RectItem.height + HeightEntrySpacing;
        }

        protected string GetRecipeTooltip(RecipeDef def) {
            string tip;
            if (recipeToolips.TryGetValue(def, out tip)) {
                return tip;
            }
            return null;
        }
    }
}
