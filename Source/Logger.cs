using System;
using Verse;

namespace EdB.PrepareCarefully {
    public static class Logger {
        private static readonly string Prefix = "[Prepare Carefully] ";
        private static readonly bool DebugEnabled = false;

        public static void Debug(string message) {
            if (DebugEnabled) {
                Log.Message(message);
            }
        }

        public static void Message(string message) {
            Log.Message(Prefix + message);
        }

        public static void Warning(string message) {
            Log.Warning(Prefix + message);
        }

        public static void Error(string message) {
            Log.Error(Prefix + message);
        }
    }
}

