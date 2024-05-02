using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class PanelName : PanelBase {
        public delegate void UpdateNameHandler(string name);
        public delegate void RandomizeNameHandler();

        public event UpdateNameHandler FirstNameUpdated;
        public event UpdateNameHandler NickNameUpdated;
        public event UpdateNameHandler LastNameUpdated;
        public event RandomizeNameHandler NameRandomized;

        protected Rect RectFirstName;
        protected Rect RectNickName;
        protected Rect RectLastName;
        protected Rect RectRandomize;
        protected Rect RectInfo;
        public ModState State { get; set; }
        public ViewState ViewState { get; set; }

        public override void Resize(Rect rect) {
            base.Resize(rect);

            float panelPadding = 12;
            float fieldPadding = 4;
            float fieldHeight = 28;
            Vector2 SizeRandomize = new Vector2(22, 22);
            Vector2 SizeInfo = new Vector2(24, 24);
            RectRandomize = new Rect(PanelRect.width - panelPadding - SizeRandomize.y,
                PanelRect.HalfHeight() - SizeRandomize.y * 0.5f, SizeRandomize.x, SizeRandomize.y);
            RectInfo = new Rect(panelPadding, PanelRect.HalfHeight() - SizeInfo.HalfY(), SizeInfo.x, SizeInfo.y);

            float availableSpace = PanelRect.width - (panelPadding * 2) - RectInfo.width - RectRandomize.width - fieldPadding;

            float firstMinWidth = 80;
            float nickMinWidth = 90;
            float lastMinWidth = 90;
            float fieldsMinWidth = firstMinWidth + nickMinWidth + lastMinWidth + (fieldPadding * 2);
            float extraSpace = availableSpace - fieldsMinWidth;
            float extraForField = Mathf.Floor(extraSpace / 3);
            float top = PanelRect.HalfHeight() - fieldHeight * 0.5f;

            RectFirstName = new Rect(RectInfo.xMax, top, firstMinWidth + extraForField, fieldHeight);
            RectNickName = new Rect(RectFirstName.xMax + fieldPadding, top, nickMinWidth + extraForField, fieldHeight);
            RectLastName = new Rect(RectNickName.xMax + fieldPadding, top, lastMinWidth + extraForField, fieldHeight);

            // Shift the info button to the left a bit to making the spacing look better.
            RectInfo.x -= 6;
        }
        protected override void DrawPanelContent() {
            CustomizedPawn customizedPawn = ViewState.CurrentPawn;
            Pawn pawn = customizedPawn?.Pawn;
            if (customizedPawn == null) {
                Logger.Debug("customizedPawn was null");
                return;
            }
            if (pawn == null) {
                Logger.Debug("pawn was null");
                return;
            }

            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            Style.SetGUIColorForButton(RectInfo);
            GUI.DrawTexture(RectInfo, Textures.TextureButtonInfo);
            if (Widgets.ButtonInvisible(RectInfo)) {
                Find.WindowStack.Add((Window)new Dialog_InfoCard(pawn));
            }
            GUI.color = Color.white;

            string first = "";
            string nick = "";
            string last = "";
            NameTriple nameTriple = pawn.Name as NameTriple;
            NameSingle nameSingle = pawn.Name as NameSingle;
            float x = RectFirstName.x;
            if (nameTriple != null) {
                first = nameTriple.First;
                nick = nameTriple.Nick;
                last = nameTriple.Last;
            }
            else if (nameSingle != null) {
                nick = (pawn.Name as NameSingle).Name;
            }

            float randomizeButtonOffset = RectRandomize.x - RectLastName.xMax;
            float randomizeButtonX = RectLastName.xMax + randomizeButtonOffset;
            if (nameTriple != null) {
                string text;
                GUI.SetNextControlName("PrepareCarefullyFirst");
                text = Widgets.TextField(RectFirstName, first);
                if (text != first && FirstNameUpdated != null) {
                    FirstNameUpdated?.Invoke(text);
                }
                if (nick == first || nick == last) {
                    GUI.color = new Color(1, 1, 1, 0.5f);
                }
                GUI.SetNextControlName("PrepareCarefullyNick");
                text = Widgets.TextField(RectNickName, nick);
                if (text != nick && NickNameUpdated != null) {
                    NickNameUpdated?.Invoke(text);
                }
                GUI.color = Color.white;
                GUI.SetNextControlName("PrepareCarefullyLast");
                text = Widgets.TextField(RectLastName, last);
                if (text != last && LastNameUpdated != null) {
                    LastNameUpdated?.Invoke(text);
                }
                TooltipHandler.TipRegion(RectFirstName, "FirstNameDesc".Translate());
                TooltipHandler.TipRegion(RectNickName, "ShortIdentifierDesc".Translate());
                TooltipHandler.TipRegion(RectLastName, "LastNameDesc".Translate());
            }
            else if (nameSingle != null) {
                string text;
                GUI.SetNextControlName("PrepareCarefullyNick");
                text = Widgets.TextField(RectFirstName, nick);
                if (text != nick && NickNameUpdated != null) {
                    NickNameUpdated?.Invoke(text);
                }
                GUI.color = Color.white;
                TooltipHandler.TipRegion(RectFirstName, "ShortIdentifierDesc".Translate());
                randomizeButtonX = RectFirstName.xMax + randomizeButtonOffset;
            }

            // Random button
            Rect randomizeRect = new Rect(randomizeButtonX, RectRandomize.y, RectRandomize.width, RectRandomize.height);
            Style.SetGUIColorForButton(randomizeRect);
            GUI.DrawTexture(randomizeRect, Textures.TextureButtonRandom);
            if (Widgets.ButtonInvisible(randomizeRect, false)) {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                GUI.FocusControl(null);
                if (LastNameUpdated != null) {
                    NameRandomized?.Invoke();
                }
            }
        }

        public void ClearSelection() {
            GUI.FocusControl(null);
        }
    }
}
