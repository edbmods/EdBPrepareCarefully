using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class PawnLoaderV5 {
        public ProviderHealthOptions ProviderHealthOptions { get; set; }

        // Maintain lists of definitions that were replaced in newer versions of the game.
        public Dictionary<string, string> thingDefReplacements = new Dictionary<string, string>();
        public Dictionary<string, string> traitReplacements = new Dictionary<string, string>();
        public Dictionary<string, string> recipeReplacements = new Dictionary<string, string>();
        public Dictionary<string, ReplacementBodyPart> bodyPartReplacements = new Dictionary<string, ReplacementBodyPart>();
        Dictionary<string, List<string>> skillDefReplacementLookup = new Dictionary<string, List<string>>();

        public Dictionary<string, Ideo> IdeoMap { get; set; } = new Dictionary<string, Ideo>();

        public class ReplacementBodyPart {
            public BodyPartDef def;
            public int index = 0;
            public ReplacementBodyPart(BodyPartDef def, int index = 0) {
                this.def = def;
                this.index = index;
            }
        }

        public PawnLoaderV5() {
            thingDefReplacements.Add("Gun_SurvivalRifle", "Gun_BoltActionRifle");
            thingDefReplacements.Add("Gun_Pistol", "Gun_Revolver");
            thingDefReplacements.Add("Medicine", "MedicineIndustrial");
            thingDefReplacements.Add("Component", "ComponentIndustrial");
            thingDefReplacements.Add("WolfTimber", "Wolf_Timber");

            traitReplacements.Add("Prosthophobe", "BodyPurist");
            traitReplacements.Add("Prosthophile", "Transhumanist");
            traitReplacements.Add("SuperImmune", "Immunity");

            InitializeRecipeReplacements();
            InitializeBodyPartReplacements();
            InitializeSkillDefReplacements();
        }

        protected void InitializeRecipeReplacements() {
            recipeReplacements.Add("InstallSyntheticHeart", "InstallSimpleProstheticHeart");
            recipeReplacements.Add("InstallAdvancedBionicArm", "InstallArchtechBionicArm");
            recipeReplacements.Add("InstallAdvancedBionicLeg", "InstallArchtechBionicLeg");
            recipeReplacements.Add("InstallAdvancedBionicEye", "InstallArchtechBionicEye");
        }

        protected void InitializeBodyPartReplacements() {
            AddBodyPartReplacement("LeftFoot", "Foot", 0);
            AddBodyPartReplacement("LeftLeg", "Leg", 0);
            AddBodyPartReplacement("LeftEye", "Eye", 0);
            AddBodyPartReplacement("LeftEar", "Ear", 0);
            AddBodyPartReplacement("LeftLung", "Lung", 0);
            AddBodyPartReplacement("LeftArm", "Arm", 0);
            AddBodyPartReplacement("LeftShoulder", "Shoulder", 0);
            AddBodyPartReplacement("LeftKidney", "Kidney", 0);
            AddBodyPartReplacement("RightFoot", "Foot", 1);
            AddBodyPartReplacement("RightLeg", "Leg", 1);
            AddBodyPartReplacement("RightEye", "Eye", 1);
            AddBodyPartReplacement("RightEar", "Ear", 1);
            AddBodyPartReplacement("RightLung", "Lung", 1);
            AddBodyPartReplacement("RightArm", "Arm", 1);
            AddBodyPartReplacement("RightShoulder", "Shoulder", 1);
            AddBodyPartReplacement("RightKidney", "Kidney", 1);
        }

        protected void InitializeSkillDefReplacements() {
            AddSkillDefReplacement("Growing", "Plants");
            AddSkillDefReplacement("Research", "Intellectual");
        }

        public void AddBodyPartReplacement(string name, string newPart, int index) {
            BodyPartDef def = DefDatabase<BodyPartDef>.GetNamedSilentFail(newPart);
            if (def == null) {
                Logger.Warning("Could not find body part definition \"" + newPart + "\" to replace body part \"" + name + "\"");
                return;
            }
            bodyPartReplacements.Add(name, new ReplacementBodyPart(def, index));
        }

        public List<string> ConvertModStringToModList(string modString) {
            if (string.IsNullOrEmpty(modString)) {
                return null;
            }
            var lastIndex = modString.LastIndexOf(" and ");
            if (lastIndex != -1) {
                modString = modString.Substring(0, lastIndex) + ", " + modString.Substring(lastIndex + 5);
            }
            return new List<string>(modString.Split(new string[] { ", " }, StringSplitOptions.None));
        }

        public PawnLoaderResult Load(string file) {
            SaveRecordPawnV5 pawnRecord = new SaveRecordPawnV5();
            string modString = "";
            string version = "";
            List<string> problems = new List<string>();
            PawnLoaderResult result = new PawnLoaderResult();
            try {
                Scribe.loader.InitLoading(ColonistFiles.FilePathForSavedColonist(file));
                Scribe_Values.Look<string>(ref version, "version", "unknown", false);
                Scribe_Values.Look<string>(ref modString, "mods", "", false);
                result.Mods = ConvertModStringToModList(modString);
                try {
                    Scribe_Deep.Look<SaveRecordPawnV5>(ref pawnRecord, "pawn", null);
                }
                catch (Exception e) {
                    Messages.Message(modString, MessageTypeDefOf.SilentInput);
                    problems.Add("EdB.PC.Dialog.PawnPreset.Error.Failed".Translate());
                    problems.Add(e.ToString());
                    Messages.Message("EdB.PC.Dialog.PawnPreset.Error.Failed".Translate(), MessageTypeDefOf.RejectInput);
                    Logger.Warning(e.ToString());
                    Logger.Warning("Colonist was created with the following mods: " + modString);
                    return result;
                }
            }
            catch (Exception e) {
                Logger.Error("Failed to load character file");
                throw e;
            }
            finally {
                UtilitySaveLoad.ClearSaveablesAndCrossRefs();
            }

            if (pawnRecord == null) {
                Messages.Message(modString, MessageTypeDefOf.SilentInput);
                problems.Add("EdB.PC.Dialog.PawnPreset.Error.Failed".Translate());
                Messages.Message("EdB.PC.Dialog.PawnPreset.Error.Failed".Translate(), MessageTypeDefOf.RejectInput);
                Logger.Warning("Colonist was created with the following mods: " + modString);
                return result;
            }

            PawnLoaderResult loaderResult = ConvertSaveRecordToCustomizedPawn(pawnRecord);
            loaderResult.Mods = result.Mods;

            return loaderResult;
        }

        public PawnLoaderResult ConvertSaveRecordToCustomizedPawn(SaveRecordPawnV5 record) {
            PawnLoaderResult result = new PawnLoaderResult();
            CustomizationsPawn customizations = new CustomizationsPawn();
            CustomizedPawn customizedPawn = new CustomizedPawn() {
                Customizations = customizations,
            };
            result.Pawn = customizedPawn;
            if (!record.id.NullOrEmpty()) {
                result.Pawn.Id = record.id;
            }
            else {
                result.Pawn.Id = System.Guid.NewGuid().ToString();
            }

            PawnKindDef pawnKindDef = null;
            if (record.pawnKindDef != null) {
                pawnKindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(record.pawnKindDef);
                if (pawnKindDef == null) {
                    result.AddWarning(string.Format("The saved pawn kind definition was not found ({0})", record.pawnKindDef));
                }
            }
            if (pawnKindDef == null) {
                pawnKindDef = Faction.OfPlayer.def.basicMemberKind;
            }
            customizations.PawnKind = pawnKindDef;

            // TODO: Faction
            Faction playerFaction = Find.FactionManager.OfPlayer;

            Ideo ideology = playerFaction?.ideos?.PrimaryIdeo;
            if (record.ideo != null) {
                if (!record.ideo.sameAsColony && IdeoMap != null) {
                    if (record.ideo?.name != null && !record.ideo.name.NullOrEmpty()) {
                        if (IdeoMap.TryGetValue(record.ideo.name, out Ideo ideo)) {
                            ideology = ideo;
                        }
                    }
                }
            }
            customizations.Ideo = ideology;

            if (record.abilities != null) {
                List<AbilityDef> abilityDefs = new List<AbilityDef>();
                foreach (var defName in record.abilities) {
                    AbilityDef def = DefDatabase<AbilityDef>.GetNamedSilentFail(defName);
                    if (def != null) {
                        abilityDefs.Add(def);
                    }
                    else {
                        result.AddWarning(string.Format("Could not add ability because its definition was not found ({0})", defName));
                    }
                }
                foreach (var abilityDef in abilityDefs) {
                    customizations.Abilities.Add(new CustomizedAbility() {
                        AbilityDef = abilityDef,
                    });
                }
            }

            if (record.genes != null) {
                if (!record.genes.customXenotypeName.NullOrEmpty()) {
                    var customXenotypes = ReflectionUtil.GetStaticPropertyValue<List<CustomXenotype>>(typeof(CharacterCardUtility), "CustomXenotypes");
                    if (customXenotypes == null) {
                        Logger.Debug("Got no custom xenotypes from the reflected property");
                    }
                    CustomXenotype xenotype = customXenotypes?.Where(x => { return x.name == record.genes.customXenotypeName; }).FirstOrDefault();
                    if (xenotype != null) {
                        customizations.CustomXenotype = xenotype;
                        customizations.UniqueXenotype = true;
                        customizations.XenotypeName = record.genes.customXenotypeName;
                    }
                    else {
                        customizations.UniqueXenotype = true;
                        customizations.XenotypeName = record.genes.customXenotypeName;
                    }
                }
                else if (!record.genes.xenotypeDef.NullOrEmpty()) {
                    XenotypeDef xenotypeDef = DefDatabase<XenotypeDef>.GetNamedSilentFail(record.genes.xenotypeDef);
                    if (xenotypeDef != null) {
                        customizations.XenotypeDef = xenotypeDef;
                    }
                }

                if (record.genes?.endogeneRecords != null || record.genes?.xenogeneRecords != null) {
                    customizations.Genes = new CustomizedGenes();
                    if (record.genes?.endogeneRecords != null) {
                        List<CustomizedGene> genes = new List<CustomizedGene>();
                        foreach (var g in record.genes.endogeneRecords) {
                            if (g == null || g.def.NullOrEmpty()) {
                                continue;
                            }
                            GeneDef def = DefDatabase<GeneDef>.GetNamedSilentFail(g.def);
                            GeneDef overriddenByEndogene = g.overriddenByEndogene.NullOrEmpty() ? null : DefDatabase<GeneDef>.GetNamedSilentFail(g.overriddenByEndogene);
                            GeneDef overriddenByXenogene = g.overriddenByXenogene.NullOrEmpty() ? null : DefDatabase<GeneDef>.GetNamedSilentFail(g.overriddenByXenogene);
                            if (def != null) {
                                genes.Add(new CustomizedGene() {
                                    GeneDef = def,
                                    OverriddenByEndogene = overriddenByEndogene,
                                    OverriddenByXenogene = overriddenByXenogene,
                                });
                            }
                        }
                        customizations.Genes.Endogenes = genes;
                    }
                    if (record.genes?.xenogeneRecords != null) {
                        List<CustomizedGene> genes = new List<CustomizedGene>();
                        foreach (var g in record.genes.xenogeneRecords) {
                            if (g == null || g.def.NullOrEmpty()) {
                                continue;
                            }
                            GeneDef def = DefDatabase<GeneDef>.GetNamedSilentFail(g.def);
                            GeneDef overriddenByEndogene = g.overriddenByEndogene.NullOrEmpty() ? null : DefDatabase<GeneDef>.GetNamedSilentFail(g.overriddenByEndogene);
                            GeneDef overriddenByXenogene = g.overriddenByXenogene.NullOrEmpty() ? null : DefDatabase<GeneDef>.GetNamedSilentFail(g.overriddenByXenogene);
                            if (def != null) {
                                genes.Add(new CustomizedGene() {
                                    GeneDef = def,
                                    OverriddenByEndogene = overriddenByEndogene,
                                    OverriddenByXenogene = overriddenByXenogene,
                                });
                            }
                        }
                        customizations.Genes.Xenogenes = genes;
                    }
                }
                else if (!record.genes.customXenotypeName.NullOrEmpty() && (record.genes?.endogenes != null || record.genes?.xenogenes != null)) {
                    customizations.Genes = new CustomizedGenes();
                    if (record.genes?.endogenes != null) {
                        List<CustomizedGene> genes = record.genes?.endogenes
                            .Where(g => !g.NullOrEmpty())
                            .Select(g => DefDatabase<GeneDef>.GetNamedSilentFail(g))
                            .Where(g => g != null)
                            .Select(g => new CustomizedGene() { GeneDef = g })
                            .ToList();
                        customizations.Genes.Endogenes = genes;
                    }
                    if (record.genes?.xenogenes != null) {
                        List<CustomizedGene> genes = record.genes?.xenogenes
                            .Where(g => !g.NullOrEmpty())
                            .Select(g => DefDatabase<GeneDef>.GetNamedSilentFail(g))
                            .Where(g => g != null)
                            .Select(g => new CustomizedGene() { GeneDef = g })
                            .ToList();
                        customizations.Genes.Xenogenes = genes;
                    }
                }
                else {
                    customizations.Genes = null;
                }
            }

            System.Random random = new System.Random();

            if (record.biologicalAgeInTicks != null) {
                customizations.BiologicalAgeInTicks = record.biologicalAgeInTicks.Value;
            }
            else if (record.biologicalAge > 0) {
                customizations.BiologicalAgeInTicks = (record.biologicalAge * 3600000L) + (random.Next(60) * AgeModifier.TicksPerDay);
            }
            else if (record.age > 0) {
                customizations.BiologicalAgeInTicks = record.age * 3600000L + (random.Next(60) * AgeModifier.TicksPerDay);
            }

            if (record.chronologicalAgeInTicks != null) {
                customizations.ChronologicalAgeInTicks = record.chronologicalAgeInTicks.Value;
            }
            else if (record.chronologicalAge > 0) {
                customizations.ChronologicalAgeInTicks = (record.chronologicalAge * 3600000L) + (random.Next(60) * AgeModifier.TicksPerDay);
            }
            else if (record.age > 0) {
                customizations.ChronologicalAgeInTicks = record.age * 3600000L + (random.Next(60) * AgeModifier.TicksPerDay);
            }

            if (customizations.BiologicalAgeInTicks > customizations.ChronologicalAgeInTicks) {
                customizations.ChronologicalAgeInTicks = customizations.BiologicalAgeInTicks;
            }

            if (record.type != null) {
                try {
                    customizedPawn.Type = (CustomizedPawnType)Enum.Parse(typeof(CustomizedPawnType), record.type);
                }
                catch (Exception) {
                    customizedPawn.Type = CustomizedPawnType.Colony;
                }
            }
            else {
                customizedPawn.Type = CustomizedPawnType.Colony;
            }

            customizations.Gender = record.gender;
            customizations.FirstName = record.firstName;
            customizations.NickName = record.nickName;
            customizations.LastName = record.lastName;

            customizations.FavoriteColor = record.favoriteColor;

            // TODO: Faction stuff for world pawns
            //if (record.originalFactionDef != null) {
            //    pawn.OriginalFactionDef = DefDatabase<FactionDef>.GetNamedSilentFail(record.originalFactionDef);
            //}
            //pawn.OriginalKindDef = pawnKindDef;

            //if (pawn.Type == CustomPawnType.Colonist) {
            //    playerFaction = Faction.OfPlayerSilentFail;
            //    if (playerFaction != null) {
            //        pawn.Pawn.SetFactionDirect(playerFaction);
            //    }
            //}
            //else if (pawn.Type == CustomPawnType.World) {
            //    if (record?.faction?.def != null) {
            //        FactionDef factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(record.faction.def);
            //        if (factionDef != null) {
            //            bool randomFaction = false;
            //            if (record.faction.index != null) {
            //                CustomFaction customFaction = null;
            //                if (!record.faction.leader) {
            //                    customFaction = PrepareCarefully.Instance.Providers.Factions.FindCustomFactionByIndex(factionDef, record.faction.index.Value);
            //                }
            //                else {
            //                    customFaction = PrepareCarefully.Instance.Providers.Factions.FindCustomFactionWithLeaderOptionByIndex(factionDef, record.faction.index.Value);
            //                }
            //                if (customFaction != null) {
            //                    pawn.Faction = customFaction;
            //                }
            //                else {
            //                    Logger.Warning("Could not place at least one preset character into a saved faction because there were not enough available factions of that type in the world");
            //                    randomFaction = true;
            //                }
            //            }
            //            else {
            //                randomFaction = true;
            //            }
            //            if (randomFaction) {
            //                CustomFaction customFaction = PrepareCarefully.Instance.Providers.Factions.FindRandomCustomFactionByDef(factionDef);
            //                if (customFaction != null) {
            //                    pawn.Faction = customFaction;
            //                }
            //            }
            //        }
            //        else {
            //            Logger.Warning("Could not place at least one preset character into a saved faction because that faction is not available in the world");
            //        }
            //    }
            //}

            HairDef h = DefDatabase<HairDef>.GetNamedSilentFail(record.hairDef);
            if (h != null) {
                customizations.Hair = h;
            }
            else {
                result.AddWarning("Could not load hair definition \"" + record.hairDef + "\"");
            }

            if (!String.IsNullOrWhiteSpace(record.headType)) {
                var headType = DefDatabase<HeadTypeDef>.GetNamedSilentFail(record.headType);
                if (headType != null) {
                    customizations.HeadType = headType;
                }
                else {
                    result.AddWarning("Could not load head type definition \"" + record.headType + "\"");
                }
            }
            else if (!String.IsNullOrEmpty(record.headGraphicPath)) {
                string[] pathParts = record.headGraphicPath.Split(new char[] { '/' }, 20);
                string lastPath = pathParts[pathParts.Length - 1];
                string[] parts = lastPath.Split('_');
                string headTypeDefName = parts[0] + "_";
                for (int i=1; i<parts.Count(); i++) {
                    headTypeDefName += parts[i];
                }
                var headType = DefDatabase<HeadTypeDef>.GetNamedSilentFail(headTypeDefName);
                if (headType != null) {
                    customizations.HeadType = headType;
                }
                else {
                    result.AddWarning("Couldn't find head type {" + headTypeDefName + "} converted from headGraphicPath {" + record.headGraphicPath + "}");
                }
            }

            customizations.HairColor = record.hairColor;
            customizations.SkinColor = record.skinColor;

            if (!record.childhood.NullOrEmpty()) {
                BackstoryDef backstory = record.childhood != null ? FindBackstory(record.childhood) : null;
                if (backstory != null) {
                    customizations.ChildhoodBackstory = backstory;
                }
                else {
                    result.AddWarning(string.Format("Could not load childhood backstory definition ({0})", record.childhood));
                }
            }
            else {
                result.AddWarning("Saved pawn had an empty childhood backstory definition");
            }
            if (!record.adulthood.NullOrEmpty()) {
                BackstoryDef backstory = FindBackstory(record.adulthood);
                if (backstory != null) {
                    customizations.AdulthoodBackstory = backstory;
                }
                else {
                    result.AddWarning("Could not load adulthood backstory definition \"" + record.adulthood + "\"");
                }
            }
            else {
                result.AddWarning("Saved pawn had an empty adulthood backstory definition");
            }

            customizations.BodyType = DefDatabase<BodyTypeDef>.GetNamedSilentFail(record.bodyType);

            BeardDef beardDef = null;
            if (record.beard != null) {
                beardDef = DefDatabase<BeardDef>.GetNamedSilentFail(record.beard);
            }
            if (beardDef == null) {
                beardDef = BeardDefOf.NoBeard;
            }
            customizations.Beard = beardDef;

            TattooDef faceTattooDef = null;
            if (record.bodyTattoo != null) {
                faceTattooDef = DefDatabase<TattooDef>.GetNamedSilentFail(record.faceTattoo);
            }
            if (faceTattooDef == null) {
                faceTattooDef = TattooDefOf.NoTattoo_Face;
            }
            customizations.FaceTattoo = faceTattooDef;

            TattooDef bodyTattooDef = null;
            if (record.bodyTattoo != null) {
                bodyTattooDef = DefDatabase<TattooDef>.GetNamedSilentFail(record.bodyTattoo);
            }
            if (bodyTattooDef == null) {
                bodyTattooDef = TattooDefOf.NoTattoo_Body;
            }
            customizations.BodyTattoo = bodyTattooDef;

            if (record.traits != null) {
                for (int i = 0; i < record.traits.Count; i++) {
                    string traitDefName = record.traits[i].def;
                    TraitDef traitDef = DefDatabase<TraitDef>.GetNamedSilentFail(traitDefName);
                    if (traitDef != null) {
                        customizations.Traits.Add(new CustomizationsTrait() { TraitDef = traitDef, Degree = record.traits[i].degree });
                    }
                    else {
                        result.AddWarning("Could not load trait definition \"" + traitDefName + "\"");
                    }
                }
            }
            else if (record.traitNames != null && record.traitDegrees != null && record.traitNames.Count == record.traitDegrees.Count) {
                for (int i = 0; i < record.traitNames.Count; i++) {
                    string traitDefName = record.traitNames[i];
                    TraitDef traitDef = DefDatabase<TraitDef>.GetNamedSilentFail(traitDefName);
                    if (traitDef != null) {
                        customizations.Traits.Add(new CustomizationsTrait() { TraitDef = traitDef, Degree = record.traitDegrees[i] });
                    }
                    else {
                        result.AddWarning("Could not load trait definition \"" + traitDefName + "\"");
                    }
                }
            }

            if (record.skills != null) {
                foreach (var skill in record.skills) {
                    SkillDef def = DefDatabase<SkillDef>.GetNamedSilentFail(skill.name);
                    if (def == null) {
                        result.AddWarning("Could not load skill definition \"" + skill.name + "\" from saved preset");
                        continue;
                    }
                    customizations.Skills.Add(new CustomizationsSkill() {
                            SkillDef = def,
                            Level = skill.value,
                            OriginalLevel = skill.value,
                            Passion = skill.passion,
                            OriginalPassion = skill.passion,
                        }
                    );
                }
            }
            else {
                result.AddWarning("Could not load skills definitions for the saved preset. No valid skill definitions found");
            }

            if (record.apparel != null) {
                foreach (var apparelRecord in record.apparel) {

                    ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(apparelRecord.apparel);
                    if (def == null) {
                        result.AddWarning("Could not load thing definition for apparel \"" + apparelRecord.apparel + "\"");
                        continue;
                    }
                    ThingDef stuffDef = null;
                    if (!string.IsNullOrEmpty(apparelRecord.stuff)) {
                        stuffDef = DefDatabase<ThingDef>.GetNamedSilentFail(apparelRecord.stuff);
                        if (stuffDef == null) {
                            result.AddWarning("Could not load stuff definition \"" + apparelRecord.stuff + "\" for apparel \"" + apparelRecord.apparel + "\"");
                            continue;
                        }
                    }
                    QualityCategory? quality = null;
                    if (!apparelRecord.quality.NullOrEmpty()) {
                        try {
                            quality = Enum.Parse(typeof(QualityCategory), apparelRecord.quality) as QualityCategory?;
                        }
                        catch (Exception) {
                            result.AddWarning("Did not recognize the saved quality attribute for an apparel item. Using the default quality instead");
                        }
                    }
                    StyleCategoryDef styleCategoryDef = null;
                    if (apparelRecord.style != null) {
                        styleCategoryDef = DefDatabase<StyleCategoryDef>.GetNamedSilentFail(apparelRecord.style);
                        if (styleCategoryDef == null) {
                            result.AddWarning("Could not style category definition \"" + apparelRecord.style + "\" for apparel \"" + apparelRecord.apparel + "\"");
                        }
                    }
                    customizations.Apparel.Add(new CustomizationsApparel() {
                        ThingDef = def,
                        StyleCategoryDef = styleCategoryDef,
                        StuffDef = stuffDef,
                        HitPoints = apparelRecord.hitPoints,
                        Quality = quality,
                        Color = apparelRecord.color
                    });

                }
            }

            var healthOptions = ProviderHealthOptions.GetOptions(customizations.PawnKind.race);
            if (healthOptions == null) {
                Logger.Debug("No health options found for pawn: " + customizations.PawnKind.race?.defName);
            }

            for (int i = 0; i < record.implants.Count; i++) {
                SaveRecordImplantV5 implantRecord = record.implants[i];

                UniqueBodyPart uniqueBodyPart = healthOptions.FindBodyPartByName(implantRecord.bodyPart, implantRecord.bodyPartIndex != null ? implantRecord.bodyPartIndex.Value : 0);
                if (uniqueBodyPart == null) {
                    uniqueBodyPart = FindReplacementBodyPart(healthOptions, implantRecord.bodyPart);
                }
                if (uniqueBodyPart == null) {
                    result.AddWarning("Could not add the implant because it could not find the needed body part \"" + implantRecord.bodyPart + "\""
                        + (implantRecord.bodyPartIndex != null ? " with index " + implantRecord.bodyPartIndex : ""));
                    continue;
                }
                BodyPartRecord bodyPart = uniqueBodyPart.Record;
                if (implantRecord.recipe != null) {
                    RecipeDef recipeDef = FindRecipeDef(implantRecord.recipe);
                    if (recipeDef == null) {
                        result.AddWarning("Could not add the implant because it could not find the recipe definition \"" + implantRecord.recipe + "\"");
                        continue;
                    }
                    bool found = false;
                    foreach (var p in recipeDef.appliedOnFixedBodyParts) {
                        if (p.defName.Equals(bodyPart.def.defName)) {
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        result.AddWarning("Could not apply the saved implant recipe \"" + implantRecord.recipe + "\" to the body part \"" + bodyPart.def.defName + "\".  Recipe does not support that part.");
                        continue;
                    }
                    Implant implant = new Implant() {
                        BodyPartRecord = bodyPart,
                        Recipe = recipeDef
                    };
                    // TODO: This looks weird; something to do with caching the label. Should rework it.
                    implant.label = implant.Label;
                    customizations.Implants.Add(implant);
                    Logger.Debug("Added implant customizations " + recipeDef?.defName);
                }
                else if (implantRecord.hediffDef != null) {
                    HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(implantRecord.hediffDef);
                    if (hediffDef != null) {
                        Implant implant = new Implant();
                        implant.BodyPartRecord = bodyPart;
                        implant.label = implant.Label;
                        implant.HediffDef = hediffDef;
                        customizations.Implants.Add(implant);
                    }
                    else {
                        result.AddWarning("Could not add implant to pawn because the specified HediffDef {" + implantRecord.hediffDef + "} for the implant was not found");
                    }
                }
            }

            foreach (var injuryRecord in record.injuries) {
                HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(injuryRecord.hediffDef);
                if (def == null) {
                    result.AddWarning("Could not add the injury because it could not find the hediff definition \"" + injuryRecord.hediffDef + "\"");
                    continue;
                }
                InjuryOption option = healthOptions.FindInjuryOptionByHediffDef(def);
                if (option == null) {
                    result.AddWarning("Could not add the injury because it could not find a matching injury option for the saved hediff \"" + injuryRecord.hediffDef + "\"");
                    continue;
                }
                
                BodyPartRecord bodyPart = null;
                if (injuryRecord.bodyPart != null) {
                    UniqueBodyPart uniquePart = healthOptions.FindBodyPartByName(injuryRecord.bodyPart,
                        injuryRecord.bodyPartIndex != null ? injuryRecord.bodyPartIndex.Value : 0);
                    if (uniquePart == null) {
                        uniquePart = FindReplacementBodyPart(healthOptions, injuryRecord.bodyPart);
                    }
                    if (uniquePart == null) {
                        result.AddWarning("Could not add the injury because it could not find the needed body part \"" + injuryRecord.bodyPart + "\""
                            + (injuryRecord.bodyPartIndex != null ? " with index " + injuryRecord.bodyPartIndex : ""));
                        continue;
                    }
                    bodyPart = uniquePart.Record;
                }
                bool isValid = true;
                Injury injury = new Injury();
                injury.Option = option;
                injury.BodyPartRecord = bodyPart;
     
                if (injuryRecord.severity != null) {
                    injury.Severity = injuryRecord.Severity;
                }
                if (injuryRecord.painFactor != null) {
                    injury.PainFactor = injuryRecord.PainFactor;
                }
                if (injuryRecord.chemical != null) {
                    injury.Chemical = DefDatabase<ChemicalDef>.GetNamedSilentFail(injuryRecord.chemical);
                    if (injury.Chemical == null) {
                        Logger.Debug("Could not load injury from saved pawn.  Chemical definition (" + injuryRecord.chemical + ") was not found");
                        isValid = false;
                    }
                }
                if (isValid) {
                    customizations.Injuries.Add(injury);
                }
            }

            // Ideoligion Certainty
            try {
                if (ideology != null) {
                    if (record.ideo != null && ModsConfig.IdeologyActive) {
                        customizations.Certainty = record.ideo.certainty;
                    }
                }
            }
            catch (Exception) {
                result.AddWarning("Failed to load ideoligion certainty value");
            }

            LoadTitles(record, customizations, result);
            LoadPossessions(record, customizations, result);
            LoadOtherValues(record, customizations, result);

            return result;
        }

        public void LoadOtherValues(SaveRecordPawnV5 record, CustomizationsPawn customizations, PawnLoaderResult result) {
            //var group = record.FindValueGroup("Example");
            //if (group != null) {
            //    string color = group.GetStringValue("Color");
            //    Logger.Debug("other value Color = " + color);
            //}
        }

        protected void LoadPossessions(SaveRecordPawnV5 record, CustomizationsPawn customizations, PawnLoaderResult result) {
            result.Pawn.Customizations.Possessions = new List<CustomizedPossession>();
            if (record.possessions != null) {
                foreach (var possession in record?.possessions) {
                    ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(possession.thingDef);
                    if (def != null) {
                        result.Pawn.Customizations.Possessions.Add(new CustomizedPossession { ThingDef = def, Count = possession.count });
                    }
                    else {
                        result.AddWarning(string.Format("Could not add possession because thing definition was not found ({0})", possession.thingDef));
                    }
                }
            }
        }

        protected void LoadTitles(SaveRecordPawnV5 record, CustomizationsPawn customizations, PawnLoaderResult result) {
            customizations.Titles = new List<CustomizationTitle>();
            if (record.titles.NullOrEmpty()) {
                return;
            }
            LinkedList<Faction> factions = new LinkedList<Faction>(Find.FactionManager.AllFactionsInViewOrder);
            foreach (var title in record.titles) {
                RoyalTitleDef titleDef = null;
                if (!title.titleDef.NullOrEmpty()) {
                    titleDef = DefDatabase<RoyalTitleDef>.GetNamedSilentFail(title.titleDef);
                    if (titleDef == null) {
                        result.AddWarning(string.Format("Could not add title. Title definition not found ({0})", title.titleDef));
                        continue;
                    }
                }
                if (title.factionDef.NullOrEmpty()) {
                    result.AddWarning(string.Format("Could not add title. Faction definition was empty"));
                    continue;
                }
                FactionDef factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(title.factionDef);
                if (factionDef == null) {
                    result.AddWarning(string.Format("Could not add title. Faction definition not found ({0})", title.factionDef));
                    continue;
                }
                if (title.favor > 0 || titleDef != null) {
                    Faction faction = factions.FirstOrDefault(f => f.def?.defName == title.factionDef);
                    if (faction != null) {
                        if (title.favor > 0 || titleDef != null) {
                            customizations.Titles.Add(new CustomizationTitle() {
                                Faction = faction,
                                TitleDef = titleDef,
                                Honor = title.favor
                            });
                            Logger.Debug("Added title " + titleDef.defName + " for " + factionDef.defName);
                            factions.Remove(faction);
                        }
                    }
                    else {
                        result.AddWarning(string.Format("Could not add title.  No matching faction to add title ({0})", title.factionDef));
                    }
                }
            }
        }

        protected RecipeDef FindRecipeDef(string name) {
            RecipeDef result = DefDatabase<RecipeDef>.GetNamedSilentFail(name);
            if (result == null) {
                return FindReplacementRecipe(name);
            }
            else {
                return result;
            }
        }

        protected RecipeDef FindReplacementRecipe(string name) {
            String replacementName = null;
            if (recipeReplacements.TryGetValue(name, out replacementName)) {
                return DefDatabase<RecipeDef>.GetNamedSilentFail(replacementName);
            }
            return null;
        }

        protected UniqueBodyPart FindReplacementBodyPart(OptionsHealth healthOptions, string name) {
            ReplacementBodyPart replacement = null;
            if (bodyPartReplacements.TryGetValue(name, out replacement)) {
                return healthOptions.FindBodyPart(replacement.def, replacement.index);
            }
            return null;
        }

        public BackstoryDef FindBackstory(string name) {
            // Assume the name is a definition name.  Look it up based on that and return it if we find it.
            BackstoryDef matchingBackstory = DefDatabase<BackstoryDef>.GetNamedSilentFail(name);
            if (matchingBackstory != null) {
                return matchingBackstory;
            }
            // If we didn't find it, the name is probably a backstory identifier.  Try to find it by matching the id.
            matchingBackstory = DefDatabase<BackstoryDef>.AllDefs.Where((BackstoryDef b) => { return b.identifier != null && b.identifier.Equals(name); }).FirstOrDefault();
            if (matchingBackstory != null) {
                return matchingBackstory;
            }
            // If we didn't find it, the identifier probably changed.  Try to find another backstory with the same identifier prefix but with a different numeric suffix at the end
            Regex expression = new Regex("\\d+$");
            string backstoryMinusVersioning = expression.Replace(name, "");
            if (backstoryMinusVersioning == name) {
                return null;
            }
            matchingBackstory = DefDatabase<BackstoryDef>.AllDefs
                .Where(b => { return b.identifier != null && b.identifier.StartsWith(backstoryMinusVersioning); })
                .FirstOrDefault();
            if (matchingBackstory != null) {
                Logger.Message("Found replacement backstory.  Using " + matchingBackstory.identifier + " in place of " + name);
                return matchingBackstory;
            }
            // If we still didn't find it, look for a def name that starts with the identifier but with the numeric suffix removed
            matchingBackstory = DefDatabase<BackstoryDef>.AllDefs
                .Where(b => b.defName.StartsWith(backstoryMinusVersioning))
                .FirstOrDefault();
            if (matchingBackstory != null) {
                Logger.Message("Found replacement backstory.  Using " + matchingBackstory.defName + " in place of " + name);
                return matchingBackstory;
            }
            return null;
        }

        protected Trait FindTrait(string name, int degree) {
            Trait trait = LookupTrait(name, degree);
            if (trait != null) {
                return trait;
            }
            else {
                if (traitReplacements.ContainsKey(name)) {
                    return LookupTrait(traitReplacements[name], degree);
                }
            }
            return null;
        }

        protected Trait LookupTrait(string name, int degree) {
            foreach (TraitDef def in DefDatabase<TraitDef>.AllDefs) {
                if (!def.defName.Equals(name)) {
                    continue;
                }
                List<TraitDegreeData> degreeData = def.degreeDatas;
                int count = degreeData.Count;
                if (count > 0) {
                    for (int i = 0; i < count; i++) {
                        if (degree == degreeData[i].degree) {
                            Trait trait = new Trait(def, degreeData[i].degree, true);
                            return trait;
                        }
                    }
                }
                else {
                    return new Trait(def, 0, true);
                }
            }
            return null;
        }

        protected void AddSkillDefReplacement(String skill, String replacement) {
            if (!skillDefReplacementLookup.TryGetValue(skill, out List<string> replacements)) {
                replacements = new List<string>();
                skillDefReplacementLookup.Add(skill, replacements);
            }
            replacements.Add(replacement);
        }

        protected SkillDef FindSkillDef(Pawn pawn, string name) {
            List<string> replacements = null;
            if (skillDefReplacementLookup.ContainsKey(name)) {
                replacements = skillDefReplacementLookup[name];
            }
            foreach (var skill in pawn.skills.skills) {
                if (skill.def.defName.Equals(name)) {
                    return skill.def;
                }
                if (replacements != null) {
                    foreach (var r in replacements) {
                        if (skill.def.defName.Equals(r)) {
                            return skill.def;
                        }
                    }
                }
            }
            return null;
        }

    }
}
