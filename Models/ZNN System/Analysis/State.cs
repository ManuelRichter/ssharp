using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
    /// <summary>
    /// A state is an binary representation of the activated or not activated faults of this model
    /// </summary>
    class State
    {
        /// <summary>
        /// carries one bool for each fault
        /// </summary>
        bool[] fault = new bool[7];

        /// <summary>
        /// Initialises a state
        /// </summary>
        public State(bool[] fault)
        {
            this.fault = fault;
        }

        /// <summary>
        /// Initialises a state with default values
        /// </summary>
        public State()
        {
            this.fault = new bool[] { false, false, false, false, false, false, false };
        }

        /// <summary>
        /// Creates a shallow copy of the state c and updates it with action a
        /// </summary>
        public State(State c, Act a)
        {
            fault = c.Copy();
            SetFault(a.GetFault(), true);
        }

        /// <summary>
        /// sets an fault active or not active
        /// </summary>
        /// <paramref name="nr"/> number of the fault to de/activate
        public void SetFault(int nr, bool b)
        {
            fault[nr] = b;
        }

        /// <summary>
        /// Returns the state as new array
        /// </summary>
        public bool[] Copy()
        {
            bool[] b = new bool[this.fault.Length];
            this.fault.CopyTo(b,0);
            return b;
        }

        public bool Equals(State s)
        {
            for (int i = 0; i < fault.Length; i++)
            {
                if (this.fault[i] != s.fault[i]) return false;
            }
            return true;
        }

        public string ToString()
        {
            string s = "";

            for (int i = 0; i < fault.Length; i++) s = s + Convert.ToInt16(this.fault[i]);

            return s;
        }
    }
}
