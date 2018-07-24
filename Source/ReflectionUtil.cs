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
    }
}
