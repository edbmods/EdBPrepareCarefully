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

        protected int selectedPawnLayer = 0;
        protected List<int> pawnLayers;
        protected List<Action> pawnLayerActions;
        protected int pawnLayerLabelIndex = -1;
        protected CustomPawn pawnLayerLabelModel = null;
        protected string pawnLayerLabel = null;

        protected FieldInfo apparelGraphicsField = null;
        protected List<List<ThingDef>> apparelLists = new List<List<ThingDef>>(PawnLayers.Count);
        protected Dictionary<BodyType, string> bodyTypeLabels = new Dictionary<BodyType, string>();
        protected List<HairDef> maleHairDefs = new List<HairDef>();
        protected List<HairDef> femaleHairDefs = new List<HairDef>();
        protected List<Color> skinColors = new List<Color>();
        protected List<Color> hairColors = new List<Color>();
        protected List<BodyType> maleBodyTypes = new List<BodyType>();
        protected List<BodyType> femaleBodyTypes = new List<BodyType>();
        protected Dictionary<ThingDef, List<ThingDef>> apparelStuffLookup = new Dictionary<ThingDef, List<ThingDef>>();
        protected int selectedStuff = 0;


        public PanelAppearance() {
            maleBodyTypes.Add(BodyType.Male);
            maleBodyTypes.Add(BodyType.Thin);
            maleBodyTypes.Add(BodyType.Fat);
            maleBodyTypes.Add(BodyType.Hulk);

            femaleBodyTypes.Add(BodyType.Female);
            femaleBodyTypes.Add(BodyType.Thin);
            femaleBodyTypes.Add(BodyType.Fat);
            femaleBodyTypes.Add(BodyType.Hulk);

            bodyTypeLabels.Add(BodyType.Fat, "EdB.PC.Pawn.BodyType.Fat".Translate());
            bodyTypeLabels.Add(BodyType.Hulk, "EdB.PC.Pawn.BodyType.Hulk".Translate());
            bodyTypeLabels.Add(BodyType.Thin, "EdB.PC.Pawn.BodyType.Thin".Translate());
            bodyTypeLabels.Add(BodyType.Male, "EdB.PC.Pawn.BodyType.Average".Translate());
            bodyTypeLabels.Add(BodyType.Female, "EdB.PC.Pawn.BodyType.Average".Translate());

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
                                    return a.LabelCap.CompareTo(b.LabelCap);
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

        protected override void DrawPanelContent(State state) {
            base.DrawPanelContent(state);
            CustomPawn customPawn = state.CurrentPawn;

            string label = PawnLayers.Label(this.selectedPawnLayer);
            if (WidgetDropdown.Button(RectLayerSelector, label, true, false, true)) {
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
                    // Disable head type selection when no valid head type could be found for the pawn (i.e. headType is null).
                    if (pawnLayer == PawnLayers.HeadType && customPawn.HeadType == null) {
                        continue;
                    }
                    label = PawnLayers.Label(pawnLayers[i]);
                    list.Add(new FloatMenuOption(label, this.pawnLayerActions[i], MenuOptionPriority.Default, null, null, 0, null, null));
                }
                Find.WindowStack.Add(new FloatMenu(list, null, false));
            }
            GUI.DrawTexture(RectPortrait, Textures.TexturePortraitBackground);

            customPawn.UpdatePortrait();
            DrawPawn(customPawn, RectPortrait);

            GUI.color = ColorPortraitBorder;
            Widgets.DrawBox(RectPortrait, 1);
            GUI.color = Color.white;

            // Conflict alert
            if (customPawn.ApparelConflict != null) {
                GUI.color = Color.white;
                Rect alertRect = new Rect(RectPortrait.x + 77, RectPortrait.y + 150, 36, 32);
                GUI.DrawTexture(alertRect, Textures.TextureAlert);
                TooltipHandler.TipRegion(alertRect, customPawn.ApparelConflict);
            }

            // Draw apparel selector
            Rect fieldRect = new Rect(RectPortrait.x, RectPortrait.y + RectPortrait.height + 5, RectPortrait.width, 28);
            DrawFieldSelector(fieldRect, PawnLayerLabel.CapitalizeFirst(),
                () => {
                    if (this.selectedPawnLayer == PawnLayers.HeadType) {
                        SoundDefOf.TickTiny.PlayOneShotOnCamera();
                        SelectNextHead(customPawn, -1);
                    }
                    else if (this.selectedPawnLayer == PawnLayers.BodyType) {
                        SoundDefOf.TickTiny.PlayOneShotOnCamera();
                        SelectNextBodyType(customPawn, -1);
                    }
                    else if (this.selectedPawnLayer == PawnLayers.Hair) {
                        SoundDefOf.TickTiny.PlayOneShotOnCamera();
                        SelectNextHair(customPawn, -1);
                    }
                    else {
                        SoundDefOf.TickTiny.PlayOneShotOnCamera();
                        SelectNextApparel(customPawn, -1);
                    }
                },
                () => {
                    if (this.selectedPawnLayer == PawnLayers.HeadType) {
                        SoundDefOf.TickTiny.PlayOneShotOnCamera();
                        SelectNextHead(customPawn, 1);
                    }
                    else if (this.selectedPawnLayer == PawnLayers.BodyType) {
                        SoundDefOf.TickTiny.PlayOneShotOnCamera();
                        SelectNextBodyType(customPawn, 1);
                    }
                    else if (this.selectedPawnLayer == PawnLayers.Hair) {
                        SoundDefOf.TickTiny.PlayOneShotOnCamera();
                        SelectNextHair(customPawn, 1);
                    }
                    else {
                        SelectNextApparel(customPawn, 1);
                        SoundDefOf.TickTiny.PlayOneShotOnCamera();
                    }
                }
            );

            if (Widgets.ButtonInvisible(fieldRect, false)) {
                if (this.selectedPawnLayer == PawnLayers.HeadType) {
                    ShowHeadDialog(customPawn);
                }
                else if (this.selectedPawnLayer == PawnLayers.BodyType) {
                    ShowBodyTypeDialog(customPawn);
                }
                else if (this.selectedPawnLayer == PawnLayers.Hair) {
                    ShowHairDialog(customPawn);
                }
                else {
                    ShowApparelDialog(customPawn, this.selectedPawnLayer);
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
                        }
                    );

                    if (Widgets.ButtonInvisible(stuffFieldRect, false)) {
                        ShowApparelStuffDialog(customPawn, this.selectedPawnLayer);
                    }

                    cursorY += stuffFieldRect.height;
                }
            }
            cursorY += 8;

            // Draw Color Selector
            if (PawnLayers.IsApparelLayer(selectedPawnLayer)) {
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
            else if (selectedPawnLayer == PawnLayers.BodyType || selectedPawnLayer == PawnLayers.HeadType) {
                DrawSkinSelector(customPawn, cursorY);
            }
            else if (selectedPawnLayer == PawnLayers.Hair) {
                DrawColorSelector(customPawn, cursorY, hairColors, true);
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
                SoundDefOf.TickLow.PlayOneShotOnCamera();
                RandomizeAppearance();
            }

            // Gender buttons.
            if (state.CurrentPawn.Pawn.RaceProps != null && state.CurrentPawn.Pawn.RaceProps.hasGenders) {
                bool genderFemaleSelected = state.CurrentPawn.Gender == Gender.Female;
                Style.SetGUIColorForButton(RectGenderFemale, genderFemaleSelected);
                GUI.DrawTexture(RectGenderFemale, Textures.TextureButtonGenderFemale);
                if (!genderFemaleSelected && Widgets.ButtonInvisible(RectGenderFemale, false)) {
                    SoundDefOf.TickTiny.PlayOneShotOnCamera();
                    GenderUpdated(Gender.Female);
                }
                bool genderMaleSelected = state.CurrentPawn.Gender == Gender.Male;
                Style.SetGUIColorForButton(RectGenderMale, genderMaleSelected);
                GUI.DrawTexture(RectGenderMale, Textures.TextureButtonGenderMale);
                if (!genderMaleSelected && Widgets.ButtonInvisible(RectGenderMale, false)) {
                    SoundDefOf.TickTiny.PlayOneShotOnCamera();
                    GenderUpdated(Gender.Male);
                }
            }
            GUI.color = Color.white;
        }

        protected void ChangePawnLayer(int layer) {
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

        protected List<Color> colorSelectorColors = new List<Color>();
        protected void DrawColorSelector(CustomPawn customPawn, float cursorY, ColorGenerator generator) {
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
            DrawColorSelector(customPawn, cursorY, colorSelectorColors, true);
        }

        protected static Vector2 SwatchSize = new Vector2(16, 16);
        protected static Vector2 SwatchPosition = new Vector2(18, 320);
        protected static Vector2 SwatchSpacing = new Vector2(22, 22);
        protected static Color ColorSwatchBorder = new Color(0.77255f, 0.77255f, 0.77255f);
        protected static Color ColorSwatchSelection = new Color(0.9098f, 0.9098f, 0.9098f);
        protected void DrawColorSelector(CustomPawn customPawn, float cursorY, List<Color> colors, bool allowAnyColor) {
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
                        SetColor(customPawn, color);
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

        protected void DrawSkinSelector(CustomPawn customPawn, float cursorY) {
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

        protected void DrawFieldSelector(Rect fieldRect, string label, Action previousAction, Action nextAction) {
            DrawFieldSelector(fieldRect, label, previousAction, nextAction, Style.ColorText);
        }

        protected void DrawFieldSelector(Rect fieldRect, string label, Action previousAction, Action nextAction, Color labelColor) {
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
                GUI.color = Style.ColorButtonHighlight;
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(prevButtonRect, Textures.TextureButtonPrevious);
            if (previousAction != null && Widgets.ButtonInvisible(prevButtonRect, false)) {
                SoundDefOf.TickTiny.PlayOneShotOnCamera();
                previousAction();
            }

            if (nextButtonRect.Contains(Event.current.mousePosition)) {
                GUI.color = Style.ColorButtonHighlight;
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(nextButtonRect, Textures.TextureButtonNext);
            if (nextAction != null && Widgets.ButtonInvisible(nextButtonRect, false)) {
                SoundDefOf.TickTiny.PlayOneShotOnCamera();
                nextAction();
            }
            Text.Anchor = TextAnchor.UpperLeft;
        }

        protected string PawnLayerLabel {
            get {
                CustomPawn customPawn = PrepareCarefully.Instance.State.CurrentPawn;
                string label = "EdB.PC.Panel.Appearance.NoneSelected".Translate();
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
                        label = "EdB.PC.Panel.Appearance.NoneSelected".Translate();
                    }
                }
                pawnLayerLabelIndex = selectedPawnLayer;
                pawnLayerLabelModel = customPawn;
                pawnLayerLabel = label;
                return label;
            }
        }

        protected void SelectNextHead(CustomPawn customPawn, int direction) {
            List<HeadType> heads = PrepareCarefully.Instance.Providers.HeadType.GetHeadTypes(customPawn.Pawn.def, customPawn.Gender);
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
            this.pawnLayerLabel = GetHeadLabel(customPawn.HeadGraphicPath);
        }

        protected void SelectNextBodyType(CustomPawn customPawn, int direction) {
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

        protected void SelectNextHair(CustomPawn customPawn, int direction) {
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

        protected void SelectNextApparel(CustomPawn customPawn, int direction) {
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
                this.pawnLayerLabel = "EdB.PC.Common.None".Translate();
            }
        }

        protected string GetHeadLabel(string path) {
            string[] values = path.Split(new string[] { "_" }, StringSplitOptions.None);
            return values[values.Count() - 2] + ", " + values[values.Count() - 1];
        }

        protected void ShowHeadDialog(CustomPawn customPawn) {
            List<HeadType> headTypes = PrepareCarefully.Instance.Providers.HeadType.GetHeadTypes(customPawn.Pawn.def, customPawn.Gender);
            Dialog_Options<HeadType> dialog = new Dialog_Options<HeadType>(headTypes) {
                NameFunc = (HeadType headType) => {
                    return headType.Label;
                },
                SelectedFunc = (HeadType headType) => {
                    return customPawn.HeadType.GraphicPath == headType.GraphicPath;
                },
                SelectAction = (HeadType headType) => {
                    customPawn.HeadType = headType;
                    this.pawnLayerLabel = headType.Label;
                },
                CloseAction = () => { }
            };
            Find.WindowStack.Add(dialog);
        }

        protected void ShowBodyTypeDialog(CustomPawn customPawn) {
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

        protected void ShowHairDialog(CustomPawn customPawn) {
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

        protected void ShowApparelDialog(CustomPawn customPawn, int layer) {
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
                    this.pawnLayerLabel = "EdB.PC.Panel.Appearance.NoneSelected".Translate();
                }
            };
            Find.WindowStack.Add(dialog);
        }

        protected void ShowApparelStuffDialog(CustomPawn customPawn, int layer) {
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
