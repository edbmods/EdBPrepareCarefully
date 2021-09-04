using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class Dialog_Options<T> : Window {
        protected Vector2 ContentMargin = new Vector2(10f, 18f);
        protected Vector2 WindowSize = new Vector2(440f, 584f);
        protected Vector2 ButtonSize = new Vector2(140f, 40f);
        protected float HeaderHeight = 32;
        protected Vector2 ContentSize;
        protected float FooterHeight = 40f;
        protected Rect ContentRect;
        protected Rect ScrollRect;
        protected Rect FooterRect;
        protected Rect HeaderRect;
        protected Rect CancelButtonRect;
        protected Rect ConfirmButtonRect;
        protected Rect SingleButtonRect;
        protected ScrollViewVertical ScrollView = new ScrollViewVertical();
        protected IEnumerable<T> options;
        protected bool confirmButtonClicked = false;
        protected float WindowPadding = 18;

        public float? InitialPositionX { get; set; } = null;
        public float? InitialPositionY { get; set; } = null;

        public bool IncludeNone = false;

        protected string headerLabel = null;
        public string HeaderLabel {
            get {
                return headerLabel;
            }
            set {
                headerLabel = value;
                ComputeSizes();
            }
        }
        public string ConfirmButtonLabel = "EdB.PC.Common.Close".Translate();

        public string CancelButtonLabel = null;

        public Action Initialize = () => { };
        public Func<T, string> NameFunc = (T) => {
            return "";
        };
        public Func<T, string> DescriptionFunc;
        public Func<T, bool> SelectedFunc = (T) => {
            return false;
        };
        public Func<T, bool> EnabledFunc = (T) => {
            return true;
        };
        public Func<string> ConfirmValidation = () => {
            return null;
        };
        public Action<T> SelectAction = (T) => { };
        public Action CloseAction = () => { };
        public Func<bool> NoneEnabledFunc = () => {
            return true;
        };
        public Func<bool> NoneSelectedFunc = () => {
            return false;
        };
        public Action SelectNoneAction = () => { };
        public Func<Rect, float> DrawHeader = (Rect rect) => {
            return 0;
        };
        public Func<IEnumerable<T>, IEnumerable<T>> FilterOptions = (IEnumerable<T> options) => {
            return options;
        };
        public Func<Vector2> IconSizeFunc = null;
        public Action<T, Rect> DrawIconFunc = null;


        public Dialog_Options(IEnumerable<T> options) {
            this.closeOnCancel = true;
            //this.doCloseButton = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = true;

            this.options = options;

            ComputeSizes();
        }

        public IEnumerable<T> Options {
            get { return options; }
            set { options = value; }
        }

        public void ScrollToTop() {
            this.ScrollView.ScrollToTop();
        }

        protected override void SetInitialSizeAndPosition() {
            Vector2 initialSize = InitialSize;
            float x = InitialPositionX.HasValue ? InitialPositionX.Value : ((float)UI.screenWidth - initialSize.x) / 2f;
            float y = InitialPositionY.HasValue ? InitialPositionY.Value : ((float)UI.screenHeight - initialSize.y) / 2f;
            windowRect = new Rect(x, y, initialSize.x, initialSize.y);
            windowRect = windowRect.Rounded();
        }

        protected void ComputeSizes() {
            float headerSize = 0;
            if (HeaderLabel != null) {
                headerSize = HeaderHeight;
            }

            ContentSize = new Vector2(WindowSize.x - WindowPadding * 2 - ContentMargin.x * 2,
                WindowSize.y - WindowPadding * 2 - ContentMargin.y * 2 - FooterHeight - headerSize);

            ContentRect = new Rect(ContentMargin.x, ContentMargin.y + headerSize, ContentSize.x, ContentSize.y);

            ScrollRect = new Rect(0, 0, ContentRect.width, ContentRect.height);

            HeaderRect = new Rect(ContentMargin.x, ContentMargin.y, ContentSize.x, HeaderHeight);

            FooterRect = new Rect(ContentMargin.x, ContentRect.y + ContentSize.y + 20,
                ContentSize.x, FooterHeight);

            SingleButtonRect = new Rect(ContentSize.x / 2 - ButtonSize.x / 2,
                (FooterHeight / 2) - (ButtonSize.y / 2),
                ButtonSize.x, ButtonSize.y);

            CancelButtonRect = new Rect(0,
                (FooterHeight / 2) - (ButtonSize.y / 2),
                ButtonSize.x, ButtonSize.y);
            ConfirmButtonRect = new Rect(ContentSize.x - ButtonSize.x,
                (FooterHeight / 2) - (ButtonSize.y / 2),
                ButtonSize.x, ButtonSize.y);

        }
        

        public override Vector2 InitialSize {
            get {
                return new Vector2(WindowSize.x, WindowSize.y);
            }
        }
        
        public override void DoWindowContents(Rect inRect) {
            
            float cursor = DrawHeader(new Rect(0, 0, inRect.width, inRect.height));

            GUI.color = Color.white;
            Rect headerRect = HeaderRect.InsetBy(0, 0, 0, cursor).OffsetBy(0, cursor);
            if (HeaderLabel != null) {
                Text.Font = GameFont.Medium;
                Widgets.Label(headerRect, HeaderLabel);
            }

            Rect contentRect = ContentRect.InsetBy(0, 0, 0, cursor).OffsetBy(0, cursor);
            Rect scrollRect = new Rect(0, 0, contentRect.width, contentRect.height);

            Text.Font = GameFont.Small;
            GUI.BeginGroup(contentRect);
            ScrollView.Begin(scrollRect);

            cursor = 0;
            
            if (IncludeNone) {
                float height = Text.CalcHeight("EdB.PC.Common.NoOptionSelected".Translate(), ContentSize.x - 32);
                if (height < 30) {
                    height = 30;
                }
                bool isEnabled = NoneEnabledFunc();
                bool isSelected = NoneSelectedFunc();
                if (Widgets.RadioButtonLabeled(new Rect(0, cursor, ContentSize.x - 32, height), "EdB.PC.Common.NoOptionSelected".Translate(), isSelected)) {
                    SelectNoneAction();
                }
                cursor += height;
                cursor += 2;
            }
            bool drawIcon = DrawIconFunc != null && IconSizeFunc != null;
            Vector2 iconSize = new Vector2(0, 0);
            if (IconSizeFunc != null) {
                iconSize = IconSizeFunc();
            }

            Rect itemRect = new Rect(0, cursor, ContentSize.x - 32, 0);
            IEnumerable<T> filteredOptions = FilterOptions(options);
            foreach (T option in filteredOptions) {
                string name = NameFunc(option);
                bool selected = SelectedFunc(option);
                bool enabled = EnabledFunc != null ? EnabledFunc(option) : true;

                float height = Text.CalcHeight(name, ContentSize.x - 32);
                if (height < 30) {
                    height = 30;
                }
                itemRect.height = height;
                Vector2 size = Text.CalcSize(name);
                if (size.x > ContentSize.x - 32) {
                    size.x = ContentSize.x;
                }
                size.y = height;

                if (cursor + height >= ScrollView.Position.y && cursor <= ScrollView.Position.y + ScrollView.ViewHeight) {
                    GUI.color = Color.white;
                    if (!enabled) {
                        GUI.color = new Color(0.65f, 0.65f, 0.65f);
                    }
                    Rect labelRect = new Rect(0, cursor + 2, ContentSize.x - 32, height);
                    if (drawIcon) {
                        Rect iconRect = new Rect(0, cursor + 2 + height / 2 - iconSize.y / 2, iconSize.x, iconSize.y);
                        DrawIconFunc(option, iconRect);
                        labelRect = labelRect.InsetBy(iconSize.x + 4, 0);
                    }
                    Widgets.Label(labelRect, name);
                    Vector2 radioButtonPosition = new Vector2(itemRect.x + itemRect.width - 24, itemRect.y + itemRect.height / 2 - 12);
                    if (!enabled) {
                        Texture2D image = Textures.TextureRadioButtonOff;
                        GUI.color = new Color(1, 1, 1, 0.28f);
                        GUI.DrawTexture(new Rect(radioButtonPosition.x, radioButtonPosition.y, 24, 24), image);
                        GUI.color = Color.white;
                    }
                    else {
                        if (Widgets.RadioButton(radioButtonPosition, selected) || Widgets.ButtonInvisible(labelRect)) {
                            SelectAction(option);
                        }
                    }

                    if (DescriptionFunc != null) {
                        Rect tipRect = new Rect(itemRect.x, itemRect.y, size.x, size.y);
                        TooltipHandler.TipRegion(tipRect, DescriptionFunc(option));
                    }
                }

                cursor += height;
                cursor += 2;
                itemRect.y = cursor;
            }
            ScrollView.End(cursor);
            GUI.EndGroup();
            GUI.color = Color.white;

            GUI.BeginGroup(FooterRect);
            Rect buttonRect = SingleButtonRect;
            if (CancelButtonLabel != null) {
                if (Widgets.ButtonText(CancelButtonRect, CancelButtonLabel, true, true, true)) {
                    this.Close(true);
                }
                buttonRect = ConfirmButtonRect;
            }
            if (Widgets.ButtonText(buttonRect, ConfirmButtonLabel, true, true, true)) {
                string validationMessage = ConfirmValidation();
                if (validationMessage != null) {
                    Messages.Message(validationMessage.Translate(), MessageTypeDefOf.RejectInput);
                }
                else {
                    this.Confirm();
                }
            }
            GUI.EndGroup();
        }

        protected void Confirm() {
            confirmButtonClicked = true;
            this.Close(true);
        }

        public override void PostClose() {
            if (ConfirmButtonLabel != null) {
                if (confirmButtonClicked && CloseAction != null) {
                    CloseAction();
                }
            }
            else {
                if (CloseAction != null) {
                    CloseAction();
                }
            }
        }
    }
}

