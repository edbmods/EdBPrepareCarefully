using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class PanelRelationshipsOther : PanelBase {
        public delegate void AddRelationshipHandler(PawnRelationDef def, CustomizedPawn source, CustomizedPawn target);
        public delegate void RemoveRelationshipHandler(CustomizedRelationship relationship);

        public event AddRelationshipHandler RelationshipAdded;
        public event RemoveRelationshipHandler RelationshipRemoved;

        private WidgetScrollViewVertical scrollView = new WidgetScrollViewVertical();
        private Rect RectScrollView;
        private Vector2 SizePawn;
        private Vector2 SizeRelationship;
        private Vector2 SizeArrow;
        private Vector2 SizeLabelSpacing;
        private float HeightLabel = 20;
        private Vector2 SizeRelationshipSpacing;
        private Color ColorPawnSource = new Color(69f / 255f, 70f / 255f, 72f / 255f);
        private Color ColorPawnTarget = new Color(20f / 255f, 20f / 255f, 21f / 255f);
        private Color ColorPawnNew = new Color(44f / 255f, 45f / 255f, 46f / 255f);
        private float SpacingGender = 9;
        private Vector2 SizeGender = new Vector2(48, 48);

        protected List<PawnRelationDef> relationDefs = new List<PawnRelationDef>();
        protected HashSet<PawnRelationDef> disabledRelationships = new HashSet<PawnRelationDef>();
        protected HashSet<CustomizedPawn> disabledTargets = new HashSet<CustomizedPawn>();
        protected List<CustomizedRelationship> relationshipsToDelete = new List<CustomizedRelationship>();

        public ModState State { get; set; }
        public ViewState ViewState { get; set; }
        public ManagerRelationships RelationshipManager { get; set; }

        public PanelRelationshipsOther() {
            relationDefs.AddRange(DefDatabase<PawnRelationDef>.AllDefs.ToList().FindAll((PawnRelationDef def) => {
                if (def.familyByBloodRelation) {
                    return false;
                }
                MethodInfo info = ReflectionUtil.Method(def.workerClass, "CreateRelation");
                if (info == null) {
                    return false;
                }
                else {
                    return true;
                }
            }));
        }
        public override string PanelHeader {
            get {
                return "EdB.PC.Panel.RelationshipsOther.Title".Translate();
            }
        }
        public override void Resize(Rect rect) {
            base.Resize(rect);
            Vector2 padding = Style.SizePanelPadding;
            RectScrollView = new Rect(padding.x, BodyRect.y, rect.width - padding.x * 2, rect.height - BodyRect.y - padding.y);

            SizeRelationshipSpacing = new Vector2(20, 10);
            SizePawn = new Vector2(90, 90);
            SizeRelationship = new Vector2(288, SizePawn.y);
            SizeArrow = new Vector2(16, 32);
            SizeLabelSpacing = new Vector2(4, 21);
        }
        protected override void DrawPanelContent() {
            base.DrawPanelContent();
            Vector2 cursor = Vector2.zero;

            GUI.BeginGroup(RectScrollView);
            scrollView.Begin(new Rect(Vector2.zero, RectScrollView.size));
            try {
                foreach (CustomizedRelationship relationship in State.Customizations.Relationships) {
                    // Don't show relationships between two hidden pawns
                    if (relationship.Source.Type != CustomizedPawnType.Hidden && relationship.Target.Type != CustomizedPawnType.Hidden) {
                        cursor = DrawRelationship(cursor, relationship);
                    }
                }
                cursor = DrawNextRelationship(cursor);

                if (cursor.x == 0) {
                    cursor.y -= SizeRelationshipSpacing.y;
                }
            }
            finally {
                scrollView.End(cursor.y + SizeRelationship.y);
                GUI.EndGroup();
            }

            if (relationshipsToDelete.Count > 0) {
                foreach (var r in relationshipsToDelete) {
                    RelationshipRemoved?.Invoke(r);
                }
                relationshipsToDelete.Clear();
            }
        }
        private string GetProfessionLabel(CustomizedPawn pawn) {
            if (pawn == null) {
                Logger.Warning("Could not get profession label for null pawn");
                return "";
            }
            bool hidden = pawn.Type == CustomizedPawnType.Hidden || pawn.Type == CustomizedPawnType.Temporary;
            if (!hidden) {
                return pawn.Type == CustomizedPawnType.Colony ? "EdB.PC.AddParentChild.Colony".Translate() : "EdB.PC.AddParentChild.World".Translate();
            }
            else {
                return pawn.Type == CustomizedPawnType.Temporary ? "EdB.PC.AddParentChild.Temporary".Translate() : "EdB.PC.AddParentChild.World".Translate();
            }
        }
        protected void DrawPortrait(Rect rect, CustomizedPawn customizedPawn) {
            bool hidden = customizedPawn.Type == CustomizedPawnType.Hidden || customizedPawn.Type == CustomizedPawnType.Temporary;
            if (!hidden) {
                Rect clipRect = new Rect(rect.x, rect.y, rect.width, 60);
                WidgetPortrait.Draw(customizedPawn.Pawn, clipRect, new Rect(0, 0, rect.width, 70).OutsetBy(10, 10).OffsetBy(0, -3));
            }
            else {
                Pawn pawn = customizedPawn.Pawn;
                GUI.color = Style.ColorButton;
                Rect portraitRect = new Rect(rect.MiddleX() - SizeGender.HalfX(), rect.y + SpacingGender, SizeGender.x, SizeGender.y);
                if (pawn.gender == Gender.Female) {
                    GUI.DrawTexture(portraitRect, Textures.TextureGenderFemaleLarge);
                }
                else if (pawn.gender == Gender.Male) {
                    GUI.DrawTexture(portraitRect, Textures.TextureGenderMaleLarge);
                }
                else {
                    GUI.DrawTexture(portraitRect, Textures.TextureGenderlessLarge);
                }
            }
        }
        protected string GetPawnShortName(CustomizedPawn customizedPawn) {
            if (customizedPawn.Type == CustomizedPawnType.Hidden) {
                return "EdB.PC.Pawn.HiddenPawnNameShort".Translate(customizedPawn.TemporaryPawn.Index);
            }
            else if (customizedPawn.Type == CustomizedPawnType.Temporary) {
                return "EdB.PC.Pawn.TemporaryPawnNameShort".Translate(customizedPawn.TemporaryPawn.Index);
            }
            else {
                Pawn pawn = customizedPawn?.Pawn;
                if (pawn == null) {
                    Logger.Warning("Pawn was null");
                    return "";
                }
                return pawn.LabelShortCap;
            }

        }
        protected Vector2 DrawRelationship(Vector2 cursor, CustomizedRelationship relationship) {
            if (cursor.x + SizeRelationship.x > RectScrollView.width) {
                cursor.x = 0;
                cursor.y += (SizeRelationship.y + SizeRelationshipSpacing.y);
            }

            Rect relationshipRect = new Rect(cursor, SizeRelationship);
            Rect sourcePawnRect = new Rect(cursor, SizePawn);
            GUI.color = ColorPawnSource;
            GUI.DrawTexture(sourcePawnRect, BaseContent.WhiteTex);
            GUI.color = Color.white;

            Rect sourcePawnName = new Rect(sourcePawnRect.x, sourcePawnRect.yMax - 34, sourcePawnRect.width, 26);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperCenter;
            GUI.color = Style.ColorText;
            Widgets.Label(sourcePawnName, GetPawnShortName(relationship.Source));
            GUI.color = Color.white;

            Rect sourceProfessionName = new Rect(sourcePawnRect.x, sourcePawnRect.yMax - 18, sourcePawnRect.width, 18);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.LowerCenter;
            GUI.color = Style.ColorText;
            Widgets.Label(sourceProfessionName, GetProfessionLabel(relationship.Source));
            GUI.color = Color.white;

            DrawPortrait(sourcePawnRect, relationship.Source);

            TooltipHandler.TipRegion(sourcePawnRect, GetTooltipText(relationship.Source));

            // Delete button.
            Rect deleteRect = new Rect(sourcePawnRect.xMax - 16, sourcePawnRect.y + 4, 12, 12);
            Style.SetGUIColorForButton(deleteRect);
            GUI.DrawTexture(deleteRect, Textures.TextureButtonDelete);
            if (Widgets.ButtonInvisible(deleteRect)) {
                relationshipsToDelete.Add(relationship);
            }

            Rect targetPawnRect = new Rect(cursor.x + SizeRelationship.x - SizePawn.x, cursor.y, SizePawn.x, SizePawn.y);
            GUI.color = ColorPawnTarget;
            GUI.DrawTexture(targetPawnRect, BaseContent.WhiteTex);
            GUI.color = Color.white;

            Rect targetPawnName = new Rect(targetPawnRect.x, targetPawnRect.yMax - 34, targetPawnRect.width, 26);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperCenter;
            GUI.color = Style.ColorText;
            Widgets.Label(targetPawnName, GetPawnShortName(relationship.Target));
            GUI.color = Color.white;

            Rect targetProfessionName = new Rect(targetPawnRect.x, targetPawnRect.yMax - 18, targetPawnRect.width, 18);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.LowerCenter;
            GUI.color = Style.ColorText;
            Widgets.Label(targetProfessionName, GetProfessionLabel(relationship.Target));
            GUI.color = Color.white;

            DrawPortrait(targetPawnRect, relationship.Target);
            
            TooltipHandler.TipRegion(targetPawnRect, GetTooltipText(relationship.Target));

            Rect sourceRelLabelRect = new Rect(sourcePawnRect.xMax, sourcePawnRect.y + SizeLabelSpacing.y, targetPawnRect.x - sourcePawnRect.xMax, HeightLabel);
            sourceRelLabelRect.width -= (SizeArrow.x + SizeLabelSpacing.x);
            GUI.color = ColorPawnSource;
            GUI.DrawTexture(sourceRelLabelRect, BaseContent.WhiteTex);
            Rect sourceRelArrowRect = new Rect(sourceRelLabelRect.xMax, sourceRelLabelRect.MiddleY() - SizeArrow.HalfY(), SizeArrow.x, SizeArrow.y);
            GUI.DrawTexture(sourceRelArrowRect, Textures.TextureArrowRight);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Style.ColorText;
            Widgets.Label(sourceRelLabelRect.OffsetBy(0, 1), relationship.InverseDef.GetGenderSpecificLabelCap(relationship.Source.Pawn));

            Rect targetRelLabelRect = new Rect(sourcePawnRect.xMax, targetPawnRect.yMax - SizeLabelSpacing.y - HeightLabel, targetPawnRect.x - sourcePawnRect.xMax, HeightLabel);
            targetRelLabelRect.width -= (SizeArrow.x + SizeLabelSpacing.x);
            targetRelLabelRect.x += (SizeArrow.x + SizeLabelSpacing.x);
            GUI.color = ColorPawnTarget;
            GUI.DrawTexture(targetRelLabelRect, BaseContent.WhiteTex);
            Rect targetRelArrowRect = new Rect(targetRelLabelRect.xMin - SizeArrow.x, targetRelLabelRect.MiddleY() - SizeArrow.HalfY(), SizeArrow.x, SizeArrow.y);
            GUI.DrawTexture(targetRelArrowRect, Textures.TextureArrowLeft);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Style.ColorText;
            Widgets.Label(targetRelLabelRect.OffsetBy(0, 1), relationship.Def.GetGenderSpecificLabelCap(relationship.Target.Pawn));

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            cursor.x += SizeRelationship.x + SizeRelationshipSpacing.x;
            return cursor;
        }

        protected string GetTooltipText(CustomizedPawn pawn) {
            bool hidden = pawn.Type == CustomizedPawnType.Hidden || pawn.Type == CustomizedPawnType.Temporary;
            if (!hidden) {
                string age = (pawn.Pawn.ageTracker.AgeChronologicalYears == pawn.Pawn.ageTracker.AgeBiologicalYears) ? "EdB.PC.Pawn.AgeWithoutChronological".Translate(pawn.Pawn.ageTracker.AgeBiologicalYears)
                    : "EdB.PC.Pawn.AgeWithChronological".Translate(pawn.Pawn.ageTracker.AgeBiologicalYears, pawn.Pawn.ageTracker.AgeChronologicalYears);
                string description = (pawn.Pawn.gender != Gender.None) ? "EdB.PC.Pawn.PawnDescriptionWithGender".Translate(UtilityPawns.GetProfessionLabel(pawn.Pawn), pawn.Pawn.gender.GetLabel(), age)
                    : "EdB.PC.AddParentChild.PawnDescriptionWithNoGender".Translate(UtilityPawns.GetProfessionLabel(pawn.Pawn), age);
                return pawn.Pawn.Name.ToStringFull + "\n" + description;
            }
            else {
                return null;
            }
        }

        protected Vector2 DrawNextRelationship(Vector2 cursor) {
            if (cursor.x + SizeRelationship.x > RectScrollView.width) {
                cursor.x = 0;
                cursor.y += SizeRelationship.y + SizeRelationshipSpacing.y;
            }

            Rect pawnRect = new Rect(cursor, SizePawn);
            if (pawnRect.Contains(Event.current.mousePosition)) {
                GUI.color = ColorPawnSource;
            }
            else {
                GUI.color = ColorPawnNew;
            }
            GUI.DrawTexture(pawnRect, BaseContent.WhiteTex);
            GUI.color = Color.white;

            float addButtonPadding = 4;
            Vector2 addButtonSize = new Vector2(16, 16);
            // Draw the add button.
            Rect addButtonRect = new Rect(pawnRect.xMax - addButtonSize.x - addButtonPadding, pawnRect.y + 4, addButtonSize.x, addButtonSize.y);
            Style.SetGUIColorForButton(pawnRect);
            GUI.DrawTexture(addButtonRect, Textures.TextureButtonAdd);
            if (Widgets.ButtonInvisible(pawnRect, false)) {
                ShowAddRelationshipDialogs();
            }
            GUI.color = Color.white;
            
            return new Vector2(cursor.x + SizeRelationship.x + SizeRelationshipSpacing.x, cursor.y);
        }

        protected void ShowAddRelationshipDialogs() {
            CustomizedPawn sourceParentChildPawn = null;
            PawnRelationDef selectedRelationship = null;
            CustomizedPawn targetParentChildPawn = null;

            DialogOptions<PawnRelationDef> relationshipDialog =
                new DialogOptions<PawnRelationDef>(null) {
                    ConfirmButtonLabel = "EdB.PC.Common.Next".Translate(),
                    CancelButtonLabel = "EdB.PC.Common.Cancel".Translate(),
                    HeaderLabel = "EdB.PC.AddRelationship.Header.Relationship".Translate(),
                    NameFunc = (PawnRelationDef def) => {
                        return def.GetGenderSpecificLabelCap(sourceParentChildPawn.Pawn);
                    },
                    SelectedFunc = (PawnRelationDef def) => {
                        return def == selectedRelationship;
                    },
                    SelectAction = (PawnRelationDef def) => {
                        selectedRelationship = def;
                    },
                    EnabledFunc = (PawnRelationDef d) => {
                        return !disabledRelationships.Contains(d);
                    },
                    ConfirmValidation = () => {
                        if (selectedRelationship == null) {
                            return "EdB.PC.AddRelationship.Error.RelationshipRequired";
                        }
                        else {
                            return null;
                        }
                    }
                };

            List<WidgetTable<CustomizedPawn>.RowGroup> sourceRowGroups = new List<WidgetTable<CustomizedPawn>.RowGroup>();
            sourceRowGroups.Add(new WidgetTable<CustomizedPawn>.RowGroup("EdB.PC.AddParentChild.Header.SelectColonist".Translate(),
                RelationshipManager.AvailableColonyPawns));
            sourceRowGroups.Add(new WidgetTable<CustomizedPawn>.RowGroup("<b>" + "EdB.PC.AddParentChild.Header.SelectWorldPawn".Translate() + "</b>",
                RelationshipManager.AvailableWorldPawns));
            WidgetTable<CustomizedPawn>.RowGroup sourceNewPawnGroup = new WidgetTable<CustomizedPawn>.RowGroup("EdB.PC.AddParentChild.Header.CreateTemporaryPawn".Translate(),
                RelationshipManager.AvailableTemporaryPawns);
            sourceRowGroups.Add(sourceNewPawnGroup);

            var sourcePawnDialog = new DialogSelectParentChildPawn() {
                HeaderLabel = "EdB.PC.AddRelationship.Header.Source".Translate(),
                SelectAction = (CustomizedPawn pawn) => { sourceParentChildPawn = pawn; },
                RowGroups = sourceRowGroups,
                DisabledPawns = null,
                ConfirmValidation = () => {
                    if (sourceParentChildPawn == null) {
                        return "EdB.PC.AddRelationship.Error.SourceRequired";
                    }
                    else {
                        return null;
                    }
                },
                CloseAction = () => {
                    // If the user selected a new pawn, replace the pawn in the new pawn list with another one.
                    int index = sourceNewPawnGroup.Rows.FirstIndexOf((CustomizedPawn p) => {
                        return p == targetParentChildPawn;
                    });
                    if (index > -1 && index < RelationshipManager.AvailableTemporaryPawns.Count()) {
                        targetParentChildPawn = RelationshipManager.ReplaceNewTemporaryCharacter(index);
                    }
                }
            };

            var targetPawnDialog = new DialogSelectParentChildPawn() {
                HeaderLabel = "EdB.PC.AddRelationship.Header.Target".Translate(),
                SelectAction = (CustomizedPawn pawn) => { targetParentChildPawn = pawn; },
                RowGroups = null, // To be filled out later
                DisabledPawns = null, // To be filled out later
                ConfirmValidation = () => {
                    if (sourceParentChildPawn == null) {
                        return "EdB.PC.AddRelationship.Error.TargetRequired";
                    }
                    else {
                        return null;
                    }
                }
            };

            WidgetTable<CustomizedPawn>.RowGroup targetNewPawnGroup = null;

            sourcePawnDialog.CloseAction = () => {
                List<PawnRelationDef> relationDefs = RelationshipManager.AllowedRelationships.Select((PawnRelationDef def) => {
                    return def;
                }).ToList();
                relationDefs.Sort((PawnRelationDef a, PawnRelationDef b) => {
                    return a.GetGenderSpecificLabelCap(sourceParentChildPawn.Pawn).CompareTo(b.GetGenderSpecificLabelCap(sourceParentChildPawn.Pawn));
                });
                relationshipDialog.Options = relationDefs;
                Find.WindowStack.Add(relationshipDialog);
            };
            relationshipDialog.CloseAction = () => {
                SetDisabledTargets(sourceParentChildPawn, selectedRelationship);
                targetPawnDialog.DisabledPawns = disabledTargets;
                targetPawnDialog.PawnForCompatibility = sourceParentChildPawn;
                var targetRowGroups = new List<WidgetTable<CustomizedPawn>.RowGroup>();
                targetRowGroups.Add(new WidgetTable<CustomizedPawn>.RowGroup("EdB.PC.AddParentChild.Header.SelectColonist".Translate(),
                    RelationshipManager.AvailableColonyPawns.Where(pawn => pawn != sourceParentChildPawn)));
                targetRowGroups.Add(new WidgetTable<CustomizedPawn>.RowGroup("<b>" + "EdB.PC.AddParentChild.Header.SelectWorldPawn".Translate() + "</b>",
                    RelationshipManager.AvailableWorldPawns.Where(pawn => pawn != sourceParentChildPawn)));
                targetNewPawnGroup = new WidgetTable<CustomizedPawn>.RowGroup("EdB.PC.AddParentChild.Header.CreateTemporaryPawn".Translate(),
                    RelationshipManager.AvailableTemporaryPawns);
                targetRowGroups.Add(targetNewPawnGroup);
                targetPawnDialog.RowGroups = targetRowGroups;

                Find.WindowStack.Add(targetPawnDialog);
            };
            targetPawnDialog.CloseAction = () => {
                // If the user selected a new pawn, replace the pawn in the new pawn list with another one.
                int index = targetNewPawnGroup.Rows.FirstIndexOf((CustomizedPawn p) => {
                    return p == targetParentChildPawn;
                });
                if (index > -1 && index < RelationshipManager.AvailableTemporaryPawns.Count()) {
                    targetParentChildPawn = RelationshipManager.ReplaceNewTemporaryCharacter(index);
                }
                this.RelationshipAdded?.Invoke(RelationshipManager.FindInverseRelationship(selectedRelationship), sourceParentChildPawn, targetParentChildPawn);
            };
            Find.WindowStack.Add(sourcePawnDialog);
        }
        
        public void SetDisabledTargets(CustomizedPawn source, PawnRelationDef relationDef) {
            disabledTargets.Clear();
            CarefullyPawnRelationDef extendedDef = DefDatabase<CarefullyPawnRelationDef>.GetNamedSilentFail(relationDef.defName);
            foreach (var pawn in RelationshipManager.ColonyAndWorldPawnsForRelationships) {
                if (source == pawn) {
                    disabledTargets.Add(pawn);
                    continue;
                }
                bool bloodRelation = relationDef.familyByBloodRelation;
                foreach (CustomizedRelationship r in State.Customizations.Relationships) {
                    if (r.Source == source && r.Target == pawn) {
                        if (r.InverseDef == relationDef || r.Def == relationDef) {
                            disabledTargets.Add(pawn);
                            break;
                        }
                        else if (extendedDef != null && extendedDef.conflicts != null && extendedDef.conflicts.Contains(r.Def.defName)) {
                            disabledTargets.Add(pawn);
                            break;
                        }
                        else if (extendedDef != null && extendedDef.conflicts != null && extendedDef.conflicts.Contains(r.InverseDef.defName)) {
                            disabledTargets.Add(pawn);
                            break;
                        }
                    }
                    else if (r.Source == pawn && r.Target == source) {
                        if (r.Def == relationDef || r.InverseDef == relationDef) {
                            disabledTargets.Add(pawn);
                            break;
                        }
                        else if (extendedDef != null && extendedDef.conflicts != null && extendedDef.conflicts.Contains(r.InverseDef.defName)) {
                            disabledTargets.Add(pawn);
                            break;
                        }
                        else if (extendedDef != null && extendedDef.conflicts != null && extendedDef.conflicts.Contains(r.Def.defName)) {
                            disabledTargets.Add(pawn);
                            break;
                        }
                    }
                }
            }
        }
    }
}
