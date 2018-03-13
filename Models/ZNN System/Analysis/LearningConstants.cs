using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
    class LearningParam
    {
        /// <summary>
        /// The count of episodes to simulate the model in
        /// </summary>
        public int episodes = 0;

        /// <summary>
        /// The count of steps per episode
        /// </summary>
        public int steps = 0;

        /// <summary>
        /// The learning rate
        /// </summary>
        public double alpha = 0;

        /// <summary>
        /// The discount factor
        /// </summary>
        public double gamma = 0;

        /// <summary>
        /// The exploration rate, 1-epsilon is the rate of exploitation
        /// </summary>
        public double epsilon = 0;
        
        /// <summary>
        /// Use SARSA to calculate the optimal sequence of faults else use QLearning
        /// </summary>
        public bool switchToSarsa = false;
        
        /// <summary>
        /// The epsilon decreases over the course of the steps
        /// </summary>
        public bool epsilonDecrements = false;

        /// <summary>
        /// Creates the instance
        /// </summary>
        /// <param name="episodes">Count of episodes to simulate the model in</param>
        /// <param name="steps">Count of steps per episode</param>
        /// <param name="alpha">Learning rate</param>
        /// <param name="gamma">Discount factor</param>
        /// <param name="epsilon">Exploration rate</param>
        /// <param name="switchToSarsa">Use SARSA to calculate the optimal sequence of faults, else use Qlearning</param>
        /// <param name="epsilonDecrements">Epsilon decreases over the course of the steps</param>        
        public LearningParam(int episodes, int steps, double alpha, double gamma, double epsilon, bool switchToSarsa, bool epsilonDecrements)
        {
            this.episodes = episodes;
            this.steps = steps;
            this.alpha = alpha;
            this.gamma = gamma;
            this.epsilon = epsilon;
            this.switchToSarsa = switchToSarsa;
            this.epsilonDecrements = epsilonDecrements;
        }

    }
}
