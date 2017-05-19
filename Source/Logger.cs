using System;
using Verse;

namespace EdB.PrepareCarefully {
    public class Logger {
        private bool debugEnabled = false;

        public bool DebugEnabled {
            get {
                return debugEnabled;
            }
            set {
                debugEnabled = value;
            }
        }

        public Logger() {
        }

        public Logger(bool debugEnabled) {
            this.debugEnabled = debugEnabled;
        }

        public void Debug(string message) {
            if (debugEnabled) {
                Log.Message(message);
            }
        }

        public void Message(string message) {
            Log.Message(message);
        }

        public void Warning(string message) {
            Log.Warning(message);
        }

        public void Error(string message) {
            Log.Error(message);
        }
    }
}

