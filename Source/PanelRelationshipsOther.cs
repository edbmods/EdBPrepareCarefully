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
        private float SpacingGender = 9;
        private Vector2 SizeGender = new Vector2(48, 48);

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
        private string GetProfessionLabel(CustomPawn pawn) {
            bool hidden = pawn.Type == CustomPawnType.Hidden || pawn.Type == CustomPawnType.Temporary;
            if (!hidden) {
                return pawn.Type == CustomPawnType.Colonist ? "EdB.PC.AddParentChild.Colony".Translate() : "EdB.PC.AddParentChild.World".Translate();
            }
            else {
                return pawn.Type == CustomPawnType.Temporary ? "EdB.PC.AddParentChild.Temporary".Translate() : "EdB.PC.AddParentChild.World".Translate();
            }
        }
        protected void DrawPortrait(Rect rect, CustomPawn pawn) {
            bool hidden = pawn.Type == CustomPawnType.Hidden || pawn.Type == CustomPawnType.Temporary;
            if (!hidden) {
                Rect portraitRect = rect.InsetBy(6);
                portraitRect.y -= 8;
                var portraitTexture = pawn.GetPortrait(portraitRect.size);
                GUI.DrawTexture(portraitRect.OffsetBy(0, -4), portraitTexture);
            }
            else {
                GUI.color = Style.ColorButton;
                Rect portraitRect = new Rect(rect.MiddleX() - SizeGender.HalfX(), rect.y + SpacingGender, SizeGender.x, SizeGender.y);
                if (pawn.Gender == Gender.Female) {
                    GUI.DrawTexture(portraitRect, Textures.TextureGenderFemaleLarge);
                }
                else if (pawn.Gender == Gender.Male) {
                    GUI.DrawTexture(portraitRect, Textures.TextureGenderMaleLarge);
                }
                else {
                    GUI.DrawTexture(portraitRect, Textures.TextureGenderlessLarge);
                }
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
            Widgets.Label(sourcePawnName, relationship.source.ShortName);
            GUI.color = Color.white;

            Rect sourceProfessionName = new Rect(sourcePawnRect.x, sourcePawnRect.yMax - 18, sourcePawnRect.width, 18);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.LowerCenter;
            GUI.color = Style.ColorText;
            Widgets.Label(sourceProfessionName, GetProfessionLabel(relationship.source));
            GUI.color = Color.white;

            DrawPortrait(sourcePawnRect, relationship.source);

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
            Widgets.Label(targetPawnName, relationship.target.ShortName);
            GUI.color = Color.white;

            Rect targetProfessionName = new Rect(targetPawnRect.x, targetPawnRect.yMax - 18, targetPawnRect.width, 18);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.LowerCenter;
            GUI.color = Style.ColorText;
            Widgets.Label(targetProfessionName, GetProfessionLabel(relationship.target));
            GUI.color = Color.white;

            DrawPortrait(targetPawnRect, relationship.target);
            
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
            bool hidden = pawn.Type == CustomPawnType.Hidden || pawn.Type == CustomPawnType.Temporary;
            if (!hidden) {
                string age = (pawn.ChronologicalAge == pawn.BiologicalAge) ? "EdB.PC.Pawn.AgeWithoutChronological".Translate(pawn.BiologicalAge)
                    : "EdB.PC.Pawn.AgeWithChronological".Translate(pawn.BiologicalAge, pawn.ChronologicalAge);
                string description = (pawn.Gender != Gender.None) ? "EdB.PC.Pawn.PawnDescriptionWithGender".Translate(pawn.ProfessionLabel, pawn.Gender.GetLabel(), age)
                    : "EdB.PC.AddParentChild.PawnDescriptionWithNoGender".Translate(pawn.ProfessionLabel, age);
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
            CustomPawn sourceParentChildPawn = null;
            PawnRelationDef selectedRelationship = null;
            CustomPawn targetParentChildPawn = null;

            Dialog_Options<PawnRelationDef> relationshipDialog =
                new Dialog_Options<PawnRelationDef>(null) {
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

            List<WidgetTable<CustomPawn>.RowGroup> sourceRowGroups = new List<WidgetTable<CustomPawn>.RowGroup>();
            sourceRowGroups.Add(new WidgetTable<CustomPawn>.RowGroup("EdB.PC.AddParentChild.Header.SelectColonist".Translate(),
                PrepareCarefully.Instance.RelationshipManager.ColonyAndWorldPawns.Where((CustomPawn pawn) => {
                    return pawn.Type == CustomPawnType.Colonist;
                })));
            List<CustomPawn> hiddenPawnsForSource = PrepareCarefully.Instance.RelationshipManager.HiddenPawns.ToList();
            hiddenPawnsForSource.Sort((a, b) => {
                if (a.Type != b.Type) {
                    return a.Type == CustomPawnType.Hidden ? -1 : 1;
                }
                else {
                    int aInt = a.Index == null ? 0 : a.Index.Value;
                    int bInt = b.Index == null ? 0 : b.Index.Value;
                    return aInt.CompareTo(bInt);
                }
            });
            sourceRowGroups.Add(new WidgetTable<CustomPawn>.RowGroup("<b>" + "EdB.PC.AddParentChild.Header.SelectWorldPawn".Translate() + "</b>",
                PrepareCarefully.Instance.RelationshipManager.ColonyAndWorldPawns.Where((CustomPawn pawn) => {
                    return pawn.Type != CustomPawnType.Colonist;
                }).Concat(hiddenPawnsForSource)));
            WidgetTable<CustomPawn>.RowGroup sourceNewPawnGroup = new WidgetTable<CustomPawn>.RowGroup("EdB.PC.AddParentChild.Header.CreateTemporaryPawn".Translate(), PrepareCarefully.Instance.RelationshipManager.TemporaryPawns);
            sourceRowGroups.Add(sourceNewPawnGroup);

            DialogSelectParentChildPawn sourcePawnDialog = new DialogSelectParentChildPawn() {
                HeaderLabel = "EdB.PC.AddRelationship.Header.Source".Translate(),
                SelectAction = (CustomPawn pawn) => { sourceParentChildPawn = pawn; },
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
                    int index = sourceNewPawnGroup.Rows.FirstIndexOf((CustomPawn p) => {
                        return p == targetParentChildPawn;
                    });
                    if (index > -1 && index < PrepareCarefully.Instance.RelationshipManager.TemporaryPawns.Count) {
                        targetParentChildPawn = PrepareCarefully.Instance.RelationshipManager.ReplaceNewTemporaryCharacter(index);
                    }
                }
            };

            DialogSelectParentChildPawn targetPawnDialog = new DialogSelectParentChildPawn() {
                HeaderLabel = "EdB.PC.AddRelationship.Header.Target".Translate(),
                SelectAction = (CustomPawn pawn) => { targetParentChildPawn = pawn; },
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

            WidgetTable<CustomPawn>.RowGroup targetNewPawnGroup = null;

            sourcePawnDialog.CloseAction = () => {
                List<PawnRelationDef> relationDefs = PrepareCarefully.Instance.RelationshipManager.AllowedRelationships.Select((PawnRelationDef def) => {
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

                List<WidgetTable<CustomPawn>.RowGroup> targetRowGroups = new List<WidgetTable<CustomPawn>.RowGroup>();
                targetRowGroups.Add(new WidgetTable<CustomPawn>.RowGroup("EdB.PC.AddParentChild.Header.SelectColonist".Translate(),
                    PrepareCarefully.Instance.RelationshipManager.ColonyAndWorldPawns.Where((CustomPawn pawn) => {
                        return pawn.Type == CustomPawnType.Colonist && pawn != sourceParentChildPawn;
                    })));
                List<CustomPawn> hiddenPawnsForTarget = PrepareCarefully.Instance.RelationshipManager.HiddenPawns.ToList();
                hiddenPawnsForTarget.Sort((a, b) => {
                    if (a.Type != b.Type) {
                        return a.Type == CustomPawnType.Hidden ? -1 : 1;
                    }
                    else {
                        int aInt = a.Index == null ? 0 : a.Index.Value;
                        int bInt = b.Index == null ? 0 : b.Index.Value;
                        return aInt.CompareTo(bInt);
                    }
                });
                targetRowGroups.Add(new WidgetTable<CustomPawn>.RowGroup("<b>" + "EdB.PC.AddParentChild.Header.SelectWorldPawn".Translate() + "</b>",
                    PrepareCarefully.Instance.RelationshipManager.ColonyAndWorldPawns.Where((CustomPawn pawn) => {
                        return pawn.Type != CustomPawnType.Colonist && pawn != sourceParentChildPawn;
                    }).Concat(hiddenPawnsForTarget)));
                targetNewPawnGroup = new WidgetTable<CustomPawn>.RowGroup("EdB.PC.AddParentChild.Header.CreateTemporaryPawn".Translate(), PrepareCarefully.Instance.RelationshipManager.TemporaryPawns);
                targetRowGroups.Add(targetNewPawnGroup);
                targetPawnDialog.RowGroups = targetRowGroups;

                Find.WindowStack.Add(targetPawnDialog);
            };
            targetPawnDialog.CloseAction = () => {
                // If the user selected a new pawn, replace the pawn in the new pawn list with another one.
                int index = targetNewPawnGroup.Rows.FirstIndexOf((CustomPawn p) => {
                    return p == targetParentChildPawn;
                });
                if (index > -1 && index < PrepareCarefully.Instance.RelationshipManager.TemporaryPawns.Count) {
                    targetParentChildPawn = PrepareCarefully.Instance.RelationshipManager.ReplaceNewTemporaryCharacter(index);
                }
                this.RelationshipAdded(PrepareCarefully.Instance.RelationshipManager.FindInverseRelationship(selectedRelationship), sourceParentChildPawn, targetParentChildPawn);
            };
            Find.WindowStack.Add(sourcePawnDialog);
        }
        
        public void SetDisabledTargets(CustomPawn source, PawnRelationDef relationDef) {
            disabledTargets.Clear();
            CarefullyPawnRelationDef extendedDef = DefDatabase<CarefullyPawnRelationDef>.GetNamedSilentFail(relationDef.defName);
            foreach (var pawn in PrepareCarefully.Instance.RelationshipManager.ColonyAndWorldPawns) {
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
