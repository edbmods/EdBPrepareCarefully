using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    public class ReflectionUtil {
        public static FieldInfo GetNonPublicField(Type type, string name) {
            FieldInfo info = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (info == null) {
                Log.Warning("Prepare Carefully could not find the field " + type.Name + "." + name + " via reflection");
            }
            return info;
        }

        public static FieldInfo GetNonPublicStaticField(Type type, string name) {
            FieldInfo info = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
            if (info == null) {
                Log.Warning("Prepare Carefully could not find the field " + type.Name + "." + name + " via reflection");
            }
            return info;
        }

        public static T GetNonPublicStatic<T>(Type type, string name) {
            FieldInfo field = GetNonPublicStaticField(type, name);
            if (field == null) {
                return default(T);
            }
            else {
                return (T)field.GetValue(null);
            }
        }

        public static FieldInfo GetNonPublicField(object target, string name) {
            return GetNonPublicField(target.GetType(), name);
        }

        public static void SetNonPublicField(object target, string name, object value) {
            FieldInfo info = GetNonPublicField(target.GetType(), name);
            if (info == null) {
                Log.Warning("Prepare Carefully failed to set a value to the field " + target.GetType().Name + "." + name + " via reflection");
            }
            else {
                info.SetValue(target, value);
            }
        }

        public static FieldInfo GetPublicField(object target, string name) {
            return GetPublicField(target.GetType(), name);
        }

        public static FieldInfo GetPublicField(Type type, string name) {
            FieldInfo info = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (info == null) {
                Log.Warning("Prepare Carefully could not find the field " + type.Name + "." + name + " via reflection");
            }
            return info;
        }

        public static void SetPublicField(object target, string name, object value) {
            FieldInfo info = GetPublicField(target.GetType(), name);
            if (info == null) {
                Log.Warning("Prepare Carefully failed to set a value to the field " + target.GetType().Name + "." + name + " via reflection");
            }
            else {
                info.SetValue(target, value);
            }
        }


        // Similar to Harmony's AccessTools.TypeByName() but with error handling around Assembly.GetTypes() to avoid
        // issues with bad assemblies.
        public static Type TypeByName(string name) {
            Type type = null;
            try {
                type = Type.GetType(name, false);
            }
            catch (Exception) {
                Log.Warning("Prepare Carefully encountered an error when trying to get type {" + name + "} using Type.GetType(name)");
            }
            if (type != null) {
                return type;
            }
            Assembly[] assemblies = null;
            try {
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
            }
            catch (Exception) {
                Log.Warning("Prepare Carefully encountered an error when trying to get the list of available assemblies");
                return null;
            }
            foreach (var assembly in assemblies) {
                try {
                    Type[] types = assembly.GetTypes();
                    type = types.FirstOrDefault(x => x.FullName == name);
                    if (type != null) {
                        return type;
                    }
                    else {
                        type = types.FirstOrDefault(x => x.Name == name);
                    }
                    if (type != null) {
                        return type;
                    }
                }
                catch (Exception) {
                    String assemblyNameAsString = "Unknown assembly";
                    try {
                        var assemblyName = assembly.GetName();
                        assemblyNameAsString = assemblyName.Name;
                    }
                    catch {
                        Log.Warning("Prepare Carefully ran into an error when trying to get an assembly name");
                    }
                    Log.Warning("Prepare Carefully was searching for the class {" + name + "}, but there was an error when getting the list of types "
                        + "from the assembly {" + assemblyNameAsString + "}.  Something might be broken in the mod to which that assembly belongs.  "
                        + "Skipping it to continue the search in the rest of the assemblies.");
                }
            }
            return null;
        }
    }
}
