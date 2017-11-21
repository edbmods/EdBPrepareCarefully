using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using System.Reflection;

namespace EdB.PrepareCarefully {
    public class Randomizer {
        private System.Random random = new System.Random();
        public System.Random Random {
            get {
                return random;
            }
        }
        public Pawn GenerateColonist() {
            Pawn result = PawnGenerator.GeneratePawn(new PawnGenerationRequestWrapper() {
            }.Request);
            return result;
        }

        public Pawn GeneratePawn(PawnGenerationRequest request) {
            Pawn result = PawnGenerator.GeneratePawn(request);
            return result;
        }

        public Pawn GenerateKindOfColonist(PawnKindDef kindDef) {
            Pawn result = PawnGenerator.GeneratePawn(new PawnGenerationRequestWrapper() {
                KindDef = kindDef
            }.Request);
            return result;
        }

        public Pawn GenerateKindOfPawn(PawnKindDef kindDef) {
            FactionDef factionDef = kindDef.defaultFactionType;
            if (factionDef == null) {
                factionDef = Faction.OfPlayer.def;
            }
            Faction faction = PrepareCarefully.Instance.Providers.Factions.GetFaction(factionDef);
            PawnGenerationRequest req = new PawnGenerationRequestWrapper() {
                Faction = faction,
                KindDef = kindDef
            }.Request;
            Pawn result = PawnGenerator.GeneratePawn(req);
            return result;
        }

        public Pawn GenerateKindAndGenderOfPawn(PawnKindDef kindDef, Gender gender) {
            FactionDef factionDef = kindDef.defaultFactionType;
            if (factionDef == null) {
                factionDef = Faction.OfPlayer.def;
            }
            Faction faction = PrepareCarefully.Instance.Providers.Factions.GetFaction(factionDef);
            PawnGenerationRequest req = new PawnGenerationRequestWrapper() {
                Faction = faction,
                KindDef = kindDef,
                FixedGender = gender
            }.Request;
            Pawn result = PawnGenerator.GeneratePawn(req);
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
            PawnKindDef kindDef = customPawn.Pawn.kindDef;
            FactionDef factionDef = kindDef.defaultFactionType;
            if (factionDef == null) {
                factionDef = Faction.OfPlayer.def;
            }
            MethodInfo method = typeof(PawnBioAndNameGenerator).GetMethod("FillBackstorySlotShuffled", BindingFlags.Static | BindingFlags.NonPublic);
            object[] arguments = new object[] { customPawn.Pawn, BackstorySlot.Adulthood, null, factionDef };
            method.Invoke(null, arguments);
            Backstory result = arguments[2] as Backstory;
            return result;
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

