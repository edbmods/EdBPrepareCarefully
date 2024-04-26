using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

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
        public Rect ApparelAlertRect { get; private set; }

        private List<WidgetField> fields = new List<WidgetField>();

        public override void Resize(float width) {
            base.Resize(width);

            ManageButtonRect = new Rect(width - 24, 10, 16, 16);
            ApparelAlertRect = ManageButtonRect;
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
            else {
                Rect alertButtonRect = ApparelAlertRect.OffsetBy(0, top);
                Style.SetGUIColorForButton(alertButtonRect);
                GUI.DrawTexture(alertButtonRect, Textures.TextureIconWarning);
                TooltipHandler.TipRegion(alertButtonRect, "EdB.PC.Panel.Apparel.ApparelLocked".Translate());
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

    }
}
