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

namespace PressureTank
{
	using SafetySharp.Modeling;

	/// <summary>
	///   Represents the sensor that monitors the pressure within the pressure tank.
	/// </summary>
	public class Sensor : Component
	{
		/// <summary>
		///   The fault that prevents the sensor from triggering when the tank has reached or exceeded
		///   its maximum allowed pressure level.
		/// </summary>
		public readonly Fault SuppressIsEmpty = new TransientFault();

		/// <summary>
		///   The fault that prevents the sensor from triggering when the tank has become empty.
		/// </summary>
		public readonly Fault SuppressIsFull = new TransientFault();

		/// <summary>
		///   The pressure level the sensor is watching for.
		/// </summary>
		[Hidden]
		public int TriggerPressure;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public Sensor()
		{
			//SuppressIsFull.AddEffect<SuppressIsFullEffect>(this);
			//SuppressIsEmpty.AddEffect<SuppressIsEmptyEffect>(this);
		}

		/// <summary>
		///   Gets a value indicating whether the triggering pressure level has been reached or exceeded.
		/// </summary>
		public virtual bool IsFull => CheckPhysicalPressure >= TriggerPressure;

		/// <summary>
		///   Gets a value indicating whether the tank is empty.
		/// </summary>
		public virtual bool IsEmpty => CheckPhysicalPressure <= 0;

		/// <summary>
		///   Senses the physical pressure level within the tank.
		/// </summary>
		public extern int CheckPhysicalPressure { get; }

		/// <summary>
		///   Prevents the sensor from triggering when the tank has reached or exceeded its maximum allowed pressure level.
		/// </summary>
		[FaultEffect]
		private sealed class SuppressIsFullEffect : Sensor
		{
			public override bool IsFull => false;
		}

		/// <summary>
		///   Prevents the sensor from triggering when the tank has become empty.
		/// </summary>
		[FaultEffect]
		private sealed class SuppressIsEmptyEffect : Sensor
		{
			public override bool IsEmpty => false;
		}
	}
}