using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
    /// <summary>
    /// Measures the covered model branches and serves as measurement for the learning algorithms
    /// </summary>
    public class BranchCoverage
    {
        /// <summary>
        /// Counts the cumulative coverage for an episode
        /// </summary>
        private static double coverage = 0;

        /// <summary>
        /// The total branch count observed in the model
        /// </summary>
        private static int totalBranchCount = 53;
        
        /// <summary>
        /// Contains a list of all visited branches per episode
        /// </summary>
        private static ArrayList pastBranches = new ArrayList();
        
        /// <summary>
        /// Deactivates covering measurement till init of the model is done
        /// </summary>
        private static bool isCovering = false;

        /// <summary>
        /// Contains the coverage value of the last step
        /// </summary>
        private static double oldCoverage = 0;

        /// <summary>
        /// Increments coverage if the visited branch is new
        /// </summary>
        /// <param name="nr"> An individual number for this branch</param>
        public static void IncrementCoverage(int nr)
        {
            if (!isCovering) return;
            
            for (int i = 0; i < pastBranches.Count; i++)
            {
                if ((int) pastBranches[i] == nr) return;
            }
            pastBranches.Add(nr);
            coverage++;
        }

        /// <summary>
        /// After initialisation of the model is done start covering measurements
        /// </summary>
        public static void StartCovering()
        {
            isCovering = true;
        }

        /// <summary>
        /// After an episode ends reset coverage
        /// </summary>
        private static void ResetCoverage()
        {
            coverage = 0;
            oldCoverage = 0;
        }

        /// <summary>
        /// Empties the visited branches and deactivates measurement till next episode begins
        /// </summary>
        public static void ResetPastBranches()
        { 
            pastBranches.Clear();
            ResetCoverage();
            isCovering = false;
        }

        /// <summary>
        /// Returns the "per step" coverage as reward
        /// </summary>
        public static double GetReward()
        {           
            double c = (coverage - oldCoverage) / totalBranchCount;
            oldCoverage = coverage;
            return c;
        }
    }
}
