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

        public Pawn GenerateNpcKindOfColonist(PawnKindDef kindDef) {
            PawnGenerationRequest req = new PawnGenerationRequestWrapper() {
                KindDef = kindDef,
                Context = PawnGenerationContext.NonPlayer
            }.Request;
            Pawn result = PawnGenerator.GeneratePawn(req);
            return result;
        }

        public Pawn GenerateSameKindOfColonist(CustomPawn pawn) {
            return GenerateKindOfColonist(pawn.Pawn.kindDef);
        }

        public Pawn GenerateSameKindOfColonist(Pawn pawn) {
            return GenerateKindOfColonist(pawn.kindDef);
        }

        public void RandomizeAll(CustomPawn customPawn) {
            Pawn pawn = GenerateSameKindOfColonist(customPawn);
            customPawn.InitializeWithPawn(pawn);
        }

        public void RandomizeBackstory(CustomPawn customPawn) {
            MethodInfo method = typeof(PawnBioAndNameGenerator).GetMethod("SetBackstoryInSlot", BindingFlags.Static | BindingFlags.NonPublic);
            object[] arguments = new object[] { customPawn.Pawn, BackstorySlot.Childhood, null, Faction.OfPlayer.def };
            method.Invoke(null, arguments);
            customPawn.Childhood = arguments[2] as Backstory;
            arguments = new object[] { customPawn.Pawn, BackstorySlot.Adulthood, null, Faction.OfPlayer.def };
            method.Invoke(null, arguments);
            customPawn.Adulthood = arguments[2] as Backstory;
        }

        public static Backstory RandomAdulthood(CustomPawn customPawn) {
            MethodInfo method = typeof(PawnBioAndNameGenerator).GetMethod("SetBackstoryInSlot", BindingFlags.Static | BindingFlags.NonPublic);
            object[] arguments = new object[] { customPawn.Pawn, BackstorySlot.Adulthood, null, Faction.OfPlayer.def };
            method.Invoke(null, arguments);
            Backstory result = arguments[2] as Backstory;
            return result;
        }

        public void RandomizeTraits(CustomPawn customPawn) {
            Pawn pawn = GenerateSameKindOfColonist(customPawn);
            List<Trait> traits = pawn.story.traits.allTraits;
            customPawn.ClearTraits();
            foreach (var trait in traits) {
                customPawn.AddTrait(trait);
            }
        }

        public void RandomizeAppearance(CustomPawn customPawn) {
            Pawn pawn;
            int tries = 0;
            do {
                pawn = GenerateSameKindOfColonist(customPawn);
                tries++;
            }
            while (pawn.gender != customPawn.Gender && tries < 1000);

            customPawn.HairDef = pawn.story.hairDef;
            customPawn.SetColor(PawnLayers.Hair, pawn.story.hairColor);
            customPawn.HeadGraphicPath = pawn.story.HeadGraphicPath;
            customPawn.MelaninLevel = pawn.story.melanin;

            for (int i = 0; i < PawnLayers.Count; i++) {
                if (PawnLayers.IsApparelLayer(i)) {
                    customPawn.SetSelectedStuff(i, null);
                    customPawn.SetSelectedApparel(i, null);
                }
            }
            foreach (Apparel current in pawn.apparel.WornApparel) {
                int layer = PawnLayers.ToPawnLayerIndex(current.def.apparel);
                if (layer != -1) {
                    customPawn.SetSelectedStuff(layer, current.Stuff);
                    customPawn.SetSelectedApparel(layer, current.def);
                }
            }
        }

        public void RandomizeName(CustomPawn customPawn) {
            Pawn pawn = GenerateSameKindOfColonist(customPawn);
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
    }
}

