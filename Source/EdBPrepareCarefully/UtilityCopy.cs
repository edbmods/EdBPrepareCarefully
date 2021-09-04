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
        public static T CopyExposable<T>(T type, Dictionary<string, IExposable> crossReferences = null) where T : IExposable {
        string xml = "<doc>" + Scribe.saver.DebugOutputFor(type) + "</doc>";
            return CopyExposable<T>(type, null, crossReferences);
        }

        // Serializes and then deserializes an instance of an IExposable class to create a deep copy.  Instantiates the copy
        // with the provided constructor arguments.  This probably won't work for objects with references.  The referenced objects
        // won't be saved unless they are defined in the same object, so they can't be loaded.
        public static T CopyExposable<T>(T type, object[] constructorArgs, Dictionary<string, IExposable> crossReferences = null) where T : IExposable {
            string xml = "<doc>" + Scribe.saver.DebugOutputFor(type) + "</doc>";
            //Logger.Debug(xml);
            InitLoadFromString(xml);
            if (crossReferences != null) {
                foreach (var pair in crossReferences) {
                    // If the cross references contain the same id as the thing that we're loading, we want to remove the object with the same
                    // id from the cross references.  If we don't, it will cause an error.
                    if (type is ILoadReferenceable loadReferenceable) {
                        if (pair.Key == loadReferenceable.GetUniqueLoadID()) {
                            continue;
                        }
                    }
                    Scribe.loader.crossRefs.RegisterForCrossRefResolve(pair.Value);
                }
            }
            T result = default(T);
            Scribe_Deep.Look(ref result, "saveable", constructorArgs);
            FinalizeLoading(crossReferences); 
            //Scribe.loader.FinalizeLoading();
            HashSet<IExposable> saveables = (HashSet<IExposable>)(typeof(PostLoadIniter).GetField("saveablesToPostLoad", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Scribe.loader.initer));
            saveables.Clear();
            return result;
        }

        // Serializes an instance of an IExposable class to a string. 
        public static string SerializeExposableToString<T>(T value) where T : IExposable {
            return Scribe.saver.DebugOutputFor(value);
        }

        // Deserializes an instance of an IExposable class from a string. 
        public static T DeserializeExposable<T>(string xml, object[] constructorArgs) where T : IExposable {
            xml = "<doc>" + xml + "</doc>";
            InitLoadFromString(xml);
            T result = default(T);
            Scribe_Deep.Look(ref result, "saveable", constructorArgs);
            FinalizeLoading();
            //Scribe.loader.FinalizeLoading();
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
                Logger.Error("Called InitLoading() but current mode is " + Scribe.mode);
                Scribe.ForceStop();
            }
            if (Scribe.loader.curParent != null) {
                Logger.Error("Current parent is not null in InitLoading");
                Scribe.loader.curParent = null;
            }
            if (Scribe.loader.curPathRelToParent != null) {
                Logger.Error("Current path relative to parent is not null in InitLoading");
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
                Logger.Error(string.Concat(new object[] {
                    "Exception while unmarshalling XML",
                    "\n",
                    ex
                }));
                Scribe.loader.ForceStop();
                throw;
            }
        }

        public static void FinalizeLoading(Dictionary<string, IExposable> crossReferences = null) {
            if (Scribe.mode != LoadSaveMode.LoadingVars) {
                Log.Error("Called FinalizeLoading() but current mode is " + Scribe.mode);
                return;
            }
            try {
                Scribe.ExitNode();
                Scribe.loader.curXmlParent = null;
                Scribe.loader.curParent = null;
                Scribe.loader.curPathRelToParent = null;
                Scribe.mode = LoadSaveMode.Inactive;
                ResolveAllCrossReferences(crossReferences);
                //Scribe.loader.crossRefs.ResolveAllCrossReferences();
                Scribe.loader.initer.DoAllPostLoadInits();
            }
            catch (Exception arg) {
                Log.Error("Exception in FinalizeLoading(): " + arg);
                Scribe.loader.ForceStop();
                throw;
            }
        }

        public static void ResolveAllCrossReferences(Dictionary<string, IExposable> crossReferences = null) {
            Scribe.mode = LoadSaveMode.ResolvingCrossRefs;
            using (List<IExposable>.Enumerator enumerator = Scribe.loader.crossRefs.crossReferencingExposables.GetEnumerator()) {
                while (enumerator.MoveNext()) {
                    ILoadReferenceable loadReferenceable = enumerator.Current as ILoadReferenceable;
                    if (loadReferenceable != null) {
                        LoadedObjectDirectory loadedObjectDirectory = ReflectionUtil.GetFieldValue<LoadedObjectDirectory>(Scribe.loader.crossRefs, "loadedObjectDirectory");
                        if (loadedObjectDirectory != null) {
                            loadedObjectDirectory.RegisterLoaded(loadReferenceable);
                        }
                        else {
                            Logger.Warning("Could not access CrossRefHandler.loadedObjectDirectory in our version of ResolveAllCrossReferences()");
                        }
                    }
                }
            }
            foreach (IExposable current in Scribe.loader.crossRefs.crossReferencingExposables) {
                try {
                    if (current is ILoadReferenceable loadReferenceable) {
                        if (crossReferences.ContainsKey(loadReferenceable.GetUniqueLoadID())) {
                            continue;
                        }
                    }
                    Scribe.loader.curParent = current;
                    Scribe.loader.curPathRelToParent = null;
                    current.ExposeData();
                }
                catch (Exception arg) {
                    Log.Warning("Could not resolve cross refs: " + arg);
                }
            }
            Scribe.loader.curParent = null;
            Scribe.loader.curPathRelToParent = null;
            Scribe.mode = LoadSaveMode.Inactive;
            Scribe.loader.crossRefs.Clear(false);
        }
    }
}
