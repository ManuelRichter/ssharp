using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
    class QValue
    {
        State state;
        Act action;

        double reward;
        
        public QValue(State state, Act act)
        {
            this.state = state;
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
        
        public override string ToString()
        {
            return state.ToString() + " " + action.ToString();
        }

        public bool Equals(State state, Act action)
        {
            return this.state.Equals(state) && this.action.Equals(action);
        }

        public bool Equals(QValue q)
        {

            return this.state.Equals(q.state) && this.action.Equals(q.action);
        }
        
        public Act GetAction()
        {
            return action;
        }

        public bool StateEquals(State s)
        {
            return this.state.Equals(s);
        }

        public string ToTable()
        {
            //if (reward != 0)
                return "State:" + "|" + this.state.ToString() + "|" + action.ToString() + "|" + reward + "|";
            //return "";
        }
    }
}
