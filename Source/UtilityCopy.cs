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
    // Provides utilities for creating deep copies of pawn properties.  Since most of these pawn-related entities
    // do not provide methods for creating deep copies, these utility methods take advantage of the IExposable interface
    // to serialize/deserialize objects via strings to create the copies.
    public static class UtilityCopy {
        // Serializes and then deserializes an instance of an IExposable class to create a deep copy.  The class must be constructable
        // with no arguments.
        public static T CopyExposable<T>(T type) where T : IExposable {
        string xml = "<doc>" + Scribe.saver.DebugOutputFor(type) + "</doc>";
            return CopyExposable<T>(type, null);
        }

        // Serializes and then deserializes an instance of an IExposable class to create a deep copy.  Instantiates the copy
        // with the provided constructor arguments.
        public static T CopyExposable<T>(T type, object[] constructorArgs) where T : IExposable {
            string xml = "<doc>" + Scribe.saver.DebugOutputFor(type) + "</doc>";
            //Log.Warning(xml);
            InitLoadFromString(xml);
            T result = default(T);
            Scribe_Deep.Look(ref result, "saveable", constructorArgs);
            Scribe.loader.FinalizeLoading();
            HashSet<IExposable> saveables = (HashSet<IExposable>)(typeof(PostLoadIniter).GetField("saveablesToPostLoad", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Scribe.loader.initer));
            saveables.Clear();
            return result;
        }

        // Given a field name, deep-copies an instance of an IExposable class from a source object to a target object via reflection.
        // Creates a deep copy by serializing and then deserializing the IExposable instance.  The class must be constructable
        // with no arguments.
        public static void CopyExposableViaReflection(string fieldName, object source, object target, object[] constructorArgs) {
            FieldInfo sourceField = source.GetType().GetField(fieldName);
            FieldInfo targetField = target.GetType().GetField(fieldName);
            if (sourceField != null && targetField != null) {
                object value = sourceField.GetValue(source);
                if (typeof(IExposable).IsAssignableFrom(value.GetType())) {
                    IExposable e = (IExposable)value;
                    IExposable copy = UtilityCopy.CopyExposable(e, constructorArgs);
                    targetField.SetValue(target, copy);
                }
            }
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
