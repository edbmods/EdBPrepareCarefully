using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace EdB.PrepareCarefully {
    public class PanelApparel : PanelModule {

        public delegate void ApparelRemovedHandler(Thing thing);
        public delegate void ApparelAddedHandler(CustomizationsApparel apparel);
        public delegate void ApparelReplacedHandler(List<CustomizationsApparel> apparelList);

        public event ApparelRemovedHandler ApparelRemoved;
        public event ApparelAddedHandler ApparelAdded;
        public event ApparelReplacedHandler ApparelReplaced;

        public ModState State { get; set; }
        public ViewState ViewState { get; set; }
        public ProviderEquipment ProviderEquipmentTypes { get; set; }
        public DialogApparel DialogManageApparel { get; set; }

        public Rect ManageButtonRect { get; private set; }

        private List<WidgetField> fields = new List<WidgetField>();

        public override void Resize(float width) {
            base.Resize(width);

            ManageButtonRect = new Rect(width - 27, 10, 16, 16);
        }

        public override float Draw(float y) {
            CustomizedPawn customizedPawn = ViewState.CurrentPawn;
            Pawn pawn = customizedPawn?.Pawn;
            if (pawn == null) {
                return y;
            }

            float top = y;
            y += Margin.y;
            y += DrawHeader(y, Width, "Apparel".Translate().Resolve());

            float rowMargin = 8;
            float rowWidth = Width - rowMargin * 2;

            if (pawn.apparel.WornApparelCount > 0) {
                int index = 0;
                foreach (Apparel item in from x in pawn.apparel.WornApparel
                                         select x into ap
                                         orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                         select ap) {
                    if (index > 0) {
                        y += 4;
                    }
                    y += WidgetEquipmentField.DrawSelectedEquipment(rowMargin, y, rowWidth, item, () => {
                        if (!pawn.apparel.AllApparelLocked) {
                            OpenManageApparelDialog();
                            DialogManageApparel.ScrollToSelectedApparel(item);
                        }
                    });
                    index++;
                }
            }
            else {
                GUI.color = Style.ColorText;
                Rect rectText = new Rect(Margin.x, y, Width - Margin.x * 2, 20);
                Widgets.Label(rectText, "EdB.PC.Panel.Incapable.None".Translate());
                y += rectText.height;
                GUI.color = Color.white;
            }

            // Manage apparel button.
            if (!pawn.apparel.AllApparelLocked) {
                Rect manageButtonRect = ManageButtonRect.OffsetBy(0, top);
                Style.SetGUIColorForButton(manageButtonRect);
                GUI.DrawTexture(manageButtonRect, Textures.TextureButtonManage);
                if (Widgets.ButtonInvisible(manageButtonRect, false)) {
                    OpenManageApparelDialog();
                }
            }

            y += Margin.y;

            return y - top;
        }

        private void OpenManageApparelDialog() {
            if (DialogManageApparel == null) {
                DialogManageApparel = new DialogApparel() {
                    ProviderEquipment = ProviderEquipmentTypes
                };
                DialogManageApparel.ApparelRemoved += (a) => { ApparelRemoved?.Invoke(a); };
                DialogManageApparel.ApparelAdded += (a) => { ApparelAdded?.Invoke(a); };
                DialogManageApparel.ApparelReplaced += (a) => { ApparelReplaced?.Invoke(a); };
            }
            DialogManageApparel.InitializeWithPawn(ViewState.CurrentPawn);
            Find.WindowStack.Add(DialogManageApparel);
        }

        private float DrawThingRow(float y, float width, int index, Thing thing, bool inventory = false) {
            float top = y;
            Rect rowRect = new Rect(0, y, width, 36);

            var guiState = UtilityGUIState.Save();
            try {
                GUI.color = Color.white;

                Widgets.DrawAtlas(rowRect, Textures.TextureFieldAtlas);

                if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null) {
                    Widgets.ThingIcon(new Rect(4f, y+3, 28f, 28f), thing);
                }
                Text.Anchor = TextAnchor.MiddleLeft;
                Rect labelRect = new Rect(36f, y, rowRect.width - 36f, 24);
                string text = thing.def.LabelCap;
                GUI.color = Style.ColorText;
                Text.WordWrap = false;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                Widgets.Label(labelRect, text.Truncate(labelRect.width - 48));
                Rect subtitleRect = new Rect(36f, labelRect.y + 17, rowRect.width - 36f, 18);
                bool hasQuality = thing.TryGetQuality(out QualityCategory quality);
                int percent = 100;
                if (thing.def.useHitPoints) {
                    int hitPoints = thing.HitPoints;
                    int maxHitPoints = thing.MaxHitPoints;
                    if (hitPoints < maxHitPoints) {
                        percent = (int)((float)hitPoints / (float)maxHitPoints * 100f);
                    }
                }
                if (thing.Stuff != null && hasQuality) {
                    Text.Font = GameFont.Tiny;
                    string subtitleText = thing.Stuff.LabelCap + ", " + quality.GetLabel();
                    if (percent != 100) {
                        subtitleText += " (" + percent + "%)";
                    }
                    Widgets.Label(subtitleRect, subtitleText.Truncate(subtitleRect.width - 48));
                }
                else if (thing.Stuff != null) {
                    Text.Font = GameFont.Tiny;
                    Widgets.Label(subtitleRect, thing.Stuff.LabelCap.Truncate(subtitleRect.width - 48));
                }
                else if (hasQuality) {
                    Text.Font = GameFont.Tiny;
                    string subtitleText = quality.GetLabel();
                    if (percent != 100) {
                        subtitleText += " (" + percent + "%)";
                    }
                    Widgets.Label(subtitleRect, subtitleText);
                }
                Text.WordWrap = true;
            }
            finally {
                guiState.Restore();
            }


            //Widgets.InfoCardButton(rect.width - 24f, y, thing);

            //if (Mouse.IsOver(rect)) {
            //    string text2 = thing.LabelNoParenthesisCap.AsTipTitle() + GenLabel.LabelExtras(thing, includeHp: true, includeQuality: true) + "\n\n" + thing.DescriptionDetailed;
            //    if (thing.def.useHitPoints) {
            //        text2 = text2 + "\n" + thing.HitPoints + " / " + thing.MaxHitPoints;
            //    }
            //    TooltipHandler.TipRegion(rect, text2);
            //}
            y += rowRect.height;
            return y - top;
            
        }
    }
}
