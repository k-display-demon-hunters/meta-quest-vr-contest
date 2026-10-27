using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using ModelDesignScene.Execution.Engine;
using NUnit.Framework;

namespace ModelDesignScene.Model.Tests
{
    public sealed class ModelDesignTests
    {
        [Test]
        public void LocalCnnBuildFollowsSlotOrderAndExcludesUnplacedBlocks()
        {
            using (ModelDefinition model = new ModelDefinition())
            {
                LayerDefinition conv = LayerDefinition.Conv2D(4, 3, 5, ActivationType.Tanh);
                LayerDefinition pool = LayerDefinition.MaxPooling2D(2, 2);
                LayerDefinition flatten = LayerDefinition.Flatten();
                LayerDefinition dense = LayerDefinition.Dense(12, ActivationType.Sigmoid);
                model.PlaceLayer(0, conv);
                model.PlaceLayer(2, pool);
                model.PlaceLayer(3, flatten);
                model.PlaceLayer(6, dense);
                LayerDefinition.Conv2D(16, 9, 9, ActivationType.Linear);

                Assert.AreEqual(8, model.Layers.Count);
                Assert.AreSame(conv, model.Layers[0]);
                Assert.IsNull(model.Layers[1]);
                ModelExecutionDefinition definition = BuildDefinition(model, out int?[] slotIndices);
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
        public void LocalCnnExecutionRejectsInvalidLayouts()
        {
            LayerDefinition[][] invalidLayouts =
            {
                new[] { LayerDefinition.Dense(12, ActivationType.ReLU) },
                new[] { LayerDefinition.Flatten(), LayerDefinition.MaxPooling2D(2, 2) },
                new[] { LayerDefinition.Conv2D(4, 3, 3, ActivationType.ReLU) },
                new[] { LayerDefinition.MaxPooling2D(2, 2), LayerDefinition.Conv2D(4, 15, 3, ActivationType.ReLU), LayerDefinition.Flatten() }
            };
            foreach (LayerDefinition[] layout in invalidLayouts)
            {
                using (ModelDefinition model = CreateModel(layout))
                {
                    ModelExecutionDefinition definition = BuildDefinition(model, out int?[] slotIndices);
                    Assert.IsFalse(CanPrepareTraining(definition, CreateData(),
                        new CnnTrainingOptions(), slotIndices));
                }
            }
            LayerDefinition flatten = LayerDefinition.Flatten();
            using (ModelDefinition model = CreateModel(
                LayerDefinition.Conv2D(4, 3, 3, ActivationType.ReLU), flatten))
            {
                ModelExecutionDefinition definition = BuildDefinition(model, out int?[] slotIndices);
                Assert.IsTrue(CanPrepareTraining(definition, CreateData(),
                    new CnnTrainingOptions(), slotIndices));
                model.RemoveLayer(1);
                Assert.IsFalse(CanPrepareTraining(BuildDefinition(model, out int?[] remainingSlots),
                    CreateData(), new CnnTrainingOptions(), remainingSlots));
            }
        }

        [Test]
        public void LocalCnnSnapshotSurvivesEditsAndMatchesOnlyPlacedConfiguration()
        {
            LayerDefinition conv = LayerDefinition.Conv2D(4, 3, 3, ActivationType.ReLU);
            using (ModelDefinition model = CreateModel(conv, LayerDefinition.Flatten()))
            {
                ModelExecutionDefinition submitted = BuildDefinition(model, out _);
                LayerDefinition.Dense(20, ActivationType.Linear);
                Assert.IsTrue(MatchesDefinition(model, submitted));

                conv.SetFilters(8);
                ModelExecutionDefinition edited = BuildDefinition(model, out _);
                Assert.AreEqual(4, submitted.Layers[0].Units);
                Assert.AreEqual(8, edited.Layers[0].Units);
                Assert.IsFalse(MatchesDefinition(model, submitted));
                Assert.IsFalse(MatchesDefinition(model, null));
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<CnnLayerDefinition>)submitted.Layers).Clear());

                conv.SetFilters(4);
                Assert.IsTrue(MatchesDefinition(model, submitted));
                model.Dispose();
                conv.SetKernelSize(5, 5);
                Assert.AreEqual(3, submitted.Layers[0].Height);
                Assert.AreEqual(3, submitted.Layers[0].Width);
            }
        }

        private static ModelExecutionDefinition BuildDefinition(ModelDefinition model, out int?[] slotIndices)
        {
            object[] arguments = { model, null };
            ModelExecutionDefinition definition = (ModelExecutionDefinition)InvokeExecutionMethod(
                "ModelExecutionDefinitionBuilder", "Build", arguments);
            slotIndices = (int?[])arguments[1];
            return definition;
        }

        private static bool MatchesDefinition(ModelDefinition model, ModelExecutionDefinition submitted)
        {
            return (bool)InvokeExecutionMethod("ModelExecutionDefinitionBuilder", "Matches", model, submitted);
        }

        private static bool CanPrepareTraining(ModelExecutionDefinition definition, TrainingData data,
            CnnTrainingOptions options, int?[] slotIndices)
        {
            return (bool)InvokeExecutionMethod("ModelExecutionValidator", "TryPrepareTraining",
                definition, data, options, slotIndices, null, null);
        }

        // An asmdef test assembly cannot directly reference the predefined Assembly-CSharp assembly.
        // Invoke the production methods without copying their implementation into the tests.
        private static object InvokeExecutionMethod(string typeName, string methodName, params object[] arguments)
        {
            Type type = Type.GetType("ModelDesignScene.Execution." + typeName + ", Assembly-CSharp", throwOnError: true);
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(type.FullName, methodName);
            }
            try
            {
                return method.Invoke(null, arguments);
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        private static TrainingData CreateData()
        {
            int size = LayerLimits.InputHeight * LayerLimits.InputWidth * LayerLimits.InputChannels;
            return new TrainingData(
                new TrainingDataset(new float[size], new[] { 0 }, size),
                new TrainingDataset(new float[size], new[] { 0 }, size),
                new TrainingDataset(new float[size], new[] { 0 }, size));
        }

        private static ModelDefinition CreateModel(params LayerDefinition[] layers)
        {
            ModelDefinition model = new ModelDefinition();
            for (int slot = 0; slot < layers.Length; slot++)
            {
                model.PlaceLayer(slot, layers[slot]);
            }
            return model;
        }

        [Test]
        public void FixedEndpointsArePreservedAndCannotBeReconfigured()
        {
            using (ModelDefinition model = new ModelDefinition())
            {
                LayerDefinition input = model.Input;
                LayerDefinition output = model.Output;
                model.PlaceLayer(0, LayerDefinition.Flatten());
                model.RemoveLayer(0);
                Assert.AreEqual(8, model.Layers.Count);
                Assert.IsNull(model.Layers[0]);
                Assert.AreSame(input, model.Input);
                Assert.AreSame(output, model.Output);
                Assert.Throws<ArgumentException>(() => model.PlaceLayer(0, input));
                Assert.Throws<ArgumentException>(() => model.PlaceLayer(0, output));
                foreach (LayerDefinition fixedBlock in new[] { input, output })
                {
                    Assert.Throws<InvalidOperationException>(() => fixedBlock.SetUnits(9));
                    Assert.Throws<InvalidOperationException>(() => fixedBlock.SetActivation(ActivationType.Tanh));
                }
            }
        }

        [Test]
        public void CollectionsAreReadOnly()
        {
            using (ModelDefinition model = new ModelDefinition())
            {
                Assert.Throws<NotSupportedException>(() => ((IList<LayerDefinition>)model.Layers).Clear());
            }
        }

        [Test]
        public void ModelChangesIgnoreUnplacedAndRemovedLayers()
        {
            using (ModelDefinition model = new ModelDefinition())
            {
                int changed = 0;
                model.OnChanged += () => changed++;
                LayerDefinition dense = LayerDefinition.Dense(4, ActivationType.ReLU);
                LayerDefinition conv = LayerDefinition.Conv2D(8, 3, 3, ActivationType.ReLU);
                dense.SetUnits(8);
                Assert.AreEqual(0, changed);
                model.PlaceLayer(0, dense);
                model.PlaceLayer(3, conv);
                Assert.AreEqual(2, changed);
                dense.SetUnits(16);
                Assert.AreEqual(3, changed);
                dense.SetUnits(16);
                Assert.AreEqual(3, changed);
                Assert.Throws<ArgumentOutOfRangeException>(() => dense.SetUnits(0));
                Assert.AreEqual(3, changed);
                model.RemoveLayer(0);
                int count = changed;
                dense.SetUnits(32);
                Assert.AreEqual(count, changed);
                Assert.IsNull(model.Layers[0]);
                Assert.AreSame(conv, model.Layers[3]);
            }
        }

        [Test]
        public void ReplacedAndDisposedLayersDoNotNotifyModel()
        {
            ModelDefinition model = new ModelDefinition();
            LayerDefinition oldLayer = LayerDefinition.Dense(4, ActivationType.ReLU);
            LayerDefinition replacement = LayerDefinition.Dense(8, ActivationType.Tanh);
            model.PlaceLayer(0, oldLayer);
            int count = 0;
            model.OnChanged += () => count++;
            model.RemoveLayer(0);
            model.PlaceLayer(0, replacement);
            Assert.AreSame(replacement, model.Layers[0]);
            oldLayer.SetUnits(16);
            Assert.AreEqual(2, count);
            replacement.SetUnits(32);
            Assert.AreEqual(3, count);
            model.Dispose();
            replacement.SetUnits(64);
            Assert.AreEqual(3, count);
            model.Dispose();
        }

        [Test]
        public void InvalidSettingsRemainAtomicAndDoNotNotify()
        {
            LayerDefinition conv = LayerDefinition.Conv2D(8, 3, 5, ActivationType.ReLU);
            int count = 0;
            conv.OnChanged += () => count++;
            Assert.Throws<ArgumentOutOfRangeException>(() => conv.SetFilters(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => conv.SetFilters(LayerLimits.Conv2DMaxFilters + 1));
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
