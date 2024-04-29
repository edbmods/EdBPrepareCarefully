using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class PanelSkills : PanelBase {
        public delegate void ResetSkillsButtonClickedHandler();
        public delegate void ClearSkillsButtonClickedHandler();
        public delegate void IncrementSkillButtonClickedHandler(SkillDef skill);
        public delegate void DecrementSkillButtonClickedHandler(SkillDef skill);
        public delegate void SkillBarClickedHandler(SkillDef skill, int value);
        public delegate void PassionButtonClickedHandler(SkillDef skill);

        public event ClearSkillsButtonClickedHandler ClearSkillsButtonClicked;
        public event ResetSkillsButtonClickedHandler ResetSkillsButtonClicked;
        public event PassionButtonClickedHandler PassionButtonClicked;
        public event IncrementSkillButtonClickedHandler IncrementSkillButtonClicked;
        public event DecrementSkillButtonClickedHandler DecrementSkillButtonClicked;
        public event SkillBarClickedHandler SkillBarClicked;

        protected WidgetScrollViewVertical scrollView = new WidgetScrollViewVertical();
        public ModState State { get; set; }
        public ViewState ViewState { get; set; }

        public PanelSkills() {
        }
        public override string PanelHeader {
            get {
                return "Skills".Translate();
            }
        }

        private static Color ColorSkillDisabled = new Color(1f, 1f, 1f, 0.5f);

        protected static Rect RectButtonClearSkills;
        protected static Rect RectButtonResetSkills;
        protected static Rect RectLabel;
        protected static Rect RectPassion;
        protected static Rect RectSkillBar;
        protected static Rect RectButtonDecrement;
        protected static Rect RectButtonIncrement;
        protected static Rect RectScrollFrame;
        protected static Rect RectScrollView;

        public override void Resize(Rect rect) {
            base.Resize(rect);

            float panelPaddingLeft = 12;
            float panelPaddingRight = 10;
            float panelPaddingBottom = 10;
            float panelPaddingTop = 4;
            float top = BodyRect.y + panelPaddingTop;

            GameFont savedFont = Text.Font;
            Text.Font = GameFont.Small;
            Vector2 maxLabelSize = new Vector2(float.MinValue, float.MinValue);
            foreach (SkillDef current in DefDatabase<SkillDef>.AllDefs) {
                Vector2 labelSize = Text.CalcSize(current.skillLabel);
                // Need to add some padding because the "n" at the end of "Construction" gets cut off if we don't.
                labelSize += new Vector2(4, 0);
                maxLabelSize.x = Mathf.Max(labelSize.x, maxLabelSize.x);
                maxLabelSize.y = Mathf.Max(labelSize.y, maxLabelSize.y);
            }
            Text.Font = savedFont;

            float labelPadding = 4;
            float availableContentWidth = PanelRect.width - panelPaddingLeft - panelPaddingRight;
            Vector2 passionSize = new Vector2(24, 24);
            float passionPadding = 2;
            Vector2 arrowButtonSize = new Vector2(16, 16);
            float arrowsWidth = 32;
            Vector2 skillBarSize = new Vector2(availableContentWidth - passionSize.x - passionPadding
                - arrowsWidth - maxLabelSize.x - labelPadding, 22);

            RectButtonClearSkills = new Rect(PanelRect.width - 65, 9, 20, 20);
            RectButtonResetSkills = new Rect(PanelRect.width - 38, 8, 23, 21);
            RectLabel = new Rect(0, 0, maxLabelSize.x, maxLabelSize.y);
            RectPassion = new Rect(RectLabel.xMax + labelPadding, (maxLabelSize.y * 0.5f - passionSize.y * 0.5f),
                passionSize.x, passionSize.y);
            RectSkillBar = new Rect(RectPassion.xMax + passionPadding, (maxLabelSize.y * 0.5f - skillBarSize.y * 0.5f),
                skillBarSize.x, skillBarSize.y);
            RectButtonDecrement = new Rect(RectSkillBar.xMax, (maxLabelSize.y * 0.5f - arrowButtonSize.y * 0.5f),
                arrowButtonSize.x, arrowButtonSize.y);
            RectButtonIncrement = new Rect(RectButtonDecrement.xMax, (maxLabelSize.y * 0.5f - arrowButtonSize.y * 0.5f),
                arrowButtonSize.x, arrowButtonSize.y);
            RectScrollFrame = new Rect(panelPaddingLeft, top,
                availableContentWidth, BodyRect.height - panelPaddingTop - panelPaddingBottom);
            RectScrollView = new Rect(0, 0, RectScrollFrame.width, RectScrollFrame.height);
        }

        protected override void DrawPanelContent() {
            base.DrawPanelContent();

            CustomizedPawn customizedPawn = ViewState.CurrentPawn;
            Pawn pawn = customizedPawn.Pawn;

            // Clear button
            Style.SetGUIColorForButton(RectButtonClearSkills);
            GUI.DrawTexture(RectButtonClearSkills, Textures.TextureButtonClearSkills);
            if (Widgets.ButtonInvisible(RectButtonClearSkills, false)) {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                ClearSkillsButtonClicked();
            }
            TooltipHandler.TipRegion(RectButtonClearSkills, "EdB.PC.Panel.Skills.ClearTip".Translate());

            // Reset button
            Style.SetGUIColorForButton(RectButtonResetSkills);
            GUI.DrawTexture(RectButtonResetSkills, Textures.TextureButtonReset);
            if (Widgets.ButtonInvisible(RectButtonResetSkills, false)) {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                ResetSkillsButtonClicked?.Invoke();
            }
            TooltipHandler.TipRegion(RectButtonResetSkills, "EdB.PC.Panel.Skills.ResetTip".Translate());

            int skillCount = customizedPawn.Pawn.skills.skills.Count;
            float rowHeight = 26;
            float height = rowHeight * skillCount;
            bool willScroll = height > RectScrollView.height;

            float cursor = 0;
            GUI.BeginGroup(RectScrollFrame);
            try {
                scrollView.Begin(RectScrollView);

                Rect rect;
                Text.Font = GameFont.Small;
                foreach (var skillRecord in pawn.skills.skills) {
                    SkillDef def = skillRecord.def;
                    // TODO: Evaluate the logic here
                    //bool disabled = IsSkillDisabled(customizedPawn, skillRecord);
                    bool disabled = skillRecord.TotallyDisabled;

                    // Draw the label.
                    GUI.color = Style.ColorText;
                    rect = RectLabel;
                    rect.y = rect.y + cursor;
                    Widgets.Label(rect, def.skillLabel.CapitalizeFirst());

                    // Draw the passion.
                    rect = RectPassion;
                    rect.y += cursor;
                    if (!disabled) {
                        Passion passion = skillRecord.passion;
                        Texture2D image;
                        if (passion == Passion.Minor) {
                            image = Textures.TexturePassionMinor;
                        }
                        else if (passion == Passion.Major) {
                            image = Textures.TexturePassionMajor;
                        }
                        else {
                            image = Textures.TexturePassionNone;
                        }
                        GUI.color = Color.white;
                        GUI.DrawTexture(rect, image);
                        if (Widgets.ButtonInvisible(rect, false)) {
                            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                            PassionButtonClicked?.Invoke(skillRecord.def);
                        }
                    }

                    // Draw the skill bar.
                    rect = RectSkillBar;
                    rect.y = rect.y + cursor;
                    if (willScroll) {
                        rect.width = rect.width - 16;
                    }
                    DrawSkill(customizedPawn, skillRecord, rect);

                    // Handle the tooltip.
                    // TODO: Should cover the whole row, not just the skill bar rect.
                    TooltipHandler.TipRegion(rect, () => GetSkillDescription(skillRecord),
                        (GetType().FullName + skillRecord?.def?.defName).GetHashCode());

                    if (!disabled) {
                        // Draw the decrement button.
                        rect = RectButtonDecrement;
                        rect.y = rect.y + cursor;
                        rect.x = rect.x - (willScroll ? 16 : 0);
                        if (rect.Contains(Event.current.mousePosition)) {
                            GUI.color = Style.ColorButtonHighlight;
                        }
                        else {
                            GUI.color = Style.ColorButton;
                        }
                        GUI.DrawTexture(rect, Textures.TextureButtonPrevious);
                        if (Widgets.ButtonInvisible(rect, false)) {
                            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                            DecreaseSkill(def);
                        }

                        // Draw the increment button.
                        rect = RectButtonIncrement;
                        rect.y = rect.y + cursor;
                        rect.x = rect.x - (willScroll ? 16 : 0);
                        if (rect.Contains(Event.current.mousePosition)) {
                            GUI.color = Style.ColorButtonHighlight;
                        }
                        else {
                            GUI.color = Style.ColorButton;
                        }
                        GUI.DrawTexture(rect, Textures.TextureButtonNext);
                        if (Widgets.ButtonInvisible(rect, false)) {
                            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                            IncreaseSkill(def);
                        }
                    }

                    cursor += rowHeight;
                }

                scrollView.End(cursor);
            }
            finally {
                GUI.EndGroup();
            }

            GUI.color = Color.white;
        }

        public static void FillableBar(Rect rect, float fillPercent, Texture2D fillTex) {
            rect.width *= fillPercent;
            GUI.DrawTexture(rect, fillTex);
        }

        public static bool IsSkillDisabled(CustomizedPawn customizedPawn, SkillRecord skill) {
            if (skill.TotallyDisabled) {
                return true;
            }
            // TODO: Do we need to look at life stages to figure this out?
            if (customizedPawn.Pawn.DevelopmentalStage == DevelopmentalStage.Newborn || customizedPawn.Pawn.DevelopmentalStage == DevelopmentalStage.Baby) {
                return true;
            }
            return false;
        }

        private void DrawSkill(CustomizedPawn customizedPawn, SkillRecord skill, Rect rect) {
            int level = skill.Level;

            bool disabled = IsSkillDisabled(customizedPawn, skill);
            if (!disabled) {
                float barSize = (level > 0 ? (float)level : 0) / 20f;
                FillableBar(rect, barSize, Textures.TextureSkillBarFill);

                int minimumLevel = 0;
                if (State.CachedSkillGains.TryGetValue(customizedPawn, out var gains)) {
                    if (gains.TryGetValue(skill.def, out var gain)) {
                        minimumLevel = gain;
                        if (minimumLevel < 0) {
                            minimumLevel = 0;
                        }
                    }
                }

                float baseBarSize = (minimumLevel > 0 ? (float)minimumLevel : 0) / 20f;
                FillableBar(rect, baseBarSize, Textures.TextureSkillBarFill);

                GUI.color = new Color(0.25f, 0.25f, 0.25f);
                Widgets.DrawBox(rect, 1);
                GUI.color = Style.ColorText;

                if (Widgets.ButtonInvisible(rect, false)) {
                    Vector2 pos = Event.current.mousePosition;
                    float x = pos.x - rect.x;
                    int value = 0;
                    if (Mathf.Floor(x / rect.width * 20f) == 0) {
                        if (x <= 1) {
                            value = 0;
                        }
                        else {
                            value = 1;
                        }
                    }
                    else {
                        value = Mathf.CeilToInt(x / rect.width * 20f);
                    }
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                    SetSkillLevel(skill.def, value);
                }
            }

            string label;
            if (disabled) {
                GUI.color = ColorSkillDisabled;
                label = "-";
            }
            else {
                label = GenString.ToStringCached(level);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            rect.x = rect.x + 3;
            rect.y = rect.y + 1;
            Widgets.Label(rect, label);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static string GetSkillDescription(SkillRecord sk) {
            try {
                return ReflectionUtil.InvokeNonPublicStaticMethod<string>(typeof(SkillUI), "GetSkillDescription", new object[] { sk }) ?? "";
            }
            catch (Exception) {
                Logger.Warning("There was an error when trying to get a skill description tooltip for skill " + sk?.def?.defName);
                return "";
            }
        }

        protected void SetSkillLevel(SkillDef skillDef, int value) {
            SkillBarClicked?.Invoke(skillDef, value);
        }

        protected void IncreaseSkill(SkillDef skillDef) {
            IncrementSkillButtonClicked?.Invoke(skillDef);
        }

        protected void DecreaseSkill(SkillDef skillDef) {
            DecrementSkillButtonClicked?.Invoke(skillDef);
        }
        
        public void ScrollToTop() {
            scrollView.ScrollToTop();
        }
    }
}
