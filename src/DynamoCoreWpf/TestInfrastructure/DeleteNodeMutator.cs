using System.Collections.Generic;

using Dynamo.Models;
using Dynamo.ViewModels;
using System;
using System.Linq;
using System.IO;
using System.Threading;
using Dynamo.DSEngine;

namespace Dynamo.TestInfrastructure
{
    /// <summary>
    /// Mutator that deletes a random node
    /// </summary>

    [MutationTest("DeleteNodeMutator")]
    class DeleteNodeMutator : AbstractMutator
    {
        public DeleteNodeMutator(DynamoViewModel viewModel)
            : base(viewModel)
        {
        }


        public override bool RunTest(NodeModel node, EngineController engine, StreamWriter writer)
        {
            return false;
        }

        public override int Mutate(NodeModel node)
        {
            throw new NotImplementedException();
        }

        public override int Mutate(Random rand)
        {
            List<NodeModel> nodes = DynamoViewModel.CurrentSpace.Nodes.ToList();

            int sliderCount = nodes.Count;

            if (sliderCount == 0)
                return 0;

            NodeModel node = nodes[rand.Next(sliderCount)];

            this.DynamoViewModel.UIDispatcher.Invoke(new Action(() =>
            {
                DynamoModel.DeleteModelCommand delCommand =
                    new DynamoModel.DeleteModelCommand(node.GUID);

                DynamoViewModel.ExecuteCommand(delCommand);
            }));

            //We've performed a single delete
            return 1;
        }
    }
}