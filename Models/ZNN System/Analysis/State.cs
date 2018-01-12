using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using SafetySharp.CaseStudies.ZNNSystem.Modeling;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
    class State
    {
        public Model model;
        public ArrayList fcs;
        
        public State(Model m)
        {
            this.model = m;
            fcs = new ArrayList();
        }
        /// <summary>
        /// Adds a condition to activate a fault on a server at a specific step
        /// </summary>
        /// <param name="faultNumber">number of the fault to activate</param>
        /// <param name="stepToActivate">number of steps to wait till activation of the fault</param>
        /// <param name="serverToFault">number of the server on which the fault should be activated</param>
        public void AddFaultCondition(int faultNumber, int stepToActivate, int serverToFault)
        {
            fcs.Add(new FaultCondition(faultNumber, stepToActivate, serverToFault));
            Console.WriteLine("Added Fault " + faultNumber + " at " + stepToActivate + ". step on Server" + serverToFault);
        }
    }
}
