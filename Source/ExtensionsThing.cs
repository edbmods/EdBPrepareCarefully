using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public static class ExtensionsThing {
        public static Color GetColor(this Thing thing) {
            ThingWithComps thingWithComps = thing as ThingWithComps;
            if (thingWithComps == null) {
                return thing.DrawColor;
            }
            CompColorable comp = thingWithComps.GetComp<CompColorable>();
            if (comp == null) {
                return thing.DrawColor;
            }
            return comp.Color;
        }
        public static QualityCategory GetQuality(this Thing thing) {
            MinifiedThing minifiedThing = thing as MinifiedThing;
            CompQuality compQuality = (minifiedThing == null) ? thing.TryGetComp<CompQuality>() : minifiedThing.InnerThing.TryGetComp<CompQuality>();
            if (compQuality == null) {
                return QualityCategory.Normal;
            }
            return compQuality.Quality;
        }
        public static void SetQuality(this Thing thing, QualityCategory quality) {
            MinifiedThing minifiedThing = thing as MinifiedThing;
            CompQuality compQuality = (minifiedThing == null) ? thing.TryGetComp<CompQuality>() : minifiedThing.InnerThing.TryGetComp<CompQuality>();
            if (compQuality != null) {
                typeof(CompQuality).GetField("qualityInt", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(compQuality, quality);
            }
        }
    }
}
