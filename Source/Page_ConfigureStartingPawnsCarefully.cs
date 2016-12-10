using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully
{
	public class Page_ConfigureStartingPawnsCarefully : Page_PrepareCarefully
	{
		private DragSlider chronologicalAgeDragSlider;
		private DragSlider biologicalAgeDragSlider;

		public static readonly Vector2 WinSize = new Vector2(1020, 764);
		public const float TopAreaHeight = 80;

		public static Color SectionBackgroundColor = new Color(0.145098f, 0.1490196f, 0.152941f);
		public static Color TextColor = new Color(0.90196f, 0.90196f, 0.90196f);
		public static Color PortraitBorderColor = new Color(0.3843f, 0.3843f, 0.3843f);
		public static Color ButtonColor = new Color(0.623529f, 0.623529f, 0.623529f);
		public static Color ButtonHighlightColor = new Color(0.97647f, 0.97647f, 0.97647f);
		public static Color ButtonDisabledColor = new Color(0.27647f, 0.27647f, 0.27647f);
		public static Color PortraitTabActiveColor = SectionBackgroundColor;
		public static Color PortraitTabInactiveColor = new Color(21.0f / 255.0f, 25.0f / 255.0f, 29.0f / 255.0f);
		public static float PortraitTabMargin = 1;
		public static float PortraitTabPadding = 11;

		protected int selectedPawnLayer = 0;
		protected List<int> pawnLayers;
		protected List<Action> pawnLayerActions;

		protected FieldInfo apparelGraphicsField = null;

		protected ScrollView skillScrollView = new ScrollView();

		public int CurrentPawnIndex {
			get {
				return State.CurrentPawnIndex;
			}
			set {
				State.CurrentPawnIndex = value;
			}
		}
		public CustomPawn CurrentPawn {
			get {
				return PrepareCarefully.Instance.Pawns[CurrentPawnIndex];
			}
		}

		protected int pawnLayerLabelIndex = -1;
		protected CustomPawn pawnLayerLabelModel = null;
		protected string pawnLayerLabel = null;

		protected List<List<ThingDef>> apparelLists = new List<List<ThingDef>>(PawnLayers.Count);

		protected List<Backstory> childhoodBackstories = new List<Backstory>();
		protected List<Backstory> adulthoodBackstories = new List<Backstory>();
		protected List<Backstory> sortedChildhoodBackstories;
		protected List<Backstory> sortedAdulthoodBackstories;
		protected List<Trait> traits = new List<Trait>();
		protected List<Trait> sortedTraits;

		protected List<string> maleHeads = new List<string>();
		protected List<string> femaleHeads = new List<string>();
		protected Dictionary<BodyType, string> bodyTypeLabels = new Dictionary<BodyType, string>();
		protected List<HairDef> maleHairDefs = new List<HairDef>();
		protected List<HairDef> femaleHairDefs = new List<HairDef>();
		protected List<Color> skinColors = new List<Color>();
		protected List<Color> hairColors = new List<Color>();
		protected List<BodyType> maleBodyTypes = new List<BodyType>();
		protected List<BodyType> femaleBodyTypes = new List<BodyType>();
		protected LeftPanelMode leftPanelMode = LeftPanelMode.Appearance;

		protected bool bionicsMode = false;
		protected Vector2 bionicsScrollPosition = new Vector2(0, 0);
		protected float bionicsScrollHeight = 0;

		protected Dictionary<ThingDef, List<ThingDef>> apparelStuffLookup = new Dictionary<ThingDef, List<ThingDef>>();

		protected int selectedStuff = 0;

		protected HashSet<Backstory> problemBackstories = new HashSet<Backstory>();

		protected Randomizer randomizer = new Randomizer();

		protected int maxAge = 99;
		protected int minAge = 14;
		protected int maxChronologicalAge = 3200;

		protected ScrollView HealthScrollView = new ScrollView();
		protected ScrollView RelationsScrollView = new ScrollView();

		protected Panel_Relations PanelRelations;
		protected Panel_Health PanelHealth;

		static Page_ConfigureStartingPawnsCarefully() {
		}

		public override Vector2 InitialSize {
			get {
				return Page_ConfigureStartingPawnsCarefully.WinSize;
			}
		}

		public enum LeftPanelMode
		{
			Appearance,
			Relations,
			Health
		};

		public Page_ConfigureStartingPawnsCarefully()
		{
			this.absorbInputAroundWindow = true;
			this.forcePause = true;

			chronologicalAgeDragSlider = new DragSlider(0.4f, 20, 100);
			chronologicalAgeDragSlider.minValue = minAge;
			chronologicalAgeDragSlider.maxValue = maxChronologicalAge;

			biologicalAgeDragSlider = new DragSlider(0.4f, 15, 100);
			biologicalAgeDragSlider.minValue = minAge;
			biologicalAgeDragSlider.maxValue = maxAge;

			foreach (string path in GraphicsCache.Instance.MaleHeadPaths) {
				maleHeads.Add(path);
			}
			foreach (string path in GraphicsCache.Instance.FemaleHeadPaths) {
				femaleHeads.Add(path);
			}

			maleBodyTypes.Add(BodyType.Male);
			maleBodyTypes.Add(BodyType.Thin);
			maleBodyTypes.Add(BodyType.Fat);
			maleBodyTypes.Add(BodyType.Hulk);

			femaleBodyTypes.Add(BodyType.Female);
			femaleBodyTypes.Add(BodyType.Thin);
			femaleBodyTypes.Add(BodyType.Fat);
			femaleBodyTypes.Add(BodyType.Hulk);

			bodyTypeLabels.Add(BodyType.Fat, "EdB.BodyType.Fat".Translate());
			bodyTypeLabels.Add(BodyType.Hulk, "EdB.BodyType.Hulk".Translate());
			bodyTypeLabels.Add(BodyType.Thin, "EdB.BodyType.Thin".Translate());
			bodyTypeLabels.Add(BodyType.Male, "EdB.BodyType.Average".Translate());
			bodyTypeLabels.Add(BodyType.Female, "EdB.BodyType.Average".Translate());

			pawnLayers = new List<int>(new int[] {
				PawnLayers.BodyType,
				PawnLayers.HeadType,
				PawnLayers.Hair,
				PawnLayers.Pants,
				PawnLayers.BottomClothingLayer,
				PawnLayers.MiddleClothingLayer,
				PawnLayers.TopClothingLayer,
				PawnLayers.Accessory,
				PawnLayers.Hat
			});
			pawnLayerActions = new List<Action>(new Action[] {
				delegate { this.ChangePawnLayer(PawnLayers.BodyType); },
				delegate { this.ChangePawnLayer(PawnLayers.HeadType); },
				delegate { this.ChangePawnLayer(PawnLayers.Hair); },
				delegate { this.ChangePawnLayer(PawnLayers.Pants); },
				delegate { this.ChangePawnLayer(PawnLayers.BottomClothingLayer); },
				delegate { this.ChangePawnLayer(PawnLayers.MiddleClothingLayer); },
				delegate { this.ChangePawnLayer(PawnLayers.TopClothingLayer); },
				delegate { this.ChangePawnLayer(PawnLayers.Accessory); },
				delegate { this.ChangePawnLayer(PawnLayers.Hat); }
			});

			// Initialize and sort hair lists.
			foreach (HairDef hairDef in DefDatabase<HairDef>.AllDefs) {
				if (hairDef.hairGender != HairGender.Male) {
					femaleHairDefs.Add(hairDef);
				}
				if (hairDef.hairGender != HairGender.Female) {
					maleHairDefs.Add(hairDef);
				}
			}
			femaleHairDefs.Sort((HairDef x, HairDef y) => {
				if (x.label == null) {
					return -1;
				}
				else if (y.label == null) {
					return 1;
				}
				else {
					return x.label.CompareTo(y.label);
				}
			});
			maleHairDefs.Sort((HairDef x, HairDef y) => {
				if (x.label == null) {
					return -1;
				}
				else if (y.label == null) {
					return 1;
				}
				else {
					return x.label.CompareTo(y.label);
				}
			});
				
			for (int i = 0; i < PawnLayers.Count; i++) {
				if (PawnLayers.IsApparelLayer(i)) {
					this.apparelLists.Add(new List<ThingDef>());
				}
				else {
					this.apparelLists.Add(null);
				}
			}

			// Get all apparel options
			foreach (ThingDef apparelDef in DefDatabase<ThingDef>.AllDefs) {
				if (apparelDef.apparel == null) {
					continue;
				}
				int layer = PawnLayers.ToPawnLayerIndex(apparelDef.apparel);
				if (layer != -1) {
					apparelLists[layer].Add(apparelDef);
				}
			}

			// Get the apparel graphics private method so that we can call it later
			apparelGraphicsField = typeof(PawnGraphicSet).GetField("apparelGraphics", BindingFlags.Instance | BindingFlags.NonPublic);

			// Organize stuff by its category
			Dictionary<StuffCategoryDef, HashSet<ThingDef>> stuffByCategory = new Dictionary<StuffCategoryDef, HashSet<ThingDef>>();
			foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs) {
				if (thingDef.IsStuff && thingDef.stuffProps != null) {
					foreach (StuffCategoryDef cat in thingDef.stuffProps.categories) {
						HashSet<ThingDef> thingDefs = null;
						if (!stuffByCategory.TryGetValue(cat, out thingDefs)) {
							thingDefs = new HashSet<ThingDef>();
							stuffByCategory.Add(cat, thingDefs);
						}
						thingDefs.Add(thingDef);
					}
				}
			}

			// For each apparel def, get the list of all materials that can be used to make it.
			foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs) {
				if (thingDef.apparel != null && thingDef.MadeFromStuff) {
					if (thingDef.stuffCategories != null) {
						List<ThingDef> stuffList = new List<ThingDef>();
						foreach (var cat in thingDef.stuffCategories) {
							HashSet<ThingDef> thingDefs;
							if (stuffByCategory.TryGetValue(cat, out thingDefs)) {
								foreach (ThingDef stuffDef in thingDefs) {
									stuffList.Add(stuffDef);
								}
							}
						}
						apparelStuffLookup[thingDef] = stuffList;
					}
				}
			}

			// Sort the apparel lists
			foreach (var list in apparelLists) {
				if (list != null) {
					list.Sort((ThingDef x, ThingDef y) => {
						if (x.label == null) {
							return -1;
						}
						else if (y.label == null) {
							return 1;
						}
						else {
							return x.label.CompareTo(y.label);
						}
					});
				}
			}

			// Iterate through each solid bio.  Find the corresponding backstories.  If the backstory description contains the bio's name
			// then mark the backstory as name-specific.  If the bio does not have HE/HIS/etc, mark the backstory as gender-specific.

			List<Backstory> backstories = BackstoryDatabase.allBackstories.Values.ToList();
			foreach (Backstory backstory in backstories) {


				if (!backstory.shuffleable && (!backstory.baseDesc.Contains("NAME") || (!backstory.baseDesc.Contains("HECAP") && !backstory.baseDesc.Contains("HE ")
					&& !backstory.baseDesc.Contains("HIS") && !backstory.baseDesc.Contains("HISCAP") && !backstory.baseDesc.Contains("HIM")))) {
					problemBackstories.Add(backstory);
				}
				if (backstory.slot == BackstorySlot.Childhood) {
					childhoodBackstories.Add(backstory);
				}
				else {
					adulthoodBackstories.Add(backstory);
				}
			}

			// Create sorted versions of the backstory lists
			sortedChildhoodBackstories = new List<Backstory>(childhoodBackstories);
			sortedChildhoodBackstories.Sort((b1, b2) => b1.Title.CompareTo(b2.Title));
			sortedAdulthoodBackstories = new List<Backstory>(adulthoodBackstories);
			sortedAdulthoodBackstories.Sort((b1, b2) => b1.Title.CompareTo(b2.Title));

			// Get all trait options
			foreach (TraitDef def in DefDatabase<TraitDef>.AllDefs) {
				List<TraitDegreeData> degreeData = def.degreeDatas;
				int count = degreeData.Count;
				if (count > 0) {
					for (int i = 0; i < count; i++) {
						Trait trait = new Trait(def, degreeData[i].degree, true);
						traits.Add(trait);
					}
				}
				else {
					traits.Add(new Trait(def, 0, true));
				}
			}

			// Create a sorted version of the trait list
			sortedTraits = new List<Trait>(traits);
			sortedTraits.Sort((t1, t2) => t1.LabelCap.CompareTo(t2.LabelCap));

			// Set up default hair colors
			hairColors.Add(new Color(0.2f, 0.2f, 0.2f));
			hairColors.Add(new Color(0.31f, 0.28f, 0.26f));
			hairColors.Add(new Color(0.25f, 0.2f, 0.15f));
			hairColors.Add(new Color(0.3f, 0.2f, 0.1f));
			hairColors.Add(new Color(0.3529412f, 0.227451f, 0.1254902f));
			hairColors.Add(new Color(0.5176471f, 0.3254902f, 0.1843137f));
			hairColors.Add(new Color(0.7568628f, 0.572549f, 0.3333333f));
			hairColors.Add(new Color(0.9294118f, 0.7921569f, 0.6117647f));

			// Set up default skin colors
			foreach (Color color in PawnColorUtils.Colors) {
				skinColors.Add(color);
			}

			this.ChangePawnLayer(pawnLayers[0]);

			PanelRelations = new Panel_Relations(SectionRectPortraitContent);
			PanelHealth = new Panel_Health(SectionRectPortraitContent);
		}

		public static void ExportBackstoriesToFile(string path)
		{
			System.Xml.Linq.XDocument doc = new System.Xml.Linq.XDocument();
			System.Xml.Linq.XElement el = new System.Xml.Linq.XElement("Backstories");
			doc.Add(el);
			foreach (Backstory backstory in BackstoryDatabase.allBackstories.Values) {
				el.Add(XmlSaver.XElementFromObject(backstory, backstory.GetType()));
			}
			doc.Save(path);
		}

		public void AddColonist()
		{
			AddColonist(randomizer.GenerateColonist());
		}

		public void AddColonist(Pawn pawn)
		{
			AddColonist(new CustomPawn(pawn));
		}

		public void AddColonist(CustomPawn pawn)
		{
			PrepareCarefully.Instance.AddPawn(pawn);
			CurrentPawnIndex = PrepareCarefully.Instance.Pawns.Count - 1;
		}

		public void DeleteColonist(int index)
		{
			CustomPawn customPawn = CurrentPawn;
			PrepareCarefully.Instance.RemovePawn(customPawn);

			if (CurrentPawnIndex >= PrepareCarefully.Instance.Pawns.Count) {
				CurrentPawnIndex = PrepareCarefully.Instance.Pawns.Count - 1;
			}
			PrepareCarefully.Instance.RelationshipManager.DeletePawnRelationships(customPawn);
		}

		private AcceptanceReport CanStart()
		{
			foreach (CustomPawn current in PrepareCarefully.Instance.Pawns) {
				if (!current.Name.IsValid) {
					return new AcceptanceReport("EveryoneNeedsValidName".Translate());
				}
			}
			return AcceptanceReport.WasAccepted;
		}

		public override void WindowUpdate()
		{
			base.WindowUpdate();
			DragSliderManager.DragSlidersUpdate();
		}

		public override void DoWindowContents(Rect inRect)
		{
			if (hidden) {
				return;
			}

			if (CurrentPawnIndex >= PrepareCarefully.Instance.Pawns.Count) {
				CurrentPawnIndex = PrepareCarefully.Instance.Pawns.Count - 1;
			}

			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(0, 0, 300, 300), "CreateCharacters".Translate());
			Text.Font = GameFont.Small;

			if (this.IsBroken) {
				return;
			}

			if (!Textures.Loaded) {
				FatalError("Textures failed to load", null);
				return;
			}

			Rect rect = new Rect(0, 80, inRect.width, inRect.height - 60 - 80);
			Widgets.DrawMenuSection(rect, true);
			int tabCount = PrepareCarefully.Instance.Pawns.Count;
			CustomPawn selectedModel = CurrentPawn;
			List<TabRecord> tabs = (from m in PrepareCarefully.Instance.Pawns select new TabRecord(tabCount < 7 ? m.Label : m.NickName, delegate { this.SelectModel(m); }, m == selectedModel)).ToList();
			DrawTabs(rect, tabs);

			// Draw the add colonist button.
			bool moreColonistsAllowed = PrepareCarefully.Instance.Pawns.Count < Config.maxColonists;
			Rect addButtonRect = new Rect(rect.width - 20, rect.y - 23, 16, 16);
			if (moreColonistsAllowed) {
				GUI.color = addButtonRect.Contains(Event.current.mousePosition) ? Page_ConfigureStartingPawnsCarefully.ButtonHighlightColor : Page_ConfigureStartingPawnsCarefully.ButtonColor;
			}
			else {
				GUI.color = Page_ConfigureStartingPawnsCarefully.ButtonDisabledColor;
			}
			GUI.DrawTexture(addButtonRect, Textures.TextureButtonAdd);
			if (moreColonistsAllowed && Widgets.ButtonInvisible(addButtonRect, false)) {
				SoundDefOf.SelectDesignator.PlayOneShotOnCamera();
				AddColonist();
			}
			GUI.color = Color.white;

			Rect innerRect = rect.ContractedBy(22);

			try {
				GUI.BeginGroup(innerRect);
				DrawNameAndDescription();
				DrawRandomizeAllButton();
				DrawGenderAndAge();
				DrawColonistSaveButtons();
				DrawPortrait();
				DrawBackstory();
				DrawTraits();
				DrawIncapable();
				DrawSkills();
			}
			catch (Exception e) {
				FatalError("Could not draw character screen.", e);
			}
			finally {
				GUI.EndGroup();
			}

			DrawCost(inRect);
			DrawPresetButtons();

			GUI.color = Color.white;

			// TODO: Alpha 14
			DoNextBackButtons(inRect, "Next".Translate(),
				delegate {
					AcceptanceReport acceptanceReport = this.CanStart();
					if (acceptanceReport.Accepted) {
						Page_Equipment equipmentPage = new Page_Equipment();
						Find.WindowStack.Add(new Page_Equipment());
						this.Close(true);
					}
					else {
						Messages.Message(acceptanceReport.Reason, MessageSound.RejectInput);
					}
				},
				delegate {
					Find.WindowStack.Add(new Dialog_Confirm("EdB.ExitPrepareCarefullyConfirm".Translate(), delegate {
						PrepareCarefully.Instance.Clear();
						this.Close(true);
					}, true, null, true));
				}
			);
		}

		public TabRecord DrawTabs(Rect baseRect, IEnumerable<TabRecord> tabsEnum)
		{
			bool moreColonistsAllowed = PrepareCarefully.Instance.Pawns.Count < Config.maxColonists;
			bool fewerColonistsAllowed = PrepareCarefully.Instance.Pawns.Count > 1;

			List<TabRecord> tabList = tabsEnum.ToList<TabRecord>();
			int tabCount = tabList.Count;
			int colonistCount = tabList.Count;
			TabRecord clickedTab = null;
			TabRecord selectedTab = (from t in tabList
				where t.selected
				select t).FirstOrDefault<TabRecord>();
			if (selectedTab == null) {
				Debug.LogWarning("Drew tabs without any being selected.");
				return tabList[0];
			}
			float num = (baseRect.width - 24) + (float)(tabCount - 1) * 10;
			float tabWidth = (float)(Math.Floor(num / ((float)tabCount)));
			if (tabWidth > 200) {
				tabWidth = 200;
			}
			Rect position = new Rect(baseRect);
			position.y -= 32;
			position.height = 9999;
			GUI.BeginGroup(position);
			try {
				Text.Anchor = TextAnchor.MiddleCenter;
				Text.Font = GameFont.Small;
				Func<TabRecord, Rect> func = delegate(TabRecord tab) {
					int tabIndex = tabList.IndexOf(tab);
					float left = (float)tabIndex * (tabWidth - 10);
					return new Rect(left, 1, tabWidth, 32);
				};

				List<TabRecord> list = tabList.ListFullCopy<TabRecord>();
				list.Remove(selectedTab);
				list.Add(selectedTab);
				TabRecord tabRecord3 = null;
				List<TabRecord> list2 = list.ListFullCopy<TabRecord>();
				list2.Reverse();

				foreach (TabRecord current in list2) {
					Rect rect = func(current);
					if (tabRecord3 == null && rect.Contains(Event.current.mousePosition)) {
						tabRecord3 = current;
					}

					Rect closeButtonRect = new Rect(rect.x + rect.width - 30, 8, 16, 16);
					if (colonistCount > 1 && current.selected && Widgets.ButtonInvisible(closeButtonRect, false)) {
						int index = tabList.IndexOf(current);
						Find.WindowStack.Add(new Dialog_Confirm("EdB.DeleteColonistConfirm".Translate(), delegate {
							DeleteColonist(index);
						}, true, null, true));
					}

					MouseoverSounds.DoRegion(rect);
					if (Widgets.ButtonInvisible(rect, false)) {
						clickedTab = current;
					}
				}

				foreach (TabRecord current2 in list) {
					Rect rect2 = func(current2);
					current2.DrawTab(rect2);
					if (fewerColonistsAllowed && current2.selected) {
						Rect closeButtonRect = new Rect(rect2.x + rect2.width - 30, 8, 16, 16);
						if (closeButtonRect.Contains(Event.current.mousePosition)) {
							GUI.color = Color.white;
							GUI.DrawTexture(closeButtonRect, Textures.TextureButtonDeleteTabHighlight);
						}
						else {
							GUI.color = new Color(0.7f, 0.7f, 0.7f);
							GUI.DrawTexture(closeButtonRect, Textures.TextureButtonDeleteTab);
						}
						GUI.color = Color.white;
					}
				}

				Text.Anchor = TextAnchor.UpperLeft;
			}
			catch (Exception e) {
				FatalError("Failed to draw colonist tabs", e);
			}
			finally {
				GUI.EndGroup();
			}
			if (clickedTab != null) {
				SoundDefOf.SelectDesignator.PlayOneShotOnCamera();
				if (clickedTab.clickedAction != null) {
					clickedTab.clickedAction();
				}
			}
			return clickedTab;
		}

		private void SelectModel(CustomPawn m)
		{
			if (CurrentPawn != m) {
				CurrentPawnIndex = PrepareCarefully.Instance.Pawns.IndexOf(m);
				if (CurrentPawnIndex > -1) {
					SoundDefOf.SelectDesignator.PlayOneShotOnCamera();
				}
			}
		}

		protected string PawnDescription
		{
			get {
				CustomPawn customPawn = CurrentPawn;
				string text = string.Concat(new string[] {
					((customPawn.Gender != Gender.Male) ? "Female".Translate() : "Male".Translate()),
					" ",
					customPawn.Pawn.def.label,
					" ",
					customPawn.Pawn.KindLabel,
					", ",
					"AgeIndicator".Translate(new object[] {
						customPawn.BiologicalAge
					})
				});
				return text;
			}
		}

		public CustomPawn SelectedPawn {
			get {
				return CurrentPawn;
			}
		}

		protected static int LeftColumnWidth = 250;
		protected static int MiddleColumnWidth = 320;
		protected static int RightColumnWidth = 330;
		protected static Vector2 SectionPadding = new Vector2(18, 12);
		protected static Vector2 SectionMargin = new Vector2(18, 12);

		protected static Rect SectionRandomizeAll = new Rect(0, 0, 64, 64);
		protected static Rect SectionRectNameAndDescripton = new Rect(SectionRandomizeAll.x + SectionRandomizeAll.width + SectionPadding.x, SectionRandomizeAll.y, 505, 64);

		protected static float PortraitTabsHeight = 28;
		protected static Rect SectionRectPortrait = new Rect(0, SectionRectNameAndDescripton.y + SectionRectNameAndDescripton.height + SectionPadding.y, LeftColumnWidth, 441);
		protected static Rect SectionRectPortraitBody = new Rect(0, SectionRectPortrait.y + PortraitTabsHeight, LeftColumnWidth, SectionRectPortrait.height - PortraitTabsHeight);
		protected static Rect SectionRectPortraitContent = new Rect(0, PortraitTabsHeight, SectionRectPortrait.width, SectionRectPortrait.height - PortraitTabsHeight);

		protected static Rect SectionRectGenderAndAge = new Rect(SectionRectPortrait.x + SectionRectPortrait.width + SectionPadding.x, SectionRectPortrait.y, MiddleColumnWidth, 52);
		protected static Rect SectionRectBackstory = new Rect(SectionRectGenderAndAge.x, SectionRectGenderAndAge.y + SectionRectGenderAndAge.height + SectionPadding.y, MiddleColumnWidth, 120);
		protected static Rect SectionRectTraits = new Rect(SectionRectGenderAndAge.x, SectionRectBackstory.y + SectionRectBackstory.height + SectionPadding.y, MiddleColumnWidth, 157);
		protected static Rect SectionColonistSave = new Rect(SectionRectGenderAndAge.x, SectionRectTraits.y + SectionRectTraits.height + SectionPadding.y, MiddleColumnWidth, SectionRectNameAndDescripton.height + 12);

		protected static Rect SectionRectSkills = new Rect(SectionRectGenderAndAge.x + SectionRectGenderAndAge.width + SectionPadding.x, 0, RightColumnWidth, 400);
		protected static Rect SectionRectIncapable = new Rect(SectionRectSkills.x, SectionRectSkills.y + SectionRectSkills.height + SectionPadding.y, RightColumnWidth, 105);

		protected void DrawRandomizeAllButton()
		{
			CustomPawn customPawn = CurrentPawn;
			GUI.color = SectionBackgroundColor;
			GUI.DrawTexture(SectionRandomizeAll, BaseContent.WhiteTex);

			Rect randomRect = new Rect(SectionRandomizeAll.x + SectionRandomizeAll.width / 2 - Textures.TextureButtonRandomLarge.width / 2 - 1,
				SectionRandomizeAll.y + SectionRandomizeAll.height / 2 - Textures.TextureButtonRandomLarge.height / 2, Textures.TextureButtonRandomLarge.width,
				Textures.TextureButtonRandomLarge.height);
			if (randomRect.Contains(Event.current.mousePosition)) {
				GUI.color = ButtonHighlightColor;
			}
			else {
				GUI.color = ButtonColor;
			}
			GUI.DrawTexture(randomRect, Textures.TextureButtonRandomLarge);
			if (Widgets.ButtonInvisible(randomRect, false)) {
				SoundDefOf.TickLow.PlayOneShotOnCamera();
				randomizer.RandomizeAll(customPawn);
			}

			GUI.color = Color.white;
		}

		protected void DrawNameAndDescription()
		{
			CustomPawn customPawn = CurrentPawn;
			GUI.color = SectionBackgroundColor;
			GUI.DrawTexture(SectionRectNameAndDescripton, BaseContent.WhiteTex);

			GUI.BeginGroup(new Rect(SectionRectNameAndDescripton.x + SectionPadding.x, SectionRectNameAndDescripton.y + 10, SectionRectNameAndDescripton.width - SectionPadding.x * 2, 44));

			Text.Anchor = TextAnchor.UpperLeft;

			GUI.color = Color.white;
			Rect nameFieldsRect = new Rect(0, 7, 442, 30);
			Rect firstNameRect = new Rect(nameFieldsRect);
			firstNameRect.width *= 0.333f;
			Rect nickNameRect = new Rect(nameFieldsRect);
			nickNameRect.width *= 0.333f;
			nickNameRect.x += nickNameRect.width + 2;
			Rect lastNameRect = new Rect(nameFieldsRect);
			lastNameRect.width *= 0.333f;
			lastNameRect.x += lastNameRect.width * 2 + 4;
			string first = customPawn.FirstName;
			string nick = customPawn.NickName;
			string last = customPawn.LastName;
			GUI.SetNextControlName("PrepareCarefullyFirst");
			CharacterCardUtility.DoNameInputRect(firstNameRect, ref first, 12);
			if (nick == first || nick == last) {
				GUI.color = new Color(1, 1, 1, 0.5f);
			}
			GUI.SetNextControlName("PrepareCarefullyNick");
			CharacterCardUtility.DoNameInputRect(nickNameRect, ref nick, 9);
			GUI.color = Color.white;
			GUI.SetNextControlName("PrepareCarefullyLast");
			CharacterCardUtility.DoNameInputRect(lastNameRect, ref last, 12);
			TooltipHandler.TipRegion(firstNameRect, "FirstNameDesc".Translate());
			TooltipHandler.TipRegion(nickNameRect, "ShortIdentifierDesc".Translate());
			TooltipHandler.TipRegion(lastNameRect, "LastNameDesc".Translate());
			customPawn.FirstName = first;
			customPawn.NickName = nick;
			customPawn.LastName = last;

			GUI.EndGroup();

			// Random button
			Rect randomRect = new Rect(SectionRectNameAndDescripton.x + SectionRectNameAndDescripton.width - 31, SectionRectNameAndDescripton.y + 21, 22, 22);
			if (randomRect.Contains(Event.current.mousePosition)) {
				GUI.color = ButtonHighlightColor;
			}
			else {
				GUI.color = ButtonColor;
			}
			GUI.DrawTexture(randomRect, Textures.TextureButtonRandom);
			if (Widgets.ButtonInvisible(randomRect, false)) {
				SoundDefOf.TickLow.PlayOneShotOnCamera();
				randomizer.RandomizeName(customPawn);
			}
		}

		protected void DrawColonistSaveButtons()
		{
			CustomPawn customPawn = CurrentPawn;
			GUI.color = SectionBackgroundColor;
			GUI.DrawTexture(SectionColonistSave, BaseContent.WhiteTex);

			float groupWidth = SectionColonistSave.width - SectionPadding.x * 2;
			GUI.BeginGroup(new Rect(SectionColonistSave.x + SectionPadding.x, SectionColonistSave.y + 10, groupWidth, 54));

			// Load/Save Colonist
			GUI.color = Color.white;
			float middle = groupWidth / 2;
			float buttonWidth = 136;
			float buttonHeight = 38;
			float buttonSpacing = 16;
			float buttonTop = 9;
			Text.Font = GameFont.Small;
			if (Widgets.ButtonText(new Rect(middle - buttonWidth - buttonSpacing / 2, buttonTop, buttonWidth, buttonHeight), "EdB.LoadColonistButton".Translate(), true, false, true)) {
				if (PrepareCarefully.Instance.Pawns.Count < Config.maxColonists) {
					Hide();
					Find.WindowStack.Add(new Dialog_LoadColonist());
				}
				else {
					Messages.Message("EdB.TooManyColonists".Translate(new object[] {
						Config.maxColonists
					}), MessageSound.RejectInput);
				}
			}
			if (Widgets.ButtonText(new Rect(middle + buttonSpacing / 2, buttonTop, buttonWidth, buttonHeight), "EdB.SaveColonistButton".Translate(), true, false, true)) {
				Hide();
				Find.WindowStack.Add(new Dialog_SaveColonist(SelectedPawn));
			}

			GUI.EndGroup();
		}

		private static Color DisabledColor = new Color(1, 1, 1, 0.27647f);
		private static Color DisabledNextPreviousButtonColor = new Color(ButtonColor.r, ButtonColor.g, ButtonColor.b, 0.4f);
		protected void DrawDisabledFieldSelector(Rect fieldRect, string label, Color textColor)
		{
			GUI.color = DisabledColor;
			Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);

			Text.Anchor = TextAnchor.MiddleCenter;
			fieldRect.y += 2;
			GUI.color = new Color(textColor.r, textColor.g, textColor.b, DisabledColor.a);
			Widgets.Label(fieldRect, label);
			GUI.color = Color.white;

			Rect prevButtonRect = new Rect(fieldRect.x - Textures.TextureButtonPrevious.width - 3, fieldRect.y + 4, Textures.TextureButtonPrevious.width, Textures.TextureButtonPrevious.height);
			Rect nextButtonRect = new Rect(fieldRect.x + fieldRect.width + 1, fieldRect.y + 4, Textures.TextureButtonPrevious.width, Textures.TextureButtonPrevious.height);

			GUI.color = DisabledNextPreviousButtonColor;
			GUI.DrawTexture(prevButtonRect, Textures.TextureButtonPrevious);
			GUI.DrawTexture(nextButtonRect, Textures.TextureButtonNext);
			Text.Anchor = TextAnchor.UpperLeft;
		}

		protected void DrawFieldSelector(Rect fieldRect, string label, Action previousAction, Action nextAction)
		{
			DrawFieldSelector(fieldRect, label, previousAction, nextAction, TextColor);
		}

		protected void DrawFieldSelector(Rect fieldRect, string label, Action previousAction, Action nextAction, Color labelColor)
		{
			GUI.color = Color.white;
			Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);

			Text.Anchor = TextAnchor.MiddleCenter;
			fieldRect.y += 2;
			GUI.color = labelColor;
			if (fieldRect.Contains(Event.current.mousePosition)) {
				GUI.color = Color.white;
			}
			Widgets.Label(fieldRect, label);
			GUI.color = Color.white;

			Rect prevButtonRect = new Rect(fieldRect.x - Textures.TextureButtonPrevious.width - 3, fieldRect.y + 4, Textures.TextureButtonPrevious.width, Textures.TextureButtonPrevious.height);
			Rect nextButtonRect = new Rect(fieldRect.x + fieldRect.width + 1, fieldRect.y + 4, Textures.TextureButtonPrevious.width, Textures.TextureButtonPrevious.height);

			if (prevButtonRect.Contains(Event.current.mousePosition)) {
				GUI.color = ButtonHighlightColor;
			}
			else {
				GUI.color = ButtonColor;
			}
			GUI.DrawTexture(prevButtonRect, Textures.TextureButtonPrevious);
			if (previousAction != null && Widgets.ButtonInvisible(prevButtonRect, false)) {
				SoundDefOf.TickTiny.PlayOneShotOnCamera();
				previousAction();
			}

			if (nextButtonRect.Contains(Event.current.mousePosition)) {
				GUI.color = ButtonHighlightColor;
			}
			else {
				GUI.color = ButtonColor;
			}
			GUI.DrawTexture(nextButtonRect, Textures.TextureButtonNext);
			if (nextAction != null && Widgets.ButtonInvisible(nextButtonRect, false)) {
				SoundDefOf.TickTiny.PlayOneShotOnCamera();
				nextAction();
			}
			Text.Anchor = TextAnchor.UpperLeft;
		}

		protected void DrawAppearance(CustomPawn customPawn)
		{
			string label = PawnLayers.Label(this.selectedPawnLayer);
			if (Widgets.ButtonText(new Rect(28, 23, 192, 28), label, true, false, true)) {
				List<FloatMenuOption> list = new List<FloatMenuOption>();
				int layerCount = this.pawnLayerActions.Count;
				for (int i = 0; i < layerCount; i++) {
					int pawnLayer = pawnLayers[i];
					// Only add apparel layers that have items.
					if (PawnLayers.IsApparelLayer(pawnLayer)) {
						if (pawnLayer == PawnLayers.Accessory) {
							if (apparelLists[pawnLayer] == null || apparelLists[pawnLayer].Count == 0) {
								continue;
							}
						}
					}
					label = PawnLayers.Label(pawnLayers[i]);
					list.Add(new FloatMenuOption(label, this.pawnLayerActions[i], MenuOptionPriority.Default, null, null, 0, null, null));
				}
				Find.WindowStack.Add(new FloatMenu(list, null, false));
			}

			Rect portraitRect = new Rect(28, 60, 192, 192);
			GUI.DrawTexture(portraitRect, Textures.TexturePortraitBackground);

			DrawPawn(portraitRect);

			GUI.color = PortraitBorderColor;
			Widgets.DrawBox(portraitRect, 1);
			GUI.color = Color.white;

			// Conflict alert
			if (customPawn.ApparelConflict != null) {
				GUI.color = Color.white;
				Rect alertRect = new Rect(portraitRect.x + 77, portraitRect.y + 150, 36, 32);
				GUI.DrawTexture(alertRect, Textures.TextureAlert);
				TooltipHandler.TipRegion(alertRect, customPawn.ApparelConflict);
			}

			// Draw apparel selector
			Rect fieldRect = new Rect(portraitRect.x, portraitRect.y + portraitRect.height + 5, portraitRect.width, 28);
			DrawFieldSelector(fieldRect, PawnLayerLabel.CapitalizeFirst(),
				() => {
					if (this.selectedPawnLayer == PawnLayers.HeadType) {
						SoundDefOf.TickTiny.PlayOneShotOnCamera();
						SelectNextHead(-1);
					}
					else if (this.selectedPawnLayer == PawnLayers.BodyType) {
						SoundDefOf.TickTiny.PlayOneShotOnCamera();
						SelectNextBodyType(-1);
					}
					else if (this.selectedPawnLayer == PawnLayers.Hair) {
						SoundDefOf.TickTiny.PlayOneShotOnCamera();
						SelectNextHair(-1);
					}
					else {
						SoundDefOf.TickTiny.PlayOneShotOnCamera();
						SelectNextApparel(-1);
					}
				},
				() => {
					if (this.selectedPawnLayer == PawnLayers.HeadType) {
						SoundDefOf.TickTiny.PlayOneShotOnCamera();
						SelectNextHead(1);
					}
					else if (this.selectedPawnLayer == PawnLayers.BodyType) {
						SoundDefOf.TickTiny.PlayOneShotOnCamera();
						SelectNextBodyType(1);
					}
					else if (this.selectedPawnLayer == PawnLayers.Hair) {
						SoundDefOf.TickTiny.PlayOneShotOnCamera();
						SelectNextHair(1);
					}
					else {
						SelectNextApparel(1);
						SoundDefOf.TickTiny.PlayOneShotOnCamera();
					}
				}
			);

			if (Widgets.ButtonInvisible(fieldRect, false)) {
				if (this.selectedPawnLayer == PawnLayers.HeadType) {
					ShowHeadDialog();
				}
				else if (this.selectedPawnLayer == PawnLayers.BodyType) {
					ShowBodyTypeDialog();
				}
				else if (this.selectedPawnLayer == PawnLayers.Hair) {
					ShowHairDialog();
				}
				else {
					ShowApparelDialog(this.selectedPawnLayer);
				}
			}

			float cursorY = fieldRect.y + 34;

			// Draw stuff selector for apparel
			if (PawnLayers.IsApparelLayer(this.selectedPawnLayer)) {
				ThingDef apparelDef = customPawn.GetSelectedApparel(selectedPawnLayer);
				if (apparelDef != null && apparelDef.MadeFromStuff) {
					if (customPawn.GetSelectedStuff(selectedPawnLayer) == null) {
						Log.Error("Selected stuff for " + PawnLayers.ToApparelLayer(selectedPawnLayer) + " is null");
					}
					Rect stuffFieldRect = new Rect(portraitRect.x, cursorY, portraitRect.width, 28);
					DrawFieldSelector(stuffFieldRect, customPawn.GetSelectedStuff(selectedPawnLayer).LabelCap,
						() => {
							ThingDef selected = customPawn.GetSelectedStuff(selectedPawnLayer);
							int index = this.apparelStuffLookup[apparelDef].FindIndex((ThingDef d) => { return selected == d; });
							index--;
							if (index < 0) {
								index = this.apparelStuffLookup[apparelDef].Count - 1;
							}
							customPawn.SetSelectedStuff(selectedPawnLayer, apparelStuffLookup[apparelDef][index]);
						},
						() => {
							ThingDef selected = customPawn.GetSelectedStuff(selectedPawnLayer);
							int index = this.apparelStuffLookup[apparelDef].FindIndex((ThingDef d) => { return selected == d; });
							index++;
							if (index >= this.apparelStuffLookup[apparelDef].Count) {
								index = 0;
							}
							customPawn.SetSelectedStuff(selectedPawnLayer, this.apparelStuffLookup[apparelDef][index]);
						}
					);

					if (Widgets.ButtonInvisible(stuffFieldRect, false)) {
						ShowApparelStuffDialog(this.selectedPawnLayer);
					}

					cursorY += stuffFieldRect.height;
				}
			}
			cursorY += 8;

			// Draw Color Selector
			if (PawnLayers.IsApparelLayer(selectedPawnLayer)) {
				ThingDef def = customPawn.GetSelectedApparel(selectedPawnLayer);
				if (def != null) {
					if (def.MadeFromStuff) {
						DrawColorSelector(cursorY, null);
					}
					else {
						DrawColorSelector(cursorY, def.colorGenerator);
					}
				}
			}
			else if (selectedPawnLayer == PawnLayers.BodyType || selectedPawnLayer == PawnLayers.HeadType) {
				DrawSkinSelector(cursorY);
			}
			else if (selectedPawnLayer == PawnLayers.Hair) {
				DrawColorSelector(cursorY, hairColors, true);
			}

			// Random button
			Rect randomRect = new Rect(SectionRectPortrait.x + 186, portraitRect.y + 10, 22, 22);
			if (randomRect.Contains(Event.current.mousePosition)) {
				GUI.color = ButtonHighlightColor;
			}
			else {
				GUI.color = ButtonColor;
			}
			GUI.DrawTexture(randomRect, Textures.TextureButtonRandom);
			if (Widgets.ButtonInvisible(randomRect, false)) {
				SoundDefOf.TickLow.PlayOneShotOnCamera();
				randomizer.RandomizeAppearance(customPawn);
			}
		}

		protected void DrawRelationships()
		{

		}

		protected void DrawPortraitTabs()
		{
			float cursor = 0;
			cursor = DrawPortraitTab(cursor, LeftPanelMode.Appearance, "EdB.Appearance");
			cursor = DrawPortraitTab(cursor, LeftPanelMode.Relations, "Relations");
			cursor = DrawPortraitTab(cursor, LeftPanelMode.Health, "Health");
		}

		protected float DrawPortraitTab(float cursor, LeftPanelMode mode, string textKey)
		{
			String text = textKey.Translate();
			float textWidth = Text.CalcSize(text).x;
			float tabWidth = textWidth + (PortraitTabPadding * 2);
			if (leftPanelMode == mode) {
				GUI.color = PortraitTabActiveColor;
			}
			else {
				GUI.color = PortraitTabInactiveColor;
			}
			Rect tabRect = new Rect(cursor, 0, tabWidth, PortraitTabsHeight);
			GUI.DrawTexture(tabRect, BaseContent.WhiteTex);
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;
			GUI.color = TextColor;
			Widgets.Label(tabRect, text);
			GUI.color = Color.white;

			if (Widgets.ButtonInvisible(tabRect, false) && leftPanelMode != mode) {
				SoundDefOf.TickTiny.PlayOneShotOnCamera();
				leftPanelMode = mode;
			}

			return cursor + tabWidth + PortraitTabMargin;
		}

		protected void DrawPortrait()
		{
			CustomPawn customPawn = CurrentPawn;
			GUI.color = SectionBackgroundColor;
			GUI.DrawTexture(SectionRectPortraitBody, BaseContent.WhiteTex);

			GUI.BeginGroup(SectionRectPortrait);
			DrawPortraitTabs();

			GUI.BeginGroup(SectionRectPortraitContent);

			GUI.color = Color.white;
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;

			if (leftPanelMode == LeftPanelMode.Appearance) {
				DrawAppearance(customPawn);
			}
			else if (leftPanelMode == LeftPanelMode.Relations) {
				PanelRelations.Draw();
			}
			else if (leftPanelMode == LeftPanelMode.Health) {
				PanelHealth.Draw();
			}


			GUI.EndGroup();
			GUI.EndGroup();

			GUI.color = Color.white;
		}

		protected List<Color> colorSelectorColors = new List<Color>();
		protected void DrawColorSelector(float cursorY, ColorGenerator generator)
		{
			colorSelectorColors.Clear();
			if (generator != null) {
				Type generatorType = generator.GetType();
				if (typeof(ColorGenerator_Options).Equals(generatorType)) {
					ColorGenerator_Options gen = generator as ColorGenerator_Options;
					foreach (ColorOption option in gen.options) {
						if (option.only != ColorValidator.ColorEmpty) {
							colorSelectorColors.Add(option.only);
						}
					}
				}
				else if (typeof(ColorGenerator_White).Equals(generatorType)) {
					colorSelectorColors.Add(Color.white);
				}
				else if (typeof(ColorGenerator_Single).Equals(generatorType)) {
					ColorGenerator_Single gen = generator as ColorGenerator_Single;
					colorSelectorColors.Add(gen.color);
				}
			}
			DrawColorSelector(cursorY, colorSelectorColors, true);
		}

		protected static Vector2 SwatchSize = new Vector2(16, 16);
		protected static Vector2 SwatchPosition = new Vector2(29, 320);
		protected static Vector2 SwatchSpacing = new Vector2(22, 22);
		protected static Color ColorSwatchBorder = new Color(0.77255f, 0.77255f, 0.77255f);
		protected static Color ColorSwatchSelection = new Color(0.9098f, 0.9098f, 0.9098f);
		protected void DrawColorSelector(float cursorY, List<Color> colors, bool allowAnyColor)
		{
			CustomPawn customPawn = CurrentPawn;
			Color currentColor = customPawn.GetColor(selectedPawnLayer);
			Rect rect = new Rect(SwatchPosition.x, cursorY, SwatchSize.x, SwatchSize.y);
			foreach (Color color in colors) {
				bool selected = (color == currentColor);
				if (selected) {
					Rect selectionRect = new Rect(rect.x - 2, rect.y - 2, SwatchSize.x + 4, SwatchSize.y + 4);
					GUI.color = ColorSwatchSelection;
					GUI.DrawTexture(selectionRect, BaseContent.WhiteTex);
				}

				Rect borderRect = new Rect(rect.x - 1, rect.y - 1, SwatchSize.x + 2, SwatchSize.y + 2);
				GUI.color = ColorSwatchBorder;
				GUI.DrawTexture(borderRect, BaseContent.WhiteTex);

				GUI.color = color;
				GUI.DrawTexture(rect, BaseContent.WhiteTex);

				if (!selected) {
					if (Widgets.ButtonInvisible(rect, false)) {
						SetColor(color);
						currentColor = color;
					}
				}

				rect.x += SwatchSpacing.x;
				if (rect.x >= 227) {
					rect.y += SwatchSpacing.y;
					rect.x = SwatchPosition.x;
				}
			}

			GUI.color = Color.white;
			if (!allowAnyColor) {
				return;
			}

			if (rect.x != SwatchPosition.x) {
				rect.x = SwatchPosition.x;
				rect.y += SwatchSpacing.y;
			}
			rect.y += 4;
			rect.width = 49;
			rect.height = 49;
			GUI.color = ColorSwatchBorder;
			GUI.DrawTexture(rect, BaseContent.WhiteTex);
			GUI.color = currentColor;
			GUI.DrawTexture(rect.ContractedBy(1), BaseContent.WhiteTex);

			GUI.color = Color.red;
			float r = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y - 1, 136, 16), currentColor.r, 0, 1);
			GUI.color = Color.green;
			float g = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y + 19, 136, 16), currentColor.g, 0, 1);
			GUI.color = Color.blue;
			float b = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y + 39, 136, 16), currentColor.b, 0, 1);
			SetColor(new Color(r, g, b));
			GUI.color = Color.white;
		}

		protected void SetColor(Color color)
		{
			CurrentPawn.SetColor(selectedPawnLayer, color);
		}

		protected void DrawSkinSelector(float cursorY)
		{
			CustomPawn customPawn = CurrentPawn;

			int currentSwatchIndex = PawnColorUtils.GetLeftIndexForValue(customPawn.MelaninLevel);
			Color currentSwatchColor = PawnColorUtils.Colors[currentSwatchIndex];

			Rect swatchRect = new Rect(SwatchPosition.x, cursorY, SwatchSize.x, SwatchSize.y);

			// Draw the swatch selection boxes.
			int colorCount = PawnColorUtils.Colors.Length - 1;
			int clickedIndex = -1;
			for (int i = 0; i < colorCount; i++) {
				Color color = PawnColorUtils.Colors[i];

				// If the swatch is selected, draw a heavier border around it.
				bool isThisSwatchSelected = (i == currentSwatchIndex);
				if (isThisSwatchSelected) {
					Rect selectionRect = new Rect(swatchRect.x - 2, swatchRect.y - 2, SwatchSize.x + 4, SwatchSize.y + 4);
					GUI.color = ColorSwatchSelection;
					GUI.DrawTexture(selectionRect, BaseContent.WhiteTex);
				}

				// Draw the border around the swatch.
				Rect borderRect = new Rect(swatchRect.x - 1, swatchRect.y - 1, SwatchSize.x + 2, SwatchSize.y + 2);
				GUI.color = ColorSwatchBorder;
				GUI.DrawTexture(borderRect, BaseContent.WhiteTex);

				// Draw the swatch itself.
				GUI.color = color;
				GUI.DrawTexture(swatchRect, BaseContent.WhiteTex);

				if (!isThisSwatchSelected) {
					if (Widgets.ButtonInvisible(swatchRect, false)) {
						clickedIndex = i;
						//currentSwatchColor = color;
					}
				}

				// Advance the swatch rect cursor position and wrap it if necessary.
				swatchRect.x += SwatchSpacing.x;
				if (swatchRect.x >= 227) {
					swatchRect.y += SwatchSpacing.y;
					swatchRect.x = SwatchPosition.x;
				}
			}

			// Draw the current color box.
			GUI.color = Color.white;
			Rect currentColorRect = new Rect(SwatchPosition.x, swatchRect.y + 4, 49, 49);
			if (swatchRect.x != SwatchPosition.x) {
				currentColorRect.y += SwatchSpacing.y;
			}
			GUI.color = ColorSwatchBorder;
			GUI.DrawTexture(currentColorRect, BaseContent.WhiteTex);
			GUI.color = customPawn.SkinColor;
			GUI.DrawTexture(currentColorRect.ContractedBy(1), BaseContent.WhiteTex);
			GUI.color = Color.white;

			// Figure out the lerp value so that we can draw the slider.
			float minValue = 0.00f;
			float maxValue = 0.99f;
			float t = PawnColorUtils.GetRelativeLerpValue(customPawn.MelaninLevel);
			if (t < minValue) {
				t = minValue;
			}
			else if (t > maxValue) {
				t = maxValue;
			}
			if (clickedIndex != -1) {
				t = minValue;
			}

			// Draw the slider.
			float newValue = GUI.HorizontalSlider(new Rect(currentColorRect.x + 56, currentColorRect.y + 18, 136, 16), t, minValue, 1);
			if (newValue < minValue) {
				newValue = minValue;
			}
			else if (newValue > maxValue) {
				newValue = maxValue;
			}
			GUI.color = Color.white;

			// If the user selected a new swatch or changed the lerp value, set a new color value.
			if (t != newValue || clickedIndex != -1) {
				if (clickedIndex != -1) {
					currentSwatchIndex = clickedIndex;
				}
				customPawn.MelaninLevel = PawnColorUtils.GetValueFromRelativeLerp(currentSwatchIndex, newValue);
			}
		}

		protected void DrawPawn(Rect rect)
		{
			CustomPawn customPawn = CurrentPawn;
			GUI.BeginGroup(rect);

			Vector2 pawnSize = new Vector2(128f, 180f);
			Rect pawnRect = new Rect(rect.width * 0.5f - pawnSize.x * 0.5f, 10 + rect.height * 0.5f - pawnSize.y * 0.5f, pawnSize.x, pawnSize.y);
			RenderTexture texture = PortraitsCache.Get(customPawn.Pawn, pawnSize, new Vector3(0, 0, 0), 1.0f);
			GUI.DrawTexture(pawnRect, (Texture)texture);

			GUI.EndGroup();
			GUI.color = Color.white;
		}

		protected static Color BodyPartReplacedFieldColor = new Color(0.451f, 0.451f, 0.8118f);
		protected static Color BodyPartBoxBorderColor = new Color(0.2471f, 0.2471f, 0.2471f);
		protected static Rect BodyPartBoxRect = new Rect(16, 40, 218, 358);
		protected static Rect BodyPartBoxScrollRect = new Rect(0, 0, BodyPartBoxRect.width - 2, BodyPartBoxRect.height - 2);
		protected static Rect OldInjuriesFieldRect = new Rect(18, BodyPartBoxRect.y + BodyPartBoxRect.height + 12, 216, 32);

		protected static Rect RectGenderButton = new Rect(0, 0, 100, 28);
		protected static Rect RectAgeLabel = new Rect(108, 2, 45, 28);
		protected static Rect RectBiologicalAgeField = new Rect(RectAgeLabel.x + RectAgeLabel.width + 18, 0, 32, 28);
		protected static Rect RectChronologicalAgeField = new Rect(RectBiologicalAgeField.x + RectBiologicalAgeField.width + 33, 0, 48, 28);
		protected void DrawGenderAndAge()
		{
			CustomPawn customPawn = CurrentPawn;
			chronologicalAgeDragSlider.minValue = customPawn.BiologicalAge;
			biologicalAgeDragSlider.maxValue = customPawn.ChronologicalAge < maxAge ? customPawn.ChronologicalAge : maxAge;

			GUI.color = SectionBackgroundColor;
			GUI.DrawTexture(SectionRectGenderAndAge, BaseContent.WhiteTex);
			GUI.BeginGroup(new Rect(SectionRectGenderAndAge.x + SectionMargin.x - 6, SectionRectGenderAndAge.y + SectionMargin.y,
				SectionRectGenderAndAge.width - SectionMargin.x * 2 + 12, SectionRectGenderAndAge.height - SectionMargin.y * 2));
				
			GUI.color = Color.white;
			string label = customPawn.Gender == Gender.Male ? "Male".Translate() : "Female".Translate();
			if (Widgets.ButtonText(RectGenderButton, label.CapitalizeFirst(), true, false, true)) {
				List<FloatMenuOption> list = new List<FloatMenuOption>();
				list.Add(new FloatMenuOption("Male".Translate().CapitalizeFirst(), delegate { this.SetGender(Gender.Male); }, MenuOptionPriority.Default, null, null, 0, null, null));
				list.Add(new FloatMenuOption("Female".Translate().CapitalizeFirst(), delegate { this.SetGender(Gender.Female); }, MenuOptionPriority.Default, null, null, 0, null, null));
				Find.WindowStack.Add(new FloatMenu(list, null, false));
			}

			GUI.color = ColorText;
			Text.Anchor = TextAnchor.MiddleRight;
			Widgets.Label(RectAgeLabel, "EdB.Age".Translate());

			// Biological Age
			GUI.color = Color.white;
			Rect fieldRect = RectBiologicalAgeField;
			Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);

			biologicalAgeDragSlider.OnGUI(fieldRect, customPawn.BiologicalAge, (int value) => {
				customPawn.BiologicalAge = value;
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
				SoundDefOf.TickTiny.PlayOneShotOnCamera();
				int amount = Event.current.shift ? 10 : 1;
				int age = customPawn.BiologicalAge - amount;
				if (age < minAge) {
					age = minAge;
				}
				else if (age > maxAge || age > customPawn.ChronologicalAge) {
					if (age > maxAge) {
						age = maxAge;
					}
					else {
						age = customPawn.ChronologicalAge;
					}
				}
				customPawn.BiologicalAge = age;
			}

			buttonRect = new Rect(fieldRect.x + fieldRect.width + 1, fieldRect.y + 6, 16, 16);
			if (!dragging &&buttonRect.Contains(Event.current.mousePosition)) {
				GUI.color = ButtonHighlightColor;
			}
			else {
				GUI.color = ButtonColor;
			}
			GUI.DrawTexture(buttonRect, Textures.TextureButtonNext);
			if (Widgets.ButtonInvisible(buttonRect, false)) {
				SoundDefOf.TickTiny.PlayOneShotOnCamera();
				int amount = Event.current.shift ? 10 : 1;
				int age = customPawn.BiologicalAge + amount;
				if (age < minAge) {
					age = minAge;
				}
				else if (age > maxAge || age > customPawn.ChronologicalAge) {
					if (age > maxAge) {
						age = maxAge;
					}
					else {
						age = customPawn.ChronologicalAge;
					}
				}
				customPawn.BiologicalAge = age;
			}

			// Chronological Age
			GUI.color = Color.white;
			fieldRect = RectChronologicalAgeField;
			Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);

			chronologicalAgeDragSlider.OnGUI(fieldRect, customPawn.ChronologicalAge, (int value) => {
				customPawn.ChronologicalAge = value;
			});
			dragging = DragSlider.IsDragging();

			buttonRect = new Rect(fieldRect.x - 17, fieldRect.y + 6, 16, 16);
			if (!dragging && buttonRect.Contains(Event.current.mousePosition)) {
				GUI.color = ButtonHighlightColor;
			}
			else {
				GUI.color = ButtonColor;
			}
			GUI.DrawTexture(buttonRect, Textures.TextureButtonPrevious);
			if (Widgets.ButtonInvisible(buttonRect, false)) {
				SoundDefOf.TickTiny.PlayOneShotOnCamera();
				int amount = Event.current.shift ? 10 : 1;
				int age = customPawn.ChronologicalAge - amount;
				if (age < customPawn.BiologicalAge) {
					age = customPawn.BiologicalAge;
				}
				if (age > maxChronologicalAge) {
					age = maxChronologicalAge;
				}
				customPawn.ChronologicalAge = age;
			}

			buttonRect = new Rect(fieldRect.x + fieldRect.width + 1, fieldRect.y + 6, 16, 16);
			if (!dragging &&buttonRect.Contains(Event.current.mousePosition)) {
				GUI.color = ButtonHighlightColor;
			}
			else {
				GUI.color = ButtonColor;
			}
			GUI.DrawTexture(buttonRect, Textures.TextureButtonNext);
			if (Widgets.ButtonInvisible(buttonRect, false)) {
				SoundDefOf.TickTiny.PlayOneShotOnCamera();
				int amount = Event.current.shift ? 10 : 1;
				int age = customPawn.ChronologicalAge + amount;
				if (age < customPawn.BiologicalAge) {
					age = customPawn.BiologicalAge;
				}
				if (age > maxChronologicalAge) {
					age = maxChronologicalAge;
				}
				customPawn.ChronologicalAge = age;
			}

			GUI.EndGroup();
		}
			
		protected void SetGender(Gender gender)
		{
			CustomPawn customPawn = CurrentPawn;
			customPawn.Gender = gender;
		}

		protected void DrawBackstory()
		{
			CustomPawn customPawn = CurrentPawn;
			GUI.color = SectionBackgroundColor;
			GUI.DrawTexture(SectionRectBackstory, BaseContent.WhiteTex);

			GUI.BeginGroup(new Rect(SectionRectBackstory.x + SectionMargin.x, SectionRectBackstory.y + SectionMargin.y,
				SectionRectBackstory.width - SectionMargin.x * 2 + 6, SectionRectBackstory.height - SectionMargin.y * 2));

			GUI.color = TextColor;
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.Label(new Rect(0, 0, 300, 40), "Backstory".Translate());

			Text.Font = GameFont.Small;
			Widgets.Label(new Rect(0, 36, 300, 32), "Childhood".Translate());

			GUI.color = Color.white;
			Rect fieldRect = new Rect(90, 32, 188, 28);
			Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);

			Text.Anchor = TextAnchor.MiddleCenter;
			fieldRect.y += 2;
			fieldRect.x -= 2;
			if (!fieldRect.Contains(Event.current.mousePosition)) {
				GUI.color = TextColor;
			}
			if (customPawn.Childhood != null) {
				Widgets.Label(fieldRect, customPawn.Childhood.Title);
			}
			GUI.color = Color.white;
			fieldRect.y -= 2;
			fieldRect.x += 2;

			if (problemBackstories.Contains(customPawn.Childhood)) {
				fieldRect.width -= 30;
				TooltipHandler.TipRegion(fieldRect, customPawn.Childhood.FullDescriptionFor(customPawn.Pawn));
				if (Widgets.ButtonInvisible(fieldRect, false)) {
					ShowBackstoryDialog(customPawn, BackstorySlot.Childhood);
				}
				fieldRect.width += 30;
				Rect problemRect = new Rect(fieldRect.x + fieldRect.width - 26, fieldRect.y + 4, 20, 20);
				GUI.DrawTexture(problemRect, Textures.TextureAlertSmall);
				TooltipHandler.TipRegion(problemRect, "EdB.BackstoryWarning".Translate());
			}
			else {
				TooltipHandler.TipRegion(fieldRect, customPawn.Childhood.FullDescriptionFor(customPawn.Pawn));
				if (Widgets.ButtonInvisible(fieldRect, false)) {
					ShowBackstoryDialog(customPawn, BackstorySlot.Childhood);
				}
			}

			Rect buttonRect = new Rect(fieldRect.x - 17, 38, 16, 16);
			if (buttonRect.Contains(Event.current.mousePosition)) {
				GUI.color = ButtonHighlightColor;
			}
			else {
				GUI.color = ButtonColor;
			}
			GUI.DrawTexture(buttonRect, Textures.TextureButtonPrevious);
			if (Widgets.ButtonInvisible(buttonRect, false)) {
				SoundDefOf.TickTiny.PlayOneShotOnCamera();
				this.SelectPreviousBackstory(0);
			}
				
			buttonRect = new Rect(fieldRect.x + fieldRect.width + 1, 38, 16, 16);
			if (buttonRect.Contains(Event.current.mousePosition)) {
				GUI.color = ButtonHighlightColor;
			}
			else {
				GUI.color = ButtonColor;
			}
			GUI.DrawTexture(buttonRect, Textures.TextureButtonNext);
			if (Widgets.ButtonInvisible(buttonRect, false)) {
				SoundDefOf.TickTiny.PlayOneShotOnCamera();
				this.SelectNextBackstory(0);
			}


			Backstory adulthood = customPawn.Adulthood;
			bool isAdult = adulthood != null;
			if (isAdult) {
				GUI.color = Color.white;
			}
			else {
				GUI.color = ButtonDisabledColor;
			}

			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.Label(new Rect(0, 70, 300, 32), "Adulthood".Translate());
			fieldRect.y += 35;
			if (isAdult) {
				GUI.color = Color.white;
			}
			else {
				GUI.color = DisabledColor;
			}
			Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);

			Text.Anchor = TextAnchor.MiddleCenter;
			fieldRect.y += 2;
			fieldRect.x -= 2;
			if (!fieldRect.Contains(Event.current.mousePosition)) {
				GUI.color = TextColor;
			}
			else {
				GUI.color = Color.white;
			}
			if (isAdult && customPawn.Adulthood != null) {
				Widgets.Label(fieldRect, customPawn.Adulthood.Title);
			}
			GUI.color = Color.white;
			fieldRect.y -= 2;
			fieldRect.x += 2;

			// Draw the problem backstory notice.
			if (isAdult) {
				if (problemBackstories.Contains(customPawn.Adulthood)) {
					fieldRect.width -= 30;
					TooltipHandler.TipRegion(new Rect(fieldRect.x, fieldRect.y, fieldRect.width - 30, fieldRect.height), customPawn.Adulthood.FullDescriptionFor(customPawn.Pawn));
					if (Widgets.ButtonInvisible(fieldRect, false)) {
						ShowBackstoryDialog(customPawn, BackstorySlot.Adulthood);
					}
					fieldRect.width += 30;

					Rect problemRect = new Rect(fieldRect.x + fieldRect.width - 26, fieldRect.y + 4, 20, 20);
					GUI.DrawTexture(problemRect, Textures.TextureAlertSmall);
					TooltipHandler.TipRegion(problemRect, "EdB.BackstoryWarning".Translate());
				}
				else {
					TooltipHandler.TipRegion(fieldRect, customPawn.Adulthood.FullDescriptionFor(customPawn.Pawn));
					if (Widgets.ButtonInvisible(fieldRect, false)) {
						ShowBackstoryDialog(customPawn, BackstorySlot.Adulthood);
					}
				}
			}

			// Draw the next/previous buttons.
			if (isAdult) {
				buttonRect = new Rect(fieldRect.x - 17, 72, 16, 16);
				if (buttonRect.Contains(Event.current.mousePosition)) {
					GUI.color = ButtonHighlightColor;
				}
				else {
					GUI.color = ButtonColor;
				}
				GUI.DrawTexture(buttonRect, Textures.TextureButtonPrevious);
				if (Widgets.ButtonInvisible(buttonRect, false)) {
					SoundDefOf.TickTiny.PlayOneShotOnCamera();
					this.SelectPreviousBackstory(1);
				}

				buttonRect = new Rect(fieldRect.x + fieldRect.width + 1, 72, 16, 16);
				if (buttonRect.Contains(Event.current.mousePosition)) {
					GUI.color = ButtonHighlightColor;
				}
				else {
					GUI.color = ButtonColor;
				}
				GUI.DrawTexture(buttonRect, Textures.TextureButtonNext);
				if (Widgets.ButtonInvisible(buttonRect, false)) {
					SoundDefOf.TickTiny.PlayOneShotOnCamera();
					this.SelectNextBackstory(1);
				}
			}
				
			GUI.EndGroup();

			// Random button
			Rect randomRect = new Rect(SectionRectBackstory.x + SectionRectBackstory.width - 32, SectionRectBackstory.y + 9, 22, 22);
			if (randomRect.Contains(Event.current.mousePosition)) {
				GUI.color = ButtonHighlightColor;
			}
			else {
				GUI.color = ButtonColor;
			}
			GUI.DrawTexture(randomRect, Textures.TextureButtonRandom);
			if (Widgets.ButtonInvisible(randomRect, false)) {
				SoundDefOf.TickLow.PlayOneShotOnCamera();
				randomizer.RandomizeBackstory(customPawn);
			}
		}

		protected void ShowHeadDialog()
		{
			CustomPawn customPawn = CurrentPawn;
			List<string> heads = customPawn.Gender == Gender.Male ? maleHeads : femaleHeads;
			Dialog_Options<string> dialog = new Dialog_Options<string>(heads) {
				NameFunc = (string head) => {
					return GetHeadLabel(head);
				},
				SelectedFunc = (string head) => {
					return customPawn.HeadGraphicPath == head;
				},
				SelectAction = (string head) => {
					customPawn.HeadGraphicPath = head;
					this.pawnLayerLabel = GetHeadLabel(customPawn.HeadGraphicPath);
				},
				CloseAction = () => { }
			};
			Find.WindowStack.Add(dialog);
		}

		protected void ShowBodyTypeDialog()
		{
			CustomPawn customPawn = CurrentPawn;
			List<BodyType> bodyTypes = customPawn.Gender == Gender.Male ? maleBodyTypes : femaleBodyTypes;
			Dialog_Options<BodyType> dialog = new Dialog_Options<BodyType>(bodyTypes) {
				NameFunc = (BodyType bodyType) => {
					return bodyTypeLabels[bodyType];
				},
				SelectedFunc = (BodyType bodyType) => {
					return customPawn.BodyType == bodyType;
				},
				SelectAction = (BodyType bodyType) => {
					customPawn.BodyType = bodyType;
					this.pawnLayerLabel = bodyTypeLabels[bodyType];
				},
				CloseAction = () => { }
			};
			Find.WindowStack.Add(dialog);
		}

		protected void ShowHairDialog()
		{
			CustomPawn customPawn = CurrentPawn;
			List<HairDef> hairDefs = customPawn.Gender == Gender.Male ? maleHairDefs : femaleHairDefs;
			Dialog_Options<HairDef> dialog = new Dialog_Options<HairDef>(hairDefs) {
				NameFunc = (HairDef hairDef) => {
					return hairDef.LabelCap;
				},
				SelectedFunc = (HairDef hairDef) => {
					return customPawn.HairDef == hairDef;
				},
				SelectAction = (HairDef hairDef) => {
					customPawn.HairDef = hairDef;
					this.pawnLayerLabel = hairDef.LabelCap;
				},
				CloseAction = () => { }
			};
			Find.WindowStack.Add(dialog);
		}

		protected void ShowApparelDialog(int layer)
		{
			CustomPawn customPawn = CurrentPawn;
			List<ThingDef> apparelList = this.apparelLists[layer];

			Dialog_Options<ThingDef> dialog = new Dialog_Options<ThingDef>(apparelList) {
				IncludeNone = true,
				NameFunc = (ThingDef apparel) => {
					return apparel.LabelCap;
				},
				SelectedFunc = (ThingDef apparel) => {
					return customPawn.GetSelectedApparel(layer) == apparel;
				},
				SelectAction = (ThingDef apparel) => {
					this.pawnLayerLabel = apparel.LabelCap;
					if (apparel.MadeFromStuff) {
						if (customPawn.GetSelectedStuff(layer) == null) {
							customPawn.SetSelectedStuff(layer, apparelStuffLookup[apparel][0]);
						}
					}
					else {
						customPawn.SetSelectedStuff(layer, null);
					}
					customPawn.SetSelectedApparel(layer, apparel);
				},
				NoneSelectedFunc = () => {
					return customPawn.GetSelectedApparel(layer) == null;
				},
				SelectNoneAction = () => {
					customPawn.SetSelectedApparel(layer, null);
					customPawn.SetSelectedStuff(layer, null);
					this.pawnLayerLabel = "EdB.None".Translate();
				}
			};
			Find.WindowStack.Add(dialog);
		}

		protected void ShowApparelStuffDialog(int layer)
		{
			CustomPawn customPawn = CurrentPawn;
			ThingDef apparel = customPawn.GetSelectedApparel(layer);
			if (apparel == null) {
				return;
			}
			List<ThingDef> stuffList = this.apparelStuffLookup[apparel];
			Dialog_Options<ThingDef> dialog = new Dialog_Options<ThingDef>(stuffList) {
				NameFunc = (ThingDef stuff) => {
					return stuff.LabelCap;
				},
				SelectedFunc = (ThingDef stuff) => {
					return customPawn.GetSelectedStuff(layer) == stuff;
				},
				SelectAction = (ThingDef stuff) => {
					customPawn.SetSelectedStuff(layer, stuff);
				}
			};
			Find.WindowStack.Add(dialog);
		}

		protected void ShowBackstoryDialog(CustomPawn customPawn, BackstorySlot slot)
		{
			Backstory originalBackstory = (slot == BackstorySlot.Childhood) ? customPawn.Childhood : customPawn.Adulthood;
			Backstory selectedBackstory = originalBackstory;
			Dialog_Options<Backstory> dialog = new Dialog_Options<Backstory>(slot == BackstorySlot.Childhood ? this.sortedChildhoodBackstories : this.sortedAdulthoodBackstories) {
				NameFunc = (Backstory backstory) => {
					return backstory.Title;
				},
				DescriptionFunc = (Backstory backstory) => {
					return backstory.FullDescriptionFor(customPawn.Pawn);
				},
				SelectedFunc = (Backstory backstory) => {
					return selectedBackstory == backstory;
				},
				SelectAction = (Backstory backstory) => {
					selectedBackstory = backstory;
				},
				CloseAction = () => {
					if (slot == BackstorySlot.Childhood) {
						customPawn.Childhood = selectedBackstory;
					}
					else {
						customPawn.Adulthood = selectedBackstory;
					}
				}
			};
			Find.WindowStack.Add(dialog);
		}

		protected void DrawTraits()
		{
			CustomPawn customPawn = CurrentPawn;
			GUI.color = SectionBackgroundColor;
			GUI.DrawTexture(SectionRectTraits, BaseContent.WhiteTex);

			GUI.BeginGroup(new Rect(SectionRectTraits.x + SectionMargin.x, SectionRectTraits.y + SectionMargin.y,
				SectionRectTraits.width - SectionMargin.x * 2, SectionRectTraits.height - SectionMargin.y * 2 + 100));

			GUI.color = TextColor;
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.Label(new Rect(0, 0, 300, 40), "Traits".Translate());
			Text.Font = GameFont.Small;

			Rect fieldRect = new Rect(13, 35, 258, 28);
			int traitIndex = 0;
			foreach (Trait trait in customPawn.Traits) {
				int localIndex = traitIndex;
				GUI.color = Color.white;
				Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);
				if (trait != null) {
					Text.Anchor = TextAnchor.MiddleCenter;
					fieldRect.y += 2;
					fieldRect.x -= 2;
					if (!fieldRect.Contains(Event.current.mousePosition)) {
						GUI.color = TextColor;
					}
					Widgets.Label(fieldRect, trait.LabelCap);
					GUI.color = Color.white;
					fieldRect.y -= 2;
					fieldRect.x += 2;
					TooltipHandler.TipRegion(fieldRect, trait.TipString(customPawn.Pawn));
				}
				if (Widgets.ButtonInvisible(fieldRect, false)) {
					
					Trait originalTrait = customPawn.GetTrait(traitIndex);
					Trait selectedTrait = originalTrait;
					Dialog_Options<Trait> dialog = new Dialog_Options<Trait>(this.sortedTraits) {
						IncludeNone = true,
						NameFunc = (Trait t) => {
							return t.LabelCap;
						},
						DescriptionFunc = (Trait t) => {
							return t.TipString(customPawn.Pawn);
						},
						SelectedFunc = (Trait t) => {
							return selectedTrait == t;
						},
						SelectAction = (Trait t) => {
							selectedTrait = t;
						},
						EnabledFunc = (Trait t) => {
							if (t == null) {
								return originalTrait != null;
							}
							else if ((originalTrait == null || !originalTrait.Label.Equals(t.Label)) && customPawn.HasTrait(t)) {
								return false;
							}
							else {
								return true;
							}
						},
						CloseAction = () => {
							customPawn.SetTrait(localIndex, selectedTrait);
						},
						NoneSelectedFunc = () => {
							return selectedTrait == null;
						},
						SelectNoneAction = () => {
							selectedTrait = null;
						}
					};
					Find.WindowStack.Add(dialog);
				}

				Rect buttonRect = new Rect(fieldRect.x - 17, fieldRect.y + 6, 16, 16);
				if (buttonRect.Contains(Event.current.mousePosition)) {
					GUI.color = ButtonHighlightColor;
				}
				else {
					GUI.color = ButtonColor;
				}
				GUI.DrawTexture(buttonRect, Textures.TextureButtonPrevious);
				if (Widgets.ButtonInvisible(buttonRect, false)) {
					SoundDefOf.TickTiny.PlayOneShotOnCamera();
					this.SelectPreviousTrait(traitIndex);
				}

				buttonRect = new Rect(fieldRect.x + fieldRect.width + 1, fieldRect.y + 6, 16, 16);
				if (buttonRect.Contains(Event.current.mousePosition)) {
					GUI.color = ButtonHighlightColor;
				}
				else {
					GUI.color = ButtonColor;
				}
				GUI.DrawTexture(buttonRect, Textures.TextureButtonNext);
				if (Widgets.ButtonInvisible(buttonRect, false)) {
					SoundDefOf.TickTiny.PlayOneShotOnCamera();
					this.SelectNextTrait(traitIndex);
				}

				traitIndex++;
				fieldRect.y += 34;
			}

			GUI.EndGroup();

			// Random button
			Rect randomRect = new Rect(SectionRectTraits.x + SectionRectTraits.width - 32, SectionRectTraits.y + 9, 22, 22);
			if (randomRect.Contains(Event.current.mousePosition)) {
				GUI.color = ButtonHighlightColor;
			}
			else {
				GUI.color = ButtonColor;
			}
			GUI.DrawTexture(randomRect, Textures.TextureButtonRandom);
			if (Widgets.ButtonInvisible(randomRect, false)) {
				SoundDefOf.TickLow.PlayOneShotOnCamera();
				randomizer.RandomizeTraits(customPawn);
			}
		}

		protected void DrawIncapable()
		{
			CustomPawn customPawn = CurrentPawn;
			GUI.color = SectionBackgroundColor;
			GUI.DrawTexture(SectionRectIncapable, BaseContent.WhiteTex);

			GUI.BeginGroup(new Rect(SectionRectIncapable.x + SectionMargin.x, SectionRectIncapable.y + SectionMargin.y,
				SectionRectIncapable.width - SectionMargin.x * 2, SectionRectIncapable.height - SectionMargin.y * 2));

			GUI.color = TextColor;
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.Label(new Rect(0, 0, 300, 40), "IncapableOf".Translate());

			string incapable = customPawn.IncapableOf;
			if (incapable == null) {
				incapable = "EdB.None".Translate();
			}
			Text.WordWrap = true;
			Text.Font = GameFont.Small;
			Widgets.Label(new Rect(0.0f, 32, 300, 999f), incapable);

			GUI.EndGroup();
		}

		protected static Rect RectButtonClearSkills = new Rect(242, 3, 20, 20);
		protected static Rect RectButtonResetSkills = new Rect(276, 2, 23, 21);
		protected void DrawSkills()
		{
			CustomPawn customPawn = CurrentPawn;
			GUI.color = SectionBackgroundColor;
			GUI.DrawTexture(SectionRectSkills, BaseContent.WhiteTex);

			Vector2 sectionMargin = new Vector2(SectionMargin.x - 8, SectionMargin.y);
			GUI.BeginGroup(new Rect(SectionRectSkills.x + sectionMargin.x, SectionRectSkills.y + sectionMargin.y,
				SectionRectSkills.width - sectionMargin.x * 2, SectionRectSkills.height - sectionMargin.y * 2));

			GUI.color = TextColor;
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.Label(new Rect(5, 0, 300, 40), "Skills".Translate());

			// Clear button
			if (RectButtonClearSkills.Contains(Event.current.mousePosition)) {
				GUI.color = ButtonHighlightColor;
			}
			else {
				GUI.color = ButtonColor;
			}
			GUI.DrawTexture(RectButtonClearSkills, Textures.TextureButtonClearSkills);
			if (Widgets.ButtonInvisible(RectButtonClearSkills, false)) {
				SoundDefOf.TickLow.PlayOneShotOnCamera();
				CurrentPawn.ClearSkills();
			}

			// Reset button
			if (RectButtonResetSkills.Contains(Event.current.mousePosition)) {
				GUI.color = ButtonHighlightColor;
			}
			else {
				GUI.color = ButtonColor;
			}
			GUI.DrawTexture(RectButtonResetSkills, Textures.TextureButtonReset);
			if (Widgets.ButtonInvisible(RectButtonResetSkills, false)) {
				SoundDefOf.TickLow.PlayOneShotOnCamera();
				CurrentPawn.RestoreSkillLevelsAndPassions();
			}

			int skillCount = customPawn.Pawn.skills.skills.Count;
			float spacing = 28;
			float height = spacing * skillCount;
			GUI.BeginGroup(new Rect(0f, 38f, 308f, 338));
			skillScrollView.Begin(new Rect(0,0,308f, 338));

			Text.Font = GameFont.Small;
			foreach (SkillDef current in DefDatabase<SkillDef>.AllDefs) {
				float x = Text.CalcSize(current.skillLabel).x;
				if (x > skillLevelLabelWidth) {
					skillLevelLabelWidth = x;
				}
			}
			Rect skillsRect = new Rect(0f, 0f, SkillWidth, height);
			if (skillScrollView.ScrollbarsVisible) {
				skillsRect.width -= 16;
			}
			GUI.BeginGroup(skillsRect);
			Vector2 offset = new Vector2(0, 0);
			int skillIndex = 0;
			foreach (SkillRecord current2 in customPawn.Pawn.skills.skills) {
				float y = (float)skillIndex * spacing + offset.y;
				DrawSkill(current2, new Vector2(offset.x, y), skillsRect.width);
				skillIndex++;
			}
			GUI.EndGroup();

			// Increase/Decrease buttons.
			Rect rect = new Rect(skillsRect.width, 4, 16, 16);
			for (int i = 0; i < skillCount; i++) {

				if (customPawn.IsSkillDisabled(customPawn.Pawn.skills.skills[i].def)) {
					rect.y += spacing;
					continue;
				}

				if (rect.Contains(Event.current.mousePosition)) {
					GUI.color = ButtonHighlightColor;
				}
				else {
					GUI.color = ButtonColor;
				}
				GUI.DrawTexture(rect, Textures.TextureButtonPrevious);
				if (Widgets.ButtonInvisible(rect, false)) {
					SoundDefOf.TickTiny.PlayOneShotOnCamera();
					DecreaseSkill(i);
				}

				rect.x += 16;
				if (rect.Contains(Event.current.mousePosition)) {
					GUI.color = ButtonHighlightColor;
				}
				else {
					GUI.color = ButtonColor;
				}
				GUI.DrawTexture(rect, Textures.TextureButtonNext);
				if (Widgets.ButtonInvisible(rect, false)) {
					SoundDefOf.TickTiny.PlayOneShotOnCamera();
					IncreaseSkill(i);
				}

				rect.y += spacing;
				rect.x = skillsRect.width;
			}

			// Draw passions.
			Rect position = new Rect(skillLevelLabelWidth + 10, 2, 24, 24);
			for (int i = 0; i < skillCount; i++) {
				SkillRecord skill = customPawn.Pawn.skills.skills[i];
				if (!customPawn.IsSkillDisabled(skill.def)) {
					Passion passion = customPawn.currentPassions[skill.def];
					if (passion > Passion.None) {
						Texture2D image = (passion != Passion.Major) ? Textures.TexturePassionMinor : Textures.TexturePassionMajor;
						GUI.color = Color.white;
						GUI.DrawTexture(position, image);
					}
					else {
						GUI.color = Color.white;
						GUI.DrawTexture(position, Textures.TexturePassionNone);
					}
					if (Widgets.ButtonInvisible(position, false)) {
						SoundDefOf.TickTiny.PlayOneShotOnCamera();
						if (Event.current.button != 1) {
							IncreasePassion(i);
						}
						else {
							DecreasePassion(i);
						}
					}
				}
				position.y += spacing;
			}

			float cursor = position.y;
			this.skillScrollView.End(cursor);
			GUI.EndGroup();

			GUI.EndGroup();
			GUI.color = Color.white;
		}
			
		private static Color ColorSkillDisabled = new Color(1f, 1f, 1f, 0.5f);

		public const float SkillWidth = 275;
		public const float SkillYSpacing = 3;
		public const float SkillHeight = 24;
		public const float SkillLevelNumberX = 140;
		public const float SkillLeftEdgeMargin = 6;

		private static float skillLevelLabelWidth = -1;

		public static void FillableBar(Rect screenRect, float fillPercent, Texture2D fillTex)
		{
			screenRect.width *= fillPercent;
			GUI.DrawTexture(screenRect, fillTex);
		}

		private void DrawSkill(SkillRecord skill, Vector2 topLeft, float skillWidth)
		{
			CustomPawn customPawn = CurrentPawn;
			Rect rect = new Rect(topLeft.x, topLeft.y, skillWidth, 24);
			GUI.BeginGroup(rect);
			Text.Anchor = TextAnchor.UpperLeft;
			Rect rect2 = new Rect(6, -1, skillLevelLabelWidth + 6, rect.height + 4);
			rect2.yMin += 3;
			GUI.color = ColorText;
			Widgets.Label(rect2, skill.def.skillLabel);
			Rect position = new Rect(rect2.xMax, 0, 24, 24);
			int level = customPawn.GetSkillLevel(skill.def);
			bool disabled = customPawn.IsSkillDisabled(skill.def);
			if (!disabled) {
				float barSize = (level > 0 ? (float)level : 0) / 20f;
				Rect screenRect = new Rect(position.xMax, 0, rect.width - position.xMax, rect.height);
				FillableBar(screenRect, barSize, Textures.TextureSkillBarFill);

				int baseLevel = customPawn.GetSkillModifier(skill.def);
				float baseBarSize = (baseLevel > 0 ? (float)baseLevel : 0) / 20f;
				screenRect = new Rect(position.xMax, 0, rect.width - position.xMax, rect.height);
				FillableBar(screenRect, baseBarSize, Textures.TextureSkillBarFill);

				GUI.color = new Color(0.25f, 0.25f, 0.25f);
				Widgets.DrawBox(screenRect, 1);
				GUI.color = ColorText;

				if (Widgets.ButtonInvisible(screenRect, false)) {
					Vector2 pos = Event.current.mousePosition;
					float x = pos.x - screenRect.x - 2;
					int value = (int) Math.Ceiling((x / screenRect.width) * 20);
					SoundDefOf.TickTiny.PlayOneShotOnCamera();
					SetSkillLevel(skill, value);
				}
			}
			Rect rect3 = new Rect(position.xMax + 4, 0, 999, rect.height);
			rect3.yMin += 3;
			string label;
			if (disabled) {
				GUI.color = ColorSkillDisabled;
				label = "-";
			}
			else {
				label = GenString.ToStringCached(level);
			}
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(rect3, label);
			GUI.color = Color.white;
			GUI.EndGroup();

			TooltipHandler.TipRegion(rect, new TipSignal(GetSkillDescription(skill), skill.def.GetHashCode() * 397945));
			//TooltipHandler.TipRegion(rect, new TipSignal("Unmodified: " + CurrentPawn.xGetUnmodifiedSkillLevel(skill.def)
			//		 + "\n" + "Modifiers: " + CurrentPawn.xGetSkillModifier(skill.def)                                         
			//		+ "\n" + GetSkillDescription(skill), skill.def.GetHashCode() * 397945));
		}

		// EdB: Copy of private static SkillUI.GetSkillDescription().
		private static string GetSkillDescription(SkillRecord sk)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (sk.TotallyDisabled) {
				stringBuilder.Append("DisabledLower".Translate().CapitalizeFirst());
			}
			else {
				stringBuilder.AppendLine(string.Concat(new object[] {
					"Level".Translate(),
					" ",
					sk.Level,
					": ",
					sk.LevelDescriptor
				}));
				stringBuilder.Append("Passion".Translate() + ": ");
				switch (sk.passion) {
					case Passion.None:
						stringBuilder.Append("PassionNone".Translate(new object[] {
							"0.3"
						}));
						break;
					case Passion.Minor:
						stringBuilder.Append("PassionMinor".Translate(new object[] {
							"1.0"
						}));
						break;
					case Passion.Major:
						stringBuilder.Append("PassionMajor".Translate(new object[] {
							"1.5"
						}));
						break;
				}
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
			stringBuilder.Append(sk.def.description);
			return stringBuilder.ToString();
		}

		protected void EnableBionicsMode() {
			bionicsMode = true;
		}

		protected void ChangePawnLayer(int layer)
		{
			bionicsMode = false;
			this.selectedPawnLayer = layer;
		}

		protected string PawnLayerLabel
		{
			get {
				CustomPawn customPawn = CurrentPawn;
				//if (pawnLayerLabelIndex == selectedPawnLayer && pawnLayerLabelModel == customPawn && pawnLayerLabel != null) {
				//	return pawnLayerLabel;
				//}

				string label = "EdB.None".Translate();
				if (selectedPawnLayer == PawnLayers.BodyType) {
					label = bodyTypeLabels[customPawn.BodyType];
				}
				else if (selectedPawnLayer == PawnLayers.HeadType) {
					label = GetHeadLabel(customPawn.HeadGraphicPath);
				}
				else if (selectedPawnLayer == PawnLayers.Hair) {
					if (customPawn.HairDef != null) {
						label = customPawn.HairDef.LabelCap;
					}
				}
				else {
					label = null;
					ThingDef def = customPawn.GetSelectedApparel(selectedPawnLayer);
					if (def != null) {
						int index = this.apparelLists[selectedPawnLayer].IndexOf(def);
						if (index > -1) {
							label = this.apparelLists[selectedPawnLayer][index].label;
						}
					}
					else {
						label = "EdB.None".Translate();
					}
				}
				pawnLayerLabelIndex = selectedPawnLayer;
				pawnLayerLabelModel = customPawn;
				pawnLayerLabel = label;
				return label;
			}
		}

		protected void SelectNextHead(int direction)
		{
			CustomPawn customPawn = CurrentPawn;
			List<string> heads = customPawn.Gender == Gender.Male ? maleHeads : femaleHeads;
			int index = heads.IndexOf(customPawn.HeadGraphicPath);
			if (index == -1) {
				Log.Warning("Could not find the current pawn's head in the cache of head graphics: " + customPawn.HeadGraphicPath);
				return;
			}
			index += direction;
			if (index < 0) {
				index = heads.Count - 1;
			}
			else if (index >= heads.Count) {
				index = 0;
			}
			customPawn.HeadGraphicPath = heads[index];
			this.pawnLayerLabel = GetHeadLabel(customPawn.HeadGraphicPath);
		}

		protected void SelectNextBodyType(int direction)
		{
			CustomPawn customPawn = CurrentPawn;
			List<BodyType> bodyTypes = customPawn.Gender == Gender.Male ? maleBodyTypes : femaleBodyTypes;
			int index = bodyTypes.IndexOf(customPawn.BodyType);
			if (index == -1) {
				Log.Warning("Could not find the current pawn's body type in list of available options: " + customPawn.BodyType);
				return;
			}
			index += direction;
			if (index < 0) {
				index = bodyTypes.Count - 1;
			}
			else if (index >= bodyTypes.Count) {
				index = 0;
			}
			customPawn.BodyType = bodyTypes[index];
			this.pawnLayerLabel = bodyTypeLabels[customPawn.BodyType];
		}

		protected void SelectNextHair(int direction)
		{
			CustomPawn customPawn = CurrentPawn;
			List<HairDef> hairDefs = customPawn.Gender == Gender.Male ? maleHairDefs : femaleHairDefs;
			int index = hairDefs.IndexOf(customPawn.HairDef);
			index += direction;
			if (index < 0) {
				index = hairDefs.Count - 1;
			}
			else if (index >= hairDefs.Count) {
				index = 0;
			}
			customPawn.HairDef = hairDefs[index];
			this.pawnLayerLabel = customPawn.HairDef.label;
		}

		protected void SelectNextApparel(int direction)
		{
			CustomPawn customPawn = CurrentPawn;
			int layer = this.selectedPawnLayer;
			List<ThingDef> apparelList = this.apparelLists[layer];
			int index = apparelList.IndexOf(customPawn.GetSelectedApparel(layer));
			index += direction;
			if (index < -1) {
				index = apparelList.Count - 1;
			}
			else if (index >= apparelList.Count) {
				index = -1;
			}
			if (index > -1) {
				this.pawnLayerLabel = apparelList[index].label;
				if (apparelList[index].MadeFromStuff) {
					if (customPawn.GetSelectedStuff(layer) == null) {
						customPawn.SetSelectedStuff(layer, apparelStuffLookup[apparelList[index]][0]);
					}
				}
				else {
					customPawn.SetSelectedStuff(layer, null);
				}
				customPawn.SetSelectedApparel(layer, apparelList[index]);
			}
			else {
				customPawn.SetSelectedApparel(layer, null);
				customPawn.SetSelectedStuff(layer, null);
				this.pawnLayerLabel = "EdB.None".Translate();
			}
		}

		protected string GetHeadLabel(string path)
		{
			string[] values = path.Split(new string[] { "_" }, StringSplitOptions.None);
			return values[values.Count() - 2] + ", " + values[values.Count() - 1];
		}

		protected void SelectNextTrait(int traitIndex)
		{
			CustomPawn customPawn = CurrentPawn;
			Trait currentTrait = customPawn.GetTrait(traitIndex);
			int index = -1;
			if (currentTrait != null) {
				index = traits.FindIndex((Trait t) => {
					return t.Label.Equals(currentTrait.Label);
				});
			}
			int count = 0;
			do {
				index++;
				if (index >= traits.Count) {
					index = -1;
				}
				if (++count > traits.Count + 1) {
					index = -1;
					break;
				}
			}
			while (index != -1 && customPawn.HasTrait(traits[index]));

			Trait newTrait = null;
			if (index > -1) {
				newTrait = traits[index];
			}
			customPawn.SetTrait(traitIndex, newTrait);
		}

		protected void SelectPreviousTrait(int traitIndex)
		{
			CustomPawn customPawn = CurrentPawn;
			Trait currentTrait = customPawn.GetTrait(traitIndex);
			int index = -1;
			if (currentTrait != null) {
				index = traits.FindIndex((Trait t) => {
					return t.Label.Equals(currentTrait.Label);
				});
			}
			int count = 0;
			do {
				index--;
				if (index < -1) {
					index = traits.Count - 1;
				}
				if (++count > traits.Count + 1) {
					index = -1;
					break;
				}
			}
			while (index != -1 && customPawn.HasTrait(traits[index]));

			Trait newTrait = null;
			if (index > -1) {
				newTrait = traits[index];
			}
			customPawn.SetTrait(traitIndex, newTrait);
		}

		protected void ClearTrait(int traitIndex)
		{
			CustomPawn customPawn = CurrentPawn;
			customPawn.SetTrait(traitIndex, null);
		}

		protected void SelectNextBackstory(int backstoryIndex)
		{
			CustomPawn customPawn = CurrentPawn;
			int index;
			if (backstoryIndex == 0) {
				index = childhoodBackstories.FindIndex((Backstory b) => { return b.identifier.Equals(customPawn.Childhood.identifier); });
				if (index > -1) {
					int newIndex = index + 1;
					if (newIndex >= childhoodBackstories.Count) {
						newIndex = 0;
					}
					customPawn.Childhood = childhoodBackstories[newIndex];
				}
				else {
					customPawn.Childhood = childhoodBackstories[0];
				}
			}
			else {
				index = adulthoodBackstories.FindIndex((Backstory b) => { return b.identifier.Equals(customPawn.Adulthood.identifier); });
				if (index > -1) {
					int newIndex = index + 1;
					if (newIndex >= adulthoodBackstories.Count) {
						newIndex = 0;
					}
					customPawn.Adulthood = adulthoodBackstories[newIndex];
				}
				else {
					customPawn.Adulthood = adulthoodBackstories[0];
				}
			}
		}

		protected void SelectPreviousBackstory(int backstoryIndex)
		{
			CustomPawn customPawn = CurrentPawn;
			int index;
			if (backstoryIndex == 0) {
				index = childhoodBackstories.FindIndex((Backstory b) => { return b.identifier.Equals(customPawn.Childhood.identifier); });
				if (index > -1) {
					int newIndex = index - 1;
					if (newIndex < 0) {
						newIndex = childhoodBackstories.Count - 1;
					}
					customPawn.Childhood = childhoodBackstories[newIndex];
				}
				else {
					customPawn.Childhood = childhoodBackstories[0];
				}
			}
			else {
				index = adulthoodBackstories.FindIndex((Backstory b) => { return b.identifier.Equals(customPawn.Adulthood.identifier); });
				if (index > -1) {
					int newIndex = index - 1;
					if (newIndex < 0) {
						newIndex = adulthoodBackstories.Count - 1;
					}
					customPawn.Adulthood = adulthoodBackstories[newIndex];
				}
				else {
					customPawn.Adulthood = adulthoodBackstories[0];
				}
			}
		}

		protected void SetSkillLevel(SkillRecord record, int value)
		{
			CustomPawn pawn = CurrentPawn;
			pawn.SetSkillLevel(record.def, value);
		}

		protected void IncreaseSkill(int skillIndex)
		{
			CustomPawn pawn = CurrentPawn;
			SkillRecord record = pawn.Pawn.skills.skills[skillIndex];
			pawn.IncrementSkillLevel(record.def);
		}

		protected void DecreaseSkill(int skillIndex)
		{
			CustomPawn pawn = CurrentPawn;
			SkillRecord record = pawn.Pawn.skills.skills[skillIndex];
			pawn.DecrementSkillLevel(record.def);
		}

		protected void IncreasePassion(int skillIndex)
		{
			CustomPawn customPawn = CurrentPawn;
			SkillRecord record = customPawn.Pawn.skills.skills[skillIndex];
			customPawn.IncreasePassion(record.def);
		}

		protected void DecreasePassion(int skillIndex)
		{
			CustomPawn customPawn = CurrentPawn;
			SkillRecord record = customPawn.Pawn.skills.skills[skillIndex];
			customPawn.DecreasePassion(record.def);
		}
	}
}
