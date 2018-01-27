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
using System.IO;

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

        private static int modelCount = 500; //number of models to instantiate/equivalent to episode count
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

            double bestRewardAllEpisodes = 0;
            int indexEpisode = 0;

            //logging
            ArrayList logSum = new ArrayList(modelCount);

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
                double sumCoverage = 0;

                for (int i = 0; i < simSteps; i++)
                {
                    Act currentAction = ChooseAction(rand, currentState, ref qTable,indexEpisode);
                    CodeCoverage.SetServer(currentAction.GetServer()); //to differentiate between servers
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
                    sumCoverage = sumCoverage + CodeCoverage.GetReward();
                    CodeCoverage.ResetCoverage();
                }
                //episode ends here
                LogQTableToCSV(ref qTable,indexEpisode);
                indexEpisode++;

                //episode cleanup
                currentState = new State(new bool[] { false, false, false, false, false, false, false });

                if (sumCoverage > fm.bestReward)
                {
                    fm.bestReward = sumCoverage;
                }
                logSum.Add(sumCoverage); //for logging
                CodeCoverage.ResetPastBranches();
                //PrintQTable(ref qTable);

                //Console.WriteLine(simulator.RuntimeModel.StateVectorLayout);
                for (int i = 0; i < modelSim.Servers.Count; i++)
                    Console.WriteLine("Completed Queries of server " + i + ": " + modelSim.Servers[i].QueryCompleteCount + " active:" + modelSim.Servers[i].IsServerActive + " dead:" + modelSim.Servers[i].IsServerDead);

            }
            //Log sum
            LogSeqToCSV(logSum);

            //get best sequence
            string seq = "";
            double bestOfAllModels = 0;
            int bestIndex = 0;

            for (int i = 0; i < fms.Length; i++)
            {
                if (fms[i].bestReward > bestOfAllModels)
                {
                    bestOfAllModels = fms[i].bestReward;
                    bestIndex = i;
                }
            }
           
            foreach (FaultCondition f in fms[bestIndex].fcs)
                seq = seq + f.faultNumber + " " + f.serverToFault + ",";

            Console.WriteLine("End of TestFaultActivation, Best sequence: <" + seq + "> in round:" + (bestIndex+1) + " with " + bestOfAllModels);
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

        private int epsilon = 100; //100% exploration

        private Act ChooseAction(Random rand, State state, ref QValue[] qTable,int indexEpisode)
        {
            if (epsilon > 30) epsilon--;

            if (rand.Next(100) < epsilon && indexEpisode != modelCount-1) //forced exploitation in last episode
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
            Console.WriteLine("Exploit in State:" + state.ToString());
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

            State nextState = new State(currentState.Copy());
            nextState.SetFault(currentAction.GetFault(), true);
            QValue nextBestQ = new QValue(nextState, GetBestAction(ref qTable, nextState)); //Q(s_new,a_new)

            double reward = GetFinalReward();

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

        private double GetFinalReward()
        {
            return CodeCoverage.GetReward();
        }

        private string Backtrack(ref QValue[] qTable)
        {
            double bestQvalue = Double.MinValue;
            int index = 0;

            //get best QValue-entry
            for (int i = 0; i < qTable.Length; i++)
            {
                if (qTable[i].GetReward() > bestQvalue)
                {
                    bestQvalue = qTable[i].GetReward();
                    index = i;
                }
            }
            return "";
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

            for (int i = 0; i < faults.Length; i++)
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
            for (int i = 0; i < qTable.Length; i++)
            {
                Console.WriteLine(qTable[i].ToTable());
            }
        }

        private void LogQTableToCSV(ref QValue[] qTable, int episode)
        {
            string path = @"C:\Users\M\Desktop\Uni\Bachelor\Bachelorarbeit\Evaluation\Episode" + episode +".csv";
            string[] output = new string[qTable.Length];
            StringBuilder s = new StringBuilder();

            for (int i = 0; i < qTable.Length; i++) output[i] = qTable[i].ToTable();
            
            s.AppendLine(string.Join(";", output));

            //for (int i = 0; i < output.GetLength(0); i++)
            //{ 
            //    s.AppendLine(string.Join(";", output[i]));
            //}

            File.WriteAllText(path, s.ToString());
        }

        private void LogSeqToCSV(ArrayList seq)
        {
            string path = @"C:\Users\M\Desktop\Uni\Bachelor\Bachelorarbeit\Evaluation\Seq.csv";
            string[] output = new string[seq.Count];
            StringBuilder s = new StringBuilder();

            for (int i = 0; i < seq.Count; i++) output[i] = seq[i].ToString();

            s.AppendLine(string.Join(";", output));
            
            File.WriteAllText(path, s.ToString());
        }
    }
}
