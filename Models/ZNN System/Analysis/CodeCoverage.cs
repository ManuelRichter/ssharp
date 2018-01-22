using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
    public class CodeCoverage
    {
        public static double coverage = 0;

        public static void IncrementCoverage(double weight)
        {
            coverage += weight;
        }

        public static void IncrementCoverage()
        {
            coverage++;
        }

        public static void ResetCoverage()
        {
            coverage = 0;
        }

        public static double GetCoverage()
        {
            return coverage;
        }
    }
}
