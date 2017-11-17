﻿// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace SafetySharp.CaseStudies.RobotCell.Analysis
{
	using System;
	using System.Collections;
	using System.Diagnostics;
	using System.Linq;

	using SafetySharp.Analysis;
	using SafetySharp.Analysis.Heuristics;
    using Modeling;

	using NUnit.Framework;

	internal class BackToBackTests
	{
		[Test, Category("Back2BackTestingHeuristicsCoalition")]
		[TestCaseSource(nameof(CreateConfigurationsCoalition))]
		public void DccaWithHeuristics(Model model)
		{
			Debug.Listeners.Clear();
            Dcca(model,
				hazard: model.ReconfigurationMonitor.ReconfigurationFailure,
				enableHeuristics: true);
		}

	    internal static IEnumerable CreateConfigurationsCoalition()
        {
            return SampleModels.CreateCoalitionConfigurations(verify: true)
                .Select(model => new TestCaseData(model).SetName(model.Name + " (Coalition)"));
        }

		private void Dcca(Model model, Formula hazard, bool enableHeuristics)
		{
			var safetyAnalysis = new SafetyAnalysis
			{
				Configuration =
				{
					CpuCount = 4,
                    StateCapacity = 1 << 12,
					GenerateCounterExample = false
				},
				FaultActivationBehavior = FaultActivationBehavior.ForceOnly
			};

			if (enableHeuristics)
			{
				safetyAnalysis.Heuristics.Add(RedundancyHeuristic(model));
				safetyAnalysis.Heuristics.Add(new SubsumptionHeuristic(model));
			}

			var result = safetyAnalysis.ComputeMinimalCriticalSets(model, hazard);
		    Console.WriteLine(result);
		    Assert.IsEmpty(result.Exceptions);
        }

	    internal static IFaultSetHeuristic RedundancyHeuristic(Model model)
	    {
	        var cartFaults = model.Carts.SelectMany(cart => new[] { cart.Broken, cart.Lost })
	                              .Concat(model.CartAgents.SelectMany(cartAgent => new [] { cartAgent.ConfigurationUpdateFailed, cartAgent.Broken }));

	        return new MinimalRedundancyHeuristic(
	            model.Faults.Except(cartFaults).ToArray(),
                model.Faults.Length / 2,
	            model.Robots.SelectMany(d => d.Tools.Where(t => t.ProductionAction == ProductionAction.Drill).Select(t => t.Broken)),
	            model.Robots.SelectMany(d => d.Tools.Where(t => t.ProductionAction == ProductionAction.Insert).Select(t => t.Broken)),
	            model.Robots.SelectMany(d => d.Tools.Where(t => t.ProductionAction == ProductionAction.Tighten).Select(t => t.Broken)),
	            model.Robots.SelectMany(d => d.Tools.Where(t => t.ProductionAction == ProductionAction.Polish).Select(t => t.Broken)));
	    }
    }
}