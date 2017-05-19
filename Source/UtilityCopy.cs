using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public static class UtilityCopy {

        // Serializes and then deserializes an instance of a pawn tracker class (i.e. Pawn_StoryTracker, Pawn_HealthTracker)
        // to create a complete deep copy.  Admittedly, it feels like a bit of hack, but it gives a guaranteed way to
        // accomplish the copy.
        public static T CopyTrackerForPawn<T>(T tracker, Pawn pawn) where T : IExposable {
            string xml = "<doc>" + Scribe.saver.DebugOutputFor(tracker) + "</doc>";
            //Log.Warning(xml);
            InitLoadFromString(xml);
            T result = default(T);
            Scribe_Deep.Look(ref result, "saveable", new object[] { pawn });
            Scribe.loader.FinalizeLoading();
            HashSet<IExposable> saveables = (HashSet<IExposable>)(typeof(PostLoadIniter).GetField("saveablesToPostLoad", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Scribe.loader.initer));
            saveables.Clear();
            return result;
        }

        // Performs the setup needed to begin a scribe loading operation, similar to ScribeLoader.InitLoad(), but
        // uses a StringReader instead of reading from a file.
        private static void InitLoadFromString(String value) {
            if (Scribe.mode != LoadSaveMode.Inactive) {
                Log.Error("Called InitLoading() but current mode is " + Scribe.mode);
                Scribe.ForceStop();
            }
            if (Scribe.loader.curParent != null) {
                Log.Error("Current parent is not null in InitLoading");
                Scribe.loader.curParent = null;
            }
            if (Scribe.loader.curPathRelToParent != null) {
                Log.Error("Current path relative to parent is not null in InitLoading");
                Scribe.loader.curPathRelToParent = null;
            }
            try {
                using (TextReader textReader = new StringReader(value)) {
                    using (XmlTextReader xmlTextReader = new XmlTextReader(textReader)) {
                        XmlDocument xmlDocument = new XmlDocument();
                        xmlDocument.Load(xmlTextReader);
                        Scribe.loader.curXmlParent = xmlDocument.DocumentElement;
                    }
                }
                Scribe.mode = LoadSaveMode.LoadingVars;
            }
            catch (Exception ex) {
                Log.Error(string.Concat(new object[] {
                    "Exception while unmarshalling XML",
                    "\n",
                    ex
                }));
                Scribe.loader.ForceStop();
                throw;
            }
        }
    }
}
