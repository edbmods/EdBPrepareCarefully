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

        public static void SetNonPublicField(object target, string name, object value) {
            FieldInfo info = GetNonPublicField(target.GetType(), name);
            if (info == null) {
                Log.Warning("Prepare Carefully failed to set a value via reflection");
            }
            else {
                info.SetValue(target, value);
            }
        }
    }
}
