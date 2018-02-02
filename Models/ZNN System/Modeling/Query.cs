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

using SafetySharp.Modeling;
using SafetySharp.CaseStudies.ZNNSystem.Analysis;

namespace SafetySharp.CaseStudies.ZNNSystem.Modeling
{
	/// <summary>
	/// Class for Query and Response
	/// </summary>
	public class Query : Component
	{
		/// <summary>
		/// State machine for query states
		/// </summary>
		private readonly StateMachine<EQueryState> _StateMachine = EQueryState.Idle;

		/// <summary>
		/// Gets the current state of the Query
		/// </summary>
		public EQueryState State => _StateMachine.State;

		/// <summary>
		/// The server to execute the query
		/// </summary>
		public ServerT SelectedServer { get; set; }

		/// <summary>
		/// The client to request the query
		/// </summary>
		public ClientT Client { get; private set; }

		/// <summary>
		/// Indicates if the query is in execution
		/// </summary>
		public bool IsExecute { get; set; }

		/// <summary>
		/// Creates a new query instance
		/// </summary>
		private Query()
		{
			//Client = client;
			//client.ConnectedProxy.Queries.Add(this);
			//SelectedServer = null;
		}

		/// <summary>
		/// Creates a new Query on the given client
		/// </summary>
		/// <param name="client">Client</param>
		public static Query GetNewQuery(ClientT client)
		{
			var query = new Query();
			client.CurrentQuery = query;
			query.Client = client;
			client.ConnectedProxy.Queries.Add(query);
			query.IsExecute = false;
			return query;
		}

		/// <summary>
		/// Update Method
		/// </summary>
		public override void Update()
		{
			if(!IsExecute)
				return;
			// else
			_StateMachine.Transition(
							 from: new [] { EQueryState.Completed,EQueryState.Idle },
							 to: EQueryState.QueryToProxy
							 //,action: Client.StartQuery
							 )
						 .Transition(
							 from: EQueryState.QueryToProxy,
							 to: EQueryState.QueryToServer,
                             guard: Client.ConnectedProxy != null,
							 action: () =>
							 {
                                 BranchCoverage.IncrementCoverage(63);
								 Client.ConnectedProxy.SelectServer(this);
							 })
						 .Transition(
							 from: EQueryState.QueryToServer,
							 to: EQueryState.OnServer,
							 guard: SelectedServer != null && Client.ConnectedProxy != null,
                             action: () =>
                             {
                                 BranchCoverage.IncrementCoverage(64);
                             })
						 .Transition(
							 from: EQueryState.OnServer,
							 to: EQueryState.LowFidelityComplete,
							 guard: SelectedServer.ExecuteQueryStep(this) && Client.ConnectedProxy != null,
                             action: () =>
                             {
                                 BranchCoverage.IncrementCoverage(65);
                             })
                         .Transition(
							 from: EQueryState.LowFidelityComplete,
							 to: EQueryState.MediumFidelityComplete,
							 guard: SelectedServer.Fidelity != EServerFidelity.Low && SelectedServer.ExecuteQueryStep(this) && Client.ConnectedProxy != null,
                             action: () =>
                             {
                                 BranchCoverage.IncrementCoverage(66);
                             })
                         .Transition(
							 from: EQueryState.MediumFidelityComplete,
							 to: EQueryState.HighFidelityComplete,
							 guard: SelectedServer.Fidelity != EServerFidelity.Medium && SelectedServer.ExecuteQueryStep(this) && Client.ConnectedProxy != null,
                             action: () =>
                             {
                                 BranchCoverage.IncrementCoverage(67);
                             })
                         .Transition(
							 from: EQueryState.LowFidelityComplete,
							 to: EQueryState.ResToProxy,
							 guard: SelectedServer.Fidelity == EServerFidelity.Low && SelectedServer.ExecuteQueryStep(this) && Client.ConnectedProxy != null,
							 action: () =>
							 {
                                 BranchCoverage.IncrementCoverage(68);
								 SelectedServer.QueryComplete(this);
							 })
						 .Transition(
							 from: EQueryState.MediumFidelityComplete,
							 to: EQueryState.ResToProxy,
							 guard: SelectedServer.Fidelity == EServerFidelity.Medium && SelectedServer.ExecuteQueryStep(this) && Client.ConnectedProxy != null,
							 action: () =>
							 {
                                 BranchCoverage.IncrementCoverage(69);
								 SelectedServer.QueryComplete(this);
							 })
						 .Transition(
							 from: EQueryState.HighFidelityComplete,
							 to: EQueryState.ResToProxy,
							 guard: SelectedServer.ExecuteQueryStep(this) && Client.ConnectedProxy != null,
							 action: () =>
							 {
                                 BranchCoverage.IncrementCoverage(70);
								 SelectedServer.QueryComplete(this);
							 })
						 .Transition(
							 from: EQueryState.ResToProxy,
							 to: EQueryState.ResToClient,
                             guard: Client.ConnectedProxy != null,
                             action: () =>
                             {
                                 BranchCoverage.IncrementCoverage(71);
                             })
						 .Transition(
							 from: EQueryState.ResToClient,
							 to: EQueryState.Completed,
                             guard: Client.ConnectedProxy != null,
							 action: () =>
							 {
								 Client.GetResponse();
								 Client.ConnectedProxy.UpdateAvgResponseTime(Client.LastResponseTime);
								 SelectedServer = null;
								 IsExecute = false;
                                 BranchCoverage.IncrementCoverage(72);
							 });
		}
	}
}
