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

        public override void Draw(State state) {
            if (ModsConfig.BiotechActive) {
                DrawForBiotech(state);
                return;
            }
            else {
                DrawForNonBiotech(state);
            }
        }

        public void DrawForNonBiotech(State state) {
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

        public void DrawForBiotech(State state) {
            base.Draw(state);

            var texture = DevelopmentalStageIconTexture(state.CurrentPawn.RandomizeDevelopmentalStage);
            float buttonSize = 20f;
            Rect dropdownRect = new Rect(PanelRect.x + 6, PanelRect.y + PanelRect.height * 0.25f - buttonSize * 0.5f, 34.0f, 18.0f);
            if (WidgetDropdown.ImageButton(dropdownRect,texture, new Vector2(buttonSize, buttonSize), false, false, true)) {
                List<FloatMenuOption> options = new List<FloatMenuOption> {
                    new FloatMenuOption("Adult".Translate().CapitalizeFirst(), delegate
                    {
                        state.CurrentPawn.RandomizeDevelopmentalStage = DevelopmentalStage.Adult;
                    }, Textures.TextureAdult, Color.white),
                    new FloatMenuOption("Child".Translate().CapitalizeFirst(), delegate
                    {
                        state.CurrentPawn.RandomizeDevelopmentalStage = DevelopmentalStage.Child;
                    }, Textures.TextureChild, Color.white),
                    new FloatMenuOption("Baby".Translate().CapitalizeFirst(), delegate
                    {
                        state.CurrentPawn.RandomizeDevelopmentalStage = DevelopmentalStage.Baby;
                    }, Textures.TextureBaby, Color.white)
                };
                Find.WindowStack.Add(new FloatMenu(options));
            }
            string tipTitle = "";
            if (state.CurrentPawn.Pawn.DevelopmentalStage == DevelopmentalStage.Adult) {
                tipTitle = "Adult".Translate().CapitalizeFirst();
            }
            else if (state.CurrentPawn.Pawn.DevelopmentalStage == DevelopmentalStage.Child) {
                tipTitle = "Child".Translate().CapitalizeFirst();
            }
            else {
                tipTitle = "Baby".Translate().CapitalizeFirst();
            }
            tipTitle = tipTitle.Colorize(ColoredText.TipSectionTitleColor);
            TooltipHandler.TipRegion(dropdownRect, "EdB.PC.Panel.Randomize.Tip.DevelopmentalStage".Translate(tipTitle));

            if (state.CurrentPawn.RandomizeXenotype != null) {
                texture = state.CurrentPawn.RandomizeXenotype.Icon;
            }
            else if (state.CurrentPawn.RandomizeCustomXenotype != null) {
                texture = state.CurrentPawn.RandomizeCustomXenotype.IconDef?.Icon;
            }
            else if (state.CurrentPawn.RandomizeAnyNonArchite) {
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
                        state.CurrentPawn.RandomizeXenotype = null;
                        state.CurrentPawn.RandomizeCustomXenotype = null;
                        state.CurrentPawn.RandomizeAnyNonArchite = true;
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
                    list.Add(new FloatMenuOption(xenotype.LabelCap, delegate
                    {
                        state.CurrentPawn.RandomizeXenotype = xenotype;
                        state.CurrentPawn.RandomizeCustomXenotype = null;
                        state.CurrentPawn.RandomizeAnyNonArchite = false;
                    },
                    xenotype.Icon, XenotypeDef.IconColor, MenuOptionPriority.Default, delegate (Rect r)
                    {
                        TooltipHandler.TipRegion(r, xenotype.descriptionShort ?? xenotype.description);
                    },
                    null, 24f, (Rect r) => Widgets.InfoCardButton(r.x, r.y + 3f, xenotype) ? true : false, null, playSelectionSound: true, 0, HorizontalJustification.Left, extraPartRightJustified: true));
                }

                var customXenotypes = ReflectionUtil.GetStaticPropertyValue<List<CustomXenotype>>(typeof(CharacterCardUtility), "CustomXenotypes");
                if (customXenotypes != null) {
                    foreach (CustomXenotype customXenotype in customXenotypes) {
                        CustomXenotype customInner = customXenotype;
                        list.Add(new FloatMenuOption(customInner.name.CapitalizeFirst() + " (" + "Custom".Translate() + ")", delegate
                        {
                            state.CurrentPawn.RandomizeCustomXenotype = customInner;
                            state.CurrentPawn.RandomizeXenotype = null;
                            state.CurrentPawn.RandomizeAnyNonArchite = false;
                        },
                        customInner.IconDef.Icon, XenotypeDef.IconColor, MenuOptionPriority.Default, null, null, 24f, delegate (Rect r) {
                            if (Widgets.ButtonImage(new Rect(r.x, r.y + (r.height - r.width) / 2f, r.width, r.width), TexButton.DeleteX, GUI.color)) {
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
            if (state.CurrentPawn.RandomizeXenotype != null) {
                xenotypeTipLabel = state.CurrentPawn.RandomizeXenotype.LabelCap;
            }
            else if (state.CurrentPawn.RandomizeCustomXenotype != null) {
                xenotypeTipLabel = state.CurrentPawn.RandomizeCustomXenotype.name;
            }
            else if (state.CurrentPawn.RandomizeAnyNonArchite) {
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
