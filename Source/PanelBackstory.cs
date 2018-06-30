using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class PanelBackstory : PanelBase {
        public delegate void UpdateBackstoryHandler(BackstorySlot slot, Backstory backstory);
        public delegate void RandomizeBackstoriesHandler();

        public event UpdateBackstoryHandler BackstoryUpdated;
        public event RandomizeBackstoriesHandler BackstoriesRandomized;

        private ProviderBackstories providerBackstories = new ProviderBackstories();
        private Rect RectAdulthoodLabel;
        private Rect RectChildhoodLabel;
        private Rect RectAdulthoodField;
        private Rect RectChildhoodField;
        private Field FieldChildhood = new Field();
        private Field FieldAdulthood = new Field();
        public PanelBackstory() {
        }
        public override string PanelHeader {
            get {
                return "Backstory".Translate();
            }
        }


        public override void Resize(Rect rect) {
            base.Resize(rect);

            float fieldHeight = 28;
            float fieldPadding = 3;
            float expandedFieldHeight = fieldHeight + fieldPadding * 2;
            float contentHeight = expandedFieldHeight * 2;
            float top = BodyRect.MiddleY() - contentHeight * 0.5f;

            float panelPadding = 16;
            float contentWidth = PanelRect.width - panelPadding - panelPadding;

            GameFont savedFont = Text.Font;
            Text.Font = GameFont.Small;
            Vector2 sizeChildhood = Text.CalcSize("Childhood".Translate());
            Vector2 sizeAdulthood = Text.CalcSize("Adulthood".Translate());
            Text.Font = savedFont;

            float labelWidth = Mathf.Max(sizeChildhood.x, sizeAdulthood.x);
            float labelPadding = 8;
            float extendedTextWidth = labelWidth + labelPadding;
            float fieldWidth = contentWidth - extendedTextWidth;

            RectChildhoodLabel = new Rect(panelPadding, top + 1, labelWidth, fieldHeight);
            RectAdulthoodLabel = new Rect(panelPadding, top + expandedFieldHeight + 1, labelWidth, fieldHeight);
            RectChildhoodField = new Rect(RectChildhoodLabel.xMax + labelPadding, top, fieldWidth, fieldHeight);
            RectAdulthoodField = new Rect(RectAdulthoodLabel.xMax + labelPadding, top + expandedFieldHeight, fieldWidth, fieldHeight);
            FieldChildhood.Rect = RectChildhoodField;
            FieldAdulthood.Rect = RectAdulthoodField;
        }
        protected override void DrawPanelContent(State state) {
            base.DrawPanelContent(state);

            CustomPawn pawn = state.CurrentPawn;
            bool isAdult = pawn.Adulthood != null;

            Text.Font = GameFont.Small;
            GUI.color = Style.ColorText;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(RectChildhoodLabel, "Childhood".Translate());
            if (!isAdult) {
                GUI.color = Style.ColorControlDisabled;
            }
            Widgets.Label(RectAdulthoodLabel, "Adulthood".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            if (pawn.Childhood != null) {
                FieldChildhood.Label = pawn.Childhood.TitleCapFor(pawn.Gender);
            }
            else {
                FieldChildhood.Label = null;
            }
            FieldChildhood.Tip = pawn.Childhood.FullDescriptionFor(pawn.Pawn);
            FieldChildhood.ClickAction = () => {
                ShowBackstoryDialog(pawn, BackstorySlot.Childhood);
            };
            FieldChildhood.PreviousAction = () => {
                NextBackstory(pawn, BackstorySlot.Childhood, -1);
            };
            FieldChildhood.NextAction = () => {
                NextBackstory(pawn, BackstorySlot.Childhood, 1);
            };

            FieldAdulthood.Enabled = isAdult;
            if (isAdult) {
                FieldAdulthood.Label = pawn.Adulthood.TitleCapFor(pawn.Gender);
                FieldAdulthood.Tip = pawn.Adulthood.FullDescriptionFor(pawn.Pawn);
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

            FieldChildhood.Draw();
            FieldAdulthood.Draw();

            // Randomize button.
            Rect randomRect = new Rect(PanelRect.width - 32, 9, 22, 22);
            if (randomRect.Contains(Event.current.mousePosition)) {
                GUI.color = Style.ColorButtonHighlight;
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(randomRect, Textures.TextureButtonRandom);
            if (Widgets.ButtonInvisible(randomRect, false)) {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                BackstoriesRandomized();
            }

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        protected void ShowBackstoryDialog(CustomPawn customPawn, BackstorySlot slot) {
            Backstory originalBackstory = (slot == BackstorySlot.Childhood) ? customPawn.Childhood : customPawn.Adulthood;
            Backstory selectedBackstory = originalBackstory;
            Dialog_Options<Backstory> dialog = new Dialog_Options<Backstory>(slot == BackstorySlot.Childhood ?
                    this.providerBackstories.ChildhoodBackstories
                    : this.providerBackstories.AdulthoodBackstories) {
                NameFunc = (Backstory backstory) => {
                    return backstory.TitleCapFor(customPawn.Gender);
                },
                DescriptionFunc = (Backstory backstory) => {
                    return backstory.FullDescriptionFor(customPawn.Pawn);
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
            backstories = (slot == BackstorySlot.Childhood) ? providerBackstories.ChildhoodBackstories
                : providerBackstories.AdulthoodBackstories;
        }
    }
}
