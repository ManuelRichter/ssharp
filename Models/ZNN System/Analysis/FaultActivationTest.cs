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
    /// <summary>
    /// Uses the reinforcement learning algorithms QLearning and Sarsa to find the optimal sequence of faults of the model in regards to branch coverage
    /// </summary>
    [TestFixture]
    public class LearningFaultActivationTest
    {
        /// <summary>
        /// The number of faults of the underlying model
        /// </summary>
        private const int numberOfFaults = 7;
        
        /// <summary>
        /// Contains all parameters that are important for the learning procedure
        /// </summary>
        private LearningParam lc = new LearningParam(300, 50, 0.5, 0.9, 100, false, true);
        
        /// <summary>
        /// One instance of the model with the number of clients and servers that should be simulated
        /// </summary>
        private Model model = new Model(3,3);
        
        /// <summary>
        /// Activates/deactivates logging to csv-file
        /// </summary>
        private bool isLogging = true;
        
        /// <summary>
        /// Array with rewards of the current episode
        /// </summary>
        private ArrayList logEpi = new ArrayList();

        /// <summary>
        /// Sum of all rewards of the current episode
        /// </summary>
        private double sumRewardEpisode = 0;
        
        /// <summary>
        /// Runs an reinforcement learning algorithm on the model
        /// </summary>
        [Test]
        public void TestLearner()
        {
            if (model.Servers.Count == 0 || model.Clients.Count == 0)
                throw new Exception("There has to be at least one server and one client");

            Random r = new Random();
            int seed = r.Next(1000);

            StartLearning(seed);
        }

        /// <summary>
        /// Commences the learning 
        /// </summary>
        /// <paramref name="seed"/> seed for the replication of results
        public void StartLearning(int seed)
        {
            Random rand = new Random(seed);
            
            int stateSpaceSize = 128; //2^numberOfFaults
            int actionSpaceSize;
            if (model.Servers.Count > model.Clients.Count) actionSpaceSize = numberOfFaults * model.Servers.Count;
            else actionSpaceSize = numberOfFaults * model.Clients.Count;

            QValue[] qTable = new QValue[stateSpaceSize * actionSpaceSize];
            FaultyModel[] fms = new FaultyModel[lc.episodes];

            InitModels(ref fms);
            InitQTable(ref qTable);

            State currentState = new State();

            //simulate models/episodes
            foreach (FaultyModel fm in fms)
            {
                var simulator = new SafetySharpSimulator(fm.model);
                var modelSim = (Model)simulator.Model;

                foreach (Fault f in modelSim.Faults) f.Activation = Activation.Suppressed;

                BranchCoverage.StartCovering();

                if (lc.switchToSarsa)
                {
                    SimulateEpisodeSarsa(rand, ref qTable, fm, simulator, modelSim);
                }
                else
                {
                    SimulateEpisodeQ(rand, ref qTable, fm, simulator, modelSim);
                }
                
                PrintEpisodeSummary(ref modelSim);
                Logging();

                //episode cleanup
                currentState = new State();
                BranchCoverage.ResetPastBranches();
            }

            Console.WriteLine("End of Test - seed:" + seed);
        }

        /// <summary>
        /// Uses the QLearning algorithm to update the QValue-Table and simulates all steps for one episode
        /// </summary>
        /// <paramref name="rand"/> the Random object with above mentioned seed
        /// <paramref name="qTable"/> contains long term reward estimates of the state-action pairs
        /// <paramref name="fm"/> containts the model that should be simulated
        /// <paramref name="simulator"/> simulates the model steps
        /// <param name="modelSim"/> contains the current simulated model
        private void SimulateEpisodeQ(Random rand,ref QValue[] qTable, FaultyModel fm, SafetySharpSimulator simulator, Model modelSim)
        {
            State currentState = new State();
            
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

                //in case there are no more servers left to interact with end episode
                if (fm.model.ProxyObserver.ReconfigurationState == ReconfStates.Failed) break;

                FeedbackQ(ref qTable, currentState, currentAction);
                currentState.SetFault(currentAction.GetFault(), true); //update state

            }//episode ends here
        }

        /// <summary>
        /// Uses the SARSA algorithm to update the QValue-Table and simulates all steps for one episode
        /// </summary>
        /// <paramref name="rand"/> the Random object with above mentioned seed
        /// <paramref name="qTable"/> contains long term reward estimates of the state-action pairs
        /// <paramref name="fm"/> containts the model that should be simulated
        /// <paramref name="simulator"/> simulates the model steps
        /// /// <param name="modelSim"/> contains the current simulated model
        private void SimulateEpisodeSarsa(Random rand, ref QValue[] qTable, FaultyModel fm, SafetySharpSimulator simulator, Model modelSim)
        {
            State currentState = new State();
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

                //in case there are no more servers left to interact with end episode
                if (fm.model.ProxyObserver.ReconfigurationState == ReconfStates.Failed) break;

                nextAction = ChooseAction(ref qTable, rand, currentState);

                FeedbackSarsa(ref qTable, currentState, currentAction, nextAction);
                currentState.SetFault(currentAction.GetFault(), true); //update state
                currentAction = nextAction;
            }
            //episode ends here
        }

        /// <summary>
        /// Instantiates a model for each episode
        /// </summary>
        private void InitModels(ref FaultyModel[] fms)
        {
            for (int i = 0; i < lc.episodes; i++)
            { 
                fms[i] = new FaultyModel(new Model(model.Clients.Count, model.Servers.Count));
            }
        }

        /// <summary>
        /// Initialises the QTable with default values
        /// </summary>
        private void InitQTable(ref QValue[] qTable)
        {
            int i = 0;
            int maxSystems;

            //as the list of client, proxy and server faults is mixed we have to get the max(clients,server)
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

        /// <summary>
        /// Chooses the next action based on the epsilon-greedy approach
        /// </summary>
        /// <paramref name="qTable"/> contains long term reward estimates of the state-action pairs
        /// <paramref name="rand"/> the Random object with above mentioned seed
        /// <paramref name="state"/> contains the agents current state
        private Act ChooseAction(ref QValue[] qTable, Random rand, State state)
        {
            if (lc.epsilonDecrements && lc.epsilon > 0.01) lc.epsilon = lc.epsilon - 0.02;

            if (rand.Next(100) < lc.epsilon)
            {
                return Explore(rand);
            }
            else return Exploit(ref qTable, state);
        }

        /// <summary>
        /// Chooses the next action randomly
        /// </summary>
        private Act Explore(Random rand)
        {
            int fault = rand.Next(numberOfFaults);

            //ConnectionToProxyFails is the only clientside fault
            if (fault == 0) 
            {
                int clients = rand.Next(model.Clients.Count);
                return new Act(fault, clients);
            }
            //else handle it like a serverside fault
            int server = rand.Next(model.Servers.Count);
            return new Act(fault,server);
        }

        /// <summary>
        /// Chooses the best action for the current state from the QTable 
        /// </summary>
        /// <paramref name="qTable"/> contains long term reward estimates of the state-action pairs
        /// <paramref name="state"/> contains the agents current state
        private Act Exploit(ref QValue[] qTable, State state)
        {
            return GetBestAction(ref qTable, state);
        }

        /// <summary>
        /// Returns the action with the highest reward for a given state
        /// </summary>
        /// <paramref name="qTable"/> contains long term reward estimates of the state-action pairs
        /// <paramref name="state"/> contains the agents current state
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

        /// <summary>
        /// Returns the index of a given state-action pair
        /// </summary>
        /// <paramref name="qTable"/> contains long term reward estimates of the state-action pairs
        /// <paramref name="QtoLookfor"/> the state-action pair to look for
        private int GetIndex(ref QValue[] qTable, QValue qToLookFor)
        {
            for (int i = 0; i < qTable.Length; i++)
            {
                if (qTable[i].Equals(qToLookFor)) return i;
            }
            return -1;
        }

        /// <summary>
        /// Calculates the long term reward for the current state and the current action given he follows the best policy in the next step (QLearning)
        /// </summary>
        /// <paramref name="qTable"/> contains long term reward estimates of the state-action pairs
        /// <paramref name="currentState"/> contains the agents current state
        /// <paramref name="currentAction"/> contains the agents current used action
        private void FeedbackQ(ref QValue[] qTable, State currentState, Act currentAction)
        {
            QValue currentQ = new QValue(currentState, currentAction);

            State nextState = new State(currentState, currentAction);
            int index = GetIndex(ref qTable, new QValue(nextState, GetBestAction(ref qTable, nextState))); //Q(s_new,a_new)
            QValue nextBestQ = qTable[index];

            double reward = BranchCoverage.GetReward();

            logEpi.Add(reward);
            sumRewardEpisode = sumRewardEpisode + reward;

            Console.WriteLine(currentAction.ToString() + " reward: " + reward);

            //Q_new(s,a) = Q(s,a) + alpha*(r + gamma*max_a_new(Q(s_new,a_new) - Q(s,a)))
            double newQ = currentQ.GetReward() + lc.alpha * (reward + lc.gamma * nextBestQ.GetReward() - currentQ.GetReward());
            qTable[GetIndex(ref qTable, currentQ)].SetReward(newQ);
        }

        /// <summary>
        /// Calculates the long term reward for the current state, the current action and the next action (SARSA)
        /// </summary>
        /// <paramref name="qTable"/> contains long term reward estimates of the state-action pairs
        /// <paramref name="currentState"/> contains the agents current state
        /// <paramref name="currentAction"/> contains the agents current used action
        /// <paramref name="currentState"/> contains the agents next action he is going to use
        private void FeedbackSarsa(ref QValue[] qTable, State currentState, Act currentAction, Act nextAction)
        {
            QValue currentQ = new QValue(currentState, currentAction);

            State nextState = new State(currentState, currentAction);

            int index = GetIndex(ref qTable, new QValue(nextState, nextAction));
            QValue nextQ = qTable[index];
            double reward = BranchCoverage.GetReward();

            logEpi.Add(reward);
            sumRewardEpisode = sumRewardEpisode + reward;

            Console.WriteLine(currentAction.ToString() + " reward: " + reward);

            //Q_new(s,a) = Q(s,a) + alpha*(r + gamma*Q(s',a') - Q(s,a)))
            double newQ = currentQ.GetReward() + lc.alpha * (reward + lc.gamma * nextQ.GetReward() - currentQ.GetReward());
            qTable[GetIndex(ref qTable, currentQ)].SetReward(newQ);
        }

        /// <summary>
        /// Activates the fault fc for a specific server
        /// </summary>
        /// <paramref name="faults"/> list of all faults 
        /// <paramref name="fc"/> the fault that should be activated
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

        /// <summary>
        /// Loggs the rewards per episode to a csv-file
        /// </summary>
        private void Logging()
        {
            if (isLogging) LogEpisodeToCSV(logEpi);
        }

        /// <summary>
        /// Outputs information about cumulative reward of an episode and server status
        /// </summary>
        private void PrintEpisodeSummary(ref Model modelSim)
        {
            Console.WriteLine("sum this episode:" + sumRewardEpisode);
            for (int i = 0; i < modelSim.Servers.Count; i++) Console.WriteLine("Completed Queries of server " + i + ": " + modelSim.Servers[i].QueryCompleteCount + " active:" + modelSim.Servers[i].IsServerActive + " dead:" + modelSim.Servers[i].IsServerDead);
            sumRewardEpisode = 0;
        }

        /// <summary>
        /// Outputs the current qTable
        /// </summary>
        private void PrintQTable(ref QValue[] qTable)
        {
            for (int i = 0; i < qTable.Length; i++)
            {
                Console.WriteLine(qTable[i].ToTable());
            }
        }

        /// <summary>
        /// Logs the rewards of an episode to a csv-file
        /// </summary>
        private void LogEpisodeToCSV(ArrayList episode)
        {
            string path = FormatPath();

            string[] output = new string[episode.Count];
            StringBuilder s = new StringBuilder();

            s.AppendLine(lc.steps.ToString() + ";" + lc.episodes.ToString());
            for (int i = 0; i < episode.Count; i++) s.AppendLine(episode[i].ToString());

            File.WriteAllText(path, s.ToString());
        }

        /// <summary>
        /// Generates the path to the csv file in dependence of learning parameters for easier evaluation
        /// </summary>
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