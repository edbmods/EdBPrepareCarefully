using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace EdB.PrepareCarefully {
    public class State {
        protected CustomPawn currentPawn;
        
        protected List<string> errors = new List<string>();
        protected List<string> messages = new List<string>();
        private List<string> missingWorkTypes = null;

        public Page_PrepareCarefully Page {
            get;
            set;
        }
        
        public List<CustomPawn> Pawns {
            get {
                return PrepareCarefully.Instance.Pawns;
            }
        }

        public CustomPawn CurrentPawn {
            get {
                return currentPawn;
            }
            set {
                currentPawn = value;
            }
        }

        public ITabView CurrentTab {
            get;
            set;
        }

        public IEnumerable<string> Errors {
            get {
                return errors;
            }
        }

        public void AddError(string error) {
            this.errors.Add(error);
        }

        public List<string> MissingWorkTypes {
            get {
                return missingWorkTypes;
            }
            set {
                missingWorkTypes = value;
            }
        }

        public void ClearErrors() {
            this.errors.Clear();
        }

        public IEnumerable<string> Messages {
            get {
                return messages;
            }
        }

        public void AddMessage(string message) {
            this.messages.Add(message);
        }

        public void ClearMessages() {
            this.messages.Clear();
        }
    }
}

