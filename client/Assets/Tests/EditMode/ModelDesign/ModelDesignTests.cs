using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace ModelDesign.Tests
{
    public sealed class ModelDesignTests
    {
        private static ModelWorkspace CreateChain(params LayerDefinition[] layers)
        {
            ModelWorkspace workspace = new ModelWorkspace();
            LayerDefinition previous = workspace.Input;
            foreach (LayerDefinition layer in layers)
            {
                workspace.AddLayer(layer);
                workspace.Connect(previous, layer);
                previous = layer;
            }
            workspace.Connect(previous, workspace.Output);
            return workspace;
        }

        private static void AssertOwnership(ModelWorkspace workspace)
        {
            HashSet<LayerDefinition> seen = new HashSet<LayerDefinition>();
            foreach (ModelDefinition model in workspace.Models)
            {
                Assert.IsNotEmpty(model.Layers);
                foreach (LayerDefinition layer in model.Layers)
                {
                    Assert.IsTrue(seen.Add(layer), "A block belongs to more than one chain.");
                    Assert.AreSame(model, workspace.GetModel(layer));
                }
            }
            Assert.IsTrue(seen.Contains(workspace.Input));
            Assert.IsTrue(seen.Contains(workspace.Output));
        }

        [Test]
        public void WorkspaceStartsWithTwoFixedDetachedBlocks()
        {
            using (ModelWorkspace workspace = new ModelWorkspace())
            {
                Assert.AreEqual(2, workspace.Models.Count);
                Assert.AreEqual(LayerType.Input, workspace.Input.Type);
                Assert.AreEqual(LayerType.Output, workspace.Output.Type);
                Assert.AreEqual(10, workspace.Output.Units);
                Assert.AreEqual(ActivationType.Softmax, workspace.Output.Activation);
                Assert.IsEmpty(workspace.ValidationIssues);
                Assert.IsFalse(ModelJsonSerializer.TrySerialize(workspace, out string json,
                    out IReadOnlyList<ValidationIssue> issues));
                Assert.IsNull(json);
                Assert.IsNotEmpty(issues);
                AssertOwnership(workspace);
            }
        }

        [Test]
        public void MlpSerializesInputAndOutputIntoExistingSchema()
        {
            using (ModelWorkspace workspace = CreateChain(LayerDefinition.Dense(32, ActivationType.ReLU)))
            {
                Assert.AreEqual("{\"modelType\":\"mlp\",\"input\":{\"shape\":[28,28,1]},\"layers\":[" +
                    "{\"type\":\"dense\",\"units\":32,\"activation\":\"relu\"}," +
                    "{\"type\":\"dense\",\"units\":10,\"activation\":\"softmax\"}]}", ModelJsonSerializer.Serialize(workspace));
                AssertOwnership(workspace);
            }
        }

        [Test]
        public void CnnPreservesSizesAndIncludesFlatten()
        {
            using (ModelWorkspace workspace = CreateChain(
                LayerDefinition.Conv2D(8, 3, 5, ActivationType.Tanh),
                LayerDefinition.MaxPooling2D(2, 3), LayerDefinition.Flatten()))
            {
                Assert.AreEqual("{\"modelType\":\"cnn\",\"input\":{\"shape\":[28,28,1]},\"layers\":[" +
                    "{\"type\":\"conv2d\",\"filters\":8,\"kernelSize\":[3,5],\"activation\":\"tanh\"}," +
                    "{\"type\":\"max_pooling2d\",\"poolSize\":[2,3]},{\"type\":\"flatten\"}," +
                    "{\"type\":\"dense\",\"units\":10,\"activation\":\"softmax\"}]}", ModelJsonSerializer.Serialize(workspace));
            }
        }

        [Test]
        public void InputConnectedDirectlyToOutputIsAValidMlp()
        {
            using (ModelWorkspace workspace = CreateChain())
            {
                Assert.IsEmpty(ModelValidator.ValidateForTransmission(workspace));
                StringAssert.DoesNotContain("flatten", ModelJsonSerializer.Serialize(workspace));
            }
        }

        [Test]
        public void DetachedInvalidChainDoesNotAffectTransmissionOrModelType()
        {
            using (ModelWorkspace workspace = CreateChain(LayerDefinition.Dense(32, ActivationType.ReLU)))
            {
                LayerDefinition dense = LayerDefinition.Dense(4, ActivationType.Tanh);
                LayerDefinition conv = LayerDefinition.Conv2D(4, 3, 3, ActivationType.ReLU);
                workspace.AddLayer(dense);
                workspace.AddLayer(conv);
                workspace.Connect(dense, conv);
                Assert.IsNotEmpty(workspace.ValidationIssues);
                Assert.IsEmpty(ModelValidator.ValidateForTransmission(workspace));
                string json = ModelJsonSerializer.Serialize(workspace);
                StringAssert.Contains("\"modelType\":\"mlp\"", json);
                StringAssert.DoesNotContain("conv2d", json);
            }
        }

        [Test]
        public void DisconnectReconnectAndDeleteSplitChainsWithoutLosingBlocks()
        {
            LayerDefinition a = LayerDefinition.Dense(8, ActivationType.ReLU);
            LayerDefinition b = LayerDefinition.Dense(4, ActivationType.Tanh);
            using (ModelWorkspace workspace = CreateChain(a, b))
            {
                ModelDefinition oldChain = workspace.InputModel;
                workspace.Disconnect(a, b);
                Assert.AreEqual(2, workspace.Models.Count);
                Assert.AreEqual(2, workspace.InputModel.Layers.Count);
                Assert.AreEqual(2, workspace.GetModel(b).Layers.Count);
                Assert.IsEmpty(workspace.ValidationIssues);
                Assert.IsNotEmpty(ModelValidator.ValidateForTransmission(workspace));
                Assert.AreEqual(4, oldChain.Layers.Count);
                AssertOwnership(workspace);
                workspace.Connect(a, b);
                Assert.AreEqual(1, workspace.Models.Count);
                Assert.IsEmpty(ModelValidator.ValidateForTransmission(workspace));
                workspace.RemoveLayer(a);
                Assert.AreEqual(2, workspace.Models.Count);
                Assert.AreEqual(1, workspace.InputModel.Layers.Count);
                Assert.Throws<ArgumentException>(() => workspace.GetModel(a));
                AssertOwnership(workspace);
                workspace.Connect(workspace.Input, b);
                Assert.IsEmpty(ModelValidator.ValidateForTransmission(workspace));
            }
        }

        [Test]
        public void InvalidConnectionsLeaveWorkspaceUnchanged()
        {
            LayerDefinition a = LayerDefinition.Dense(8, ActivationType.ReLU);
            LayerDefinition b = LayerDefinition.Dense(4, ActivationType.ReLU);
            using (ModelWorkspace workspace = CreateChain(a))
            {
                workspace.AddLayer(b);
                Assert.Throws<InvalidOperationException>(() => workspace.Connect(workspace.Input, b));
                Assert.Throws<InvalidOperationException>(() => workspace.Connect(b, a));
                Assert.Throws<InvalidOperationException>(() => workspace.Connect(a, workspace.Input));
                Assert.Throws<InvalidOperationException>(() => workspace.Connect(workspace.Output, b));
                Assert.Throws<InvalidOperationException>(() => workspace.Connect(b, b));
                Assert.Throws<InvalidOperationException>(() => workspace.Connect(b, workspace.Input));
                Assert.Throws<InvalidOperationException>(() => workspace.Disconnect(workspace.Input, workspace.Output));
                Assert.Throws<InvalidOperationException>(() => workspace.Disconnect(a, b));
                Assert.AreEqual(2, workspace.Models.Count);
                Assert.AreEqual(3, workspace.InputModel.Layers.Count);
                AssertOwnership(workspace);
            }
        }

        [Test]
        public void FixedBlocksCannotBeDeletedDuplicatedReplacedOrReconfigured()
        {
            using (ModelWorkspace workspace = new ModelWorkspace())
            {
                LayerDefinition dense = LayerDefinition.Dense(4, ActivationType.ReLU);
                foreach (LayerDefinition fixedBlock in new[] { workspace.Input, workspace.Output })
                {
                    Assert.Throws<InvalidOperationException>(() => workspace.AddLayer(fixedBlock));
                    Assert.Throws<InvalidOperationException>(() => workspace.RemoveLayer(fixedBlock));
                    Assert.Throws<InvalidOperationException>(() => workspace.ReplaceLayer(fixedBlock, dense));
                    Assert.Throws<InvalidOperationException>(() => fixedBlock.SetUnits(9));
                    Assert.Throws<InvalidOperationException>(() => fixedBlock.SetActivation(ActivationType.Tanh));
                }
                Assert.AreEqual(2, workspace.Models.Count);
                AssertOwnership(workspace);
            }
        }

        [Test]
        public void BlocksCannotBelongToMultipleWorkspaces()
        {
            using (ModelWorkspace first = new ModelWorkspace())
            using (ModelWorkspace second = new ModelWorkspace())
            {
                LayerDefinition dense = LayerDefinition.Dense(4, ActivationType.ReLU);
                first.AddLayer(dense);
                Assert.Throws<InvalidOperationException>(() => first.AddLayer(dense));
                Assert.Throws<InvalidOperationException>(() => second.AddLayer(dense));
                Assert.Throws<ArgumentException>(() => second.Connect(second.Input, dense));
                first.RemoveLayer(dense);
                second.AddLayer(dense);
                AssertOwnership(first);
                AssertOwnership(second);
            }
        }

        [Test]
        public void CollectionsAreReadOnly()
        {
            using (ModelWorkspace workspace = new ModelWorkspace())
            {
                Assert.Throws<NotSupportedException>(() => ((IList<ModelDefinition>)workspace.Models).Clear());
                Assert.Throws<NotSupportedException>(() => ((IList<LayerDefinition>)workspace.InputModel.Layers).Clear());
            }
        }

        [TestCase(LayerType.Dense, LayerType.Dense, false)]
        [TestCase(LayerType.Conv2D, LayerType.Dense, true)]
        [TestCase(LayerType.Conv2D, LayerType.Output, true)]
        [TestCase(LayerType.Dense, LayerType.Conv2D, true)]
        [TestCase(LayerType.Dense, LayerType.Flatten, true)]
        [TestCase(LayerType.Flatten, LayerType.Flatten, true)]
        [TestCase(LayerType.Flatten, LayerType.Dense, false)]
        [TestCase(LayerType.Conv2D, LayerType.Flatten, false)]
        [TestCase(LayerType.MaxPooling2D, LayerType.Conv2D, false)]
        public void DetachedFragmentsValidateWithoutEndpointRequirements(LayerType first, LayerType second, bool invalid)
        {
            using (ModelWorkspace workspace = new ModelWorkspace())
            {
                LayerDefinition a = CreateLayer(first);
                LayerDefinition b = second == LayerType.Output ? workspace.Output : CreateLayer(second);
                workspace.AddLayer(a);
                if (second != LayerType.Output) { workspace.AddLayer(b); }
                workspace.Connect(a, b);
                IReadOnlyList<ValidationIssue> issues = ModelValidator.ValidateEditing(workspace.GetModel(a));
                Assert.AreEqual(invalid, issues.Count != 0);
                if (invalid)
                {
                    Assert.AreSame(workspace.GetModel(a), issues[0].Model);
                    Assert.AreEqual("/blocks/1", issues[0].Path);
                }
            }
        }

        private static LayerDefinition CreateLayer(LayerType type)
        {
            switch (type)
            {
                case LayerType.Conv2D: return LayerDefinition.Conv2D(8, 3, 3, ActivationType.ReLU);
                case LayerType.MaxPooling2D: return LayerDefinition.MaxPooling2D(2, 2);
                case LayerType.Flatten: return LayerDefinition.Flatten();
                default: return LayerDefinition.Dense(4, ActivationType.ReLU);
            }
        }

        [Test]
        public void ValidationEventsRefreshOnEditsAndIgnoreRemovedBlocks()
        {
            using (ModelWorkspace workspace = new ModelWorkspace())
            {
                int changed = 0;
                int validated = 0;
                workspace.OnChanged += () => changed++;
                workspace.OnValidationChanged += issues =>
                {
                    validated++;
                    Assert.AreSame(workspace.ValidationIssues, issues);
                };
                LayerDefinition dense = LayerDefinition.Dense(4, ActivationType.ReLU);
                LayerDefinition conv = LayerDefinition.Conv2D(8, 3, 3, ActivationType.ReLU);
                workspace.AddLayer(dense);
                workspace.AddLayer(conv);
                workspace.Connect(dense, conv);
                Assert.IsNotEmpty(workspace.ValidationIssues);
                dense.SetUnits(16);
                Assert.AreEqual(4, validated);
                Assert.AreEqual(changed, validated);
                dense.SetUnits(16);
                Assert.AreEqual(4, validated);
                Assert.Throws<ArgumentOutOfRangeException>(() => dense.SetUnits(0));
                Assert.AreEqual(4, validated);
                workspace.Disconnect(dense, conv);
                Assert.IsEmpty(workspace.ValidationIssues);
                workspace.RemoveLayer(dense);
                int count = validated;
                dense.SetUnits(32);
                Assert.AreEqual(count, validated);
                AssertOwnership(workspace);
            }
        }

        [Test]
        public void ReplacedAndDisposedBlocksDoNotNotifyWorkspace()
        {
            ModelWorkspace workspace = new ModelWorkspace();
            LayerDefinition oldLayer = LayerDefinition.Dense(4, ActivationType.ReLU);
            LayerDefinition replacement = LayerDefinition.Dense(8, ActivationType.Tanh);
            workspace.AddLayer(oldLayer);
            workspace.Connect(workspace.Input, oldLayer);
            workspace.Connect(oldLayer, workspace.Output);
            int count = 0;
            workspace.OnChanged += () => count++;
            workspace.ReplaceLayer(oldLayer, replacement);
            Assert.AreSame(replacement, workspace.InputModel.Layers[1]);
            oldLayer.SetUnits(16);
            Assert.AreEqual(1, count);
            replacement.SetUnits(32);
            Assert.AreEqual(2, count);
            workspace.Dispose();
            replacement.SetUnits(64);
            Assert.AreEqual(2, count);
            Assert.Throws<ObjectDisposedException>(() => workspace.AddLayer(oldLayer));
            workspace.Dispose();
        }

        [TestCase(ActivationType.Linear, "linear")]
        [TestCase(ActivationType.ReLU, "relu")]
        [TestCase(ActivationType.Sigmoid, "sigmoid")]
        [TestCase(ActivationType.Tanh, "tanh")]
        [TestCase(ActivationType.Softmax, "softmax")]
        public void SettingsUpdateReferencesEventsAndJson(ActivationType activation, string token)
        {
            LayerDefinition conv = LayerDefinition.Conv2D(8, 3, 3, ActivationType.ReLU);
            LayerDefinition pool = LayerDefinition.MaxPooling2D(2, 2);
            LayerDefinition dense = LayerDefinition.Dense(32, ActivationType.ReLU);
            using (ModelWorkspace workspace = CreateChain(conv, pool, LayerDefinition.Flatten(), dense))
            {
                conv.SetFilters(16);
                conv.SetKernelSize(5, 7);
                conv.SetActivation(activation);
                pool.SetPoolSize(3, 4);
                dense.SetUnits(64);
                dense.SetActivation(activation);
                Assert.AreSame(conv, workspace.InputModel.Layers[1]);
                string json = ModelJsonSerializer.Serialize(workspace);
                StringAssert.Contains("\"filters\":16,\"kernelSize\":[5,7],\"activation\":\"" + token + "\"", json);
                StringAssert.Contains("\"poolSize\":[3,4]", json);
                StringAssert.Contains("\"units\":64,\"activation\":\"" + token + "\"", json);
            }
        }

        [Test]
        public void InvalidSettingsRemainAtomicAndDoNotNotify()
        {
            LayerDefinition conv = LayerDefinition.Conv2D(8, 3, 5, ActivationType.ReLU);
            int count = 0;
            conv.OnChanged += () => count++;
            Assert.Throws<ArgumentOutOfRangeException>(() => conv.SetFilters(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => conv.SetKernelSize(7, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => conv.SetActivation((ActivationType)99));
            Assert.Throws<InvalidOperationException>(() => conv.SetUnits(8));
            Assert.Throws<InvalidOperationException>(() => conv.SetPoolSize(2, 2));
            Assert.AreEqual(8, conv.Filters);
            Assert.AreEqual(3, conv.Height);
            Assert.AreEqual(5, conv.Width);
            Assert.AreEqual(0, count);
            Assert.Throws<ArgumentOutOfRangeException>(() => LayerDefinition.Dense(0, ActivationType.ReLU));
            Assert.Throws<ArgumentOutOfRangeException>(() => LayerDefinition.MaxPooling2D(2, 0));
        }

        [Test]
        public void IncompleteAndInvalidInputChainsReturnNoJson()
        {
            using (ModelWorkspace workspace = new ModelWorkspace())
            {
                LayerDefinition conv = LayerDefinition.Conv2D(4, 3, 3, ActivationType.ReLU);
                workspace.AddLayer(conv);
                workspace.Connect(workspace.Input, conv);
                Assert.IsEmpty(workspace.ValidationIssues);
                Assert.IsFalse(ModelJsonSerializer.TrySerialize(workspace, out string json,
                    out IReadOnlyList<ValidationIssue> issues));
                Assert.IsNull(json);
                workspace.Connect(conv, workspace.Output);
                Assert.IsNotEmpty(workspace.ValidationIssues);
                Assert.Throws<InvalidOperationException>(() => ModelJsonSerializer.Serialize(workspace));
                workspace.Disconnect(conv, workspace.Output);
                LayerDefinition flatten = LayerDefinition.Flatten();
                workspace.AddLayer(flatten);
                workspace.Connect(conv, flatten);
                workspace.Connect(flatten, workspace.Output);
                Assert.IsEmpty(workspace.ValidationIssues);
                Assert.IsTrue(ModelJsonSerializer.TrySerialize(workspace, out json, out issues));
            }
        }

        [Test]
        public void SerializationIsCultureIndependent()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            using (ModelWorkspace workspace = CreateChain(LayerDefinition.Dense(32, ActivationType.ReLU)))
            {
                try
                {
                    string expected = ModelJsonSerializer.Serialize(workspace);
                    CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
                    Assert.AreEqual(expected, ModelJsonSerializer.Serialize(workspace));
                }
                finally
                {
                    CultureInfo.CurrentCulture = original;
                }
            }
        }
    }
}
