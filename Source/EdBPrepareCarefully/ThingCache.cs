using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace EdB.PrepareCarefully {
    public class ThingCache {
        public ThingCache() {
        }

        protected Dictionary<ThingDef, LinkedList<Thing>> cache = new Dictionary<ThingDef, LinkedList<Thing>>();

        public Thing Get(ThingDef thingDef) {
            return Get(thingDef, null);
        }

        public Thing Get(ThingDef thingDef, ThingDef stuffDef) {
            if (thingDef.MadeFromStuff && stuffDef == null) {
                stuffDef = GenStuff.DefaultStuffFor(thingDef);
            }
            LinkedList<Thing> cachedThings;
            if (cache.TryGetValue(thingDef, out cachedThings)) {
                LinkedListNode<Thing> thingNode = cachedThings.Last;
                if (thingNode != null) {
                    cachedThings.Remove(thingNode);
                    Thing result = thingNode.Value;
                    result.SetStuffDirect(stuffDef);
                    return result;
                }
            }
            return ThingMaker.MakeThing(thingDef, stuffDef);
        }

        public void Put(Thing thing) {
            ThingDef def = thing.def;
            LinkedList<Thing> cachedThings = null;
            if (!cache.TryGetValue(def, out cachedThings)) {
                cachedThings = new LinkedList<Thing>();
                cache.Add(def, cachedThings);
            }
            thing.SetQuality(QualityCategory.Normal);
            thing.HitPoints = thing.MaxHitPoints;
            cachedThings.AddLast(thing);
        }
    }
}
