using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using System.Reflection;

namespace EdB.PrepareCarefully {
    public class Randomizer {
        public static readonly int MaxAttempts = 10;
        private System.Random random = new System.Random();
        public System.Random Random {
            get {
                return random;
            }
        }

        protected Pawn AttemptToGeneratePawn(PawnGenerationRequest request) {
            Exception lastException = null;
            Faction savedRequestFaction = request.Faction;
            for (int i = 0; i < MaxAttempts; i++) {
                try {
                    Pawn pawn = PawnGenerator.GeneratePawn(request);

                    // We're trying to always generate pawns with a faction to avoid issues with some mods that patch pawn generation
                    // but don't bother to do null checks on the faction.  Unfortunately, setting the faction to the colony faction
                    // when creating a pawn from a different pawn kind results in bad apparel generation, i.e. synthread tribalwear
                    // or too much missing clothing.  So after we generate the pawn with the colony faction, we temporarily set the
                    // pawns faction to null and regenerate the apparel.
                    pawn.apparel?.DestroyAll(DestroyMode.Vanish);
                    pawn.equipment?.DestroyAllEquipment(DestroyMode.Vanish);
                    pawn.inventory?.DestroyAll(DestroyMode.Vanish);
                    request.Faction = null;
                    Faction faction = pawn.Faction;
                    // We only need to null out the faction if the pawn kind def is not the colony's pawn kind def.
                    if (request.KindDef != null && request.KindDef.defaultFactionType != Find.World.factionManager.OfPlayer.def) {
                        pawn.SetFaction(null);
                    }
                    PawnApparelGenerator.Reset();
                    PawnApparelGenerator.GenerateStartingApparelFor(pawn, request);
                    if (pawn.Faction != faction) {
                        pawn.SetFaction(faction);
                    }
                    return pawn;
                }
                catch (Exception e) {
                    lastException = e;
                }
                finally {
                    request.Faction = savedRequestFaction;
                }
            }
            throw lastException;
        }

        public Pawn GenerateColonist() {
            Pawn result = AttemptToGeneratePawn(new PawnGenerationRequestWrapper() {
                KindDef = Find.FactionManager.OfPlayer.def.basicMemberKind,
                Faction = Find.FactionManager.OfPlayer,
                Context = PawnGenerationContext.PlayerStarter
            }.Request);
            return result;
        }

        public Pawn GenerateColonistAsCloseToAsPossible(Pawn pawn) {
            var request = new PawnGenerationRequestWrapper() {
                KindDef = pawn.kindDef,
                Faction = pawn.Faction,
                FixedBiologicalAge = pawn.ageTracker.AgeBiologicalYears,
                FixedChronologicalAge = pawn.ageTracker.AgeChronologicalYears,
                FixedGender = pawn.gender,
                FixedIdeology = pawn.Ideo
            };
            Pawn result = AttemptToGeneratePawn(request.Request);
            return result;
        }

        public Pawn GeneratePawn(PawnGenerationRequest request) {
            Pawn result = AttemptToGeneratePawn(request);
            return result;
        }

        public Pawn GenerateKindOfColonist(PawnKindDef kindDef) {
            Pawn result = AttemptToGeneratePawn(new PawnGenerationRequestWrapper() {
                Faction = Find.FactionManager.OfPlayer,
                KindDef = kindDef
            }.Request);
            return result;
        }

        public Pawn GenerateKindOfPawn(PawnKindDef kindDef) {
            var wrapper = new PawnGenerationRequestWrapper() {
                Faction = Find.FactionManager.OfPlayer,
                KindDef = kindDef
            };
            Ideo ideo = Find.FactionManager.OfPlayer.ideos.GetRandomIdeoForNewPawn();
            if (ideo != null) {
                wrapper.FixedIdeology = ideo;
            }
            Pawn pawn = AttemptToGeneratePawn(wrapper.Request);
            return pawn;
        }

        public Pawn GenerateKindAndGenderOfPawn(PawnKindDef kindDef, Gender gender) {
            var wrapper = new PawnGenerationRequestWrapper() {
                Faction = Find.FactionManager.OfPlayer,
                KindDef = kindDef,
                FixedGender = gender
            };
            Ideo ideo = Find.FactionManager.OfPlayer.ideos.GetRandomIdeoForNewPawn();
            if (ideo != null) {
                wrapper.FixedIdeology = ideo;
            }
            Pawn result = AttemptToGeneratePawn(wrapper.Request);
            return result;
        }

        public Pawn GenerateSameKindAndGenderOfPawn(CustomPawn customPawn) {
            return GenerateKindAndGenderOfPawn(customPawn.Pawn.kindDef, customPawn.Gender);
        }

        public Pawn GenerateSameKindOfPawn(CustomPawn customPawn) {
            return GenerateKindOfPawn(customPawn.Pawn.kindDef);
        }

        public Pawn GenerateSameKindOfPawn(Pawn pawn) {
            return GenerateKindOfPawn(pawn.kindDef);
        }

        public static Backstory RandomAdulthood(CustomPawn customPawn) {
            return PrepareCarefully.Instance.Providers.Backstories.GetAdulthoodBackstoriesForPawn(customPawn).RandomElement();
        }

        public void RandomizeName(CustomPawn customPawn) {
            Pawn pawn = GenerateSameKindOfPawn(customPawn);
            pawn.gender = customPawn.Gender;
            Name name = PawnBioAndNameGenerator.GeneratePawnName(pawn, NameStyle.Full, null);
            NameTriple nameTriple = name as NameTriple;
            customPawn.Name = nameTriple;
        }

        public CustomPawn PickBondedPawnForPet(IEnumerable<CustomPawn> pawns) {
            if (pawns == null || pawns.Count() == 0) {
                return null;
            }
            double chanceOfBonding = 0.5;
            if (this.random.NextDouble() < chanceOfBonding) {
                return null;
            }
            CustomPawn pawn = pawns.RandomElement();
            if (pawn.Traits.FirstOrDefault((Trait trait) => {
                return trait.def.defName == "Psychopath";
            }) != null) {
                return null;
            }
            return pawns.RandomElement();
        }

        public Pawn EmptyPawn(PawnKindDef kindDef) {
            Pawn result = (Pawn)ThingMaker.MakeThing(kindDef.race, null);
            result.kindDef = kindDef;
            if (kindDef.RaceProps.hasGenders) {
                if (this.random.Next(2) == 0) {
                    result.gender = Gender.Male;
                }
                else {
                    result.gender = Gender.Female;
                }
            }
            else {
                result.gender = Gender.None;
            }
            PawnComponentsUtility.CreateInitialComponents(result);
            return result;
        }
    }
}

