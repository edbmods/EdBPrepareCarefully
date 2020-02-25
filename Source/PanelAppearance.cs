using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class PanelAppearance : PanelBase {
        public delegate void RandomizeAppearanceHandler();
        public delegate void UpdateGenderHandler(Gender gender);

        public event RandomizeAppearanceHandler RandomizeAppearance;
        public event UpdateGenderHandler GenderUpdated;

        public static Color ColorPortraitBorder = new Color(0.3843f, 0.3843f, 0.3843f);

        public Rect RectGenderFemale;
        public Rect RectGenderMale;

        protected PawnLayer selectedPawnLayer = null;
        protected List<int> pawnLayers;
        protected List<Action> pawnLayerActions = new List<Action>();
        protected PawnLayer pawnLayerLabelLayer = null;
        protected CustomPawn pawnLayerLabelModel = null;
        protected string pawnLayerLabel = null;
        
        protected List<Color> skinColors = new List<Color>();
        protected Dictionary<ThingDef, List<ThingDef>> apparelStuffLookup = new Dictionary<ThingDef, List<ThingDef>>();
        protected int selectedStuff = 0;
        protected CustomPawn currentPawn = null;
        
        public PanelAppearance() {

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

            // Get material definitions so that we can use them for sorting later.
            ThingDef synthreadDef = DefDatabase<ThingDef>.GetNamedSilentFail("Synthread");
            ThingDef devilstrandDef = DefDatabase<ThingDef>.GetNamedSilentFail("DevilstrandCloth");
            ThingDef hyperweaveDef = DefDatabase<ThingDef>.GetNamedSilentFail("Hyperweave");

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
                        stuffList.Sort((ThingDef a, ThingDef b) => {
                            if (a != b) {
                                if (a == synthreadDef) {
                                    return -1;
                                }
                                else if (b == synthreadDef) {
                                    return 1;
                                }
                                else if (a == ThingDefOf.Cloth) {
                                    return -1;
                                }
                                else if (b == ThingDefOf.Cloth) {
                                    return 1;
                                }
                                else if (a == devilstrandDef) {
                                    return -1;
                                }
                                else if (b == devilstrandDef) {
                                    return 1;
                                }
                                else if (a == hyperweaveDef) {
                                    return -1;
                                }
                                else if (b == hyperweaveDef) {
                                    return 1;
                                }
                                else {
                                    return a.LabelCap.Resolve().CompareTo(b.LabelCap.Resolve());
                                }
                            }
                            else {
                                return 0;
                            }
                        });
                        apparelStuffLookup[thingDef] = stuffList;
                    }
                }
            }

            // Set up default skin colors
            foreach (Color color in PawnColorUtils.Colors) {
                skinColors.Add(color);
            }
            
            this.ChangePawnLayer(null);
        }

        private Rect RectLayerSelector;
        private Rect RectPortrait;
        private Rect RectButtonRandomize;

        public override void Resize(Rect rect) {
            base.Resize(rect);

            Vector2 panelPadding = new Vector2(16, 12);
            Vector2 panelContentSize = PanelRect.size - panelPadding - panelPadding;
            Vector2 randomizeButtonSize = new Vector2(22, 22);
            float buttonHeight = 28;

            RectLayerSelector = new Rect(panelPadding.x, panelPadding.y, panelContentSize.x, buttonHeight);
            RectPortrait = new Rect(panelPadding.x, RectLayerSelector.yMax + 8, panelContentSize.x, panelContentSize.x);
            RectButtonRandomize = new Rect(RectPortrait.xMax - randomizeButtonSize.x - 12, RectPortrait.y + 12,
                randomizeButtonSize.x, randomizeButtonSize.y);
            RectGenderFemale = new Rect(RectPortrait.x + 8, RectPortrait.y + 6, 18, 24);
            RectGenderMale = new Rect(RectGenderFemale.xMax + 2, RectPortrait.y + 6, 20, 24);
        }

        public void ChangePawn(CustomPawn pawn) {
            // Make sure the selected layer is still valid for the new pawn.
            List<PawnLayer> layers = PrepareCarefully.Instance.Providers.PawnLayers.GetLayersForPawn(pawn);
            if (selectedPawnLayer != null) {
                selectedPawnLayer = layers.FirstOrDefault((layer) => { return layer.Name == selectedPawnLayer.Name; });
            }
            if (selectedPawnLayer == null) {
                selectedPawnLayer = layers.FirstOrDefault();
            }

            // Prepare the delegate actions for the layer selected dropdown.
            pawnLayerActions.Clear();
            foreach (var layer in layers) {
                pawnLayerActions.Add(delegate { this.ChangePawnLayer(layer); });
            }

            currentPawn = pawn;
        }

        protected List<PawnLayer> currentPawnLayers = null;
        public void UpdatePawnLayers() {
            var pawn = PrepareCarefully.Instance.State.CurrentPawn;
            currentPawnLayers = PrepareCarefully.Instance.Providers.PawnLayers.GetLayersForPawn(pawn);
            if (selectedPawnLayer != null) {
                foreach (var layer in currentPawnLayers) {
                    if (layer.Name == selectedPawnLayer.Name) {
                        selectedPawnLayer = layer;
                        /*
                        if (layer.Options != null) {
                            bool found = false;
                            foreach (var option in layer.Options) {
                                if (layer.IsOptionSelected(pawn, option)) {
                                    found = true;
                                    break;
                                }
                            }
                        }
                        */
                        break;
                    }
                }
            }
        }

        protected override void DrawPanelContent(State state) {
            base.DrawPanelContent(state);
            CustomPawn customPawn = state.CurrentPawn;
            if (currentPawn != state.CurrentPawn) {
                ChangePawn(state.CurrentPawn);
            }
            if (currentPawnLayers == null) {
                UpdatePawnLayers();
            }
            string label = this.selectedPawnLayer.Label;
            if (WidgetDropdown.Button(RectLayerSelector, label, true, false, true)) {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                int layerCount = this.pawnLayerActions.Count;
                int i = 0;
                foreach (var layer in currentPawnLayers) {
                    label = layer.Label;
                    list.Add(new FloatMenuOption(label, this.pawnLayerActions[i], MenuOptionPriority.Default, null, null, 0, null, null));
                    i++;
                }
                Find.WindowStack.Add(new FloatMenu(list, null, false));
            }
            GUI.DrawTexture(RectPortrait, Textures.TexturePortraitBackground);

            customPawn.UpdatePortrait();
            DrawPawn(customPawn, RectPortrait);

            GUI.color = ColorPortraitBorder;
            Widgets.DrawBox(RectPortrait, 1);
            GUI.color = Color.white;
            
            // Draw world pawn alert
            if (state.CurrentPawn.Type == CustomPawnType.World && this.selectedPawnLayer.Apparel) {
                CustomFaction faction = state.CurrentPawn.Faction;
                if (faction == null || !faction.Leader) {
                    Rect alertRect = new Rect(RectPortrait.x + 77, RectPortrait.y + 150, 36, 32);
                    GUI.DrawTexture(alertRect, Textures.TextureAlert);
                    TooltipHandler.TipRegion(alertRect, "EdB.PC.Panel.Appearance.WorldPawnAlert".Translate());
                }
            }
            // Apparel conflict alert
            else if (customPawn.ApparelConflict != null) {
                GUI.color = Color.white;
                Rect alertRect = new Rect(RectPortrait.x + 77, RectPortrait.y + 150, 36, 32);
                GUI.DrawTexture(alertRect, Textures.TextureAlert);
                TooltipHandler.TipRegion(alertRect, customPawn.ApparelConflict);
            }
            
            // Draw selector field.
            Rect fieldRect = new Rect(RectPortrait.x, RectPortrait.y + RectPortrait.height + 5, RectPortrait.width, 40);
            Action previousSelectionAction = null;
            Action nextSelectionAction = null;
            Action clickAction = null;

            OptionsApparel apparelOptions = null;
            List<ThingDef> apparelList = null;
            if (this.selectedPawnLayer.Apparel) {
                apparelOptions = PrepareCarefully.Instance.Providers.Apparel.GetApparelForRace(customPawn);
                apparelList = apparelOptions.GetApparel(this.selectedPawnLayer);
            }

            if (this.selectedPawnLayer.Options != null) {
                if (this.selectedPawnLayer.Options.Count > 1) {
                    previousSelectionAction = () => {
                        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                        SelectNextPawnLayerOption(customPawn, -1);
                    };
                    nextSelectionAction = () => {
                        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                        SelectNextPawnLayerOption(customPawn, 1);
                    };
                }
                if (this.selectedPawnLayer.Options.Count > 0) {
                    clickAction = () => {
                        ShowPawnLayerOptionsDialog(customPawn);
                    };
                }
            }
            else {
                if (apparelList.Count > 1) {
                    previousSelectionAction = () => {
                        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                        SelectNextApparel(customPawn, -1);
                    };
                    nextSelectionAction = () => {
                        SelectNextApparel(customPawn, 1);
                        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                    };
                }
                if (apparelList.Count > 0) {
                    clickAction = () => {
                        ShowApparelDialog(customPawn, this.selectedPawnLayer);
                    };
                }
            }
            
            string selectorLabel = PawnLayerLabel.CapitalizeFirst();
            //if (hairList != null && hairList.Count == 0) {
            //    selectorLabel = "EdB.PC.Common.NoOptionAvailable".Translate();
            //}
            if (apparelList != null && apparelList.Count == 0) {
                selectorLabel = "EdB.PC.Common.NoOptionAvailable".Translate();
            }
            DrawFieldSelector(fieldRect, selectorLabel, previousSelectionAction, nextSelectionAction, clickAction);

            float cursorY = fieldRect.yMax + 6;
            
            // Draw stuff selector for apparel
            if (this.selectedPawnLayer.Apparel) {
                ThingDef apparelDef = customPawn.GetSelectedApparel(selectedPawnLayer);
                if (apparelDef != null && apparelDef.MadeFromStuff) {
                    if (customPawn.GetSelectedStuff(selectedPawnLayer) == null) {
                        Log.Error("Selected stuff for " + selectedPawnLayer.ApparelLayer + " is null");
                    }
                    Rect stuffFieldRect = new Rect(RectPortrait.x, cursorY, RectPortrait.width, 28);
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
                        },
                        () => {
                            ShowApparelStuffDialog(customPawn, this.selectedPawnLayer);
                        }
                    );

                    cursorY += stuffFieldRect.height;
                }
            }
            cursorY += 8;
            
            // Draw Color Selector
            if (selectedPawnLayer.Apparel) {
                if (apparelList != null && apparelList.Count > 0) {
                    ThingDef def = customPawn.GetSelectedApparel(selectedPawnLayer);
                    if (def != null && def.HasComp(typeof(CompColorable))) {
                        if (def.MadeFromStuff) {
                            DrawColorSelector(customPawn, cursorY, null);
                        }
                        else {
                            DrawColorSelector(customPawn, cursorY, def.colorGenerator);
                        }
                    }
                }
            }
            else if (selectedPawnLayer.ColorSelectorType == ColorSelectorType.RGB) {
                DrawColorSelectorForPawnLayer(customPawn, cursorY, selectedPawnLayer.ColorSwatches, true);
            }
            else if (selectedPawnLayer.ColorSelectorType == ColorSelectorType.Skin) {
                AlienRace alienRace = customPawn.AlienRace;
                if (alienRace == null || alienRace.UseMelaninLevels) {
                    DrawHumanlikeColorSelector(customPawn, cursorY);
                }
                else if (alienRace.ChangeableColor) {
                    DrawAlienPawnColorSelector(customPawn, cursorY, alienRace.PrimaryColors, true);
                }
            }

            // Random button
            if (RectButtonRandomize.Contains(Event.current.mousePosition)) {
                GUI.color = Style.ColorButtonHighlight;
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(RectButtonRandomize, Textures.TextureButtonRandom);
            if (Widgets.ButtonInvisible(RectButtonRandomize, false)) {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                RandomizeAppearance();
            }
            
            // Gender buttons.
            if (state.CurrentPawn.Pawn.RaceProps != null && state.CurrentPawn.Pawn.RaceProps.hasGenders) {
                bool genderFemaleSelected = state.CurrentPawn.Gender == Gender.Female;
                Style.SetGUIColorForButton(RectGenderFemale, genderFemaleSelected);
                GUI.DrawTexture(RectGenderFemale, Textures.TextureButtonGenderFemale);
                if (!genderFemaleSelected && Widgets.ButtonInvisible(RectGenderFemale, false)) {
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                    GenderUpdated(Gender.Female);
                }
                bool genderMaleSelected = state.CurrentPawn.Gender == Gender.Male;
                Style.SetGUIColorForButton(RectGenderMale, genderMaleSelected);
                GUI.DrawTexture(RectGenderMale, Textures.TextureButtonGenderMale);
                if (!genderMaleSelected && Widgets.ButtonInvisible(RectGenderMale, false)) {
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                    GenderUpdated(Gender.Male);
                }
            }
            
            GUI.color = Color.white;
        }

        protected void ChangePawnLayer(PawnLayer layer) {
            this.selectedPawnLayer = layer;
        }

        protected void DrawPawn(CustomPawn customPawn, Rect rect) {
            GUI.BeginGroup(rect);

            Vector2 pawnSize = new Vector2(128f, 180f);
            Rect pawnRect = new Rect(rect.width * 0.5f - pawnSize.x * 0.5f, 10 + rect.height * 0.5f - pawnSize.y * 0.5f, pawnSize.x, pawnSize.y);
            RenderTexture texture = customPawn.GetPortrait(pawnSize);
            GUI.DrawTexture(pawnRect, (Texture)texture);

            GUI.EndGroup();
            GUI.color = Color.white;
        }

        protected void DrawColorSelector(CustomPawn customPawn, float cursorY, ColorGenerator generator) {
            DrawColorSelector(customPawn, cursorY, generator != null ? generator.GetColorList() : null, true);
        }

        protected static float SwatchLimit = 210;
        protected static Vector2 SwatchSize = new Vector2(15, 15);
        protected static Vector2 SwatchPosition = new Vector2(18, 320);
        protected static Vector2 SwatchSpacing = new Vector2(21, 21);
        protected static Color ColorSwatchBorder = new Color(0.77255f, 0.77255f, 0.77255f);
        protected static Color ColorSwatchSelection = new Color(0.9098f, 0.9098f, 0.9098f);
        protected void DrawColorSelector(CustomPawn customPawn, float cursorY, List<Color> colors, bool allowAnyColor) {
            Color currentColor = customPawn.GetColor(selectedPawnLayer);
            Rect rect = new Rect(SwatchPosition.x, cursorY, SwatchSize.x, SwatchSize.y);
            if (colors != null) {
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
                            SetColor(customPawn, color);
                            currentColor = color;
                        }
                    }

                    rect.x += SwatchSpacing.x;
                    if (rect.x >= SwatchLimit - SwatchSize.x) {
                        rect.y += SwatchSpacing.y;
                        rect.x = SwatchPosition.x;
                    }
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
            float originalR = currentColor.r;
            float originalG = currentColor.g;
            float originalB = currentColor.b;
            float r = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y - 1, 136, 16), currentColor.r, 0, 1);
            GUI.color = Color.green;
            float g = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y + 19, 136, 16), currentColor.g, 0, 1);
            GUI.color = Color.blue;
            float b = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y + 39, 136, 16), currentColor.b, 0, 1);
            if (!CloseEnough(r, originalR) || !CloseEnough(g, originalG) || !CloseEnough(b, originalB)) {
                SetColor(customPawn, new Color(r, g, b));
            }

            GUI.color = Color.white;
        }

        protected void DrawColorSelectorForPawnLayer(CustomPawn customPawn, float cursorY, List<Color> swatches, bool allowAnyColor) {
            Color currentColor = selectedPawnLayer.GetSelectedColor(customPawn);
            Rect rect = new Rect(SwatchPosition.x, cursorY, SwatchSize.x, SwatchSize.y);
            if (swatches != null) {
                foreach (Color color in swatches) {
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
                            selectedPawnLayer.SelectColor(customPawn, color);
                            currentColor = color;
                        }
                    }

                    rect.x += SwatchSpacing.x;
                    if (rect.x >= SwatchLimit - SwatchSize.x) {
                        rect.y += SwatchSpacing.y;
                        rect.x = SwatchPosition.x;
                    }
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
            float originalR = currentColor.r;
            float originalG = currentColor.g;
            float originalB = currentColor.b;
            float r = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y - 1, 136, 16), currentColor.r, 0, 1);
            GUI.color = Color.green;
            float g = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y + 19, 136, 16), currentColor.g, 0, 1);
            GUI.color = Color.blue;
            float b = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y + 39, 136, 16), currentColor.b, 0, 1);
            if (!CloseEnough(r, originalR) || !CloseEnough(g, originalG) || !CloseEnough(b, originalB)) {
                selectedPawnLayer.SelectColor(customPawn, new Color(r, g, b));
            }

            GUI.color = Color.white;
        }

        protected void DrawAlienPawnColorSelector(CustomPawn customPawn, float cursorY, List<Color> colors, bool allowAnyColor) {
            Color currentColor = customPawn.Pawn.story.SkinColor;
            Color clickedColor = currentColor;
            Rect rect = new Rect(SwatchPosition.x, cursorY, SwatchSize.x, SwatchSize.y);
            if (colors != null) {
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
                            clickedColor = color;
                        }
                    }

                    rect.x += SwatchSpacing.x;
                    if (rect.x >= SwatchLimit - SwatchSize.x) {
                        rect.y += SwatchSpacing.y;
                        rect.x = SwatchPosition.x;
                    }
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

            float originalR = currentColor.r;
            float originalG = currentColor.g;
            float originalB = currentColor.b;
            GUI.color = Color.red;
            float r = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y - 1, 136, 16), currentColor.r, 0, 1);
            GUI.color = Color.green;
            float g = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y + 19, 136, 16), currentColor.g, 0, 1);
            GUI.color = Color.blue;
            float b = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y + 39, 136, 16), currentColor.b, 0, 1);
            if (!CloseEnough(r, originalR) || !CloseEnough(g, originalG) || !CloseEnough(b, originalB)) {
                clickedColor = new Color(r, g, b);
            }

            GUI.color = Color.white;

            if (clickedColor != currentColor) {
                customPawn.SkinColor = clickedColor;
            }
        }

        protected bool CloseEnough(float a, float b) {
            if (a > b - 0.0001f && a < b + 0.0001f) {
                return true;
            }
            else {
                return false;
            }
        }

        protected void SetColor(CustomPawn customPawn, Color color) {
            customPawn.SetColor(selectedPawnLayer, color);
        }

        protected void DrawHumanlikeColorSelector(CustomPawn customPawn, float cursorY) {
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
                if (swatchRect.x >= SwatchLimit - SwatchSize.x) {
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
                float melaninLevel = PawnColorUtils.GetValueFromRelativeLerp(currentSwatchIndex, newValue);
                customPawn.MelaninLevel = melaninLevel;
            }
        }

        protected void DrawFieldSelector(Rect fieldRect, string label, Action previousAction, Action nextAction, Action clickAction) {
            DrawFieldSelector(fieldRect, label, previousAction, nextAction, clickAction, Style.ColorText);
        }

        protected void DrawFieldSelector(Rect fieldRect, string label, Action previousAction, Action nextAction, Action clickAction, Color labelColor) {
            GUI.color = Color.white;
            Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);

            Text.Anchor = TextAnchor.MiddleCenter;
            fieldRect.y += 2;
            GUI.color = labelColor;
            if (clickAction != null && fieldRect.Contains(Event.current.mousePosition)) {
                GUI.color = Color.white;
            }
            Widgets.Label(fieldRect, label);
            GUI.color = Color.white;

            // Draw previous and next buttons.  Disable the buttons if no action arguments were passed in.
            float buttonHalfHeight = Textures.TextureButtonPrevious.height * 0.5f;
            Rect prevButtonRect = new Rect(fieldRect.x - Textures.TextureButtonPrevious.width - 2, fieldRect.MiddleY() - buttonHalfHeight, Textures.TextureButtonPrevious.width, Textures.TextureButtonPrevious.height);
            Rect nextButtonRect = new Rect(fieldRect.x + fieldRect.width + 1, fieldRect.MiddleY() - buttonHalfHeight, Textures.TextureButtonPrevious.width, Textures.TextureButtonPrevious.height);
            if (previousAction != null) {
                Style.SetGUIColorForButton(prevButtonRect);
                GUI.DrawTexture(prevButtonRect, Textures.TextureButtonPrevious);
                if (previousAction != null && Widgets.ButtonInvisible(prevButtonRect, false)) {
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                    previousAction();
                }
            }
            else {
                GUI.color = Style.ColorButtonDisabled;
                GUI.DrawTexture(prevButtonRect, Textures.TextureButtonPrevious);
            }
            if (nextAction != null) {
                Style.SetGUIColorForButton(nextButtonRect);
                GUI.DrawTexture(nextButtonRect, Textures.TextureButtonNext);
                if (nextAction != null && Widgets.ButtonInvisible(nextButtonRect, false)) {
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                    nextAction();
                }
            }
            else {
                GUI.color = Style.ColorButtonDisabled;
                GUI.DrawTexture(nextButtonRect, Textures.TextureButtonNext);
            }

            if (clickAction != null && Widgets.ButtonInvisible(fieldRect, false)) {
                clickAction();
            }

            Text.Anchor = TextAnchor.UpperLeft;
        }

        protected string PawnLayerLabel {
            get {
                CustomPawn customPawn = PrepareCarefully.Instance.State.CurrentPawn;
                string label = "EdB.PC.Panel.Appearance.NoneSelected".Translate();
                /*
                if (selectedPawnLayer == PrepareCarefully.Instance.Providers.PawnLayers.BodyLayer) {
                    label = PrepareCarefully.Instance.Providers.BodyTypes.GetBodyTypeLabel(customPawn.BodyType);
                }
                else if (selectedPawnLayer == PrepareCarefully.Instance.Providers.PawnLayers.HeadLayer) {
                    label = GetHeadLabel(customPawn);
                }
                */
                if (selectedPawnLayer.Options != null) {
                    PawnLayerOption option = selectedPawnLayer.GetSelectedOption(customPawn);
                    if (option != null) {
                        label = option.Label;
                    }
                }
                else {
                    label = null;
                    ThingDef def = customPawn.GetSelectedApparel(selectedPawnLayer);
                    if (def != null) {
                        label = def.LabelCap;
                    }
                    else {
                        label = "EdB.PC.Panel.Appearance.NoneSelected".Translate();
                    }
                }
                pawnLayerLabelLayer = selectedPawnLayer;
                pawnLayerLabelModel = customPawn;
                pawnLayerLabel = label;
                return label;
            }
        }

        protected void SelectNextHead(CustomPawn customPawn, int direction) {
            List<CustomHeadType> heads = PrepareCarefully.Instance.Providers.HeadTypes.GetHeadTypes(customPawn.Pawn.def, customPawn.Gender).ToList();
            int index = heads.IndexOf(customPawn.HeadType);
            if (index == -1) {
                return;
            }
            index += direction;
            if (index < 0) {
                index = heads.Count - 1;
            }
            else if (index >= heads.Count) {
                index = 0;
            }
            customPawn.HeadType = heads[index];
            this.pawnLayerLabel = GetHeadLabel(customPawn);
        }

        protected void SelectNextBodyType(CustomPawn customPawn, int direction) {
            ProviderBodyTypes provider = PrepareCarefully.Instance.Providers.BodyTypes;
            List<BodyTypeDef> bodyTypes = provider.GetBodyTypesForPawn(customPawn);
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
            this.pawnLayerLabel = provider.GetBodyTypeLabel(customPawn.BodyType);
        }

        protected void SelectNextApparel(CustomPawn customPawn, int direction) {
            PawnLayer layer = this.selectedPawnLayer;
            List<ThingDef> apparelList = PrepareCarefully.Instance.Providers.Apparel.GetApparel(customPawn, layer);
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
                this.pawnLayerLabel = "EdB.PC.Common.None".Translate();
            }
        }
        
        protected void SelectNextPawnLayerOption(CustomPawn customPawn, int direction) {
            int optionCount = selectedPawnLayer.Options.Count;
            int? optionalIndex = selectedPawnLayer.GetSelectedIndex(customPawn);
            if (optionalIndex == null) {
                return;
            }
            int index = optionalIndex.Value;
            index += direction;
            if (index < 0) {
                index = optionCount - 1;
            }
            else if (index >= optionCount) {
                index = 0;
            }
            PawnLayerOption option = selectedPawnLayer.Options[index];
            selectedPawnLayer.SelectOption(customPawn, option);
            this.pawnLayerLabel = option.Label;
        }

        protected string GetHeadLabel(CustomPawn pawn) {
            if (pawn.HeadType != null) {
                return pawn.HeadType.Label;
            }
            else {
                return "EdB.PC.Common.Default".Translate();
            }
        }

        protected void ShowPawnLayerOptionsDialog(CustomPawn customPawn) {
            List<PawnLayerOption> options = selectedPawnLayer.Options;
            Dialog_Options<PawnLayerOption> dialog = new Dialog_Options<PawnLayerOption>(options) {
                NameFunc = (PawnLayerOption option) => {
                    return option.Label;
                },
                SelectedFunc = (PawnLayerOption option) => {
                    return selectedPawnLayer.IsOptionSelected(customPawn, option);
                },
                SelectAction = (PawnLayerOption option) => {
                    selectedPawnLayer.SelectOption(customPawn, option);
                    this.pawnLayerLabel = option.Label;
                },
                CloseAction = () => { }
            };
            Find.WindowStack.Add(dialog);
        }

        protected void ShowApparelDialog(CustomPawn customPawn, PawnLayer layer) {
            List<ThingDef> apparelList = PrepareCarefully.Instance.Providers.Apparel.GetApparel(customPawn, layer);
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
                    this.pawnLayerLabel = "EdB.PC.Panel.Appearance.NoneSelected".Translate();
                }
            };
            Find.WindowStack.Add(dialog);
        }

        protected void ShowApparelStuffDialog(CustomPawn customPawn, PawnLayer layer) {
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

    }
}
