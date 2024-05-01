using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class PawnSaver {
        public ProviderHealthOptions ProviderHealthOptions { get; set; }

        public void SaveToFile(CustomizedPawn customizedPawn, string colonistName) {
            if (customizedPawn?.Customizations == null) {
                Logger.Error("Failed to save character file because customizations were null");
            }
            SaveRecordPawnV5 pawn = ConvertCustomizedPawnToSaveRecord(customizedPawn);
            try {
                Scribe.saver.InitSaving(ColonistFiles.FilePathForSavedColonist(colonistName), "character");
                string versionStringFull = "5";
                Scribe_Values.Look<string>(ref versionStringFull, "version", null, false);
                string modString = GenText.ToCommaList(Enumerable.Select<ModContentPack, string>(LoadedModManager.RunningMods, (Func<ModContentPack, string>)(mod => mod.Name)), true);
                Scribe_Values.Look<string>(ref modString, "mods", null, false);

                Scribe_Deep.Look<SaveRecordPawnV5>(ref pawn, "pawn");
            }
            catch (Exception e) {
                Logger.Error("Failed to save preset file");
                throw e;
            }
            finally {
                Scribe.saver.FinalizeSaving();
                Scribe.mode = LoadSaveMode.Inactive;
            }
        }

        public SaveRecordPawnV5 ConvertCustomizedPawnToSaveRecord(CustomizedPawn customizedPawn) {
            CustomizationsPawn customizations = customizedPawn.Customizations;
            SaveRecordPawnV5 result = new SaveRecordPawnV5();
            result.id = customizedPawn.Id ?? Guid.NewGuid().ToString();
            result.thingDef = customizedPawn.Pawn.def.defName;
            result.type = customizedPawn.Type.ToString();
            //if (customizedPawn.Type == CustomPawnType.World && customizedPawn.Faction != null) {
            //    result.faction = new SaveRecordFactionV4() {
            //        def = customizedPawn.Faction?.Def?.defName,
            //        index = customizedPawn.Faction.Index,
            //        leader = customizedPawn.Faction.Leader
            //    };
            //}
            result.pawnKindDef = customizations.PawnKind?.defName;
            //result.pawnKindDef = customizedPawn.OriginalKindDef?.defName ?? customizedPawn.Pawn.kindDef.defName;
            //result.originalFactionDef = customizedPawn.OriginalFactionDef?.defName;
            result.gender = customizations.Gender;
            result.adulthood = customizations.AdulthoodBackstory?.defName;
            result.childhood = customizations.ChildhoodBackstory?.defName;
            result.skinColor = customizations.SkinColor;
            //result.melanin = customizations.MelaninLevel;
            result.hairDef = customizations.Hair?.defName;
            result.hairColor = customizations.HairColor;
            result.bodyType = customizations.BodyType?.defName;
            result.headType = customizations.HeadType?.defName;
            result.beard = customizations.Beard?.defName;
            result.faceTattoo = customizations.FaceTattoo?.defName;
            result.bodyTattoo = customizations.BodyTattoo?.defName;
            result.firstName = customizations.FirstName;
            result.nickName = customizations.NickName;
            result.lastName = customizations.LastName;
            result.age = 0;
            //result.biologicalAge = customizations.BiologicalAge;
            //result.chronologicalAge = customizations.ChronologicalAge;
            result.biologicalAgeInTicks = customizations.BiologicalAgeInTicks;
            result.chronologicalAgeInTicks = customizations.ChronologicalAgeInTicks;
            result.favoriteColor = customizations.FavoriteColor;

            ConvertTraits(result, customizations);
            ConvertSkills(result, customizations);
            ConvertApparel(result, customizations);
            OptionsHealth healthOptions = ProviderHealthOptions.GetOptions(customizedPawn.Pawn);
            ConvertImplants(result, customizations, healthOptions);
            ConvertInjuries(result, customizations, healthOptions);
            ConvertPossessions(result, customizations);
            ConvertAbilities(result, customizations);
            ConvertIdeo(result, customizations);
            ConvertGenes(result, customizations);
            ConvertTitles(result, customizations);
            ConvertOtherValues(result, customizations);

            return result;

        }

        public void ConvertOtherValues(SaveRecordPawnV5 result, CustomizationsPawn customizations) {
            //var group = new SaveRecordValueGroupV5() { name = "Example" };
            //group.SetValue("Color", "Red");
        }

        public void ConvertTitles(SaveRecordPawnV5 result, CustomizationsPawn customizations) {
            if (!ModsConfig.RoyaltyActive) {
                return;
            }
            List<SaveRecordTitleV5> titleList = new List<SaveRecordTitleV5>();
            //Dictionary<Faction, SaveRecordTitleV5> titleLookup = new Dictionary<Faction, SaveRecordTitleV5>();
            if (customizations.Titles != null) {
                foreach (var title in customizations.Titles) {
                    var record = new SaveRecordTitleV5() {
                        factionName = title.Faction?.Name,
                        factionDef = title.Faction?.def?.defName,
                        titleDef = title.TitleDef?.defName,
                        favor = title.Honor,
                    };
                    titleList.Add(record);
                    //titleLookup.Add(title.Faction, record);
                }
            }
            //foreach (var faction in Find.FactionManager.AllFactionsListForReading) {
            //    int favor = customizedPawn.Pawn.royalty.GetFavor(faction);
            //    if (favor > 0) {
            //        if (titleLookup.TryGetValue(faction, out var title)) {
            //            title.favor = favor;
            //        }
            //        else {
            //            var record = new SaveRecordTitleV5() {
            //                factionName = faction?.Name,
            //                factionDef = faction?.def?.defName,
            //                favor = favor,
            //            };
            //            titleList.Add(record);
            //            titleLookup.Add(faction, record);
            //        }
            //    }
            //}
            if (titleList.Count > 0) {
                result.titles = titleList;
            }
        }

        public void ConvertGenes(SaveRecordPawnV5 result, CustomizationsPawn customizations) {
            if (!ModsConfig.BiotechActive) {
                return;
            }
            result.genes = new SaveRecordGenesV5() {
                xenotypeDef = customizations.XenotypeDef?.defName,
                customXenotypeName = customizations.XenotypeName,
                uniqueXenotype = customizations.UniqueXenotype,
                endogenes = customizations.Genes?.Endogenes.ConvertAll(g => g.GeneDef?.defName),
                xenogenes = customizations.Genes?.Xenogenes.ConvertAll(g => g.GeneDef?.defName),
                endogeneRecords = customizations.Genes?.Endogenes.ConvertAll(g => new SaveRecordGeneV5() {
                    def = g.GeneDef?.defName,
                    overriddenByEndogene = g.OverriddenByEndogene?.defName,
                    overriddenByXenogene = g.OverriddenByXenogene?.defName,
                }),
                xenogeneRecords = customizations.Genes?.Xenogenes.ConvertAll(g => new SaveRecordGeneV5() {
                    def = g.GeneDef?.defName,
                    overriddenByEndogene = g.OverriddenByEndogene?.defName,
                    overriddenByXenogene = g.OverriddenByXenogene?.defName,
                }),
            };
        }

        public void ConvertIdeo(SaveRecordPawnV5 result, CustomizationsPawn customizations) {
            if (!ModsConfig.IdeologyActive || customizations.Ideo == null) {
                return;
            }
            if (!Find.IdeoManager.classicMode) {
                Ideo ideo = customizations.Ideo;
                result.ideo = new SaveRecordIdeoV5() {
                    certainty = customizations.Certainty.Value,
                    name = ideo?.name,
                    sameAsColony = ideo == Find.FactionManager.OfPlayer.ideos.PrimaryIdeo,
                    culture = ideo?.culture.defName
                };
                if (ideo != null) {
                    result.ideo.memes = new List<string>(ideo.memes.Select(m => m.defName));
                }
            }
            else {
                result.ideo = new SaveRecordIdeoV5() {
                    sameAsColony = true,
                };
            }
            //Logger.Debug(string.Join(", ", customizedPawn.Pawn.ideo.Ideo?.memes.Select(m => m.defName)));
        }

        public void ConvertAbilities(SaveRecordPawnV5 result, CustomizationsPawn customizations) {
            if (customizations.Abilities != null) {
                result.abilities.AddRange(customizations.Abilities.Select(a => a.AbilityDef.defName));
            }
        }

        public void ConvertPossessions(SaveRecordPawnV5 result, CustomizationsPawn customizations) {
            if (!customizations.Possessions.NullOrEmpty()) {
                result.possessions = new List<SaveRecordPossessionV5>();
                foreach (var p in customizations.Possessions) {
                    if (p.ThingDef != null && p.Count > 0) {
                        result.possessions.Add(new SaveRecordPossessionV5() {
                            thingDef = p.ThingDef.defName,
                            count = p.Count,
                        });
                    }
                }
            }
        }

        public void ConvertTraits(SaveRecordPawnV5 result, CustomizationsPawn customizations) {
            foreach (var trait in customizations.Traits) {
                if (trait != null) {
                    result.traits.Add(new SaveRecordTraitV5() {
                        def = trait.TraitDef?.defName,
                        degree = trait.Degree
                    });
                }
            }
        }

        public void ConvertSkills(SaveRecordPawnV5 result, CustomizationsPawn customizations) {
            foreach (var skill in customizations.Skills) {
                result.skills.Add(new SaveRecordSkillV4() {
                    name = skill.SkillDef?.defName,
                    value = skill.Level,
                    passion = skill.Passion
                });
            }
        }

        public void ConvertApparel(SaveRecordPawnV5 result, CustomizationsPawn customizations) {
            foreach (var apparel in customizations.Apparel) {
                result.apparel.Add(new SaveRecordApparelV5() {
                    apparel = apparel.ThingDef?.defName,
                    style = apparel.StyleCategoryDef?.defName,
                    stuff = apparel.StuffDef?.defName,
                    quality = apparel.Quality.ToString(),
                    hitPoints = apparel.HitPoints,
                    color = apparel.Color
                });
            }
        }
        public void ConvertInjuries(SaveRecordPawnV5 result, CustomizationsPawn customizations, OptionsHealth healthOptions) {
            foreach (Injury injury in customizations.Injuries) {
                var saveRecord = new SaveRecordInjuryV5(injury);
                if (injury.BodyPartRecord != null) {
                    UniqueBodyPart part = healthOptions.FindBodyPartsForRecord(injury.BodyPartRecord);
                    if (part != null && part.Index > 0) {
                        saveRecord.bodyPartIndex = part.Index;
                    }
                }
                result.injuries.Add(saveRecord);
            }
        }

        public void ConvertImplants(SaveRecordPawnV5 result, CustomizationsPawn customizations, OptionsHealth healthOptions) {
            foreach (Implant implant in customizations.Implants) {
                var saveRecord = new SaveRecordImplantV5(implant);
                if (implant.BodyPartRecord != null) {
                    UniqueBodyPart part = healthOptions.FindBodyPartsForRecord(implant.BodyPartRecord);
                    if (part != null && part.Index > 0) {
                        saveRecord.bodyPartIndex = part.Index;
                    }
                }
                result.implants.Add(saveRecord);
            }
        }
    }
}
