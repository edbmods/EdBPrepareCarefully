using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
namespace EdB.PrepareCarefully {
    public class ProviderPawnLayers {
        private Dictionary<ValueTuple<ThingDef, Gender, DevelopmentalStage>, List<PawnLayer>> pawnLayerCache = new Dictionary<ValueTuple<ThingDef, Gender, DevelopmentalStage>, List<PawnLayer>>();

        public ProviderHair ProviderHair { get; set; }
        public ProviderBeards ProviderBeards { get; set; }
        public ProviderBodyTypes ProviderBodyTypes { get; set; }
        public ProviderHeadTypes ProviderHeadTypes { get; set; }
        public ProviderAlienRaces ProviderAlienRaces { get; set; }
        public ProviderFaceTattoos ProviderFaceTattoos { get; set; }
        public ProviderBodyTattoos ProviderBodyTattoos { get; set; }

        public List<PawnLayer> GetLayersForPawn(CustomizedPawn pawn) {
            if (!pawnLayerCache.TryGetValue(ValueTuple.Create(pawn.Pawn.def, pawn.Pawn.gender, pawn.Pawn.DevelopmentalStage), out var result)) {
                result = InitializePawnLayers(pawn.Pawn.def, pawn.Pawn.gender, pawn.Pawn.DevelopmentalStage);
                pawnLayerCache.Add(ValueTuple.Create(pawn.Pawn.def, pawn.Pawn.gender, pawn.Pawn.DevelopmentalStage), result);
            }
            return result;
        }
        public List<PawnLayer> InitializePawnLayers(ThingDef pawnDef, Gender gender, DevelopmentalStage stage) {
            AlienRace race = ProviderAlienRaces.GetAlienRace(pawnDef);
            if (race == null) {
                return InitializeDefaultPawnLayers(pawnDef, gender, stage);
            }
            else {
                return InitializeAlienPawnLayers(pawnDef, gender, race);
            }
        }
        private List<PawnLayer> InitializeDefaultPawnLayers(ThingDef pawnDef, Gender gender, DevelopmentalStage stage) {
            List<PawnLayer> defaultLayers = new List<PawnLayer>() {
                InitializeHairLayer(pawnDef, gender),
                InitializeBeardLayer(pawnDef, gender),
                InitializeHeadLayer(pawnDef, gender)
            };
            if (stage != DevelopmentalStage.Baby && stage != DevelopmentalStage.Child && stage != DevelopmentalStage.Newborn) {
                defaultLayers.Add(InitializeBodyLayer(pawnDef, gender));
            }

            if (ModLister.IdeologyInstalled) {
                PawnLayer faceTattooLayer = InitializeFaceTattooLayer(pawnDef, gender);
                if (faceTattooLayer.Options.Count > 1) {
                    defaultLayers.Add(faceTattooLayer);
                }
                PawnLayer bodyTattooLayer = InitializeBodyTattooLayer(pawnDef, gender);
                if (bodyTattooLayer.Options.Count > 1) {
                    defaultLayers.Add(bodyTattooLayer);
                }
            }

            return defaultLayers;
        }
        private List<PawnLayer> InitializeAlienPawnLayers(ThingDef pawnDef, Gender gender, AlienRace race) {
            List<PawnLayer> layers = new List<PawnLayer>();
            if (race.HasHair) {
                layers.Add(InitializeHairLayer(pawnDef, gender));
            }
            if (race.HasBeards) {
                layers.Add(InitializeBeardLayer(pawnDef, gender));
            }
            layers.Add(InitializeHeadLayer(pawnDef, gender));
            layers.Add(InitializeBodyLayer(pawnDef, gender));

            if (race.Addons != null) {
                OptionsHair optionsHair = ProviderHair.GetHairsForRace(pawnDef);
                foreach (var addon in race.Addons) {
                    PawnLayerAlienAddon layer = new PawnLayerAlienAddon();
                    layer.Name = addon.Name;
                    layer.Label = addon.Name;
                    if (addon.Skin) {
                        layer.Skin = true;
                    }
                    else {
                        layer.Hair = true;
                        layer.ColorSelectorType = ColorSelectorType.RGB;
                        layer.ColorSwatches = optionsHair.Colors;
                    }
                    layer.AlienAddon = addon;
                    layer.Options = InitializeAlienAddonOptions(race, addon);
                    if (layer.Options == null || layer.Options.Count == 1) {
                        continue;
                    }
                    layers.Add(layer);
                }
            }

            if (ModLister.IdeologyInstalled && race.HasTattoos) {
                layers.AddRange(new PawnLayer[] {
                    InitializeFaceTattooLayer(pawnDef, gender),
                    InitializeBodyTattooLayer(pawnDef, gender),
                });
            }
            return layers;
        }
        private PawnLayer InitializeHairLayer(ThingDef pawnDef, Gender gender) {
            PawnLayer result = new PawnLayerHair() { Name = "Hair", Label = ("EdB.PC.Pawn.PawnLayer.Hair").Translate() };
            result.Options = InitializeHairOptions(pawnDef, gender);
            result.ColorSwatches = ProviderHair.GetHairsForRace(pawnDef).Colors;
            return result;
        }
        private List<PawnLayerOption> InitializeHairOptions(ThingDef pawnDef, Gender gender) {
            List<PawnLayerOption> options = new List<PawnLayerOption>();
            List<HairDef> hairDefs = ProviderHair.GetHairs(pawnDef, gender);
            foreach (var def in hairDefs) {
                PawnLayerOptionHair option = new PawnLayerOptionHair();
                option.HairDef = def;
                options.Add(option);
            }
            return options;
        }
        private PawnLayer InitializeBeardLayer(ThingDef pawnDef, Gender gender) {
            PawnLayer result = new PawnLayerBeard() { Name = "Beard", Label = ("EdB.PC.Pawn.PawnLayer.Beard").Translate() };
            result.Options = InitializeBeardOptions(pawnDef, gender);
            result.ColorSwatches = ProviderHair.GetHairsForRace(pawnDef).Colors;
            return result;
        }
        private List<PawnLayerOption> InitializeBeardOptions(ThingDef pawnDef, Gender gender) {
            List<PawnLayerOption> options = new List<PawnLayerOption>();
            List<BeardDef> beardDefs = ProviderBeards.GetBeards(pawnDef, gender);
            foreach (var def in beardDefs) {
                PawnLayerOptionBeard option = new PawnLayerOptionBeard();
                option.BeardDef = def;
                options.Add(option);
            }
            return options;
        }
        private PawnLayerFaceTattoo InitializeFaceTattooLayer(ThingDef pawnDef, Gender gender) {
            var result = new PawnLayerFaceTattoo {
                Name = "FaceTattoo",
                Label = ("EdB.PC.Pawn.PawnLayer.FaceTattoo").Translate(),
                Options = InitializeFaceTattooOptions(pawnDef, gender)
            };
            return result;
        }
        private List<PawnLayerOption> InitializeFaceTattooOptions(ThingDef pawnDef, Gender gender) {
            List<PawnLayerOption> options = new List<PawnLayerOption>();
            List<TattooDef> defs = ProviderFaceTattoos.GetTattoos(pawnDef, gender);
            foreach (var def in defs) {
                PawnLayerOptionTattoo option = new PawnLayerOptionTattoo {
                    TattooDef = def
                };
                options.Add(option);
            }
            return options;
        }
        private PawnLayerBodyTattoo InitializeBodyTattooLayer(ThingDef pawnDef, Gender gender) {
            var result = new PawnLayerBodyTattoo {
                Name = "BodyTattoo",
                Label = ("EdB.PC.Pawn.PawnLayer.BodyTattoo").Translate(),
                Options = InitializeBodyTattooOptions(pawnDef, gender)
            };
            return result;
        }
        private List<PawnLayerOption> InitializeBodyTattooOptions(ThingDef pawnDef, Gender gender) {
            List<PawnLayerOption> options = new List<PawnLayerOption>();
            List<TattooDef> defs = ProviderBodyTattoos.GetTattoos(pawnDef, gender);
            foreach (var def in defs) {
                PawnLayerOptionTattoo option = new PawnLayerOptionTattoo();
                option.TattooDef = def;
                options.Add(option);
            }
            return options;
        }
        private PawnLayer InitializeHeadLayer(ThingDef pawnDef, Gender gender) {
            PawnLayer result = new PawnLayerHead() { Name = "Head", Label = ("EdB.PC.Pawn.PawnLayer.HeadType").Translate() };
            result.Options = InitializeHeadOptions(pawnDef, gender);
            return result;
        }
        private List<PawnLayerOption> InitializeHeadOptions(ThingDef pawnDef, Gender gender) {
            List<PawnLayerOption> options = new List<PawnLayerOption>();
            foreach (var headType in ProviderHeadTypes.GetHeadTypes(pawnDef, gender)) {
                PawnLayerOptionHead option = new PawnLayerOptionHead();
                option.HeadType = headType;
                option.Label = ProviderHeadTypes.GetHeadTypeLabel(headType);
                options.Add(option);
            }
            return options;
        }
        private PawnLayer InitializeBodyLayer(ThingDef pawnDef, Gender gender) {
            PawnLayer result = new PawnLayerBody() { Name = "Body", Label = ("EdB.PC.Pawn.PawnLayer.BodyType").Translate() };
            result.Options = InitializeBodyOptions(pawnDef, gender);
            return result;
        }
        private List<PawnLayerOption> InitializeBodyOptions(ThingDef pawnDef, Gender gender) {
            List<PawnLayerOption> options = new List<PawnLayerOption>();
            foreach (var bodyType in ProviderBodyTypes.GetBodyTypesForPawn(pawnDef, gender).Where(b => b != BodyTypeDefOf.Baby && b != BodyTypeDefOf.Child).ToList()) {
                PawnLayerOptionBody option = new PawnLayerOptionBody();
                option.BodyTypeDef = bodyType;
                option.Label = ProviderBodyTypes.GetBodyTypeLabel(bodyType);
                option.Selectable = bodyType != BodyTypeDefOf.Baby && bodyType != BodyTypeDefOf.Child;
                options.Add(option);
            }
            return options;
        }
        private List<PawnLayerOption> InitializeAlienAddonOptions(AlienRace race, AlienRaceBodyAddon addon) {
            if (addon.OptionCount == 0) {
                return null;
            }
            List<PawnLayerOption> result = new List<PawnLayerOption>();
            for (int i=0; i<addon.OptionCount; i++) {
                PawnLayerOptionAlienAddon option = new PawnLayerOptionAlienAddon();
                option.Label = "EdB.PC.Pawn.PawnLayer.AlienAddonOption".Translate(i + 1);
                option.Index = i;
                result.Add(option);
            }
            return result;
        }
    }
}
