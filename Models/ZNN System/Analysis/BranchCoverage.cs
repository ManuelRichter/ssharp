using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
    public class BranchCoverage
    {
        private static double coverage = 0;
        private static int totalBranchCount = 53;  
        private static ArrayList pastBranches = new ArrayList();
        private static bool isCovering = false;
        private static double oldCoverage = 0;

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

        public static void StartCovering()
        {
            isCovering = true;
        }
        
        private static void ResetCoverage()
        {
            coverage = 0;
            oldCoverage = 0;
        }

        public static void ResetPastBranches()
        { 
            pastBranches.Clear();
            ResetCoverage();
        }
        
        public static double GetReward()
        {           
            double c = (coverage - oldCoverage) / totalBranchCount;
            oldCoverage = coverage;
            return c;
        }
    }
}
