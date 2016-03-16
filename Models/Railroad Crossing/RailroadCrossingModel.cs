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

namespace SafetySharp.CaseStudies.RailroadCrossing
{
	using Analysis;
	using ModelElements;
	using ModelElements.ModelElements.Context;
	using ModelElements.ModelElements.CrossingController;
	using ModelElements.ModelElements.TrainController;
	using Modeling;

	public class RailroadCrossingModel : Model
	{
		public RailroadCrossingModel()
		{
			CrossingControl = new CrossingControl
			{
				Sensor = new BarrierSensor(),
				Motor = new BarrierMotor(),
				Radio = new RadioModule(),
				Timer = new Timer(),
				TrainSensor = new TrainSensor()
			};

			TrainControl = new TrainControl
			{
				Brakes = new Brakes(),
				Odometer = new Odometer(),
				Radio = new RadioModule()
			};

			Component.Bind(nameof(Barrier.Speed), nameof(CrossingControl.Motor.Speed));
			Component.Bind(nameof(CrossingControl.Sensor.BarrierAngle), nameof(Barrier.Angle));

			Component.Bind(nameof(Train.Acceleration), nameof(TrainControl.Brakes.Acceleration));
			Component.Bind(nameof(CrossingControl.TrainSensor.TrainPosition), nameof(Train.Position));

			Component.Bind(nameof(TrainControl.Radio.RetrieveFromChannel), nameof(Channel.Receive));
			Component.Bind(nameof(TrainControl.Radio.DeliverToChannel), nameof(Channel.Send));

			Component.Bind(nameof(CrossingControl.Radio.RetrieveFromChannel), nameof(Channel.Receive));
			Component.Bind(nameof(CrossingControl.Radio.DeliverToChannel), nameof(Channel.Send));

			Component.Bind(nameof(TrainControl.Odometer.TrainPosition), nameof(Train.Position));
			Component.Bind(nameof(TrainControl.Odometer.TrainSpeed), nameof(Train.Speed));
		}

		[Root(Role.SystemOfInterest)]
		public CrossingControl CrossingControl { get; }

		[Root(Role.SystemOfInterest)]
		public TrainControl TrainControl { get; }

		[Root(Role.SystemContext)]
		public Train Train { get; } = new Train();

		[Root(Role.SystemContext)]
		public Barrier Barrier { get; } = new Barrier();

		[Root(Role.SystemContext)]
		public RadioChannel Channel { get; } = new RadioChannel();
	}
}