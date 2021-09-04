﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    class ScenPart_CustomScatterThingsNearPlayerStart : ScenPart_ScatterThings {
        protected int radius = 4;
        public ScenPart_CustomScatterThingsNearPlayerStart() {
            // Set the def to match the standard scatter part that we'll be replacing with this one.
            // Doing so makes sure that this part gets sorted as expected when building the scenario description
            this.def = ScenPartDefOf.ScatterThingsNearPlayerStart;
        }
        public ThingDef ThingDef {
            get {
                return this.thingDef;
            }
            set {
                this.thingDef = value;
            }
        }
        public ThingDef StuffDef {
            get {
                return this.stuff;
            }
            set {
                this.stuff = value;
            }
        }
        public int Count {
            get {
                return this.count;
            }
            set {
                this.count = value;
            }
        }
        protected override bool NearPlayerStart {
            get {
                return true;
            }
        }
        public int Radius {
            get {
                return this.radius;
            }
            set {
                this.radius = value;
            }
        }
        public override void GenerateIntoMap(Map map) {
            if (Find.GameInitData == null) {
                return;
            }
            new GenStep_CustomScatterThings {
                nearPlayerStart = this.NearPlayerStart,
                thingDef = this.thingDef,
                stuff = this.stuff,
                count = this.count,
                spotMustBeStandable = true,
                minSpacing = 5f,
                clusterSize = ((this.thingDef.category != ThingCategory.Building) ? 4 : 1),
                radius = 4 + radius
            }.Generate(map, new GenStepParams());
        }
        public override string Summary(Scenario scen) {
            return ScenSummaryList.SummaryWithList(scen, "PlayerStartsWith", ScenPart_StartingThing_Defined.PlayerStartWithIntro);
        }
        public override IEnumerable<string> GetSummaryListEntries(string tag) {
            if (tag == "PlayerStartsWith") {
                List<string> entries = new List<string>();
                entries.Add(GenLabel.ThingLabel(thingDef, stuff, count).CapitalizeFirst());
                return entries;
            }
            else {
                return Enumerable.Empty<string>();
            }
        }
    }
}
