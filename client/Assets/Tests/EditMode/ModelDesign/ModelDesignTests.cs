using System;
using System.Collections.Generic;
using CNNTraining;
using NUnit.Framework;

namespace ModelDesign.Tests
{
    public sealed class ModelDesignTests
    {
        [Test]
        public void LocalCnnBuildFollowsInputDirectionAndExcludesDetachedBlocks()
        {
            using (ModelWorkspace workspace = new ModelWorkspace())
            {
                LayerDefinition conv = LayerDefinition.Conv2D(4, 3, 5, ActivationType.Tanh);
                LayerDefinition pool = LayerDefinition.MaxPooling2D(2, 2);
                LayerDefinition flatten = LayerDefinition.Flatten();
                LayerDefinition dense = LayerDefinition.Dense(12, ActivationType.Sigmoid);
                foreach (LayerDefinition layer in new[] { conv, pool, flatten, dense })
                {
                    workspace.AddLayer(layer);
                }
                // Physical chains may be stored backwards; execution must always start at Input.
                workspace.Connect(workspace.Output, dense);
                workspace.Connect(dense, flatten);
                workspace.Connect(flatten, pool);
                workspace.Connect(pool, conv);
                workspace.Connect(conv, workspace.Input);
                workspace.AddLayer(LayerDefinition.Conv2D(999, 99, 99, ActivationType.Linear));

                Assert.AreSame(workspace.Output, workspace.InputModel.Layers[0]);
                Assert.IsEmpty(ModelValidator.ValidateForExecution(workspace));
                ModelExecutionDefinition definition = ModelExecutionDefinitionBuilder.Build(workspace);
                Assert.AreEqual(28 * 28, definition.InputSize);
                Assert.AreEqual(1, definition.Channels);
                Assert.AreEqual(5, definition.Layers.Count);
                Assert.AreEqual(LayerKind.Conv2D, definition.Layers[0].Kind);
                Assert.AreEqual(4, definition.Layers[0].Units);
                Assert.AreEqual(3, definition.Layers[0].Height);
                Assert.AreEqual(5, definition.Layers[0].Width);
                Assert.AreEqual(CnnActivation.Tanh, definition.Layers[0].Activation);
                Assert.AreEqual(LayerKind.MaxPooling2D, definition.Layers[1].Kind);
                Assert.AreEqual(LayerKind.Flatten, definition.Layers[2].Kind);
                Assert.AreEqual(12, definition.Layers[3].Units);
                Assert.AreEqual(CnnActivation.Sigmoid, definition.Layers[3].Activation);
                Assert.AreEqual(LayerKind.Dense, definition.Layers[4].Kind);
                Assert.AreEqual(10, definition.Layers[4].Units);
                Assert.AreEqual(CnnActivation.Softmax, definition.Layers[4].Activation);
            }
        }

        [Test]
        public void LocalCnnExecutionRejectsInvalidLayoutsAndResourceLimits()
        {
            LayerDefinition[][] invalidLayouts =
            {
                new[] { LayerDefinition.Dense(12, ActivationType.ReLU) },
                new[] { LayerDefinition.MaxPooling2D(2, 2), LayerDefinition.Flatten() },
                new[] { LayerDefinition.Conv2D(4, 3, 3, ActivationType.ReLU) },
                new[] { LayerDefinition.Conv2D(4, 29, 3, ActivationType.ReLU), LayerDefinition.Flatten() }
            };
            foreach (LayerDefinition[] layout in invalidLayouts)
            {
                using (ModelWorkspace workspace = CreateChain(layout))
                {
                    Assert.IsFalse(ModelExecutionDefinitionBuilder.TryBuild(workspace, null,
                        out ModelExecutionDefinition definition, out IReadOnlyList<ValidationIssue> issues));
                    Assert.IsNull(definition);
                    Assert.IsNotEmpty(issues);
                    Assert.Throws<InvalidOperationException>(() => ModelExecutionDefinitionBuilder.Build(workspace));
                }
            }
            LayerDefinition flatten = LayerDefinition.Flatten();
            using (ModelWorkspace workspace = CreateChain(
                LayerDefinition.Conv2D(4, 3, 3, ActivationType.ReLU), flatten))
            {
                Assert.IsEmpty(ModelValidator.ValidateForExecution(workspace));
                Assert.IsNotEmpty(ModelValidator.ValidateForExecution(workspace,
                    new CnnTrainingOptions(maxParameters: 100)));
                workspace.Disconnect(flatten, workspace.Output);
                Assert.IsNotEmpty(ModelValidator.ValidateForExecution(workspace));
            }
        }

        [Test]
        public void LocalCnnSnapshotSurvivesEditsAndMatchesOnlyConnectedConfiguration()
        {
            LayerDefinition conv = LayerDefinition.Conv2D(4, 3, 3, ActivationType.ReLU);
            using (ModelWorkspace workspace = CreateChain(conv, LayerDefinition.Flatten()))
            {
                ModelExecutionDefinition submitted = ModelExecutionDefinitionBuilder.Build(workspace);
                workspace.AddLayer(LayerDefinition.Dense(20, ActivationType.Linear));
                Assert.IsTrue(ModelExecutionDefinitionBuilder.Matches(submitted,
                    ModelExecutionDefinitionBuilder.Build(workspace)));

                conv.SetFilters(8);
                ModelExecutionDefinition edited = ModelExecutionDefinitionBuilder.Build(workspace);
                Assert.AreEqual(4, submitted.Layers[0].Units);
                Assert.AreEqual(8, edited.Layers[0].Units);
                Assert.IsFalse(ModelExecutionDefinitionBuilder.Matches(submitted, edited));
                Assert.IsFalse(ModelExecutionDefinitionBuilder.Matches(submitted, null));
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<CnnLayerDefinition>)submitted.Layers).Clear());

                conv.SetFilters(4);
                Assert.IsTrue(ModelExecutionDefinitionBuilder.Matches(submitted,
                    ModelExecutionDefinitionBuilder.Build(workspace)));
                workspace.Dispose();
                conv.SetKernelSize(5, 5);
                Assert.AreEqual(3, submitted.Layers[0].Height);
                Assert.AreEqual(3, submitted.Layers[0].Width);
            }
        }

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
        [TestCase(LayerType.Dense, LayerType.Flatten, false)]
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

        [Test]
        public void AddAndConnectIsAtomicAndNotifiesOnce()
        {
            using (ModelWorkspace workspace = new ModelWorkspace())
            {
                int changed = 0;
                int validated = 0;
                workspace.OnChanged += () => changed++;
                workspace.OnValidationChanged += issues => validated++;
                LayerDefinition dense = LayerDefinition.Dense(8, ActivationType.ReLU);
                workspace.AddAndConnect(workspace.Input, dense);
                Assert.AreEqual(1, changed);
                Assert.AreEqual(1, validated);
                Assert.AreEqual(2, workspace.Models.Count);
                LayerDefinition rejected = LayerDefinition.Dense(4, ActivationType.ReLU);
                Assert.Throws<InvalidOperationException>(() => workspace.AddAndConnect(workspace.Input, rejected));
                Assert.Throws<InvalidOperationException>(() => workspace.AddAndConnect(dense, dense));
                Assert.Throws<InvalidOperationException>(() => workspace.AddAndConnect(dense, workspace.Output));
                Assert.AreEqual(1, changed);
                Assert.AreEqual(1, validated);
                workspace.AddLayer(rejected);
                AssertOwnership(workspace);
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

    }
}
