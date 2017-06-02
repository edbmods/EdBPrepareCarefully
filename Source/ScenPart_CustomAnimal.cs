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
        public PawnKindDef animalKindDef = null;
        protected Gender gender = Gender.None;
        public int count = 1;
        public ScenPart_CustomAnimal() {
            // Set the def to match the standard starting animal that we'll be replacing with this one.
            // Doing so makes sure that this part gets sorted as expected when building the scenario description
            this.def = ScenPartDefOf.StartingAnimal;
        }
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
        public override string Summary(Scenario scen) {
            return ScenSummaryList.SummaryWithList(scen, "PlayerStartsWith", ScenPart_StartingThing_Defined.PlayerStartWithIntro);
        }
        public override IEnumerable<string> GetSummaryListEntries(string tag) {
            if (tag == "PlayerStartsWith") {
                StringBuilder label = new StringBuilder();
                List<string> entries = new List<string>();
                if (this.KindDef.RaceProps.hasGenders) {
                    label.Append("PawnMainDescGendered".Translate(new object[] { this.gender.GetLabel(), this.KindDef.label }).CapitalizeFirst());
                }
                else {
                    label.Append(this.KindDef.label.CapitalizeFirst());
                }
                label.Append(" x");
                label.Append(count.ToString());
                entries.Add(label.ToString());
                return entries;
            }
            else {
                return Enumerable.Empty<string>();
            }
        }
        public override void ExposeData() {
            base.ExposeData();
            Scribe_Defs.Look<PawnKindDef>(ref this.animalKindDef, "animalKind");
            Scribe_Values.Look<int>(ref this.count, "count", 0, false);
            Scribe_Values.Look<float>(ref this.bondToRandomPlayerPawnChance, "bondToRandomPlayerPawnChance", 0f, false);
        }
    }
}
