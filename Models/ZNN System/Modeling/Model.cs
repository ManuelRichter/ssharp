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

using System.Collections.Generic;
using System.Linq;
using System;
using SafetySharp.Modeling;
using SafetySharp.CaseStudies.ZNNSystem.Analysis;

namespace SafetySharp.CaseStudies.ZNNSystem.Modeling
{
	/// <summary>
	/// Represents the ZNN.com News System case study.
	/// </summary>
	public class Model : ModelBase
	{
		/// <summary>
		/// The cost of one server unit.
		/// </summary>
		public static int ServerUnitCost = 10;

		/// <summary>
		/// Available units per server
		/// </summary>
		public static int DefaultAvailableServerUnits = 10;

		/// <summary>
		/// Defines the value for high response time
		/// </summary>
		public static int HighResponseTimeValue = 7;

		/// <summary>
		/// Defines the value for low response time
		/// </summary>
		public static int LowResponseTimeValue = 3;

		/// <summary>
		/// Available Budget for server costs.
		/// </summary>
		public static int MaxBudget = 125;

		/// <summary>
		/// Count of latest response times to be used for calculating averange response time
		/// </summary>
		public static int LastResponseCountForAvgTime = 2;

		/// <summary>
		/// The Proxy
		/// </summary>
		[Root(RootKind.Controller)]
		public ProxyT Proxy { get; set; }

		/// <summary>
		/// Proxy observer
		/// </summary>
		[Root(RootKind.Controller)]
		public ProxyObserver ProxyObserver { get; set; }

		/// <summary>
		/// All connected Clients
		/// </summary>
		[Root(RootKind.Plant)]
		public List<ClientT> Clients => Proxy.ConnectedClients;

		/// <summary>
		/// All connected Servers
		/// </summary>
		[Root(RootKind.Plant)]
		public List<ServerT> Servers => Proxy.ConnectedServers;

		/// <summary>
		/// All Queries
		/// </summary>
		[Root(RootKind.Plant)]
		public List<Query> Queries => Proxy.Queries;

		/// <summary>
		/// All active Queries
		/// </summary>
		public List<Query> ActiveQueries => Queries.Where(x => x.State != EQueryState.Idle && x.State != EQueryState.Completed).ToList();

		/// <summary>
		/// Initializes a new instance with default values
		/// </summary>
		public Model()
			: this(10, 5) {}

		public Model(int clientCount, int serverCount)
		{
			Proxy = new ProxyT();
			ProxyObserver = new ProxyObserver(Proxy);

			// Add a few clients with Queries
			for(int i = 0; i < clientCount; i++)
			{
                Query.GetNewQuery(new ClientT(new Random(i), Proxy));
			}

			// Add a few server
			for(int i = 0; i < serverCount; i++)
			{
                ServerT.GetNewServer(Proxy);
			}
		}

	}
}
