

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
