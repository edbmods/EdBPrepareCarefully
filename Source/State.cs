using System;
using System.Collections.Generic;
using System.Linq;

namespace EdB.PrepareCarefully {
    public class State {
        protected int currentPawnIndex;

        protected List<string> errors = new List<string>();
        protected List<string> messages = new List<string>();

        public Page_PrepareCarefully Page {
            get;
            set;
        }

        public int CurrentPawnIndex {
            get {
                return currentPawnIndex;
            }
            set {
                currentPawnIndex = value;
            }
        }

        public List<CustomPawn> Pawns {
            get {
                return PrepareCarefully.Instance.Pawns;
            }
        }

        public CustomPawn CurrentPawn {
            get {
                return PrepareCarefully.Instance.Pawns[currentPawnIndex];
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

