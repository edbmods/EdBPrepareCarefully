using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public abstract class Dialog_Preset : Window {
        protected const float DeleteButtonSpace = 5;
        protected const float MapDateExtraLeftMargin = 220;

        private static readonly Color ManualSaveTextColor = new Color(1, 1, 0.6f);
        private static readonly Color AutosaveTextColor = new Color(0.75f, 0.75f, 0.75f);

        protected const float MapEntrySpacing = 8;
        protected const float BoxMargin = 20;
        protected const float MapNameExtraLeftMargin = 15;
        protected const float MapEntryMargin = 6;

        private Vector2 scrollPosition = Vector2.zero;

        protected string interactButLabel = "Error";
        protected float bottomAreaHeight;

        public Dialog_Preset() {
            this.closeOnCancel = true;
            this.doCloseButton = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = true;
        }

        public override Vector2 InitialSize {
            get {
                return new Vector2(600, 700);
            }
        }

        protected abstract void DoMapEntryInteraction(string mapName);

        protected virtual void DoSpecialSaveLoadGUI(Rect inRect) {
        }

        public override void PostClose() {
            GUI.FocusControl(null);
        }

        public override void DoWindowContents(Rect inRect) {
            Vector2 vector = new Vector2(inRect.width - 16, 36);
            Vector2 vector2 = new Vector2(100, vector.y - 6);
            inRect.height -= 45;
            List<FileInfo> list = PresetFiles.AllFiles.ToList<FileInfo>();
            float num = vector.y + 3;
            float height = (float)list.Count * num;
            Rect viewRect = new Rect(0, 0, inRect.width - 16, height);
            Rect outRect = new Rect(inRect.AtZero());
            outRect.height -= this.bottomAreaHeight;
            Widgets.BeginScrollView(outRect, ref this.scrollPosition, viewRect);
            float num2 = 0;
            int num3 = 0;
            foreach (FileInfo current in list) {
                Rect rect = new Rect(0, num2, vector.x, vector.y);
                if (num3 % 2 == 0) {
                    GUI.DrawTexture(rect, Textures.TextureAlternateRow);
                }
                Rect innerRect = rect.ContractedBy(3);
                GUI.BeginGroup(innerRect);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(current.Name);
                GUI.color = ManualSaveTextColor;
                Rect rect2 = new Rect(15, 0, innerRect.width, innerRect.height);
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.Font = GameFont.Small;
                Widgets.Label(rect2, fileNameWithoutExtension);
                GUI.color = Color.white;
                Rect rect3 = new Rect(250, 0, innerRect.width, innerRect.height);
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(1, 1, 1, 0.5f);
                Widgets.Label(rect3, current.LastWriteTime.ToString("g"));
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                float num4 = vector.x - 6 - vector2.x - vector2.y;
                Rect butRect = new Rect(num4, 0, vector2.x, vector2.y);
                if (Widgets.ButtonText(butRect, this.interactButLabel, true, false, true)) {
                    this.DoMapEntryInteraction(Path.GetFileNameWithoutExtension(current.Name));
                }
                Rect rect4 = new Rect(num4 + vector2.x + 5, 0, vector2.y, vector2.y);
                if (Widgets.ButtonImage(rect4, Textures.TextureDeleteX)) {
                    FileInfo localFile = current;
                    Find.UIRoot.windows.Add(new Dialog_Confirm("EdB.PC.Dialog.Preset.ConfirmDelete".Translate(new object[] {
                        localFile.Name
                    }), delegate {
                        localFile.Delete();
                    }, true, null, true));
                }
                TooltipHandler.TipRegion(rect4, "EdB.PC.Dialog.Preset.DeleteTooltip".Translate());
                GUI.EndGroup();
                num2 += vector.y + 3;
                num3++;
            }
            Widgets.EndScrollView();
            this.DoSpecialSaveLoadGUI(inRect.AtZero());
        }
    }
}

