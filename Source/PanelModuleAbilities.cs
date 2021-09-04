using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class PanelModuleAbilities : PanelModule {
        public static readonly Vector2 FieldPadding = new Vector2(6, 6);

        public delegate void AddAbilityHandler(AbilityDef abilityDef);
        public delegate void SetAbilitiesHandler(IEnumerable<AbilityDef> abilityDefs);
        public delegate void RemoveAbilityHandler(Ability ability);

        public event AddAbilityHandler AbilityAdded;
        public event SetAbilitiesHandler AbilitiesSet;
        public event RemoveAbilityHandler AbilityRemoved;

        public Rect FieldRect;
        public Rect IconRect;
        public Rect NoneRect;
        public Rect CertaintyLabelRect;
        public Rect CertaintySliderRect;
        protected Field FieldFaction = new Field();
        protected LabelTrimmer labelTrimmer = new LabelTrimmer();
        protected List<Ability> itemsToRemove = new List<Ability>();
        protected HashSet<AbilityDef> disallowed = new HashSet<AbilityDef>();

        public override void Resize(float width) {
            base.Resize(width);
            //FieldRect = new Rect(FieldPadding.x, 0, width - FieldPadding.x * 2, 36);
            float iconPadding = 2;
            float extraPaddingForDeleteButton = 16;
            IconRect = new Rect(iconPadding, iconPadding, 48, 48);
            FieldRect = new Rect(0, 0, iconPadding * 2 + extraPaddingForDeleteButton + IconRect.width, iconPadding * 2 + IconRect.height);
            NoneRect = new Rect(12, 0, width - 24, 32);
        }

        public float Measure() {
            return 0;
        }

        public override bool IsVisible(State state) {
            return base.IsVisible(state);
        }

        public override float Draw(State state, float y) {
            float top = y;
            y += Margin.y;

            y += DrawHeader(y, Width, "Abilities".Translate().CapitalizeFirst().Resolve());

            CustomPawn pawn = state.CurrentPawn;
            Pawn_AbilityTracker abilityTracker = pawn.Pawn.abilities;

            Action clickAction = null;
            int index = 0;
            float x = FieldPadding.x;
            foreach (var ability in abilityTracker.abilities) {
                if (ability == null) {
                    continue;
                }

                GUI.color = Color.white;

                if (x + FieldRect.width > Width - FieldPadding.x) {
                    x = FieldPadding.x;
                    y += FieldRect.height + FieldPadding.y;
                }

                Rect fieldRect = FieldRect.OffsetBy(x, y);
                Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);

                Rect iconRect = new Rect(fieldRect.x + IconRect.x, fieldRect.y + IconRect.y, IconRect.width, IconRect.height);
                if (Mouse.IsOver(iconRect)) {
                    Widgets.DrawHighlight(iconRect);
                }
                GUI.DrawTexture(iconRect, ability.def.uiIcon);
                if (Widgets.ButtonInvisible(iconRect, false)) {
                    clickAction = () => Find.WindowStack.Add(new Dialog_InfoCard(ability.def));
                }
                TooltipHandler.TipRegion(iconRect, ability.Tooltip + "\n\n" + ("ClickToLearnMore".Translate()));

                // Remove ability button.
                Rect deleteRect = new Rect(fieldRect.xMax - 15, fieldRect.y + 4, 12, 12);
                if (deleteRect.Contains(Event.current.mousePosition)) {
                    GUI.color = Style.ColorButtonHighlight;
                }
                else {
                    GUI.color = Style.ColorButton;
                }
                GUI.DrawTexture(deleteRect, Textures.TextureButtonDelete);
                if (Widgets.ButtonInvisible(deleteRect, false)) {
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                    itemsToRemove.Add(ability);
                }
                GUI.color = Color.white;

                index++;
                x += FieldRect.width + FieldPadding.x;
            }
            // If the index is still zero, then the pawn has no abilities.  Draw the "none" label.
            if (index == 0) {
                GUI.color = Style.ColorText;
                Widgets.Label(NoneRect.OffsetBy(0, y - 4), "EdB.PC.Panel.Abilities.None".Translate());
                y += NoneRect.height;
            }
            else {
                y += FieldRect.height + FieldPadding.y;
            }

            GUI.color = Color.white;

            // Fire any action that was triggered
            if (clickAction != null) {
                clickAction();
                clickAction = null;
            }

            // Add button
            Rect addRect = new Rect(Width - 24, top + 12, 16, 16);
            Style.SetGUIColorForButton(addRect);
            int traitCount = state.CurrentPawn.Traits.Count();
            GUI.DrawTexture(addRect, Textures.TextureButtonAdd);
            if (Widgets.ButtonInvisible(addRect, false)) {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                disallowed.Clear();
                if (pawn.Pawn?.abilities?.abilities != null) {
                    disallowed.AddRange(pawn.Pawn.abilities.abilities.Select(a => a.def));
                }
                DialogAbilities dialog = new DialogAbilities(pawn) {
                    HeaderLabel = "EdB.PC.Dialog.Abilities.Header".Translate(),
                    CloseAction = (IEnumerable<AbilityDef> abilities) => {
                        AbilitiesSet(abilities);
                    }
                };
                Find.WindowStack.Add(dialog);
            }

            // Remove any items that were marked for deletion
            if (itemsToRemove.Count > 0) {
                foreach (var ability in itemsToRemove) {
                    AbilityRemoved(ability);
                }
                itemsToRemove.Clear();
            }

            y += Margin.y;
            return y - top;
        }
    }
}
