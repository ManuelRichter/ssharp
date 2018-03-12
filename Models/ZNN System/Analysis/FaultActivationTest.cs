using ISSE.SafetyChecking.Modeling;
using NUnit.Framework;
using SafetySharp.Analysis;
using SafetySharp.CaseStudies.ZNNSystem.Modeling;
using System;
using System.Collections;
using System.IO;
using System.Text;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
    [TestFixture]
    public class FaultActivationTest
    {
        private const int numberOfFaults = 7;
        private LearningParam lc = new LearningParam(300, 50, 0.5, 0.2, 100, false, true);
        private Model model = new Model(3,3);
        
        //logging
        private bool isLogging = true;
        private ArrayList logEpi = new ArrayList();
        private double sumRewardEpisode = 0;

        [Test]
        public void TestLearner()
        {
            if (model.Servers.Count == 0 || model.Clients.Count == 0)
                throw new Exception("There has to be at least one server and one client");

            Random r = new Random();
            int seed = r.Next(1000);

            StartLearning(seed);
        }

        public void StartLearning(int seed)
        {
            Random rand = new Random(seed);
            
            int stateSpaceSize = 128; //2^7
            int actionSpaceSize;
            if (model.Servers.Count > model.Clients.Count) actionSpaceSize = numberOfFaults * model.Servers.Count;
            else actionSpaceSize = numberOfFaults * model.Clients.Count;

            QValue[] qTable = new QValue[stateSpaceSize * actionSpaceSize];
            FaultyModel[] fms = new FaultyModel[lc.episodes];

            InitModel(ref fms);
            InitQTable(ref qTable);

            State currentState = new State();

            //simulate models
            foreach (FaultyModel fm in fms)
            {
                var simulator = new SafetySharpSimulator(fm.model);
                var modelSim = (Model)simulator.Model;

                foreach (Fault f in modelSim.Faults) f.Activation = Activation.Suppressed;

                BranchCoverage.StartCovering();

                if (lc.switchToSarsa)
                {
                    SimulateEpisodeSarsa(rand, ref qTable, currentState, fm, simulator, modelSim);
                }
                else
                {
                    SimulateEpisodeQ(rand, ref qTable, currentState, fm, simulator, modelSim);
                }
                
                PrintEpisodeSummary(ref modelSim);
                Logging();

                //episode cleanup
                currentState = new State();
                BranchCoverage.ResetPastBranches();
            }

            Console.WriteLine("End of Test - seed:" + seed);
        }

        private void SimulateEpisodeQ(Random rand,ref QValue[] qTable, State currentState, FaultyModel fm, SafetySharpSimulator simulator, Model modelSim)
        {
            for (int currentStep = 0; currentStep < lc.steps; currentStep++)
            {
                Act currentAction = ChooseAction(ref qTable, rand, currentState);
                fm.AddFaultCondition(currentAction.ConvertToFC(currentStep));  //use current step to activate

                //activate fault
                foreach (FaultCondition fc in fm.fcs)
                {
                    if (currentStep == fc.stepToActivate) ChooseServerToFault(modelSim.Faults, fc);
                }

                //process fault
                simulator.SimulateStep();

                if (fm.model.ProxyObserver.ReconfigurationState == ReconfStates.Failed) break; //end episode

                FeedbackQ(ref qTable, currentState, currentAction);
                currentState.SetFault(currentAction.GetFault(), true); //update state

            }//episode ends here
        }

        private void SimulateEpisodeSarsa(Random rand, ref QValue[] qTable, State currentState, FaultyModel fm, SafetySharpSimulator simulator, Model modelSim)
        {
            Act nextAction;
            Act currentAction = ChooseAction(ref qTable, rand, currentState);

            for (int currentStep = 0; currentStep < lc.steps; currentStep++)
            {
                //activate fault
                fm.AddFaultCondition(currentAction.ConvertToFC(currentStep));  //use current step to activate
                foreach (FaultCondition fc in fm.fcs)
                {
                    if (currentStep == fc.stepToActivate) ChooseServerToFault(modelSim.Faults, fc);
                }

                //process fault
                simulator.SimulateStep();

                if (fm.model.ProxyObserver.ReconfigurationState == ReconfStates.Failed) break; //end episode

                nextAction = ChooseAction(ref qTable, rand, currentState);

                FeedbackSarsa(ref qTable, currentState, currentAction, nextAction);
                currentState.SetFault(currentAction.GetFault(), true); //update state
                currentAction = nextAction;
            }
            //episode ends here
        }
        
        private void InitModel(ref FaultyModel[] fms)
        {
            for (int i = 0; i < lc.episodes; i++)
            { 
                fms[i] = new FaultyModel(new Model(model.Clients.Count, model.Servers.Count));
            }
        }

        private void InitQTable(ref QValue[] qTable)
        {
            int i = 0;
            int maxSystems;

            if (model.Servers.Count > model.Clients.Count) maxSystems = model.Servers.Count;
            else maxSystems = model.Clients.Count;

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
                                            for (int nserv = 0; nserv < maxSystems; nserv++)
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

        private Act ChooseAction(ref QValue[] qTable, Random rand, State state)
        {
            if (lc.epsilonDecrements && lc.epsilon > 0.01) lc.epsilon = lc.epsilon - 0.02;

            if (rand.Next(100) < lc.epsilon)
            {
                return Explore(rand);
            }
            else return Exploit(ref qTable, state);
        }

        private Act Explore(Random rand)
        {
            int fault = rand.Next(numberOfFaults);
            
            if (fault == 0)
            {
                int clients = rand.Next(model.Clients.Count);
                return new Act(fault, clients);
            }
            int server = rand.Next(model.Servers.Count);
            return new Act(fault,server);
        }

        private Act Exploit(ref QValue[] qTable, State state)
        {
            return GetBestAction(ref qTable, state);
        }
        
        //returns action with highest reward for given state
        private Act GetBestAction(ref QValue[] qTable, State state)
        {
            double maxReward = 0;
            int bestIndex = 1;

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

        private int GetIndexOfQValue(ref QValue[] qTable, QValue QtoLookfor)
        {
            for (int i = 0; i < qTable.Length; i++)
                if (qTable[i].Equals(QtoLookfor)) return i;

            return -1;
        }

        private void FeedbackQ(ref QValue[] qTable, State currentState, Act currentAction)
        {
            QValue currentQ = new QValue(currentState, currentAction);

            State nextState = new State(currentState, currentAction);
            int index = GetIndexOfQValue(ref qTable, new QValue(nextState, GetBestAction(ref qTable, nextState))); //Q(s_new,a_new)
            QValue nextBestQ = qTable[index];

            double reward = BranchCoverage.GetReward();

            logEpi.Add(reward);
            sumRewardEpisode = sumRewardEpisode + reward;

            Console.WriteLine(currentAction.ToString() + " reward: " + reward);

            //Q_new(s,a) = Q(s,a) + alpha*(r + gamma*max_a_new(Q(s_new,a_new) - Q(s,a)))
            double newQ = currentQ.GetReward() + lc.alpha * (reward + lc.gamma * nextBestQ.GetReward() - currentQ.GetReward());
            qTable[GetIndex(ref qTable, currentQ)].SetReward(newQ);
        }

        private void FeedbackSarsa(ref QValue[] qTable, State currentState, Act currentAction, Act nextAction)
        {
            QValue currentQ = new QValue(currentState, currentAction);

            State nextState = new State(currentState, currentAction);

            int index = GetIndexOfQValue(ref qTable, new QValue(nextState, nextAction));
            QValue nextQ = qTable[index];
            double reward = BranchCoverage.GetReward();

            logEpi.Add(reward);
            sumRewardEpisode = sumRewardEpisode + reward;

            Console.WriteLine(currentAction.ToString() + " reward: " + reward);

            //Q_new(s,a) = Q(s,a) + alpha*(r + gamma*Q(s',a') - Q(s,a)))
            double newQ = currentQ.GetReward() + lc.alpha * (reward + lc.gamma * nextQ.GetReward() - currentQ.GetReward());
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
                    if (counter == fc.serverToFault || faultname == "ServerSelectionFails")
                    {
                        faults[i].Activation = Activation.Forced;
                        return;
                    }
                    counter++;
                }
            }
        }
        
        private void Logging()
        {
            if (isLogging) LogEpisodeToCSV(logEpi);
        }

        private void PrintEpisodeSummary(ref Model modelSim)
        {
            Console.WriteLine("sum this episode:" + sumRewardEpisode);
            for (int i = 0; i < modelSim.Servers.Count; i++) Console.WriteLine("Completed Queries of server " + i + ": " + modelSim.Servers[i].QueryCompleteCount + " active:" + modelSim.Servers[i].IsServerActive + " dead:" + modelSim.Servers[i].IsServerDead);
            sumRewardEpisode = 0;
        }

        private void PrintQTable(ref QValue[] qTable)
        {
            for (int i = 0; i < qTable.Length; i++)
            {
                Console.WriteLine(qTable[i].ToTable());
            }
        }
        
        private void LogEpisodeToCSV(ArrayList episode)
        {
            string path = FormatPath();

            string[] output = new string[episode.Count];
            StringBuilder s = new StringBuilder();
            s.AppendLine(lc.steps.ToString() + ";" + lc.episodes.ToString());
            for (int i = 0; i < episode.Count; i++) s.AppendLine(episode[i].ToString());

            File.WriteAllText(path, s.ToString());
        }

        private string FormatPath()
        {
            string path = @".\..\..\..\Evaluation\";

            if (!lc.epsilonDecrements && lc.epsilon == 100) return (path + "EpisodesRandom.csv");

            if (lc.switchToSarsa) path = path + "SEpisodes";
            else path = path + "QEpisodes";

            if (lc.epsilonDecrements)
            {
                if (lc.gamma == 0.9) path = path + "DecEpsG9.csv";
                else path = path + "DecEpsG2.csv";
            }
            else
            {
                if (lc.gamma == 0.9) path = path + "25G9.csv";
                else path = path + "25G2.csv";
            }

            return path;
        }
    }
}