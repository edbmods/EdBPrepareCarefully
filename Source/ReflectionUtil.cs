using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    public class ReflectionUtil {

        public static MethodInfo Method(Type type, string name) {
            MethodInfo info = type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
            if (info != null) {
                return info;
            }
            info = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (info != null) {
                return info;
            }
            info = type.GetMethod(name, BindingFlags.Public | BindingFlags.Static);
            if (info != null) {
                return info;
            }
            info = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
            if (info != null) {
                return info;
            }
            Logger.Warning("Could not find the method " + type.Name + "." + name + " via reflection");
            return null;
        }

        public static MethodInfo MethodWithParameters(Type type, string name, Type[] argumentTypes) {
            MethodInfo info = type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, argumentTypes, null);
            if (info != null) {
                return info;
            }
            info = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance, null, CallingConventions.Any, argumentTypes, null);
            if (info != null) {
                return info;
            }
            info = type.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, CallingConventions.Any, argumentTypes, null);
            if (info != null) {
                return info;
            }
            info = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static, null, CallingConventions.Any, argumentTypes, null);
            if (info != null) {
                return info;
            }
            Logger.Warning("Could not find the method " + type.Name + "." + name + " with the given parameters via reflection");
            return null;
        }

        public static MethodInfo RequiredMethod(Type type, string name, Type[] argumentTypes = null) {
            MethodInfo info = argumentTypes == null ? Method(type, name) : MethodWithParameters(type, name, argumentTypes);
            if (info != null) {
                return info;
            }
            throw new MissingMethodException("Prepare Carefully requires but could not find the method " + type.Name + "."
                + name + " via reflection");
        }

        public static PropertyInfo Property(Type type, string name) {
            PropertyInfo info = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (info != null) {
                return info;
            }
            info = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (info != null) {
                return info;
            }
            info = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Static);
            if (info != null) {
                return info;
            }
            info = type.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
            if (info != null) {
                return info;
            }
            Logger.Warning("Could not find the property " + type.Name + "." + name + " via reflection");
            return null;
        }

        public static FieldInfo Field(Type type, string name) {
            FieldInfo info = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (info != null) {
                return info;
            }
            info = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (info != null) {
                return info;
            }
            info = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
            if (info != null) {
                return info;
            }
            info = type.GetField(name, BindingFlags.Public | BindingFlags.Static);
            if (info != null) {
                return info;
            }
            Logger.Warning("Could not find the field " + type.Name + "." + name + " via reflection");
            return null;
        }

        public static FieldInfo RequiredField(Type type, string name) {
            FieldInfo info = Field(type, name);
            if (info != null) {
                return info;
            }
            throw new MissingFieldException("Prepare Carefully requires but could not find the field " + type.Name + "."
                + name + " via reflection");
        }


        public static FieldInfo GetNonPublicField(Type type, string name) {
            FieldInfo info = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (info == null) {
                Logger.Warning("Could not find the field " + type.Name + "." + name + " via reflection");
            }
            return info;
        }

        public static FieldInfo GetNonPublicStaticField(Type type, string name) {
            FieldInfo info = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
            if (info == null) {
                Logger.Warning("Could not find the field " + type.Name + "." + name + " via reflection");
            }
            return info;
        }

        public static T InvokeNonPublicStaticMethod<T>(Type type, string name, object[] arguments) {
            MethodInfo info = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
            if (info == null) {
                Logger.Warning("Could not find the static method " + type.Name + "." + name + " via reflection");
                return default(T);
            }
            object result = info.Invoke(null, arguments);
            return (T)result;
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
                Logger.Warning("Failed to set a value to the field " + target.GetType().Name + "." + name + " via reflection");
            }
            else {
                info.SetValue(target, value);
            }
        }

        public static FieldInfo GetPublicField(object target, string name) {
            return GetPublicField(target.GetType(), name);
        }

        public static T GetFieldValue<T>(object target, string name) {
            if (target == null) {
                Logger.Warning("Could not get value from field {" + name + "} using reflection because the target was null");
                return default(T);
            }
            FieldInfo field = Field(target.GetType(), name);
            if (field == null) {
                Logger.Warning("Could not get value from field {" + name + "} using reflection because we could not find the field on the target's type");
                return default(T);
            }
            object o = field.GetValue(target);
            if (o == null) {
                //Logger.Debug("Got the value from field {" + name + "} using reflection, but it was null");
                return default(T);
            }
            if (typeof(T).IsAssignableFrom(o.GetType())) {
                return (T)field.GetValue(target);
            }
            else {
                Logger.Warning("Could not cast the value from field {" + name + "} whose type is {" + o.GetType().FullName + "} to the specified type {" + typeof(T).FullName + "}");
            }
            return default(T);
        }

        public static T GetPropertyValue<T>(object target, string name) {
            if (target == null) {
                Logger.Warning("Could not get value from property {" + name + "} using reflection because the target was null");
                return default(T);
            }
            PropertyInfo property = Property(target.GetType(), name);
            if (property == null) {
                Logger.Warning("Could not get value from property {" + name + "} using reflection because we could not find the property on the target's type");
                return default(T);
            }
            object o = property.GetValue(target);
            if (o == null) {
                //Logger.Debug("Got the value from property {" + name + "} using reflection, but it was null");
                return default(T);
            }
            if (typeof(T).IsAssignableFrom(o.GetType())) {
                return (T)o;
            }
            else {
                Logger.Warning("Could not cast the value from property {" + name + "} whose type is {" + o.GetType().FullName + "} to the specified type {" + typeof(T).FullName + "}");
            }
            return default(T);
        }

        public static T GetStaticPropertyValue<T>(Type target, string name) {
            if (target == null) {
                Logger.Warning("Could not get value from property {" + name + "} using reflection because the target was null");
                return default;
            }
            PropertyInfo property = Property(target, name);
            if (property == null) {
                Logger.Warning("Could not get value from property {" + name + "} using reflection because we could not find the property on the target's type");
                return default;
            }
            object o = property.GetValue(null);
            if (o == null) {
                //Logger.Debug("Got the value from property {" + name + "} using reflection, but it was null");
                return default;
            }
            if (typeof(T).IsAssignableFrom(o.GetType())) {
                return (T)o;
            }
            else {
                Logger.Warning("Could not cast the value from property {" + name + "} whose type is {" + o.GetType().FullName + "} to the specified type {" + typeof(T).FullName + "}");
            }
            return default;
        }

        public static void InvokeActionMethod(object target, string name, object[] args = null) {
            MethodInfo method = Method(target.GetType(), name);
            if (method == null) {
                //Logger.Debug("Could not invoke method with {" + name + "}.  Method not found on type {" + target.GetType() + "}");
            }
            method.Invoke(target, args ?? new object[] { });
        }

        public static FieldInfo GetPublicField(Type type, string name) {
            FieldInfo info = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (info == null) {
                Logger.Warning("Could not find the field " + type.Name + "." + name + " via reflection");
            }
            return info;
        }

        public static void SetFieldValue(object target, string name, object value) {
            if (target == null) {
                Logger.Warning("Failed to set a value to the field " + target.GetType().Name + "." + name + " via reflection because the target object was null");
                return;
            }
            FieldInfo info = Field(target.GetType(), name);
            if (info == null) {
                Logger.Warning("Failed to set a value to the field " + target.GetType().Name + "." + name + " via reflection");
            }
            else {
                info.SetValue(target, value);
            }
        }

        public static void SetPublicField(object target, string name, object value) {
            FieldInfo info = GetPublicField(target.GetType(), name);
            if (info == null) {
                Logger.Warning("Failed to set a value to the field " + target.GetType().Name + "." + name + " via reflection");
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
                Logger.Warning("Encountered an error when trying to get type {" + name + "} using Type.GetType(name)");
            }
            if (type != null) {
                return type;
            }
            Assembly[] assemblies = null;
            try {
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
            }
            catch (Exception) {
                Logger.Warning("Encountered an error when trying to get the list of available assemblies");
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
                        Logger.Warning("Ran into an error when trying to get an assembly name");
                    }
                    Logger.Warning("While searching for the class {" + name + "}, there was an error when getting the list of types "
                        + "from the assembly {" + assemblyNameAsString + "}.  Something might be broken in the mod to which that assembly belongs.  "
                        + "Skipping it to continue the search in the rest of the assemblies.");
                }
            }
            return null;
        }
    }
}
