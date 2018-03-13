using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using SafetySharp.CaseStudies.ZNNSystem.Modeling;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
    /// <summary>
    /// Contains the model and the set of all activated or soon to be activated fault conditions
    /// </summary>
    class FaultyModel
    {
        public Model model;
        public ArrayList fcs;
        
        public FaultyModel(Model m)
        {
            this.model = m;
            fcs = new ArrayList();
        }

        /// <summary>
        /// Adds a condition to activate a fault on a server at a specific step
        /// </summary>
        /// <param name="fc"> the fault which should be activated</param>
        public void AddFaultCondition(FaultCondition fc)
        {
            this.fcs.Add(fc);
        }
    }
}
