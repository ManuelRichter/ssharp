using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
    class FaultCondition
    {
        public int faultNumber;  //number of the fault to activate
        public int stepToActivate; //steps till activation of the fault
        public int serverToFault; //
        public bool faultActivated; //true if the fault has been activated 

        public FaultCondition(int faultNumber, int stepToActivate, int serverToFault)
        {
            this.faultNumber = faultNumber;
            this.stepToActivate = stepToActivate;
            this.serverToFault = serverToFault;
            this.faultActivated = false;
        } 

    }
}
