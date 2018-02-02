using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
    class State
    {
        bool[] fault = new bool[7];

        public State(bool[] fault)
        {
            this.fault = fault;
        }

        public State(State c, Act a)
        {
            fault = c.Copy();
            SetFault(a.GetFault(), true);
        }

        public void SetFault(int nr, bool b)
        {
            fault[nr] = b;
        }
        
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
