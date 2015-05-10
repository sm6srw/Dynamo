using System;
using System.Collections.Generic;
using System.Linq;

using Dynamo.Models;
using Dynamo.ViewModels;
using System.Reflection;
using System.IO;
using System.Threading;
using Dynamo.DSEngine;

namespace Dynamo.TestInfrastructure
{
    [MutationTest("IntegerSliderMutator")]
    class IntegerSliderMutator : AbstractMutator
    {
        public IntegerSliderMutator(DynamoViewModel viewModel)
            : base(viewModel)
        {            
        }

        public override Type GetNodeType()
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string assemblyDir = Path.GetDirectoryName(assemblyPath);
            string pathToNodesDll = assemblyDir + "\\nodes\\DSCoreNodesUI.dll";
            Assembly assembly = Assembly.LoadFile(pathToNodesDll);
            Type type = assembly.GetType("Dynamo.Nodes.IntegerSlider");

            return type;
        }

        public override bool RunTest(NodeModel node, EngineController engine, StreamWriter writer)
        {
            return false;
        }

        public override int Mutate(Random rand)
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string assemblyDir = Path.GetDirectoryName(assemblyPath);
            string pathToNodesDll = assemblyDir + "\\nodes\\DSCoreNodesUI.dll";
            Assembly assembly = Assembly.LoadFile(pathToNodesDll);

            Type type = assembly.GetType("DSCoreNodesUI.Input.IntegerSlider");

            List<NodeModel> integerSliders = 
                DynamoViewModel.CurrentSpace.Nodes.Where(n => n.GetType() == type).ToList();

            int sliderCount = integerSliders.Count;

            if (sliderCount == 0)
                return 0;

            NodeModel node = integerSliders[rand.Next(sliderCount)];

            PropertyInfo propInfo = type.GetProperty("Min");
            dynamic propertyMin = propInfo.GetValue(node, null);
            propInfo = type.GetProperty("Max");
            dynamic propertyMax = propInfo.GetValue(node, null);

            int min = 0;
            int max = 0;
            int returnCode = 0;

            if (Int32.TryParse(propertyMin.ToString(), out min) &&
                Int32.TryParse(propertyMax.ToString(), out max))
            {
                string value = (rand.Next(min, max)).ToString();

                DynamoViewModel.UIDispatcher.Invoke(new Action(() =>
                {
                    DynamoModel.UpdateModelValueCommand updateValue =
                        new DynamoModel.UpdateModelValueCommand(System.Guid.Empty, node.GUID, "Value", value);
                    DynamoViewModel.ExecuteCommand(updateValue);
                }));

                returnCode = 1;
            }

            return returnCode;   
        }

        public override int Mutate(NodeModel node)
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string assemblyDir = Path.GetDirectoryName(assemblyPath);
            string pathToNodesDll = assemblyDir + "\\nodes\\DSCoreNodesUI.dll";
            Assembly assembly = Assembly.LoadFile(pathToNodesDll);

            Type type = assembly.GetType("Dynamo.Nodes.IntegerSlider");

            PropertyInfo propInfo = type.GetProperty("Min");
            dynamic propertyMin = propInfo.GetValue(node, null);
            propInfo = type.GetProperty("Max");
            dynamic propertyMax = propInfo.GetValue(node, null);

            int min = 0;
            int max = 0;
            int returnCode = 0;
            Random rand = new Random();

            if (Int32.TryParse(propertyMin.ToString(), out min) &&
                Int32.TryParse(propertyMax.ToString(), out max))
            {
                string value = (rand.Next(min, max)).ToString();

                DynamoViewModel.UIDispatcher.Invoke(new Action(() =>
                {
                    DynamoModel.UpdateModelValueCommand updateValue =
                        new DynamoModel.UpdateModelValueCommand(System.Guid.Empty, node.GUID, "Value", value);
                    DynamoViewModel.ExecuteCommand(updateValue);
                }));

                returnCode = 1;
            }

            return returnCode;
        }        
    }
}