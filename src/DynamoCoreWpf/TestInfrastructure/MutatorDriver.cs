using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;

using Dynamo.Models;
using Dynamo.ViewModels;

namespace Dynamo.TestInfrastructure
{
    /// <summary>
    /// Class to handle driving test mutations into the graph
    /// </summary>
    public class MutatorDriver
    {
        private readonly DynamoViewModel dynamoViewModel;

        public MutatorDriver(DynamoViewModel dynamoViewModel)
        {
            this.dynamoViewModel = dynamoViewModel;
        }

        internal void RunMutationTests()
        {
            Random rand = new Random();

            List<AbstractMutator> mutators = new List<AbstractMutator>();
            mutators.Add(new IntegerSliderMutator(dynamoViewModel));
            //mutators.Add(new DeleteNodeMutator(dynamoViewModel));
            //mutators.Add(new ConnectorMutator(dynamoViewModel));

            new Thread(
                () =>
                    {

                        for (int i = 0; i < 1000; i++)
                        {
                            AbstractMutator mutator = mutators[rand.Next(mutators.Count)];

                            if (rand.NextDouble() < 0.01)
                            {
                                mutator = new ConnectorMutator(dynamoViewModel);
                            }

                            RunGraph();

                            var valueMap = ComputeValueMap();

                            int undoCount = mutator.Mutate(rand);

                            RunGraph();

                            Thread.Sleep(1);

                            UndoN(undoCount);

                            RunGraph();

                            Thread.Sleep(1);


                            if (!CompareValueMap(valueMap))
                                throw new MutationTestFailureException();

                        }
                    }).Start();


        }

        internal void RunMutationTests2()
        {
            String logTarget = dynamoViewModel.Model.Logger.LogPath + "MutationLog.log";

            StreamWriter writer = new StreamWriter(logTarget);

            writer.WriteLine("MutateTest Internal activate");

            System.Diagnostics.Debug.WriteLine("MutateTest Internal activate");

            new Thread(() =>
            {
                try
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();

                    var mutators = new List<AbstractMutator>();

                    foreach (Type type in assembly.GetTypes())
                    {
                        var attribute = Attribute.GetCustomAttribute(type, typeof(MutationTestAttribute));
                        if (attribute != null)
                        {
                            object[] args = new object[] { dynamoViewModel };
                            var classInstance = Activator.CreateInstance(type, args);
                            mutators.Add((AbstractMutator)classInstance);
                        }
                    }

                    foreach (var mutator in mutators)
                        InvokeTest(mutator, writer);
                }
                finally
                {
                    dynamoViewModel.Model.Logger.Log("Fuzz testing finished.");

                    writer.Flush();
                    writer.Close();
                    writer.Dispose();
                }

            }).Start();
        }

        private void InvokeTest(AbstractMutator mutator, StreamWriter writer)
        {
            bool passed = false;

            Type type = mutator.GetNodeType();
            if (type == null)
                return;

            var att = (MutationTestAttribute)Attribute.GetCustomAttribute(mutator.GetType(), 
                typeof(MutationTestAttribute));

            var nodes = dynamoViewModel.Model.CurrentWorkspace.Nodes.ToList();
            if (type != typeof(NodeModel))
                nodes = dynamoViewModel.Model.CurrentWorkspace.Nodes.Where(t => t.GetType() == type).ToList();

            if (nodes.Count == 0)
                return;

            try
            {
                Random rand = new Random(1);

                for (int i = 0; i < mutator.NumberOfLaunches; i++)
                {
                    NodeModel node = nodes[rand.Next(nodes.Count)];

                    writer.WriteLine("##### - Beginning run " + att.Caption + ": " + i);
                    writer.WriteLine("### - Beginning eval");

                    dynamoViewModel.UIDispatcher.Invoke(new Action(() =>
                    {
                        DynamoModel.RunCancelCommand runCancel =
                            new DynamoModel.RunCancelCommand(false, false);

                        dynamoViewModel.ExecuteCommand(runCancel);
                    }));
                    while (!dynamoViewModel.HomeSpaceViewModel.RunSettingsViewModel.RunButtonEnabled)
                    {
                        Thread.Sleep(10);
                    }

                    writer.WriteLine("### - Eval complete");
                    writer.Flush();

                    var engineController =
                        dynamoViewModel.EngineController;

                    passed = mutator.RunTest(node, engineController, writer);

                    if (!passed)
                    {
                        writer.WriteLine("### - Test failed");
                        break;
                    }
                }
            }
            catch(Exception ex)
            {
                dynamoViewModel.Model.Logger.Log(att.Caption + ": FAILED: " + ex.Message);
            }
            finally
            {
                dynamoViewModel.Model.Logger.Log(att.Caption + ": " + (passed ? "pass" : "FAIL"));
            }
        }




        protected void UndoN(int count)
        {
            dynamoViewModel.UIDispatcher.Invoke(
                new Action(
                    () =>
                    {
                        for (int i = 0; i < count; i++)
                        {
                            DynamoModel.UndoRedoCommand undoCommand =
                                new DynamoModel.UndoRedoCommand(
                                    DynamoModel.UndoRedoCommand.Operation.Undo);
                            dynamoViewModel.ExecuteCommand(undoCommand);
                        }
                    }));
        }

        protected void RunGraph()
        {
            dynamoViewModel.HomeSpace.Run();
            while (!dynamoViewModel.HomeSpace.RunSettings.RunEnabled)
            {
                Thread.Sleep(10);
            }
        }

        protected Dictionary<Guid, String> ComputeValueMap()
        {
            var valueMap = new Dictionary<Guid, String>();
            
            foreach (NodeModel node in dynamoViewModel.CurrentSpace.Nodes)
            {
                if (node.OutPorts.Count > 0)
                {
                    Guid guid = node.GUID;
                    Object data = node.GetValue(0, dynamoViewModel.Model.EngineController).Data;
                    String val = data != null ? data.ToString() : "null";
                    valueMap.Add(guid, val);
                }
            }

            return valueMap;
        }

        protected bool CompareValueMap(Dictionary<Guid, String> previousResult)
        {
            Dictionary<Guid, String> current = ComputeValueMap();

            if (current.Count != previousResult.Count)
                return false;

            return previousResult.Keys.All(guid => 
                current.ContainsKey(guid) && previousResult[guid] == current[guid]);
        }

    }

    internal class MutationTestFailureException : System.Exception { }
}