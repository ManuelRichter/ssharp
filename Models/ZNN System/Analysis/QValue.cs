using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
    /// <summary>
    /// Contains the state-action pair with their respective long term rewards
    /// </summary>
    class QValue
    {
        /// <summary>
        /// The current state
        /// </summary>
        State state;

        /// <summary>
        /// The current action
        /// </summary>
        Act action;

        /// <summary>
        /// The long term reward for this state-action pair
        /// </summary>
        double reward;

        /// <summary>
        /// Creates an Instance
        /// </summary>
        public QValue(State state, Act act)
        {
            this.state = state;
            this.action = act;

            reward = 0.0f;
        }

        /// <summary>
        /// Updates the reward
        /// </summary>
        public void SetReward(double r)
        {
            reward = r;
        }

        /// <summary>
        /// Returns the reward
        /// </summary>
        public double GetReward()
        {
            return reward;
        }

        /// <summary>
        /// Returns the action
        /// </summary>
        public Act GetAction()
        {
            return action;
        }

        /// <summary>
        /// Checks if the current state matches state s
        /// </summary>
        public bool StateEquals(State s)
        {
            return this.state.Equals(s);
        }

        /// <summary>
        /// Returns the current Qvalue in a table like format
        /// </summary>
        public string ToTable()
        {
            return "State:" + "|" + this.state.ToString() + "|" + action.ToString() + "|" + reward + "|";
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
        
    }
}
