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
        private static int branchCount = 80; //total count of branches
        private static ArrayList pastBranches = new ArrayList();
        private static int currentServer = 0;

        public static void IncrementCoverage(double weight)
        {
            coverage += weight;
        }

        public static void IncrementCoverage(int nr)
        {
            int id = nr + currentServer * 1000; // creates id with server and fault encoded
            for (int i = 0; i < pastBranches.Count; i++)
            {
                if ((int)pastBranches[i] == id)
                    return;
            }
            pastBranches.Add(id);
            coverage++;
        }

        public static void SetServer(int s)
        {
            currentServer = s;
        }

        public static void ResetCoverage()
        {
            coverage = 0;
        }

        public static void ResetPastBranches()
        { 
            pastBranches.Clear();
        }

        public static double GetCoverage()
        {
            return coverage;
        }

        public static double GetReward()
        {
            return coverage / branchCount;
        }
    }
}
