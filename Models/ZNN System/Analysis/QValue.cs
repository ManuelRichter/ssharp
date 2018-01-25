using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
    class QValue
    {
        int step;
        Act action;

        double reward;

        public QValue(int step,int nextFault, int nextServer) //State (step) Action (nextFault,nextServer)
        {
            this.step = step;
            this.action = new Act(nextFault,nextServer);

            reward = 0.0f;
        }

        public QValue(int step, Act act) //State (step) Action (nextFault,nextServer)
        {
            this.step = step;
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
            return "step:" + step.ToString() + " " + action.ToString();
        }

        public bool Equals(int step, Act action)
        {
            return this.step == step && this.action.Equals(action);
        }

        public bool Equals(QValue q)
        {
            return this.step == q.step && this.action.Equals(q.action);
        }

        public bool Equals(int step)
        {
            return this.step == step;
        }

        public Act GetAction()
        {
            return action;
        }

        public string ToTable()
        {
            if (reward != 0)
                return "step:" + "|" + this.step + "|" + action.ToString() + "|" + reward + "|";
            return "";
        }
    }
}
