using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
    class QValue
    {
        FaultCondition fc;
        Act action;

        double reward;

        public QValue(int fault,int step,int server,int nextFault, int nextServer) //State (fault,step,server) Action (nextFault,nextServer)
        {
            this.fc = new FaultCondition(fault,step,server);
            this.action = new Act(nextFault,nextServer);

            reward = 0.0f;
        }

        public QValue(FaultCondition fc, Act act) //State (fault,step,server) Action (nextFault,nextServer)
        {
            this.fc = fc;
            this.action = act;

            reward = 0.0f;
        }

        public void SetReward(double r)
        {
            reward = r;
        }

        public double GetReward()
        {
            return reward;
        }

        public string ToString()
        {
            return fc.ToString() + " " + action.ToString();
        }

        public bool Equals(FaultCondition otherFC, Act action)
        {
            return this.fc.Equals(otherFC) && this.action.Equals(action);
        }

        public bool Equals(QValue q)
        {
            return this.fc.Equals(q.fc) && this.action.Equals(q.action);
        }

        public bool Equals(FaultCondition otherFC)
        {
            return this.fc.Equals(otherFC);
        }

        public Act getAction()
        {
            return action;
        }

        public string ToTable()
        {
            if (reward != 0)  return "fc:" + "|" + fc.faultNumber + "|" + fc.stepToActivate + "|" + fc.serverToFault + "|" + action.ToString() + "|" + reward;
            return "";
        }
    }
}
