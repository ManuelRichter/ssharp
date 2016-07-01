﻿// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
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

namespace SafetySharp.Analysis
{
	using System;
	using Modeling;

	/// <summary>
	///   Represents a base class for external model checker tools.
	/// </summary>
	public static class ModelChecker
	{
		/// <summary>
		///   Checks whether the <paramref name="formula" /> holds in all states of the <paramref name="model" />. The appropriate model
		///   checker is chosen automatically.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="formula">The formula that should be checked.</param>
		public static AnalysisResult Check(ModelBase model, Formula formula)
		{
			return new LtsMin().Check(model, formula);
		}

		/// <summary>
		///   Checks whether the <paramref name="invariant" /> holds in all states of the <paramref name="model" />. The appropriate
		///   model checker is chosen automatically.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="invariant">The invariant that should be checked.</param>
		public static AnalysisResult CheckInvariant(ModelBase model, Formula invariant)
		{
			return new SSharpChecker().CheckInvariant(model, invariant);
		}

		public static Probability CalculateProbabilityOfHazard(ModelBase model, Formula hazard)
		{
			Probability probabilityOfHazard;

			var probabilityOfHazardFormula = new CalculateProbabilityToReachStateFormula(hazard);

			using (var probabilityChecker = new ProbabilityChecker(model))
			{
				var checkProbabilityOfHazard = probabilityChecker.CalculateProbability(probabilityOfHazardFormula);
				probabilityChecker.CreateMarkovChain();
				probabilityChecker.DefaultChecker = new Mrmc(probabilityChecker);
				probabilityOfHazard = checkProbabilityOfHazard.Calculate();
			}
			return probabilityOfHazard;
		}
	}
}