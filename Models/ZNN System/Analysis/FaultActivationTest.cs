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


        [Test]
        public void TestQLearner()
        {
            Random r = new Random();
            
            for (int i = 0; i < 1; i++)
            {
                int seed = r.Next(1000);
                TestFaultActivation(300, 50, 3, 3, seed, 100);
            }
        }

        private int modelCount = 0; //number of models to instantiate/equivalent to episode count
        private int simSteps = 0; //number of steps to simulate

        private int serverCount = 0;
        private int clientCount = 0;
        private int numberOfFaults = 7;

        private int seed = 0;
        private double epsilon = 0; //% exploration

        //logging
        private ArrayList logEpi = new ArrayList();
        double sum = 0;

        public void TestFaultActivation(int episodes,int steps,int servers,int clients, int seed, int epsilon)
        {
            Init(episodes, steps, servers, clients, seed, epsilon);
            
            Random rand = new Random(seed);

            //const for qLearning
            int stateSize = 128; //2^7
            int actionSize = numberOfFaults * serverCount;

            QValue[] qTable = new QValue[stateSize * actionSize]; //ca. 2600 entries
            FaultyModel[] fms = new FaultyModel[modelCount];
            
            int indexEpisode = 0;

            //logging
            bool logSession = false;
            bool logEpisode = true;
            
            //instantiate models
            for (int i = 0; i < modelCount; i++) fms[i] = new FaultyModel(new Model(clientCount, serverCount));

            //instatiate qTable
            InitQTable(ref qTable);
            
            State currentState = new State();

            //simulate models
            foreach (FaultyModel fm in fms)
            {
                var simulator = new SafetySharpSimulator(fm.model);
                var modelSim = (Model)simulator.Model;
                double sumCoverage = 0;

                foreach (Fault f in modelSim.Faults)
                    f.Activation = Activation.Suppressed;

                BranchCoverage.StartCovering();
                
                for (int i = 0; i < simSteps; i++)
                {
                    Act currentAction = ChooseAction(rand, currentState, ref qTable, indexEpisode);
                    //BranchCoverage.SetServer(currentAction.GetServer()); //to differentiate between servers
                    fm.AddFaultCondition(currentAction.ConvertToFC(i));  //use current step to activate
                    
                    //activate fault
                    foreach (FaultCondition fc in fm.fcs)
                    {
                        if (i == fc.stepToActivate) ChooseServerToFault(modelSim.Faults, fc);
                    }

                    //process fault
                    simulator.SimulateStep();

                    if (fm.model.ProxyObserver.ReconfigurationState == ReconfStates.Failed) break; //end episode

                    Feedback(ref qTable, currentState, currentAction);
                    currentState.SetFault(currentAction.GetFault(), true); //update state
                    
                }
                //episode ends here
                if (logEpisode) LogEpisodeToCSV(logEpi);
                indexEpisode++;
                Console.WriteLine("sum this episode:" + sum);
                sum = 0;
                //episode cleanup
                currentState = new State();
                BranchCoverage.ResetPastBranches();
                
                for (int i = 0; i < modelSim.Servers.Count; i++) Console.WriteLine("Completed Queries of server " + i + ": " + modelSim.Servers[i].QueryCompleteCount + " active:" + modelSim.Servers[i].IsServerActive + " dead:" + modelSim.Servers[i].IsServerDead);

            }

            //Log session
            //if (logSession) LogSession(episodes,steps,servers,clients,seed,epsilon,bestIndex,bestOfAllModels,true);
            
            Console.WriteLine("End of TestFaultActivation");
        }

        private void Init(int episodes, int steps, int servers, int clients, int seed, int epsilon)
        {
            modelCount = episodes;
            simSteps = steps;
            serverCount = servers;
            clientCount = clients;
            this.seed = seed;
            this.epsilon = epsilon;
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
        }

        private Act ChooseAction(Random rand, State state, ref QValue[] qTable,int indexEpisode)
        {
            //if (epsilon > 0.01) epsilon = epsilon - 0.02;

            if (rand.Next(100) < epsilon) 
            {
                return Explore(rand);
            }
            else return Exploit(ref qTable, state);
        }

        private Act Explore(Random rand)
        {
            //Console.WriteLine("Explore:");
            return new Act(rand.Next(numberOfFaults), rand.Next(serverCount));
        }

        private Act Exploit(ref QValue[] qTable, State state)
        {
            //Console.WriteLine("Exploit in State:" + state.ToString());
            return GetBestAction(ref qTable, state);
        }

        //returns action with highest reward for given state
        private Act GetBestAction(ref QValue[] qTable, State state)
        {
            double maxReward = 0;
            int bestIndex = 6;

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
            double alpha = 0.5;
            double gamma = 0.9;
            QValue currentQ = new QValue(currentState, currentAction);

            State nextState = new State(currentState,currentAction);
            QValue nextBestQ = new QValue(nextState, GetBestAction(ref qTable, nextState)); //Q(s_new,a_new)

            double reward = BranchCoverage.GetReward();
            logEpi.Add(reward);
            sum = sum + reward;
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

        /// <summary>
        /// activates the fault for a specific server
        /// </summary>
        private static void ChooseServerToFault(Fault[] faults, FaultCondition fc)
        {
            string faultname = "";

            switch (fc.faultNumber)
            {
                case 0:
                    faultname = "ConnectionToProxyFails";
                    break;
                case 1:
                    faultname = "ServerSelectionFails";
                    break;
                case 2:
                    faultname = "SetServerFidelityFails";
                    break;
                case 3:
                    faultname = "ServerDeath";
                    break;
                case 4:
                    faultname = "ServerCannotDeactivated";
                    break;
                case 5:
                    faultname = "ServerCannotActivated";
                    break;
                case 6:
                    faultname = "CannotExecuteQueries";
                    break;
            }

            int counter = 0;

            for (int i = 0; i < faults.Length; i++)
            {
                if (faults[i].Name == faultname)
                { 
                    if (counter == fc.serverToFault || faultname == "ServerSelectionFails") //ssf appeares only once in faults[]
                    {
                        faults[i].Activation = Activation.Forced;
                        //Console.WriteLine("Fault enforced");
                        return;
                    }
                    counter++;
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
            string path = @".\..\..\..\Evaluation\Episode" + episode +".csv";
            string[] output = new string[qTable.Length];
            StringBuilder s = new StringBuilder();

            for (int i = 0; i < qTable.Length; i++) output[i] = qTable[i].ToTable();
            
            s.AppendLine(string.Join(";", output));
            
            File.WriteAllText(path, s.ToString());
        }

        private void LogSession(int episodes, int steps, int servers, int clients, int seed, int epsilon, string seq,int bestEpisode, double bestReward, bool asTable)
        {
            string sa, sb;
            string path;

            if (asTable)
            {
                path = @".\..\..\..\Evaluation\EvalTable.csv";
                sa = episodes + ";" + steps + ";" + servers + ";" + clients + ";" + seed + ";" + epsilon + "|";
                sb = seq + ";" + (bestEpisode + 1) + ";" + bestReward + "\n";
            } else
            {
                path = @".\..\..\..\Evaluation\Eval.csv";
                sa = "Epi:" + episodes + " step" + steps + " S:" + servers + " C:" + clients + " seed:" + seed + "e:" + epsilon + " ";
                sb = "" + seq + " in episode:" + (bestEpisode+1) + " with " + bestReward + " coverage\n";
            }
            File.AppendAllText(path, sa + sb);
        }

        private void LogEpisodeToCSV(ArrayList episode)
        {
            string path = @".\..\..\..\Evaluation\EpisodesRandom.csv";
            string[] output = new string[episode.Count];
            StringBuilder s = new StringBuilder();
            s.AppendLine(simSteps.ToString()+ ";" + modelCount.ToString());
            for (int i = 0; i < episode.Count; i++) s.AppendLine(episode[i].ToString());
            
            File.WriteAllText(path, s.ToString());
        }
    }
}
