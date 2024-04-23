using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class WidgetEquipmentLoadingProgressBar {
        protected Vector2 ProgressBarSize { get; set; } = new Vector2(250, 18);

        public void Draw(Rect rect, ProviderEquipment providerEquipmentTypes) {
            if (providerEquipmentTypes == null) {
                return;
            }
            GameFont savedFont = Text.Font;
            Color savedColor = GUI.color;
            try {
                Rect progressBarRect = new Rect(rect.HalfWidth() - ProgressBarSize.x * 0.5f, rect.HalfHeight() - ProgressBarSize.y * 0.5f, ProgressBarSize.x, ProgressBarSize.y);
                var progress = providerEquipmentTypes.LoadingProgress;
                GUI.color = Color.gray;
                Widgets.DrawBox(progressBarRect);
                if (progress.defCount > 0) {
                    int totalCount = progress.defCount * 2;
                    int processed = progress.stuffProcessed + progress.thingsProcessed;
                    float percent = (float)processed / (float)totalCount;
                    float barWidth = progressBarRect.width * percent;
                    Widgets.DrawRectFast(new Rect(progressBarRect.x, progressBarRect.y, barWidth, progressBarRect.height), Color.green);
                }
                GUI.color = Style.ColorText;
                Text.Font = GameFont.Tiny;
                string label = "EdB.PC.Equipment.LoadingProgress.Initializing".Translate();
                if (progress.phase == EquipmentDatabase.LoadingPhase.ProcessingStuff) {
                    label = "EdB.PC.Equipment.LoadingProgress.StuffDefs".Translate();
                }
                else if (progress.phase == EquipmentDatabase.LoadingPhase.ProcessingThings) {
                    label = "EdB.PC.Equipment.LoadingProgress.ThingDefs".Translate();
                }
                else if (progress.phase == EquipmentDatabase.LoadingPhase.Loaded) {
                    label = "EdB.PC.Equipment.LoadingProgress.Finished".Translate();
                }
                Widgets.Label(new Rect(progressBarRect.x, progressBarRect.yMax + 2, progressBarRect.width, 20), label);
            }
            finally {
                Text.Font = savedFont;
                GUI.color = savedColor;
            }
        }
    }
}
