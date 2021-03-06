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

namespace Tests.SimpleExecutableModel.Analysis.Dcca
{
	using System;
	using ISSE.SafetyChecking;
	using ISSE.SafetyChecking.AnalysisModelTraverser;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.Modeling;
	using ISSE.SafetyChecking.Simulator;
	using ISSE.SafetyChecking.Utilities;
	using Shouldly;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;
	using ISSE.SafetyChecking.MinimalCriticalSetAnalysis;

	public class HiddenHazard : AnalysisTest
	{
		public HiddenHazard(ITestOutputHelper output = null) : base(output)
		{
		}

		// Currently, ActivationMinimalTransitionSetBuilder only checks if a state has been reached with a smaller fault set,
		// but the pair (formulas,state) must be used as reference.
		[Fact(Skip = "Bug in ActivationMinimalTransitionSetBuilder")]
		public void Check()
		{
			var m = new Model();
			
			var checker = new SimpleSafetyAnalysis
			{
				Backend = SafetyAnalysisBackend.FaultOptimizedOnTheFly,
				Configuration = AnalysisConfiguration.Default
			};
			checker.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			checker.Configuration.DefaultTraceOutput = Output.TextWriterAdapter();

			var result = checker.ComputeMinimalCriticalSets(m, Model.InvariantViolated);
			result.MinimalCriticalSets.Count.ShouldBe(1);
		}

		public class Model : SimpleModelBase
		{
			public override Fault[] Faults { get; } = { new TransientFault { Identifier = 0, ProbabilityOfOccurrence = new Probability(0.1) } };
			public override bool[] LocalBools { get; } = new bool[] { false };
			public override int[] LocalInts { get; } = new int[0];

			private Fault F1 => Faults[0];

			public override void SetInitialState()
			{
				State = 0;
			}

			private bool ViolateInvariant
			{
				get { return LocalBools[0]; }
				set { LocalBools[0] = value; }
			}

			public override void Update()
			{
				ViolateInvariant = false;
				F1.TryActivate();

				if (F1.IsActivated)
					ViolateInvariant = true;
			}

			public static readonly Formula InvariantViolated = new SimpleLocalVarIsTrue(0);
		}
	}
}