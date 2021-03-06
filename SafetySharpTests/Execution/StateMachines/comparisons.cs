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

namespace Tests.Execution.StateMachines
{
	using System;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class Comparisons : TestObject
	{
		protected override void Check()
		{
			var sm1 = new StateMachine<int>(17);
			(sm1 == 17).ShouldBe(true);
			(sm1 == 13).ShouldBe(false);
			(sm1 != 17).ShouldBe(false);
			(sm1 != 13).ShouldBe(true);

			(17 == sm1).ShouldBe(true);
			(13 == sm1).ShouldBe(false);
			(17 != sm1).ShouldBe(false);
			(13 != sm1).ShouldBe(true);

			var sm2 = new StateMachine<E>(E.A);
			(sm2 == E.A).ShouldBe(true);
			(sm2 == E.B).ShouldBe(false);
			(sm2 != E.A).ShouldBe(false);
			(sm2 != E.B).ShouldBe(true);

			(E.A == sm2).ShouldBe(true);
			(E.B == sm2).ShouldBe(false);
			(E.A != sm2).ShouldBe(false);
			(E.B != sm2).ShouldBe(true);

			var sm3 = new StateMachine<E>(E.B);
			(sm3 == E.B).ShouldBe(true);
			(sm3 == E.A).ShouldBe(false);
			(sm3 != E.B).ShouldBe(false);
			(sm3 != E.A).ShouldBe(true);

			(E.B == sm3).ShouldBe(true);
			(E.A == sm3).ShouldBe(false);
			(E.B != sm3).ShouldBe(false);
			(E.A != sm3).ShouldBe(true);

			var e = E.B;
			(sm3 == e).ShouldBe(true);
			(sm3 != e).ShouldBe(false);
			(e == sm3).ShouldBe(true);
			(e != sm3).ShouldBe(false);
		}

		private enum E
		{
			A,
			B
		}
	}
}