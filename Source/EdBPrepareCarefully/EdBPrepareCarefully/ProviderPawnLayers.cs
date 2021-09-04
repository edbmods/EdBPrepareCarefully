using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
namespace EdB.PrepareCarefully {
    public class ProviderPawnLayers {
        private Dictionary<RaceProperties, List<PawnLayer>> pawnLayerLookup = new Dictionary<RaceProperties, List<PawnLayer>>();
        private Dictionary<AlienRace, List<PawnLayer>> alienPawnLayers = new Dictionary<AlienRace, List<PawnLayer>>();
        private PawnLayer pantsLayer = new PawnLayer() { Name = "Pants", Apparel = true, ApparelLayer = ApparelLayerDefOf.OnSkin, Label = ("EdB.PC.Pawn.PawnLayer.Pants").Translate() };
        private PawnLayer bottomClothingLayer = new PawnLayer() { Name = "BottomClothingLayer", Apparel = true, ApparelLayer = ApparelLayerDefOf.OnSkin, Label = ("EdB.PC.Pawn.PawnLayer.BottomClothingLayer").Translate() };
        private PawnLayer middleClothingLayer = new PawnLayer() { Name = "MiddleClothingLayer", Apparel = true, ApparelLayer = ApparelLayerDefOf.Middle, Label = ("EdB.PC.Pawn.PawnLayer.MiddleClothingLayer").Translate() };
        private PawnLayer topClothingLayer = new PawnLayer() { Name = "TopClothingLayer", Apparel = true, ApparelLayer = ApparelLayerDefOf.Shell, Label = ("EdB.PC.Pawn.PawnLayer.TopClothingLayer").Translate() };
        private PawnLayer hatLayer = new PawnLayer() { Name = "Hat", Apparel = true, ApparelLayer = ApparelLayerDefOf.Overhead, Label = ("EdB.PC.Pawn.PawnLayer.Hat").Translate() };
        private PawnLayer accessoryLayer = new PawnLayer() { Name = "Accessory", Apparel = true, ApparelLayer = ApparelLayerDefOf.Belt, Label = ("EdB.PC.Pawn.PawnLayer.Accessory").Translate() };
        private PawnLayer eyeCoveringLayer = new PawnLayer() { Name = "EyeCovering", Apparel = true, ApparelLayer = ApparelLayerDefOf.EyeCover, Label = ("EdB.PC.Pawn.PawnLayer.EyeCovering").Translate() };
        private Dictionary<Pair<ThingDef, Gender>, List<PawnLayer>> pawnLayerCache = new Dictionary<Pair<ThingDef, Gender>, List<PawnLayer>>();
        public ProviderPawnLayers() {

        }
        public List<PawnLayer> GetLayersForPawn(CustomPawn pawn) {
            List<PawnLayer> result = null;
            if (!pawnLayerCache.TryGetValue(new Pair<ThingDef, Gender>(pawn.Pawn.def, pawn.Gender), out result)) {
                result = InitializePawnLayers(pawn.Pawn.def, pawn.Gender);
                pawnLayerCache.Add(new Pair<ThingDef, Gender>(pawn.Pawn.def, pawn.Gender), result);
            }
            return result;
        }
        public List<PawnLayer> InitializePawnLayers(ThingDef pawnDef, Gender gender) {
            AlienRace race = PrepareCarefully.Instance.Providers.AlienRaces.GetAlienRace(pawnDef);
            if (race == null) {
                return InitializeDefaultPawnLayers(pawnDef, gender);
            }
            else {
                return InitializeAlienPawnLayers(pawnDef, gender, race);
            }
        }
        private List<PawnLayer> InitializeDefaultPawnLayers(ThingDef pawnDef, Gender gender) {
            List<PawnLayer> defaultLayers = new List<PawnLayer>() {
                InitializeHairLayer(pawnDef, gender),
                InitializeBeardLayer(pawnDef, gender),
                InitializeHeadLayer(pawnDef, gender),
                InitializeBodyLayer(pawnDef, gender)
            };

            if (ModLister.IdeologyInstalled) {
                defaultLayers.Add(InitializeFaceTattooLayer(pawnDef, gender));
                defaultLayers.Add(InitializeBodyTattooLayer(pawnDef, gender));
            }

            defaultLayers.AddRange(new PawnLayer[] {
                pantsLayer,
                bottomClothingLayer,
                middleClothingLayer,
                topClothingLayer,
                hatLayer,
                accessoryLayer,
                eyeCoveringLayer
            });

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
                OptionsHair optionsHair = PrepareCarefully.Instance.Providers.Hair.GetHairsForRace(pawnDef);
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

            layers.AddRange(new PawnLayer[] {
                pantsLayer,
                bottomClothingLayer,
                middleClothingLayer,
                topClothingLayer,
                hatLayer,
                accessoryLayer,
                eyeCoveringLayer
            });
            return layers;
        }
        private PawnLayer InitializeHairLayer(ThingDef pawnDef, Gender gender) {
            PawnLayer result = new PawnLayerHair() { Name = "Hair", Label = ("EdB.PC.Pawn.PawnLayer.Hair").Translate() };
            result.Options = InitializeHairOptions(pawnDef, gender);
            result.ColorSwatches = PrepareCarefully.Instance.Providers.Hair.GetHairsForRace(pawnDef).Colors;
            return result;
        }
        private List<PawnLayerOption> InitializeHairOptions(ThingDef pawnDef, Gender gender) {
            List<PawnLayerOption> options = new List<PawnLayerOption>();
            List<HairDef> hairDefs = PrepareCarefully.Instance.Providers.Hair.GetHairs(pawnDef, gender);
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
            result.ColorSwatches = PrepareCarefully.Instance.Providers.Hair.GetHairsForRace(pawnDef).Colors;
            return result;
        }
        private List<PawnLayerOption> InitializeBeardOptions(ThingDef pawnDef, Gender gender) {
            List<PawnLayerOption> options = new List<PawnLayerOption>();
            List<BeardDef> beardDefs = PrepareCarefully.Instance.Providers.Beards.GetBeards(pawnDef, gender);
            foreach (var def in beardDefs) {
                PawnLayerOptionBeard option = new PawnLayerOptionBeard();
                option.BeardDef = def;
                options.Add(option);
            }
            return options;
        }
        private PawnLayer InitializeFaceTattooLayer(ThingDef pawnDef, Gender gender) {
            PawnLayer result = new PawnLayerFaceTattoo() { Name = "FaceTattoo", Label = ("EdB.PC.Pawn.PawnLayer.FaceTattoo").Translate() };
            result.Options = InitializeFaceTattooOptions(pawnDef, gender);
            return result;
        }
        private List<PawnLayerOption> InitializeFaceTattooOptions(ThingDef pawnDef, Gender gender) {
            List<PawnLayerOption> options = new List<PawnLayerOption>();
            List<TattooDef> defs = PrepareCarefully.Instance.Providers.FaceTattoos.GetTattoos(pawnDef, gender);
            foreach (var def in defs) {
                PawnLayerOptionTattoo option = new PawnLayerOptionTattoo();
                option.TattooDef = def;
                options.Add(option);
            }
            return options;
        }
        private PawnLayer InitializeBodyTattooLayer(ThingDef pawnDef, Gender gender) {
            PawnLayer result = new PawnLayerBodyTattoo() { Name = "BodyTattoo", Label = ("EdB.PC.Pawn.PawnLayer.BodyTattoo").Translate() };
            result.Options = InitializeBodyTattooOptions(pawnDef, gender);
            return result;
        }
        private List<PawnLayerOption> InitializeBodyTattooOptions(ThingDef pawnDef, Gender gender) {
            List<PawnLayerOption> options = new List<PawnLayerOption>();
            List<TattooDef> defs = PrepareCarefully.Instance.Providers.BodyTattoos.GetTattoos(pawnDef, gender);
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
            foreach (var headType in PrepareCarefully.Instance.Providers.HeadTypes.GetHeadTypes(pawnDef, gender)) {
                PawnLayerOptionHead option = new PawnLayerOptionHead();
                option.HeadType = headType;
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
            foreach (var bodyType in PrepareCarefully.Instance.Providers.BodyTypes.GetBodyTypesForPawn(pawnDef, gender)) {
                PawnLayerOptionBody option = new PawnLayerOptionBody();
                option.BodyTypeDef = bodyType;
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
        public PawnLayer FindLayerForApparelLayer(ApparelLayerDef layer) {
            if (layer == ApparelLayerDefOf.OnSkin) {
                return bottomClothingLayer;
            }
            else if (layer == ApparelLayerDefOf.Middle) {
                return middleClothingLayer;
            }
            else if (layer == ApparelLayerDefOf.Shell) {
                return topClothingLayer;
            }
            else if (layer == ApparelLayerDefOf.Overhead) {
                return hatLayer;
            }
            else if (layer == ApparelLayerDefOf.Belt) {
                return accessoryLayer;
            }
            else if (layer == ApparelLayerDefOf.EyeCover) {
                return eyeCoveringLayer;
            }
            else {
                return null;
            }
        }

        public PawnLayer FindLayerForApparel(ThingDef def) {
            ApparelProperties apparelProperties = def.apparel;
            if (apparelProperties == null) {
                Logger.Warning("Trying to find an apparel layer for a non-apparel thing definition " + def.defName);
                return null;
            }
            ApparelLayerDef layer = apparelProperties.LastLayer;
            if (layer == ApparelLayerDefOf.OnSkin && apparelProperties.bodyPartGroups.Count == 1) {
                if (apparelProperties.bodyPartGroups[0].Equals(BodyPartGroupDefOf.Legs)) {
                    return pantsLayer;
                }
                else if (apparelProperties.bodyPartGroups[0].defName == "Hands") {
                    return null;
                }
                else if (apparelProperties.bodyPartGroups[0].defName == "Feet") {
                    return null;
                }
            }
            if (layer == ApparelLayerDefOf.OnSkin) {
                return bottomClothingLayer;
            }
            else if (layer == ApparelLayerDefOf.Middle) {
                return middleClothingLayer;
            }
            else if (layer == ApparelLayerDefOf.Shell) {
                return topClothingLayer;
            }
            else if (layer == ApparelLayerDefOf.Overhead) {
                return hatLayer;
            }
            else if (layer == ApparelLayerDefOf.Belt) {
                return accessoryLayer;
            }
            else if (layer == ApparelLayerDefOf.EyeCover) {
                return eyeCoveringLayer;
            }
            else {
                Logger.Warning(String.Format("Cannot find matching layer for apparel: {0}.  Last layer: {1}", def.defName, apparelProperties.LastLayer));
                return null;
            }
        }
        public PawnLayer FindLayerFromDeprecatedIndex(int index) {
            switch (index) {
                case 0:
                    // TODO
                    return null;
                case 1:
                    return bottomClothingLayer;
                case 2:
                    return pantsLayer;
                case 3:
                    return middleClothingLayer;
                case 4:
                    return topClothingLayer;
                case 5:
                    // TODO
                    return null;
                case 6:
                    // TODO
                    return null;
                case 7:
                    return hatLayer;
                case 8:
                    return accessoryLayer;
                default:
                    return null;
            }
        }
    }
}
