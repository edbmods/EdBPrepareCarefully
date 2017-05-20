using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class WidgetNumberField {

        private static readonly int ClickDelay = 250;
        private string id;
        private Action<int> updateAction = null;
        private int minValue = 0;
        private int maxValue = int.MaxValue;
        private int ticksSinceClick = 0;
        private bool showTextField = false;
        private int? newValue = null;
        private string focusedControl = "";
        private bool shouldFocusField = false;
        private GUIStyle textFieldStyle = null;
        private DragSlider dragSlider = new DragSlider();

        public WidgetNumberField() {
            id = "CONTROL-" + Guid.NewGuid();
            dragSlider.minValue = minValue;
            dragSlider.maxValue = maxValue;
        }

        public Action<int> UpdateAction {
            get {
                return updateAction;
            }
            set {
                updateAction = value;
            }
        }

        public int MinValue {
            get {
                return minValue;
            }
            set {
                minValue = value;
                dragSlider.minValue = value;
            }
        }

        public int MaxValue {
            get {
                return maxValue;
            }
            set {
                maxValue = value;
                dragSlider.maxValue = value;
            }
        }

        public DragSlider DragSlider {
            get {
                return dragSlider;
            }
            set {
                dragSlider = value;
            }
        }

        protected void Update(int value) {
            if (value < minValue) {
                value = minValue;
            }
            else if (value > maxValue) {
                value = maxValue;
            }
            if (updateAction != null) {
                updateAction(value);
            }
            newValue = null;
        }

        public void Draw(Rect rect, int value) {
            GUI.color = Style.ColorText;
            Text.Font = GameFont.Small;

            bool dragging = false;
            string currentControl = GUI.GetNameOfFocusedControl();
            if (currentControl != focusedControl) {
                focusedControl = currentControl;
                if (id != focusedControl) {
                    if (newValue != null) {
                        SoundDefOf.TickTiny.PlayOneShotOnCamera();
                        if (newValue == value) {
                        }
                        else if (newValue >= minValue && newValue <= maxValue) {
                            Update(newValue.Value);
                        }
                        else {
                            Update(newValue.Value);
                        }
                    }
                    showTextField = false;
                }
            }
            if (showTextField) {
                if (textFieldStyle == null) {
                    textFieldStyle = new GUIStyle(Verse.Text.CurTextFieldStyle);
                    textFieldStyle.alignment = TextAnchor.MiddleCenter;
                }
                if (shouldFocusField) {
                    newValue = value;
                }
                GUI.SetNextControlName(id);
                string previousText = newValue == null ? "" : newValue.Value + "";
                string text = GUI.TextField(rect, previousText, textFieldStyle);
                if (shouldFocusField) {
                    shouldFocusField = false;
                    GUI.FocusControl(id);
                }
                if (previousText != text) {
                    if (string.IsNullOrEmpty(text)) {
                        newValue = null;
                    }
                    else {
                        try {
                            newValue = int.Parse(text);
                        }
                        catch (Exception) { }
                    }
                }
                if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.Tab || Event.current.keyCode == KeyCode.KeypadEnter) {
                    GUI.FocusControl(null);
                }
                if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.Ignore) {
                    if (!rect.Contains(Event.current.mousePosition)) {
                        GUI.FocusControl(null);
                    }
                }
            }
            else {
                Widgets.DrawAtlas(rect, Textures.TextureFieldAtlas);
                dragSlider.OnGUI(rect, value, (int v) => {
                    Update(v);
                });
                if (rect.Contains(Event.current.mousePosition)) {
                    if (Event.current.type == EventType.MouseDown) {
                        ticksSinceClick = Environment.TickCount;
                    }
                    else if (Event.current.type == EventType.MouseUp) {
                        int newTicks = Environment.TickCount;
                        if (newTicks - ticksSinceClick < ClickDelay) {
                            showTextField = true;
                            shouldFocusField = true;
                        }
                        ticksSinceClick = 0;
                    }
                }
                dragging = DragSlider.IsDragging();
            }

            // Draw the decrement button.
            Rect buttonRect = new Rect(rect.x - 17, rect.y + 6, 16, 16);
            if (!dragging) {
                Style.SetGUIColorForButton(buttonRect);
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(buttonRect, Textures.TextureButtonPrevious);
            if (Widgets.ButtonInvisible(buttonRect, false)) {
                SoundDefOf.TickTiny.PlayOneShotOnCamera();
                int amount = Event.current.shift ? 10 : 1;
                int newValue = value - amount;
                Update(newValue);
            }

            // Draw the increment button.
            buttonRect = new Rect(rect.x + rect.width + 1, rect.y + 6, 16, 16);
            if (!dragging) {
                Style.SetGUIColorForButton(buttonRect);
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(buttonRect, Textures.TextureButtonNext);
            if (Widgets.ButtonInvisible(buttonRect, false)) {
                SoundDefOf.TickTiny.PlayOneShotOnCamera();
                int amount = Event.current.shift ? 10 : 1;
                int newValue = value + amount;
                Update(newValue);
            }

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }

}
