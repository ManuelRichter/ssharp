using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
    class Act
    {
        int faultNumber;
        int serverToFault;

        public Act(int faultNumber, int serverToFault)
        {
            this.faultNumber = faultNumber;
            this.serverToFault = serverToFault;
        }

        public int GetFault()
        {
            return faultNumber;
        }

        public string ToString()
        {
            return "action:" + faultNumber + " server:" + serverToFault;
        }

        public FaultCondition ConvertToFC(int step)
        {
            return new FaultCondition(this.faultNumber,step,this.serverToFault);
        }

        public bool Equals(Act otherAct)
        {
            return this.faultNumber == otherAct.faultNumber && this.serverToFault == otherAct.serverToFault;
        }
    }
}
