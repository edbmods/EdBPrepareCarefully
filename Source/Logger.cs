using System;
using Verse;

namespace EdB.PrepareCarefully {
    public static class Logger {
        private static readonly bool DebugEnabled = false;
        private static readonly string Prefix = "[Prepare Carefully] ";

        public static void Debug(string message) {
            if (DebugEnabled) {
                Log.Message("<color='#33ff33'>" + message + "</color>", true);
            }
        }

        public static void Message(string message) {
            Log.Message(Prefix + message);
        }

        public static void Warning(string message) {
            Log.Warning(Prefix + message);
        }

        public static void Warning(string message, Exception e) {
            Log.Warning(Prefix + message + "\n" + e);
        }

        public static void Error(string message) {
            Log.Error(Prefix + message);
        }

        public static void Error(string message, Exception e) {
            Log.Error(Prefix + message + "\n" + e);
        }
    }
}

