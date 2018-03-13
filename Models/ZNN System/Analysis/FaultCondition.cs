using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
    /// <summary>
    /// A Faultcondition contains all information needed to activate a fault
    /// </summary>
    class FaultCondition
    {
        /// <summary>
        /// Every fault has his number (see ChooseServerToFault())
        /// </summary>
        public int faultNumber;

        /// <summary>
        /// Wait till stepToActivate to activate the fault
        /// </summary>
        public int stepToActivate;

        /// <summary>
        /// Chooses the server on which the fault occurs
        /// </summary>
        public int serverToFault; 
        
        /// <summary>
        /// creates an instance
        /// </summary>
        public FaultCondition(int faultNumber, int stepToActivate, int serverToFault)
        {
            this.faultNumber = faultNumber;
            this.stepToActivate = stepToActivate;
            this.serverToFault = serverToFault;
        }

        public string ToString()
        {
            return "fault:" + faultNumber + "step:" + stepToActivate + "server:" + serverToFault;
        }
        
        public bool Equals(FaultCondition otherFC)
        {
            return (this.faultNumber == otherFC.faultNumber && this.stepToActivate == otherFC.stepToActivate && this.serverToFault == otherFC.serverToFault);
        }
    }
}
