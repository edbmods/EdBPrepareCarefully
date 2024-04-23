using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class PanelRandomize : PanelBase {
        public delegate void RandomizeAllHandler();

        public event RandomizeAllHandler RandomizeAllClicked;
        public ModState State { get; set; }
        public ViewState ViewState { get; set; }

        public override void Draw() {
            if (ModsConfig.BiotechActive) {
                DrawForBiotech();
                return;
            }
            else {
                DrawForNonBiotech();
            }
        }

        public void DrawForNonBiotech() {
            base.Draw();
            Rect randomRect = new Rect(
                PanelRect.x + PanelRect.width / 2 - Textures.TextureButtonRandomLarge.width / 2 - 1,
                PanelRect.y + PanelRect.height / 2 - Textures.TextureButtonRandomLarge.height / 2,
                Textures.TextureButtonRandomLarge.width,
                Textures.TextureButtonRandomLarge.height
            );
            try {
                if (randomRect.Contains(Event.current.mousePosition)) {
                    GUI.color = Style.ColorButtonHighlight;
                }
                else {
                    GUI.color = Style.ColorButton;
                }
                GUI.DrawTexture(randomRect, Textures.TextureButtonRandomLarge);
                if (Widgets.ButtonInvisible(randomRect, false)) {
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                    RandomizeAllClicked?.Invoke();
                }
            }
            finally {
                GUI.color = Color.white;
            }
        }

        public void DrawForBiotech() {
            base.Draw();

            CustomizedPawn customizedPawn = ViewState.CurrentPawn;

            if (!ViewState.PawnRandomizerOptions.TryGetValue(customizedPawn, out PawnRandomizerOptions randomizerOptions)) {
                randomizerOptions = new PawnRandomizerOptions {
                    DevelopmentalStage = customizedPawn.Pawn.DevelopmentalStage,
                    Xenotype = customizedPawn.Pawn.genes.Xenotype,
                    CustomXenotype = customizedPawn.Pawn.genes.CustomXenotype
                };
                ViewState.PawnRandomizerOptions.Add(customizedPawn, randomizerOptions);
            }

            var texture = DevelopmentalStageIconTexture(randomizerOptions.DevelopmentalStage);
            float buttonSize = 20f;
            Rect dropdownRect = new Rect(PanelRect.x + 6, PanelRect.y + PanelRect.height * 0.25f - buttonSize * 0.5f, 34.0f, 18.0f);
            if (WidgetDropdown.ImageButton(dropdownRect, texture, new Vector2(buttonSize, buttonSize), false, false, true)) {
                List<FloatMenuOption> options = new List<FloatMenuOption> {
                    new FloatMenuOption("Adult".Translate().CapitalizeFirst(), delegate
                    {
                        randomizerOptions.DevelopmentalStage = DevelopmentalStage.Adult;
                    }, Textures.TextureAdult, Color.white),
                    new FloatMenuOption("Child".Translate().CapitalizeFirst(), delegate
                    {
                        randomizerOptions.DevelopmentalStage = DevelopmentalStage.Child;
                    }, Textures.TextureChild, Color.white),
                    new FloatMenuOption("Baby".Translate().CapitalizeFirst(), delegate
                    {
                        randomizerOptions.DevelopmentalStage = DevelopmentalStage.Baby;
                    }, Textures.TextureBaby, Color.white)
                };
                Find.WindowStack.Add(new FloatMenu(options));
            }
            string tipTitle = "";
            if (customizedPawn.Pawn.DevelopmentalStage == DevelopmentalStage.Adult) {
                tipTitle = "Adult".Translate().CapitalizeFirst();
            }
            else if (customizedPawn.Pawn.DevelopmentalStage == DevelopmentalStage.Child) {
                tipTitle = "Child".Translate().CapitalizeFirst();
            }
            else {
                tipTitle = "Baby".Translate().CapitalizeFirst();
            }
            tipTitle = tipTitle.Colorize(ColoredText.TipSectionTitleColor);
            TooltipHandler.TipRegion(dropdownRect, "EdB.PC.Panel.Randomize.Tip.DevelopmentalStage".Translate(tipTitle));

            if (randomizerOptions.Xenotype != null) {
                texture = randomizerOptions.Xenotype.Icon;
            }
            else if (randomizerOptions.CustomXenotype != null) {
                texture = randomizerOptions.CustomXenotype.IconDef?.Icon;
            }
            else if (randomizerOptions.AnyNonArchite) {
                texture = Textures.TextureButtonRandom;
            }
            else {
                texture = null;
            }
            buttonSize = 22.0f;
            dropdownRect = new Rect(PanelRect.x + 6, PanelRect.y + PanelRect.height * 0.75f - buttonSize * 0.5f, 32.0f, 18.0f);
            if (WidgetDropdown.ImageButton(dropdownRect, texture, new Vector2(buttonSize, buttonSize), false, false, true)) {
                List<FloatMenuOption> list = new List<FloatMenuOption> {
                    new FloatMenuOption("AnyNonArchite".Translate().CapitalizeFirst(), delegate
                    {
                        randomizerOptions.Xenotype = null;
                        randomizerOptions.CustomXenotype = null;
                        randomizerOptions.AnyNonArchite = true;
                    }),
                    new FloatMenuOption("XenotypeEditor".Translate() + "...", delegate
                    {
                        Find.WindowStack.Add(new Dialog_CreateXenotype(-1, delegate
                        {
                            CharacterCardUtility.cachedCustomXenotypes = null;
                        }));
                    })
                };

                foreach (XenotypeDef item in DefDatabase<XenotypeDef>.AllDefs.OrderBy((XenotypeDef x) => 0f - x.displayPriority)) {
                    XenotypeDef xenotype = item;
                    list.Add(new FloatMenuOption(xenotype.LabelCap, delegate {
                        randomizerOptions.Xenotype = xenotype;
                        randomizerOptions.CustomXenotype = null;
                        randomizerOptions.AnyNonArchite = false;
                    },
                    xenotype.Icon, XenotypeDef.IconColor, MenuOptionPriority.Default, delegate (Rect r) {
                        TooltipHandler.TipRegion(r, xenotype.descriptionShort ?? xenotype.description);
                    },
                    null, 24f, (Rect r) => Widgets.InfoCardButton(r.x, r.y + 3f, xenotype) ? true : false, null, playSelectionSound: true, 0, HorizontalJustification.Left, extraPartRightJustified: true));
                }

                var customXenotypes = ReflectionUtil.GetStaticPropertyValue<List<CustomXenotype>>(typeof(CharacterCardUtility), "CustomXenotypes");
                if (customXenotypes != null) {
                    foreach (CustomXenotype customXenotype in customXenotypes) {
                        CustomXenotype customInner = customXenotype;
                        list.Add(new FloatMenuOption(customInner.name.CapitalizeFirst() + " (" + "Custom".Translate() + ")", delegate {
                            randomizerOptions.CustomXenotype = customInner;
                            randomizerOptions.Xenotype = null;
                            randomizerOptions.AnyNonArchite = false;
                        },
                        customInner.IconDef.Icon, XenotypeDef.IconColor, MenuOptionPriority.Default, null, null, 24f, delegate (Rect r) {
                            if (Widgets.ButtonImage(new Rect(r.x, r.y + (r.height - r.width) / 2f, r.width, r.width), TexButton.Delete, GUI.color)) {
                                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmDelete".Translate(customInner.name.CapitalizeFirst()), delegate {
                                    string path = GenFilePaths.AbsFilePathForXenotype(customInner.name);
                                    if (File.Exists(path)) {
                                        File.Delete(path);
                                        CharacterCardUtility.cachedCustomXenotypes = null;
                                    }
                                }, destructive: true));
                                return true;
                            }
                            return false;
                        }, null, playSelectionSound: true, 0, HorizontalJustification.Left, extraPartRightJustified: true));
                    }
                }
                Find.WindowStack.Add(new FloatMenu(list));
            }
            string xenotypeTipLabel = "";
            if (randomizerOptions.Xenotype != null) {
                xenotypeTipLabel = randomizerOptions.Xenotype.LabelCap;
            }
            else if (randomizerOptions.CustomXenotype != null) {
                xenotypeTipLabel = randomizerOptions.CustomXenotype.name;
            }
            else if (randomizerOptions.AnyNonArchite) {
                xenotypeTipLabel = "AnyNonArchite".Translate().CapitalizeFirst();
            }
            xenotypeTipLabel = xenotypeTipLabel.Colorize(ColoredText.TipSectionTitleColor);
            TooltipHandler.TipRegion(dropdownRect, "EdB.PC.Panel.Randomize.Tip.Xenotype".Translate(xenotypeTipLabel));

            Rect randomRect = new Rect(
                PanelRect.x + PanelRect.width - Textures.TextureButtonRandomLarge.width - 16,
                PanelRect.y + PanelRect.HalfHeight() - Textures.TextureButtonRandomLarge.height * 0.5f,
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

            GUI.color = Style.ColorTabViewBackground;
            Widgets.DrawLineVertical(PanelRect.x + 48, PanelRect.y + 6, PanelRect.height - 12);
            GUI.color = Color.white;
        }
        protected Texture2D DevelopmentalStageIconTexture(DevelopmentalStage developmentalStage) {
            if (developmentalStage == DevelopmentalStage.Baby) {
                return Textures.TextureBaby;
            }
            else if (developmentalStage == DevelopmentalStage.Child) {
                return Textures.TextureChild;
            }
            else {
                return Textures.TextureAdult;
            }
        }
    }
}
