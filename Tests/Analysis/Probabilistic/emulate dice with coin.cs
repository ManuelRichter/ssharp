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

namespace Tests.Analysis.Probabilistic
{
	using System;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	// Knuth's die. Described e.g. on http://wwwhome.cs.utwente.nl/~timmer/scoop/casestudies/knuth/knuth.html

	internal class EmulateDiceWithCoin : ProbabilisticAnalysisTestObject
	{

		private enum S
		{
			InitialThrow,
			Throw1To3,
			Throw4To6,
			Throw1Or2,
			Throw3OrRethrow,
			Throw4Or5,
			Throw6OrRethrow,
			Final1,
			Final2,
			Final3,
			Final4,
			Final5,
			Final6,
		}

		protected override void Check()
		{
			var c = new C();
			Probability probabilityOfFinal1;

			using (var probabilityChecker = new ProbabilityChecker(TestModel.New(c)))
			{
				var typeOfModelChecker = (Type)Arguments[0];
				var modelChecker = (ProbabilisticModelChecker)Activator.CreateInstance(typeOfModelChecker,probabilityChecker);

				var checkProbabilityOf1 = probabilityChecker.CalculateProbabilityToReachStates(c.IsInStateFinal1());
				probabilityChecker.CreateProbabilityMatrix();
				probabilityChecker.DefaultChecker = modelChecker;
				probabilityOfFinal1 = checkProbabilityOf1.Check();
				//probabilityOfFinal1 = checkProbabilityOf1.CheckWithChecker(modelChecker);
			}

			probabilityOfFinal1.Between(0.1, 0.2).ShouldBe(true);
		}

		private class C : Component
		{
			public Formula IsInStateFinal1()
			{
				return StateMachine.State == S.Final1;
			}

			public readonly StateMachine<S> StateMachine = new StateMachine<S>(S.InitialThrow);

			public override void Update()
			{
				StateMachine.Transition(
					from: S.InitialThrow,
					to: new[] { S.Throw1To3, S.Throw4To6 })
				.Transition(
					from: S.Throw1To3,
					to: new[] { S.Throw1Or2, S.Throw3OrRethrow })
				.Transition(
					from: S.Throw1Or2,
					to: new[] { S.Final1, S.Final2 })
				.Transition(
					from: S.Throw3OrRethrow,
					to: new[] { S.Final3, S.Throw1To3 })
				.Transition(
					from: S.Throw4To6,
					to: new[] { S.Throw4Or5, S.Throw6OrRethrow })
				.Transition(
					from: S.Throw4Or5,
					to: new[] { S.Final4, S.Final5 })
				.Transition(
					from: S.Throw6OrRethrow,
					to: new[] { S.Final6, S.Throw4To6 });
			}
		}

	}
}