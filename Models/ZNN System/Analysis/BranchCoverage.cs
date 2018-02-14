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
        public static double coverage = 0;
        private static int totalBranchCount = 53; //total count of branches 
        private static int branchCount = totalBranchCount;
        private static ArrayList pastBranches = new ArrayList();
        private static int currentServer = 0;
        private static bool isCovering = false;
        private static int bestCov = 0;
        private static double oldCoverage = 0;

        public static void IncrementCoverage(int nr)
        {
            if (!isCovering) return;
            if (bestCov < pastBranches.Count)
                bestCov = pastBranches.Count;

            int id = nr;// + currentServer * 1000; // creates id with server and fault encoded
            for (int i = 0; i < pastBranches.Count; i++)
            {
                if ((int)pastBranches[i] == id)
                    return;
            }
            pastBranches.Add(id);
            coverage++;
        }

        public static void StartCovering()
        {
            isCovering = true;
        }
        
        public static void SetServer(int s)
        {
            currentServer = s;
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
            branchCount = totalBranchCount;
        }

        public static double GetCoverage()
        {
            return coverage;
        }

        public static double GetReward()
        {
           
            double c = (coverage - oldCoverage) / branchCount;
            oldCoverage = coverage;
            return c;
        }
    }
}
