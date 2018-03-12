using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
    class LearningConstants
    {
        public int episodes = 0;
        public int steps = 0;
        public double alpha = 0;
        public double gamma = 0;
        public double epsilon = 0;
        public bool switchToSarsa = false;
        public bool epsilonDecrements = false;

        public LearningConstants(int episodes, int steps, double alpha, double gamma, double epsilon, bool switchToSarsa, bool epsilonDecrements)
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
