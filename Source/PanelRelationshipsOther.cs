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
        public delegate void AddRelationshipHandler(PawnRelationDef def, CustomPawn source, CustomPawn target);
        public delegate void RemoveRelationshipHandler(CustomRelationship relationship);

        public event AddRelationshipHandler RelationshipAdded;
        public event RemoveRelationshipHandler RelationshipRemoved;

        private ScrollViewVertical scrollView = new ScrollViewVertical();
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

        protected List<PawnRelationDef> relationDefs = new List<PawnRelationDef>();
        protected HashSet<PawnRelationDef> disabledRelationships = new HashSet<PawnRelationDef>();
        protected HashSet<CustomPawn> disabledTargets = new HashSet<CustomPawn>();
        protected List<CustomRelationship> relationshipsToDelete = new List<CustomRelationship>();
        public PanelRelationshipsOther() {
            relationDefs.AddRange(DefDatabase<PawnRelationDef>.AllDefs.ToList().FindAll((PawnRelationDef def) => {
                if (def.familyByBloodRelation) {
                    return false;
                }
                MethodInfo info = def.workerClass.GetMethod("CreateRelation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
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
        protected override void DrawPanelContent(State state) {
            base.DrawPanelContent(state);
            Vector2 cursor = Vector2.zero;

            GUI.BeginGroup(RectScrollView);
            scrollView.Begin(new Rect(Vector2.zero, RectScrollView.size));
            try {
                foreach (CustomRelationship relationship in PrepareCarefully.Instance.RelationshipManager.Relationships) {
                    cursor = DrawRelationship(cursor, relationship);
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
                    RelationshipRemoved(r);
                }
                relationshipsToDelete.Clear();
            }
        }
        protected Vector2 DrawRelationship(Vector2 cursor, CustomRelationship relationship) {
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
            Widgets.Label(sourcePawnName, relationship.source.Pawn.LabelShort);
            GUI.color = Color.white;

            Rect sourceProfessionName = new Rect(sourcePawnRect.x, sourcePawnRect.yMax - 18, sourcePawnRect.width, 18);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.LowerCenter;
            GUI.color = Style.ColorText;
            Widgets.Label(sourceProfessionName, relationship.source.ProfessionLabelShort);
            GUI.color = Color.white;

            Rect sourcePortraitRect = sourcePawnRect.InsetBy(6);
            sourcePortraitRect.y -= 8;
            var sourcePortraitTexture = relationship.source.GetPortrait(sourcePortraitRect.size);
            GUI.DrawTexture(sourcePortraitRect.OffsetBy(0, -4), sourcePortraitTexture);

            TooltipHandler.TipRegion(sourcePawnRect, GetTooltipText(relationship.source));

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
            Widgets.Label(targetPawnName, relationship.target.Pawn.LabelShort);
            GUI.color = Color.white;

            Rect targetProfessionName = new Rect(targetPawnRect.x, targetPawnRect.yMax - 18, targetPawnRect.width, 18);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.LowerCenter;
            GUI.color = Style.ColorText;
            Widgets.Label(targetProfessionName, relationship.target.ProfessionLabelShort);
            GUI.color = Color.white;
            
            Rect targetPortraitRect = targetPawnRect.InsetBy(6);
            targetPortraitRect.y -= 8;
            var targetPortraitTexture = relationship.target.GetPortrait(targetPortraitRect.size);
            GUI.DrawTexture(targetPortraitRect.OffsetBy(0, -4), targetPortraitTexture);
            
            TooltipHandler.TipRegion(targetPawnRect, GetTooltipText(relationship.target));

            Rect sourceRelLabelRect = new Rect(sourcePawnRect.xMax, sourcePawnRect.y + SizeLabelSpacing.y, targetPawnRect.x - sourcePawnRect.xMax, HeightLabel);
            sourceRelLabelRect.width -= (SizeArrow.x + SizeLabelSpacing.x);
            GUI.color = ColorPawnSource;
            GUI.DrawTexture(sourceRelLabelRect, BaseContent.WhiteTex);
            Rect sourceRelArrowRect = new Rect(sourceRelLabelRect.xMax, sourceRelLabelRect.MiddleY() - SizeArrow.HalfY(), SizeArrow.x, SizeArrow.y);
            GUI.DrawTexture(sourceRelArrowRect, Textures.TextureArrowRight);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Style.ColorText;
            Widgets.Label(sourceRelLabelRect.OffsetBy(0, 1), relationship.inverseDef.GetGenderSpecificLabelCap(relationship.source.Pawn));

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
            Widgets.Label(targetRelLabelRect.OffsetBy(0, 1), relationship.def.GetGenderSpecificLabelCap(relationship.target.Pawn));

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            cursor.x += SizeRelationship.x + SizeRelationshipSpacing.x;
            return cursor;
        }
        protected string GetTooltipText(CustomPawn pawn) {
            string age = (pawn.ChronologicalAge == pawn.BiologicalAge) ? "EdB.PC.Pawn.AgeWithoutChronological".Translate(new object[] { pawn.BiologicalAge })
                : "EdB.PC.Pawn.AgeWithChronological".Translate(new object[] { pawn.BiologicalAge, pawn.ChronologicalAge });
            string description = (pawn.Gender != Gender.None) ? "EdB.PC.Pawn.PawnDescriptionWithGender".Translate(new object[] { pawn.ProfessionLabel, pawn.Gender.GetLabel(), age })
                : "EdB.PC.AddParentChild.PawnDescriptionWithNoGender".Translate(new object[] { pawn.ProfessionLabel, age });
            return pawn.Pawn.Name.ToStringFull + "\n" + description;
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
            CustomPawn sourcePawn = null;
            PawnRelationDef selectedRelationship = null;
            CustomPawn targetPawn = null;

            Dialog_Options<PawnRelationDef> relationshipDialog =
                new Dialog_Options<PawnRelationDef>(null) {
                    ConfirmButtonLabel = "EdB.PC.Common.Next",
                    CancelButtonLabel = "EdB.PC.Common.Cancel",
                    HeaderLabel = "EdB.PC.AddRelationship.Header.Relationship",
                    NameFunc = (PawnRelationDef def) => {
                        return def.GetGenderSpecificLabelCap(sourcePawn.Pawn);
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

            DialogSelectPawn sourcePawnDialog = new DialogSelectPawn() {
                HeaderLabel = "EdB.PC.AddRelationship.Header.Source".Translate(),
                SelectAction = (CustomPawn pawn) => { sourcePawn = pawn; },
                Pawns = PrepareCarefully.Instance.Pawns,
                ConfirmValidation = () => {
                    if (sourcePawn == null) {
                        return "EdB.PC.AddRelationship.Error.SourceRequired";
                    }
                    else {
                        return null;
                    }
                }
            };
            DialogSelectPawn targetPawnDialog = new DialogSelectPawn() {
                HeaderLabel = "EdB.PC.AddRelationship.Header.Target".Translate(),
                SelectAction = (CustomPawn pawn) => { targetPawn = pawn; },
                ConfirmValidation = () => {
                    if (targetPawn == null) {
                        return "EdB.PC.AddRelationship.Error.TargetRequired";
                    }
                    else {
                        return null;
                    }
                }
            };

            sourcePawnDialog.CloseAction = () => {
                List<PawnRelationDef> relationDefs = PrepareCarefully.Instance.RelationshipManager.AllowedRelationships.Select((PawnRelationDef def) => {
                    return def;
                }).ToList();
                relationDefs.Sort((PawnRelationDef a, PawnRelationDef b) => {
                    return a.GetGenderSpecificLabelCap(sourcePawn.Pawn).CompareTo(b.GetGenderSpecificLabelCap(sourcePawn.Pawn));
                });
                relationshipDialog.Options = relationDefs;
                Find.WindowStack.Add(relationshipDialog);
            };
            relationshipDialog.CloseAction = () => {
                SetDisabledTargets(sourcePawn, selectedRelationship);
                targetPawnDialog.DisabledPawns = disabledTargets;
                List<CustomPawn> otherPawns = PrepareCarefully.Instance.Pawns.FindAll((CustomPawn p) => {
                    return p != sourcePawn;
                });
                targetPawnDialog.Pawns = otherPawns;
                Find.WindowStack.Add(targetPawnDialog);
            };
            targetPawnDialog.CloseAction = () => {
                this.RelationshipAdded(PrepareCarefully.Instance.RelationshipManager.FindInverseRelationship(selectedRelationship), sourcePawn, targetPawn);
            };
            Find.WindowStack.Add(sourcePawnDialog);
        }
        
        public void SetDisabledTargets(CustomPawn source, PawnRelationDef relationDef) {
            disabledTargets.Clear();
            CarefullyPawnRelationDef extendedDef = DefDatabase<CarefullyPawnRelationDef>.GetNamedSilentFail(relationDef.defName);
            foreach (var pawn in PrepareCarefully.Instance.Pawns) {
                if (source == pawn) {
                    disabledTargets.Add(pawn);
                    continue;
                }
                bool bloodRelation = relationDef.familyByBloodRelation;
                foreach (CustomRelationship r in PrepareCarefully.Instance.RelationshipManager.Relationships) {
                    if (r.source == source && r.target == pawn) {
                        if (r.inverseDef == relationDef || r.def == relationDef) {
                            disabledTargets.Add(pawn);
                            break;
                        }
                        else if (extendedDef != null && extendedDef.conflicts != null && extendedDef.conflicts.Contains(r.def.defName)) {
                            disabledTargets.Add(pawn);
                            break;
                        }
                        else if (extendedDef != null && extendedDef.conflicts != null && extendedDef.conflicts.Contains(r.inverseDef.defName)) {
                            disabledTargets.Add(pawn);
                            break;
                        }
                    }
                    else if (r.source == pawn && r.target == source) {
                        if (r.def == relationDef || r.inverseDef == relationDef) {
                            disabledTargets.Add(pawn);
                            break;
                        }
                        else if (extendedDef != null && extendedDef.conflicts != null && extendedDef.conflicts.Contains(r.inverseDef.defName)) {
                            disabledTargets.Add(pawn);
                            break;
                        }
                        else if (extendedDef != null && extendedDef.conflicts != null && extendedDef.conflicts.Contains(r.def.defName)) {
                            disabledTargets.Add(pawn);
                            break;
                        }
                    }
                }
            }
        }
    }
}
