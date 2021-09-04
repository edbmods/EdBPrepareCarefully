using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class PanelRandomize : PanelBase {
        public delegate void RandomizeAllHandler();

        public event RandomizeAllHandler RandomizeAllClicked;

        public override void Draw(State state) {
            base.Draw(state);

            Rect randomRect = new Rect(
                PanelRect.x + PanelRect.width / 2 - Textures.TextureButtonRandomLarge.width / 2 - 1,
                PanelRect.y + PanelRect.height / 2 - Textures.TextureButtonRandomLarge.height / 2,
                Textures.TextureButtonRandomLarge.width,
                Textures.TextureButtonRandomLarge.height
            );
            if (randomRect.Contains(Event.current.mousePosition)) {
                GUI.color = Style.ColorButtonHighlight;
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(randomRect, Textures.TextureButtonRandomLarge);
            if (Widgets.ButtonInvisible(randomRect, false)) {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                RandomizeAllClicked();
            }

            GUI.color = Color.white;

        }
    }
}
