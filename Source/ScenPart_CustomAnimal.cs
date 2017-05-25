using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    class ScenPart_CustomAnimal : ScenPart {
        private float bondToRandomPlayerPawnChance = 0.5f;
        protected PawnKindDef animalKindDef = null;
        protected Gender gender = Gender.None;
        protected int count = 1;
        public PawnKindDef KindDef {
            get {
                return animalKindDef;
            }
            set {
                animalKindDef = value;
            }
        }
        public Gender Gender {
            get {
                return gender;
            }
            set {
                gender = value;
            }
        }
        public int Count {
            get {
                return count;
            }
            set {
                count = value;
            }
        }
        public override IEnumerable<Thing> PlayerStartingThings() {
            List<Thing> result = new List<Thing>();
            if (animalKindDef == null) {
                return result;
            }
            for (int i=0; i<count; i++) {
                Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequestWrapper() {
                    FixedGender = gender,
                    Faction = Faction.OfPlayer,
                    KindDef = animalKindDef,
                    Context = PawnGenerationContext.NonPlayer
                }.Request);
                if (pawn.Name == null || pawn.Name.Numerical) {
                    pawn.Name = PawnBioAndNameGenerator.GeneratePawnName(pawn, NameStyle.Full, null);
                }
                if (Rand.Value < bondToRandomPlayerPawnChance) {
                    Pawn bonded = Find.GameInitData.startingPawns.RandomElement<Pawn>();
                    if (!bonded.story.traits.HasTrait(TraitDefOf.Psychopath)) {
                        bonded.relations.AddDirectRelation(PawnRelationDefOf.Bond, pawn);
                    }
                }
                result.Add(pawn);
            }
            return result;
        }
    }
}
