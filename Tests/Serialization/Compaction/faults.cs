﻿// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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

namespace Tests.Serialization.Compaction
{
	using System.Reflection;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class Faults : SerializationObject
	{
		protected override void Check()
		{
			var c = new C { B = false };

			GenerateCode(SerializationMode.Optimized, c);
			StateSlotCount.ShouldBe(1);
			StateVectorLayout.Groups.Length.ShouldBe(2);
			StateVectorLayout.FaultBytes.ShouldBe(1);

			StateVectorLayout.Groups[0].ElementSizeInBits.ShouldBe(1);
			StateVectorLayout.Groups[0].OffsetInBytes.ShouldBe(0);
			StateVectorLayout.Groups[0].Slots.Length.ShouldBe(2);
			StateVectorLayout.Groups[0].Slots[0].Object.ShouldBe(c.F1);
			StateVectorLayout.Groups[0].Slots[0].Field.ShouldBe(typeof(Fault).GetField("_isActivated", BindingFlags.Instance | BindingFlags.NonPublic));
			StateVectorLayout.Groups[0].Slots[1].Object.ShouldBe(c.F2);
			StateVectorLayout.Groups[0].Slots[1].Field.ShouldBe(typeof(Fault).GetField("_isActivated", BindingFlags.Instance | BindingFlags.NonPublic));
			StateVectorLayout.Groups[0].GroupSizeInBytes.ShouldBe(1);

			StateVectorLayout.Groups[1].ElementSizeInBits.ShouldBe(1);
			StateVectorLayout.Groups[1].OffsetInBytes.ShouldBe(1);
			StateVectorLayout.Groups[1].Slots.Length.ShouldBe(1);
			StateVectorLayout.Groups[1].Slots[0].Object.ShouldBe(c);
			StateVectorLayout.Groups[1].Slots[0].Field.ShouldBe(typeof(C).GetField("B"));
			StateVectorLayout.Groups[1].GroupSizeInBytes.ShouldBe(1);
		}

		private class C
		{
			public bool B;

			public readonly Fault F1 = new TransientFault();
			public readonly Fault F2 = new PersistentFault();
		}

	}
}