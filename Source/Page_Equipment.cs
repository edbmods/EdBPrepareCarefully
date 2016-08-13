using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully
{

	public class Page_Equipment : Page_PrepareCarefully
	{
		private DragSlider equipmentDragSlider = new DragSlider(0.3f, 12, 400);

		private static Color ButtonColor = new Color(0.623529f, 0.623529f, 0.623529f);
		private static Color ButtonHighlightColor = new Color(0.97647f, 0.97647f, 0.97647f);

		private static readonly Vector2 WinSize = new Vector2(1020, 764);

		private const float TopAreaHeight = 80;

		protected List<TabRecord> tabs = new List<TabRecord>();
		protected int selectedTab = 0;

		protected GraphicsCache cache = new GraphicsCache();

		protected List<List<EquipmentDatabaseEntry>> equipmentLists = new List<List<EquipmentDatabaseEntry>>();

		protected List<Vector2> sourceScrollPositions;
		protected List<float> sourceScrollViewHeights;
		protected Vector2 destScrollPosition = Vector2.zero;
		protected float destScrollViewHeight = 0;
		protected float newDestScrollPosition = -1f;

		protected List<int> sourceSelections;
		protected int destSelection = -1;

		static Page_Equipment()
		{
			ResetTextures();
		}

		public Page_Equipment()
		{
			//base.SetCentered(Page_Equipment.WinSize);
			//this.drawPriority = 2000;
			this.absorbInputAroundWindow = true;
			this.forcePause = true;

			tabs.Add(new TabRecord("EdB.EquipmentTab.Resources".Translate(), delegate { this.ChangeTab(0); }, true));
			tabs.Add(new TabRecord("EdB.EquipmentTab.Food".Translate(), delegate { this.ChangeTab(1); }, false));
			tabs.Add(new TabRecord("EdB.EquipmentTab.Weapons".Translate(), delegate { this.ChangeTab(2); }, false));
			tabs.Add(new TabRecord("EdB.EquipmentTab.Apparel".Translate(), delegate { this.ChangeTab(3); }, false));
			tabs.Add(new TabRecord("EdB.PrepareCarefully.EquipmentTab.Implants".Translate(), delegate { this.ChangeTab(4); }, false));
			tabs.Add(new TabRecord("EdB.PrepareCarefully.EquipmentTab.Buildings".Translate(), delegate { this.ChangeTab(5); }, false));
			tabs.Add(new TabRecord("EdB.PrepareCarefully.EquipmentTab.Animals".Translate(), delegate { this.ChangeTab(6); }, false));

			sourceScrollPositions = new List<Vector2>();
			sourceScrollViewHeights = new List<float>();
			sourceSelections = new List<int>();
			for (int i = 0; i < tabs.Count; i++) {
				sourceScrollPositions.Add(Vector2.zero);
				sourceScrollViewHeights.Add(0);
				sourceSelections.Add(-1);
			}

			BuildEquipmentLists();
			SortEquipmentLists();
		}

		public override Vector2 InitialSize {
			get {
				return Page_Equipment.WinSize;
			}
		}

		public static void ResetTextures()
		{
		}

		public override void WindowUpdate()
		{
			base.WindowUpdate();
			DragSliderManager.DragSlidersUpdate();
		}

		public void SortEquipmentLists()
		{
			SortField field = PrepareCarefully.Instance.SortField;
			SortOrder nameOrder = PrepareCarefully.Instance.NameSortOrder;
			SortOrder costOrder = PrepareCarefully.Instance.CostSortOrder;
			foreach (var list in equipmentLists) {
				if (PrepareCarefully.Instance.SortField == SortField.Cost) {
					list.Sort((EquipmentDatabaseEntry x, EquipmentDatabaseEntry y) => {
						if (costOrder == SortOrder.Ascending) {
							int result = x.cost.CompareTo(y.cost);
							if (result != 0) {
								return result;
							}
						}
						else {
							int result = y.cost.CompareTo(x.cost);
							if (result != 0) {
								return result;
							}
						}
						if (nameOrder == SortOrder.Ascending) {
							return x.Label.CompareTo(y.Label);
						}
						else {
							return y.Label.CompareTo(x.Label);
						}
					});
				}
				else {
					list.Sort((EquipmentDatabaseEntry x, EquipmentDatabaseEntry y) => {
						if (nameOrder == SortOrder.Ascending) {
							int result = x.Label.CompareTo(y.Label);
							if (result != 0) {
								return result;
							}
						}
						else {
							int result = y.Label.CompareTo(x.Label);
							if (result != 0) {
								return result;
							}
						}
						if (costOrder == SortOrder.Ascending) {
							return x.cost.CompareTo(y.cost);
						}
						else {
							return y.cost.CompareTo(x.cost);
						}
					});
				}
			}
		}

		public void BuildEquipmentLists()
		{
			equipmentLists.Add(PrepareCarefully.Instance.EquipmentEntries.Resources);
			equipmentLists.Add(PrepareCarefully.Instance.EquipmentEntries.Food);
			equipmentLists.Add(PrepareCarefully.Instance.EquipmentEntries.Weapons);
			equipmentLists.Add(PrepareCarefully.Instance.EquipmentEntries.Apparel);
			equipmentLists.Add(PrepareCarefully.Instance.EquipmentEntries.Implants);
			equipmentLists.Add(PrepareCarefully.Instance.EquipmentEntries.Buildings);
			equipmentLists.Add(PrepareCarefully.Instance.EquipmentEntries.Animals);
			int count = equipmentLists.Count;
			for (int i = 0; i<count; i++) {
				if (equipmentLists[i].Count > 0) {
					sourceSelections[i] = 0;
				}
			}
			if (PrepareCarefully.Instance.Equipment.Count > 0) {
				destSelection = 0;
			}
		}

		public void ChangeTab(int index)
		{
			tabs[selectedTab].selected = false;
			tabs[index].selected = true;
			selectedTab = index;
		}

		private AcceptanceReport CanStart()
		{
			if (Config.pointsEnabled) {
				if (PrepareCarefully.Instance.PointsRemaining < 0) {
					return new AcceptanceReport("EdB.NotEnoughPoints".Translate());
				}
			}
			int pawnCount = PrepareCarefully.Instance.Pawns.Count;
			if (Config.hardMaxColonists != null && pawnCount > Config.hardMaxColonists) {
				if (Config.hardMaxColonists == 1) {
					return new AcceptanceReport("EdB.PrepareCarefully.TooManyColonists1".Translate(new object[] { Config.maxColonists }));
				}
				else {
					return new AcceptanceReport("EdB.PrepareCarefully.TooManyColonists".Translate(new object[] { Config.maxColonists }));
				}
			}
			if (pawnCount < Config.minColonists) {
				if (Config.minColonists == 1) {
					return new AcceptanceReport("EdB.PrepareCarefully.NotEnoughColonists1".Translate(new object[] { Config.minColonists }));
				}
				else {
					return new AcceptanceReport("EdB.PrepareCarefully.NotEnoughColonists".Translate(new object[] { Config.minColonists }));
				}
			}

			return AcceptanceReport.WasAccepted;
		}

		public override void DoWindowContents(Rect inRect)
		{
			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(0, 0, 500, 300), "EdB.SelectResourcesAndEquipment".Translate());
			Text.Font = GameFont.Small;

			string label = "Start".Translate();
				
			// TODO: Alpha 14
			DoNextBackButtons(inRect, label,
				delegate {
					if (TryStartGame()) {
						this.Close(false);
						PrepareCarefully.Instance.NextPage();
					}
				},
				delegate {
					Find.WindowStack.Add(new Page_ConfigureStartingPawnsCarefully());
					this.Close(true);
				}
			);

			if (IsBroken) {
				return;
			}

			Rect rect = new Rect(0, 80, inRect.width, inRect.height - 60 - 80);
			Widgets.DrawMenuSection(rect, true);
			TabDrawer.DrawTabs(new Rect(0, 80, rect.width - 1, rect.height), tabs);
			Rect innerRect = rect.ContractedBy(22);

			GUI.BeginGroup(innerRect);
			DrawAvailableEquipmentList(equipmentLists[this.selectedTab]);
			DrawSelectedEquipmentList();
			GUI.EndGroup();

			DrawCost(inRect);
			DrawPresetButtons();
		}

		protected static Rect RectSourceBox = new Rect(0, 16, 450, 440);
		protected static Rect RectSourceContent = RectSourceBox.ContractedBy(1);
		protected static Rect RectSourceEntry = new Rect(0, 0, RectSourceContent.width, 42);
		protected static Rect RectSourceItem = new Rect(10, 2, 38, 38);
		protected static Rect RectSourceButton = new Rect(RectSourceBox.x + (RectSourceBox.width / 2) - 80, RectSourceBox.y + RectSourceBox.height + 8, 160, 34);
		protected static Rect RectSourceCostLabel = new Rect(RectSourceBox.x + 320, RectSourceBox.y - 18, 100, 22);
		protected static Rect RectSourceNameLabel = new Rect(RectSourceBox.x + 66, RectSourceBox.y - 18, 100, 22);
		protected static Color ColorBoxOutline = new Color(0.3608f, 0.3608f, 0.3608f);
		protected static Color ColorBoxBackground = new Color(0.145098f, 0.149f, 0.15294f);
		protected static Color ColorEntryBackground = new Color(0.1882f, 0.19216f, 0.19608f);
		protected static Color ColorSelectedEntry = new Color(0.05f, 0.05f, 0.05f);
		protected static Vector2 SizeTextureSortIndicator = new Vector2(8, 4);
		protected void DrawAvailableEquipmentList(List<EquipmentDatabaseEntry> entries)
		{
			SortField sortField = PrepareCarefully.Instance.SortField;
			Text.Font = GameFont.Tiny;
			bool resort = false;

			Text.Anchor = TextAnchor.LowerLeft;
			string nameLabelText = "EdB.PrepareCarefully.EquipmentName".Translate();
			Vector2 nameLabelSize = Text.CalcSize(nameLabelText);
			Rect nameLabelButtonRect = new Rect(RectSourceNameLabel.x - 12, RectSourceNameLabel.y, nameLabelSize.x + 12, RectSourceNameLabel.height);
			if (nameLabelButtonRect.Contains(Event.current.mousePosition)) {
				GUI.color = Color.white;
			}
			else {
				GUI.color = ColorText;
			}
			Widgets.Label(RectSourceNameLabel, nameLabelText);
			if (sortField == SortField.Name) {
				SortOrder sortOrder = PrepareCarefully.Instance.NameSortOrder;
				if (sortOrder == SortOrder.Ascending) {
					Rect rect = new Rect(RectSourceNameLabel.x - 13, RectSourceNameLabel.y + 8, SizeTextureSortIndicator.x, SizeTextureSortIndicator.y);
					GUI.DrawTexture(rect, Textures.TextureSortAscending);
					if (Widgets.ButtonInvisible(nameLabelButtonRect, false)) {
						PrepareCarefully.Instance.NameSortOrder = SortOrder.Descending;
						resort = true;
					}
				}
				else {
					Rect rect = new Rect(RectSourceNameLabel.x - 13, RectSourceNameLabel.y + 7, SizeTextureSortIndicator.x, SizeTextureSortIndicator.y);
					GUI.DrawTexture(rect, Textures.TextureSortDescending);
					if (Widgets.ButtonInvisible(nameLabelButtonRect, false)) {
						PrepareCarefully.Instance.NameSortOrder = SortOrder.Ascending;
						resort = true;
					}
				}
			}
			else if (Widgets.ButtonInvisible(nameLabelButtonRect, false)) {
				PrepareCarefully.Instance.SortField = SortField.Name;
				PrepareCarefully.Instance.NameSortOrder = PrepareCarefully.Instance.CostSortOrder;
				resort = true;
			}

			Text.Anchor = TextAnchor.LowerRight;
			string costLabelText = "EdB.Cost".Translate();
			Vector2 costLabelSize = Text.CalcSize(costLabelText);
			Rect costLabelButtonRect = new Rect(RectSourceCostLabel.x + RectSourceCostLabel.width - costLabelSize.x - 12, RectSourceNameLabel.y, costLabelSize.x + 12, RectSourceNameLabel.height);
			if (costLabelButtonRect.Contains(Event.current.mousePosition)) {
				GUI.color = Color.white;
			}
			else {
				GUI.color = ColorText;
			}
			Widgets.Label(RectSourceCostLabel, costLabelText);
			if (sortField == SortField.Cost) {
				SortOrder sortOrder = PrepareCarefully.Instance.CostSortOrder;
				if (sortOrder == SortOrder.Ascending) {
					Rect rect = new Rect(RectSourceCostLabel.x + RectSourceCostLabel.width - costLabelSize.x - 12, RectSourceNameLabel.y + 8, SizeTextureSortIndicator.x, SizeTextureSortIndicator.y);
					GUI.DrawTexture(rect, Textures.TextureSortAscending);
					if (Widgets.ButtonInvisible(costLabelButtonRect, false)) {
						PrepareCarefully.Instance.CostSortOrder = SortOrder.Descending;
						resort = true;
					}
				}
				else {
					Rect rect = new Rect(RectSourceCostLabel.x + RectSourceCostLabel.width - costLabelSize.x - 12, RectSourceNameLabel.y + 7, SizeTextureSortIndicator.x, SizeTextureSortIndicator.y);
					GUI.DrawTexture(rect, Textures.TextureSortDescending);
					if (Widgets.ButtonInvisible(costLabelButtonRect, false)) {
						PrepareCarefully.Instance.CostSortOrder = SortOrder.Ascending;
						resort = true;
					}
				}
			}
			else if (Widgets.ButtonInvisible(costLabelButtonRect, false)) {
				PrepareCarefully.Instance.SortField = SortField.Cost;
				PrepareCarefully.Instance.CostSortOrder = PrepareCarefully.Instance.NameSortOrder;
				resort = true;
			}

			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;

			GUI.color = ColorBoxBackground;
			GUI.DrawTexture(RectSourceBox, BaseContent.WhiteTex);
			GUI.color = ColorBoxOutline;
			Widgets.DrawBox(RectSourceBox, 1);

			GUI.color = Color.white;

			GUI.BeginGroup(RectSourceContent);
			Rect scrollRect = new Rect(0, 0, RectSourceContent.width, RectSourceContent.height);
			Rect viewRect = new Rect(scrollRect.x, scrollRect.y, scrollRect.width - 16, sourceScrollViewHeights[selectedTab]);

			Vector2 scrollPositions = sourceScrollPositions[selectedTab];
			Widgets.BeginScrollView(scrollRect, ref scrollPositions, viewRect);
			sourceScrollPositions[selectedTab] = scrollPositions;

			try {
				Rect rectEntry = RectSourceEntry;
				Rect rectText = RectSourceEntry;
				rectText.x += 65;
				rectText.width = 290;
				Rect rectCost = RectSourceEntry;
				rectCost.x += 320;
				rectCost.width = 100;
				Rect rectItem = RectSourceItem;
				bool alternateBackground = false;
				float top = sourceScrollPositions[selectedTab].y - rectEntry.height;
				float bottom = sourceScrollPositions[selectedTab].y + RectSourceBox.height;
				int index = -1;
				foreach (EquipmentDatabaseEntry entry in entries) {
					index++;
					ThingDef def = entry.def;

					if (alternateBackground) {
						GUI.color = ColorEntryBackground;
						alternateBackground = false;
					}
					else {
						GUI.color = ColorBoxBackground;
						alternateBackground = true;
					}
					if (sourceSelections[selectedTab] == index) {
						GUI.color = ColorSelectedEntry;
					}

					GUI.DrawTexture(rectEntry, BaseContent.WhiteTex);

					GUI.color = ColorText;
					Text.Font = GameFont.Small;
					Text.Anchor = TextAnchor.MiddleLeft;
					Widgets.Label(rectText, entry.Label);

					Text.Anchor = TextAnchor.MiddleRight;
					Widgets.Label(rectCost, "" + entry.cost);

					DrawEquipmentIcon(rectItem, entry);

					if (rectEntry.y > top && rectEntry.y < bottom) {
						if (Event.current.type == EventType.MouseDown && rectEntry.Contains(Event.current.mousePosition)) {
							if (Event.current.clickCount == 1) {
								if (Event.current.button == 1) {
									if (sourceSelections[selectedTab] != index) {
										sourceSelections[selectedTab] = index;
									}
									List<FloatMenuOption> list = new List<FloatMenuOption>();
									ThingDef thingDef = def;
									ThingDef stuffDef = entry.stuffDef;
									list.Add(new FloatMenuOption("ThingInfo".Translate(), delegate {
										Find.WindowStack.Add(new Dialog_InfoCard(thingDef, stuffDef));
									}, MenuOptionPriority.Medium, null, null, 0, null));
									Find.WindowStack.Add(new FloatMenu(list, null, false));
								}
								else {
									sourceSelections[selectedTab] = index;
									SoundDefOf.TickHigh.PlayOneShotOnCamera();
								}
							}
							else if (Event.current.clickCount == 2) {
								if (PrepareCarefully.Instance.AddEquipment(entry)) {
									SoundDefOf.TickLow.PlayOneShotOnCamera();
								}
								else {
									SoundDefOf.TickHigh.PlayOneShotOnCamera();
								}
								ScrollTo(entry);
							}
						}
					}
					rectEntry.y += rectEntry.height;
					rectText.y += rectEntry.height;
					rectCost.y += rectEntry.height;
					rectItem.y += rectEntry.height;
				}

				if (Event.current.type == EventType.Layout) {
					sourceScrollViewHeights[selectedTab] = rectEntry.y;
				}
			}
			catch (Exception e) {
				FatalError("Could not draw available equipment and resources", e);
			}
			finally {
				Widgets.EndScrollView();
				GUI.EndGroup();
			}

			if (sourceSelections[selectedTab] != -1) {
				if (Widgets.ButtonText(RectSourceButton, "EdB.AddButton".Translate(), true, false, true)) {
					var entry = equipmentLists[selectedTab][sourceSelections[selectedTab]];
					if (PrepareCarefully.Instance.AddEquipment(entry)) {
						SoundDefOf.TickLow.PlayOneShotOnCamera();
					}
					else {
						SoundDefOf.TickHigh.PlayOneShotOnCamera();
					}
					ScrollTo(entry);
				}
			}

			if (resort) {
				SoundDefOf.TickLow.PlayOneShotOnCamera();
				SortEquipmentLists();
			}
		}

		protected void DrawEquipmentIcon(Rect rect, EquipmentDatabaseEntry entry)
		{
			GUI.color = entry.color;
			if (entry.thing == null) {
				// EdB: Inline copy of static Widgets.ThingIcon(Rect, ThingDef) with the selected
				// color based on the stuff.
				GUI.color = entry.color;
				// EdB: Inline copy of static private method with modifications to keep scaled icons within the
				// bounds of the specified Rect and to draw them using the stuff color.
				//Widgets.ThingIconWorker(rect, thing.def, thingDef.uiIcon);
				float num = GenUI.IconDrawScale(entry.def);
				Rect resizedRect = rect;
				if (num != 1f) {
					// For items that are going to scale out of the bounds of the icon rect, we need to shrink
					// the bounds a little.
					if (num > 1) {
						resizedRect = rect.ContractedBy(4);
					}
					resizedRect.width *= num;
					resizedRect.height *= num;
					resizedRect.center = rect.center;
				}
				GUI.DrawTexture(resizedRect, entry.def.uiIcon);
				GUI.color = Color.white;
			}
			else {
				// EdB: Inline copy of static Widgets.ThingIcon(Rect, Thing) with graphics switched to show a side view
				// instead of a front view.
				Thing thing = entry.thing;
				GUI.color = thing.DrawColor;
				Texture resolvedIcon;
				if (!thing.def.uiIconPath.NullOrEmpty()) {
					resolvedIcon = thing.def.uiIcon;
				}
				else if (thing is Pawn) {
					Pawn pawn = (Pawn)thing;
					if (!pawn.Drawer.renderer.graphics.AllResolved) {
						pawn.Drawer.renderer.graphics.ResolveAllGraphics();
					}
					Material matSingle = pawn.Drawer.renderer.graphics.nakedGraphic.MatSide;
					resolvedIcon = matSingle.mainTexture;
					GUI.color = matSingle.color;
				}
				else {
					resolvedIcon = thing.Graphic.ExtractInnerGraphicFor(thing).MatSide.mainTexture;
				}
				// EdB: Inline copy of static private method.
				//Widgets.ThingIconWorker(rect, thing.def, resolvedIcon);
				float num = GenUI.IconDrawScale(thing.def);
				if (num != 1f) {
					Vector2 center = rect.center;
					rect.width *= num;
					rect.height *= num;
					rect.center = center;
				}
				GUI.DrawTexture(rect, resolvedIcon);
			}
			GUI.color = Color.white;
		}

		protected void ScrollTo(EquipmentDatabaseEntry entry)
		{
			int index = PrepareCarefully.Instance.Equipment.FindIndex((SelectedEquipment e) => {
				return e.def == entry.def && e.stuffDef == entry.stuffDef;
			});
			if (index != -1) {
				destSelection = index;
				float pos = (float)index * RectDestEntry.height;
				if (pos < destScrollPosition.y) {
					newDestScrollPosition = pos;
				}
				else if (pos > destScrollPosition.y + RectDestContent.height - RectDestEntry.height) {
					newDestScrollPosition = pos + RectDestEntry.height - RectDestContent.height;
				}
			}
		}

		protected static Rect RectDestBox = new Rect(490, RectSourceBox.y, RectSourceBox.width, RectSourceBox.height);
		protected static Rect RectDestContent = RectDestBox.ContractedBy(1);
		protected static Rect RectDestEntry = new Rect(0, 0, RectDestContent.width, 42);
		protected static Rect RectDestItem = new Rect(10, 2, 38, 38);
		protected static Rect RectDestButton = new Rect(RectDestBox.x + (RectDestBox.width / 2) - (RectSourceButton.width / 2),
			RectDestBox.y + RectDestBox.height + 8, RectSourceButton.width, RectSourceButton.height);

		protected void DrawSelectedEquipmentList()
		{
			if (destSelection >= PrepareCarefully.Instance.Equipment.Count) {
				destSelection = PrepareCarefully.Instance.Equipment.Count - 1;
			}

			GUI.color = ColorBoxBackground;
			GUI.DrawTexture(RectDestBox, BaseContent.WhiteTex);
			GUI.color = ColorBoxOutline;
			Widgets.DrawBox(RectDestBox, 1);

			try {
				GUI.color = Color.white;
				GUI.BeginGroup(RectDestContent);
				Rect scrollRect = new Rect(0, 0, RectDestContent.width, RectDestContent.height);
				Rect viewRect = new Rect(scrollRect.x, scrollRect.y, scrollRect.width - 16, destScrollViewHeight);

				Widgets.BeginScrollView(scrollRect, ref destScrollPosition, viewRect);
				Rect rectEntry = RectDestEntry;
				Rect rectText = RectDestEntry;
				rectText.x += 65;
				rectText.width = 320;
				Rect rectCost = RectDestEntry;
				rectCost.x += 352;
				rectCost.y += 7;
				rectCost.height -= 14;
				rectCost.width = 60;
				Rect rectItem = RectDestItem;
				Rect rectEntryButton = RectDestEntry;
				rectEntryButton.width = 320;
				bool alternateBackground = false;
				float top = destScrollPosition.y - rectEntry.height;
				float bottom = destScrollPosition.y + RectDestBox.height;
				int index = -1;
				foreach (SelectedEquipment customPawn in PrepareCarefully.Instance.Equipment) {
					index++;
					ThingDef def = customPawn.def;
					EquipmentDatabaseEntry entry = PrepareCarefully.Instance.EquipmentEntries[customPawn.EquipmentKey];
					if (entry == null) {
						string thing = def != null ? def.defName : "null";
						string stuff = customPawn.stuffDef != null ? customPawn.stuffDef.defName : "null";
						Log.Warning(string.Format("Could not draw unrecognized resource/equipment.  Invalid item was removed.  This may have been caused by an invalid thing/stuff combination. (thing = {0}, stuff={1})", thing, stuff));
						PrepareCarefully.Instance.RemoveEquipment(customPawn);
						continue;
					}
					SelectedEquipment loadoutRecord = PrepareCarefully.Instance.Find(entry);

					if (alternateBackground) {
						GUI.color = ColorEntryBackground;
						alternateBackground = false;
					}
					else {
						GUI.color = ColorBoxBackground;
						alternateBackground = true;
					}
					if (destSelection == index) {
						GUI.color = ColorSelectedEntry;
					}

					GUI.DrawTexture(rectEntry, BaseContent.WhiteTex);

					GUI.color = ColorText;
					Text.Font = GameFont.Small;
					Text.Anchor = TextAnchor.MiddleLeft;
					Widgets.Label(rectText, entry.LabelNoCount);

					DrawEquipmentIcon(rectItem, entry);

					Rect fieldRect = rectCost;
					Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);

					equipmentDragSlider.OnGUI(fieldRect, loadoutRecord.count, (int value) => {
						var record = loadoutRecord;
						record.count = value;
					});
					bool dragging = DragSlider.IsDragging();

					Rect buttonRect = new Rect(fieldRect.x - 17, fieldRect.y + 6, 16, 16);
					if (!dragging && buttonRect.Contains(Event.current.mousePosition)) {
						GUI.color = ButtonHighlightColor;
					}
					else {
						GUI.color = ButtonColor;
					}
					GUI.DrawTexture(buttonRect, Textures.TextureButtonPrevious);
					if (Widgets.ButtonInvisible(buttonRect, false)) {
						SoundDefOf.TickHigh.PlayOneShotOnCamera();
						int amount = Event.current.shift ? 10 : 1;
						loadoutRecord.count -= amount;
						if (loadoutRecord.count < 0) {
							loadoutRecord.count = 0;
						}
					}

					buttonRect = new Rect(fieldRect.x + fieldRect.width + 1, fieldRect.y + 6, 16, 16);
					if (!dragging && buttonRect.Contains(Event.current.mousePosition)) {
						GUI.color = ButtonHighlightColor;
					}
					else {
						GUI.color = ButtonColor;
					}
					GUI.DrawTexture(buttonRect, Textures.TextureButtonNext);
					if (Widgets.ButtonInvisible(buttonRect, false)) {
						SoundDefOf.TickHigh.PlayOneShotOnCamera();
						int amount = Event.current.shift ? 10 : 1;
						loadoutRecord.count += amount;
					}

					if (rectEntry.y > top && rectEntry.y < bottom) {
						if (Event.current.type == EventType.MouseDown && rectEntryButton.Contains(Event.current.mousePosition)) {
							if (Event.current.clickCount == 1) {
								if (Event.current.button == 1) {
									if (destSelection != index) {
										destSelection = index;
									}
									List<FloatMenuOption> list = new List<FloatMenuOption>();
									ThingDef thingDef = customPawn.def;
									ThingDef stuffDef = customPawn.stuffDef;
									list.Add(new FloatMenuOption("ThingInfo".Translate(), delegate {
										Find.WindowStack.Add(new Dialog_InfoCard(thingDef, stuffDef));
									}, MenuOptionPriority.Medium, null, null, 0, null));
									Find.WindowStack.Add(new FloatMenu(list, null, false));
								}
								else {
									destSelection = index;
									SoundDefOf.TickHigh.PlayOneShotOnCamera();
								}
							}
							else if (Event.current.clickCount == 2) {
								if (customPawn.count > 0) {
									SoundDefOf.TickHigh.PlayOneShotOnCamera();
									int amount = Event.current.shift ? 10 : 1;
									loadoutRecord.count -= amount;
									if (loadoutRecord.count < 0) {
										loadoutRecord.count = 0;
									}
								}
								else {
									SoundDefOf.TickLow.PlayOneShotOnCamera();
									PrepareCarefully.Instance.RemoveEquipment(PrepareCarefully.Instance.EquipmentEntries[customPawn.EquipmentKey]);
								}
							}
						}
					}
					rectEntry.y += rectEntry.height;
					rectText.y += rectEntry.height;
					rectCost.y += rectEntry.height;
					rectItem.y += rectEntry.height;
					rectEntryButton.y += rectEntry.height;
				}

				if (Event.current.type == EventType.Layout) {
					destScrollViewHeight = rectEntry.y;
				}
			}
			catch (Exception e) {
				FatalError("Could not draw selected resources", e);
			}
			finally {
				Widgets.EndScrollView();
				GUI.EndGroup();
			}

			if (newDestScrollPosition >= 0) {
				destScrollPosition.y = newDestScrollPosition;
				newDestScrollPosition = -1;
			}
				
			GUI.color = Color.white;

			if (destSelection != -1) {
				if (Widgets.ButtonText(RectDestButton, "EdB.RemoveButton".Translate(), true, false, true)) {
					var customPawn = PrepareCarefully.Instance.Equipment[destSelection];
					SoundDefOf.TickLow.PlayOneShotOnCamera();
					PrepareCarefully.Instance.RemoveEquipment(PrepareCarefully.Instance.EquipmentEntries[customPawn.EquipmentKey]);
				}
			}

		}

		private bool TryStartGame()
		{
			AcceptanceReport acceptanceReport = this.CanStart();
			if (!acceptanceReport.Accepted) {
				Messages.Message(acceptanceReport.Reason, MessageSound.RejectInput);
				return false;
			}

			PrepareCarefully.Instance.Active = true;
			PrepareCarefully.Instance.CreateColonists();
			return true;
		}
	}
}
