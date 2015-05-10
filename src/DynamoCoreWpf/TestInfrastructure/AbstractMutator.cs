using System.Collections.Generic;
using System.Threading;

using Dynamo.Models;
using Dynamo.ViewModels;
using System;
using System.IO;
using Dynamo.DSEngine;

namespace Dynamo.TestInfrastructure
{
    abstract class AbstractMutator
    {
        //Convienence state, the presence of this state cache means that
        //usage of this mutator should be short lived
        protected DynamoViewModel DynamoViewModel;
        protected DynamoModel DynamoModel;

        protected AbstractMutator(DynamoViewModel dynamoViewModel)
        {
            this.DynamoViewModel = dynamoViewModel;
            this.DynamoModel = dynamoViewModel.Model;
        }

        /// <summary>
        /// Returns the number of undoable operations that have been performed 
        /// </summary>
        /// <returns></returns>
        public abstract int Mutate(NodeModel node);

        public abstract int Mutate(Random rand);
                                                                                                   

        public abstract bool RunTest(NodeModel node, EngineController engine, StreamWriter writer);

        public virtual Type GetNodeType()
        {
            return typeof(NodeModel);
        }

        public virtual int NumberOfLaunches
        {
            get { return 1000; }
        }

        protected void UndoN(int count)
        {
            DynamoViewModel.UIDispatcher.Invoke(
                new Action(
                    () =>
                    {
                        for (int i = 0; i < count; i++)
                        {
                            DynamoModel.UndoRedoCommand undoCommand =
                                new DynamoModel.UndoRedoCommand(
                                    DynamoModel.UndoRedoCommand.Operation.Undo);
                            DynamoViewModel.ExecuteCommand(undoCommand);
                        }
                    }));
        }

        protected void RunGraph()
        {
            DynamoViewModel.HomeSpace.Run();
            while (!DynamoViewModel.HomeSpace.RunSettings.RunEnabled)
            {
                Thread.Sleep(10);
            }
        }

    }
}
