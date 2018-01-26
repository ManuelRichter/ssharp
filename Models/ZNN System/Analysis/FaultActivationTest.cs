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
            Assert.AreEqual(2, proxy.ConnectedClients.Count);
            Console.WriteLine("Count of active servers:" + proxy.ActiveServerCount);

            proxy.IncrementServerPool();

            var q = Query.GetNewQuery(proxy.ConnectedClients[0]);
            proxy.ConnectedClients[0].StartQuery();
            Console.WriteLine(q.State);
            for (int i = 0; i <= 5; i++) { q.Update(); Console.WriteLine(q.State); }

            Console.WriteLine("Count of active servers:" + proxy.ActiveServerCount);
        }

        private static int modelCount = 50; //number of models to instantiate/equivalent to episode count
        private static int simSteps = 10; //number of steps to simulate

        private static int serverCount = 3;
        private static int clientCount = 5;
        private static int numberOfFaults = 7;

        [Test]
        public void TestFaultActivation()
        {
            Random rand = new Random();
            const int probToActivate = 100; //probability per step to activate the fault

            //const for qLearning
            int stateSize = 128; //2^7
            int actionSize = numberOfFaults * serverCount;

            QValue[] qTable = new QValue[stateSize * actionSize]; //ca. 2600 entries
            FaultyModel[] fms = new FaultyModel[modelCount];

            double bestReward = 0;

            //instantiate models
            for (int i = 0; i < modelCount; i++) fms[i] = new FaultyModel(new Model(serverCount, clientCount));

            //instatiate qTable
            InitQTable(ref qTable);

            //add faultconditions
            //for (int i = 0; i < modelCount; i++) fms[i].AddFaultCondition(12, 4, 3); 
            //for (int i = 0; i < modelCount; i++) fms[i].AddFaultCondition(12, 3, 1); 
            State currentState = new State(new bool[] { false, false, false, false, false, false, false });

            //list all faults
            for (int i = 0; i < fms.First().model.Faults.Length; i++) Console.WriteLine(i + ". " + fms.First().model.Faults[i].Name);

            //simulate models
            foreach (FaultyModel fm in fms)
            {
                var simulator = new SafetySharpSimulator(fm.model);
                var modelSim = (Model)simulator.Model;

                for (int i = 0; i < simSteps; i++)
                {
                    Act currentAction = ChooseAction(rand, currentState, ref qTable);

                    fm.AddFaultCondition(currentAction.ConvertToFC(i)); //use current step to activate

                    //activate fault
                    if (rand.Next(100) <= probToActivate)
                    {
                        foreach (FaultCondition fc in fm.fcs)
                        {
                            if (i == fc.stepToActivate) ChooseServerToFault(modelSim.Faults, fc);
                        }
                    }

                    //process fault
                    simulator.SimulateStep();
                    //foreach (Query q in modelSim.ActiveQueries) Console.Write("State:" + q.State + " ResponseTime:" + q.Client.LastResponseTime);
                    //Console.WriteLine("Servercount:" + modelSim.Proxy.ConnectedServers.Count + " Active:" + modelSim.Proxy.ActiveServerCount);

                    Feedback(ref qTable, currentState, currentAction);
                    currentState.SetFault(currentAction.GetFault(), true); //update state

                    //deactivate fault (needed?)
                    foreach (FaultCondition fc in fm.fcs)
                    {
                        if (i >= fc.stepToActivate && !fc.faultActivated)
                        {
                            fc.faultActivated = true;
                            //modelSim.Faults[fc.faultNumber].Activation = Activation.Suppressed;
                            //Console.WriteLine("Suppressed the activation of:" + modelSim.Faults[fc.faultNumber].Name);
                        }
                    }
                }//episode ends here
                currentState = new State(new bool[] { false, false, false, false, false, false, false });

                //episode cleanup
                pastFaults.Clear();
                if (CodeCoverage.coverage > bestReward) bestReward = CodeCoverage.coverage;
                CodeCoverage.ResetCoverage();
                PrintQTable(ref qTable);

                //Console.WriteLine(simulator.RuntimeModel.StateVectorLayout);
                for (int i = 0; i < modelSim.Servers.Count; i++)
                    Console.WriteLine("Completed Queries of server " + i + ": " + modelSim.Servers[i].QueryCompleteCount + " active:" + modelSim.Servers[i].IsServerActive + " dead:" + modelSim.Servers[i].IsServerDead);

            }
            string seq = Backtrack(ref qTable);
            Console.WriteLine("End of TestFaultActivation, best reward:" + bestReward + "Best sequence: <" + seq + ">");
        }

        private void InitQTable(ref QValue[] qTable)
        {
            int i = 0;

            for (int f1 = 0; f1 < 2; f1++)
            {
                for (int f2 = 0; f2 < 2; f2++)
                {
                    for (int f3 = 0; f3 < 2; f3++)
                    {
                        for (int f4 = 0; f4 < 2; f4++)
                        {
                            for (int f5 = 0; f5 < 2; f5++)
                            {
                                for (int f6 = 0; f6 < 2; f6++)
                                {
                                    for (int f7 = 0; f7 < 2; f7++)
                                    {
                                        for (int nf = 0; nf < numberOfFaults; nf++)
                                        {
                                            for (int nserv = 0; nserv < serverCount; nserv++)
                                            {
                                                bool[] arr = new bool[] { Convert.ToBoolean(f1), Convert.ToBoolean(f2), Convert.ToBoolean(f3), Convert.ToBoolean(f4), Convert.ToBoolean(f5), Convert.ToBoolean(f6), Convert.ToBoolean(f7) };
                                                qTable[i] = new QValue(new State(arr), new Act(nf, nserv));
                                                i++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Console.WriteLine("Last entry:" + qTable[i - 1].ToString());
        }

        private int epsilon = 30; //30% exploration

        private Act ChooseAction(Random rand, State state, ref QValue[] qTable)
        {
            if (rand.Next(100) < epsilon)
            {
                return Explore(rand);
            }
            else return Exploit(ref qTable, state);
        }

        private Act Explore(Random rand)
        {
            Console.WriteLine("Explore:");
            return new Act(rand.Next(numberOfFaults), rand.Next(serverCount));
        }

        private Act Exploit(ref QValue[] qTable, State state)
        {
            Console.WriteLine("Exploit:");
            return GetBestAction(ref qTable, state);
        }

        //returns action with highest reward for given state
        private Act GetBestAction(ref QValue[] qTable, State state)
        {
            double maxReward = double.MinValue;
            int bestIndex = 0;

            for (int i = 0; i < qTable.Length; i++)
            {
                if (qTable[i].GetReward() > maxReward && qTable[i].StateEquals(state))
                {
                    maxReward = qTable[i].GetReward();
                    bestIndex = i;
                }
            }

            return qTable[bestIndex].GetAction();
        }

        private void Feedback(ref QValue[] qTable, State currentState, Act currentAction)
        {
            double alpha = 0.5f;
            double gamma = 0.95f;
            QValue currentQ = new QValue(currentState, currentAction);
            //QValue nextBestQ = new QValue(state, GetBestAction(ref qTable,state)); //notsure
            State nextState = currentState;
            nextState.SetFault(currentAction.GetFault(), true);
            QValue nextBestQ = new QValue(nextState, GetBestAction(ref qTable, nextState)); //Q(s_new,a_new)
            double reward = GetFinalReward(currentAction);

            Console.WriteLine(currentAction.ToString() + " reward: " + reward);

            //Q_new(s,a) = Q(s,a) + alpha*(r + gamma*max_a_new(Q(s_new,a_new) - Q(s,a)))
            double newQ = currentQ.GetReward() + alpha * (reward + gamma * nextBestQ.GetReward() - currentQ.GetReward());
            qTable[GetIndex(ref qTable, currentQ)].SetReward(newQ);
        }

        private int GetIndex(ref QValue[] qTable, QValue q)
        {
            for (int i = 0; i < qTable.Length; i++)
            {
                if (qTable[i].Equals(q)) return i;
            }
            return -1;
        }

        private Stack<Act> pastFaults = new Stack<Act>();

        private double GetFinalReward(Act currentAct)
        {
            foreach (Act a in pastFaults)
            {
                if (a.Equals(currentAct)) return 0.1;
            }
            if (pastFaults.Count > 3) pastFaults.Pop();
            pastFaults.Push(currentAct);
            return GetCodeCoverage();
        }

        private double GetCodeCoverage()
        {
            double temp = CodeCoverage.GetCoverage();
            return temp;
        }

        private string Backtrack(ref QValue[] qTable)
        {
            double bestQvalue = Double.MinValue;
            int index = 0;

            //get best QValue-entry
            for (int i = 0; i<qTable.Length;i++)
            {
                if (qTable[i].GetReward() > bestQvalue)
                {
                    bestQvalue = qTable[i].GetReward();
                    index = i;
                }
            }

            //track back
            //for (int i = 0; i < qTable.Length; i++)
            //{
                
            //}
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
        private void PrintQTable(ref QValue[] qTable)
        {
            for (int i = 0;i<qTable.Length;i++)
            {
                Console.WriteLine(qTable[i].ToTable());
            }
        }
    }
}
