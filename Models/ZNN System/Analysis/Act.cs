

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
    /// <summary>
    /// Contains the fault and the server on which the fault occurs forming the action of the agent
    /// </summary>
    class Act
    {
        /// <summary>
        /// Every fault has a unique number (see ChooseServerToFault())
        /// </summary>
        int faultNumber;

        /// <summary>
        /// The number of the server which should receive a fault
        /// </summary>
        int serverToFault;

        /// <summary>
        /// Creates an instance of an action
        /// </summary>
        public Act(int faultNumber, int serverToFault)
        {
            this.faultNumber = faultNumber;
            this.serverToFault = serverToFault;
        }

        /// <summary>
        /// Returns the number of the fault
        /// </summary>
        public int GetFault()
        {
            return faultNumber;
        }

        /// <summary>
        /// Returns the number of the server which should receive the fault
        /// </summary>
        public int GetServer()
        {
            return serverToFault;
        }

        public string ToString()
        {
            return "action:" + faultNumber + " server:" + serverToFault;
        }

        /// <summary>
        /// Converts an action to a fault condition so the model is able to use it
        /// </summary>
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
