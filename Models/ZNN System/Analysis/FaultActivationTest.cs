using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using NUnit.Framework;
using SafetySharp.CaseStudies.ZNNSystem.Modeling;
using SafetySharp.Analysis;
using ISSE.SafetyChecking.Modeling;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
    [TestFixture]
    public class FaultActivationTest
    {
        [Test]
        public void TestCanServerActivate()
        {
            Console.WriteLine("Testing Server activation");
            var proxy = new ProxyT();
            ServerT.GetNewServer(proxy);
            ServerT.GetNewServer(proxy);
            ServerT.GetNewServer(proxy);
            ServerT.GetNewServer(proxy);
            Assert.AreEqual(0, proxy.ActiveServerCount);
            Assert.AreEqual(4, proxy.ConnectedServers.Count);
            Console.WriteLine("Server connected");
            
            ClientT.GetNewClient(proxy);
            ClientT.GetNewClient(proxy);
            Assert.AreEqual(2,proxy.ConnectedClients.Count);
            Console.WriteLine("Count of active servers:" + proxy.ActiveServerCount);

            proxy.IncrementServerPool(); 

            var q = Query.GetNewQuery(proxy.ConnectedClients[0]);
            proxy.ConnectedClients[0].StartQuery();
            Console.WriteLine(q.State);
            for (int i = 0; i <= 5; i++) { q.Update(); Console.WriteLine(q.State); }

            Console.WriteLine("Count of active servers:" + proxy.ActiveServerCount);
        }

        [Test]
        public void TestFaultActivation()
        {
            Random rand = new Random();
            int modelCount = 1; //number of models to instantiate
            int simSteps = 10; //number of steps to simulate
            int probToActivate = 100; //probability per step to activate the fault
            State[] states = new State[modelCount];
            
            //instantiate models
            for (int i = 0; i < modelCount; i++) states[i] = new State(new Model(5,3)); //Faultarray depends on servercount!
            
            //add faultconditions
            for (int i = 0; i < modelCount; i++) states[i].AddFaultCondition(12, 4, 3); 
            for (int i = 0; i < modelCount; i++) states[i].AddFaultCondition(12, 3, 1); 

            //simulate models
            foreach (State s in states)
            {                
                var simulator = new SafetySharpSimulator(s.model);
                var modelSim = (Model)simulator.Model;
                for (int i = 0; i < modelSim.Faults.Length; i++) Console.WriteLine(i + ". " + modelSim.Faults[i].Name);
                
                for (int i = 0; i <= simSteps; i++)
                {
                    //activate fault
                    if (rand.Next(100) <= probToActivate)
                    {
                        foreach (FaultCondition fc in s.fcs)
                        {
                            if (i == fc.stepToActivate) ChooseServerToFault(modelSim.Faults, fc);
                        }
                    }

                    //process fault
                    simulator.SimulateStep();
                    foreach (Query q in modelSim.ActiveQueries) Console.Write("State:" + q.State + " ResponseTime:" + q.Client.LastResponseTime);

                    Console.WriteLine("Servercount:" + modelSim.Proxy.ConnectedServers.Count + " Active:" + modelSim.Proxy.ActiveServerCount);

                    //deactivate fault (needed?)
                    foreach (FaultCondition fc in s.fcs)
                    {
                        if (i >= fc.stepToActivate && !fc.faultActivated)
                        {
                            fc.faultActivated = true;
                            //modelSim.Faults[fc.faultNumber].Activation = Activation.Suppressed;
                            //Console.WriteLine("Suppressed the activation of:" + modelSim.Faults[fc.faultNumber].Name);
                        }
                    }
                }

                //Console.WriteLine(simulator.RuntimeModel.StateVectorLayout);
                for (int i = 0; i < modelSim.Servers.Count; i++)
                    Console.WriteLine("Completed Queries of server " + i + ": " + modelSim.Servers[i].QueryCompleteCount + " active:" + modelSim.Servers[i].IsServerActive + " dead:" + modelSim.Servers[i].IsServerDead);
                
            }

            Console.WriteLine("End of TestFaultActivation");
        }
        
        /// <summary>
        /// activates the fault for a specific server
        /// </summary>
        private static void ChooseServerToFault(Fault[] faults, FaultCondition fc)
        {
            string faultname = faults[fc.faultNumber].Name;
            int counter = 0;

            for (int i = 0;i<faults.Length;i++)
            {
                if (faults[i].Name == faultname)
                {
                    counter++;
                    if (counter == fc.serverToFault)
                    {
                        faults[i].Activation = Activation.Forced;
                        Console.WriteLine("Forced the activation of:" + faults[fc.faultNumber].Name);
                    }
                }
            }
        }
    }


}
