using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace ModelDesign.Tests
{
    public sealed class ModelDesignTests
    {
        [Test]
        public void MlpSerializesOnlySpecifiedFields()
        {
            ModelDefinition model = new ModelDefinition();
            model.AddLayer(LayerDefinition.Dense(32, ActivationType.ReLU));
            model.AddLayer(LayerDefinition.Output());
            Assert.AreEqual("{\"schemaVersion\":1,\"modelType\":\"mlp\",\"input\":{\"shape\":[28,28,1]},\"layers\":[" +
                "{\"type\":\"dense\",\"units\":32,\"activation\":\"relu\"}," +
                "{\"type\":\"dense\",\"units\":10,\"activation\":\"softmax\"}]}", ModelJsonSerializer.Serialize(model));
        }

        [Test]
        public void OutputOnlyModelUsesAutomaticInputFlattening()
        {
            ModelDefinition model = new ModelDefinition();
            model.AddLayer(LayerDefinition.Output());
            Assert.IsEmpty(ModelValidator.Validate(model));
            StringAssert.DoesNotContain("flatten", ModelJsonSerializer.Serialize(model));
        }

        [TestCase(LayerType.Conv2D)]
        [TestCase(LayerType.MaxPooling2D)]
        [TestCase(LayerType.Flatten)]
        public void SpatialLayersCannotFollowDenseFirstInput(LayerType type)
        {
            ModelDefinition model = new ModelDefinition();
            model.AddLayer(LayerDefinition.Dense(8, ActivationType.ReLU));
            model.AddLayer(type == LayerType.Conv2D
                ? LayerDefinition.Conv2D(4, 3, 3, ActivationType.ReLU)
                : type == LayerType.MaxPooling2D
                    ? LayerDefinition.MaxPooling2D(2, 2)
                    : LayerDefinition.Flatten());
            model.AddLayer(LayerDefinition.Output());
            Assert.IsFalse(ModelJsonSerializer.TrySerialize(model, out string json,
                out IReadOnlyList<ValidationIssue> issues));
            Assert.IsNull(json);
            Assert.AreEqual("/layers/1", issues[0].Path);
        }

        [Test]
        public void CnnPreservesRectangularSizesAndLayerOrder()
        {
            ModelDefinition model = new ModelDefinition();
            model.AddLayer(LayerDefinition.Conv2D(8, 3, 5, ActivationType.Tanh));
            model.AddLayer(LayerDefinition.MaxPooling2D(2, 3));
            model.AddLayer(LayerDefinition.Flatten());
            model.AddLayer(LayerDefinition.Output());
            Assert.AreEqual("{\"schemaVersion\":1,\"modelType\":\"cnn\",\"input\":{\"shape\":[28,28,1]},\"layers\":[" +
                "{\"type\":\"conv2d\",\"filters\":8,\"kernelSize\":[3,5],\"activation\":\"tanh\"}," +
                "{\"type\":\"max_pooling2d\",\"poolSize\":[2,3]},{\"type\":\"flatten\"}," +
                "{\"type\":\"dense\",\"units\":10,\"activation\":\"softmax\"}]}", ModelJsonSerializer.Serialize(model));
        }

        [TestCase(ActivationType.Linear, "linear")]
        [TestCase(ActivationType.ReLU, "relu")]
        [TestCase(ActivationType.Sigmoid, "sigmoid")]
        [TestCase(ActivationType.Tanh, "tanh")]
        [TestCase(ActivationType.Softmax, "softmax")]
        public void HiddenActivationsSerializeWithSchemaSpelling(ActivationType activation, string token)
        {
            ModelDefinition model = new ModelDefinition();
            model.AddLayer(LayerDefinition.Conv2D(4, 3, 3, activation));
            model.AddLayer(LayerDefinition.Flatten());
            model.AddLayer(LayerDefinition.Dense(8, activation));
            model.AddLayer(LayerDefinition.Output());
            string json = ModelJsonSerializer.Serialize(model);
            StringAssert.Contains("\"kernelSize\":[3,3],\"activation\":\"" + token + "\"", json);
            StringAssert.Contains("\"units\":8,\"activation\":\"" + token + "\"", json);
        }

        [TestCase(false, "mlp")]
        [TestCase(true, "cnn")]
        public void ExplicitFlattenAndPoolingAreClassifiedCorrectly(bool includePooling, string expectedType)
        {
            ModelDefinition model = new ModelDefinition();
            if (includePooling)
            {
                model.AddLayer(LayerDefinition.MaxPooling2D(2, 2));
            }
            model.AddLayer(LayerDefinition.Flatten());
            model.AddLayer(LayerDefinition.Output());
            StringAssert.Contains("\"modelType\":\"" + expectedType + "\"", ModelJsonSerializer.Serialize(model));
        }

        [Test]
        public void ModelTypeIsRecomputedAfterEditing()
        {
            ModelDefinition model = new ModelDefinition();
            model.AddLayer(LayerDefinition.Flatten());
            model.AddLayer(LayerDefinition.Output());
            StringAssert.Contains("\"modelType\":\"mlp\"", ModelJsonSerializer.Serialize(model));
            model.InsertLayer(0, LayerDefinition.Conv2D(4, 3, 3, ActivationType.ReLU));
            StringAssert.Contains("\"modelType\":\"cnn\"", ModelJsonSerializer.Serialize(model));
            model.RemoveLayer(0);
            StringAssert.Contains("\"modelType\":\"mlp\"", ModelJsonSerializer.Serialize(model));
        }

        [Test]
        public void EditOperationsPreserveOrderAndReplaceSettings()
        {
            ModelDefinition model = new ModelDefinition();
            LayerDefinition flatten = LayerDefinition.Flatten();
            LayerDefinition output = LayerDefinition.Output();
            model.AddLayer(flatten);
            model.AddLayer(output);
            model.InsertLayer(0, LayerDefinition.Dense(4, ActivationType.ReLU));
            model.MoveLayer(0, 1);
            model.ReplaceLayer(1, LayerDefinition.Dense(16, ActivationType.Sigmoid));
            Assert.AreSame(flatten, model.Layers[0]);
            Assert.AreEqual(16, model.Layers[1].Units);
            Assert.AreEqual(ActivationType.Sigmoid, model.Layers[1].Activation);
            Assert.AreSame(output, model.Layers[2]);
            Assert.IsEmpty(ModelValidator.Validate(model));
            model.RemoveLayer(1);
            Assert.AreEqual(2, model.Layers.Count);
        }

        [Test]
        public void InvalidMoveDoesNotRemoveLayer()
        {
            ModelDefinition model = new ModelDefinition();
            LayerDefinition layer = LayerDefinition.Flatten();
            model.AddLayer(layer);
            Assert.Throws<ArgumentOutOfRangeException>(() => model.MoveLayer(0, 1));
            Assert.AreSame(layer, model.Layers[0]);
            Assert.Throws<ArgumentNullException>(() => model.ReplaceLayer(0, null));
            Assert.AreSame(layer, model.Layers[0]);
        }

        [Test]
        public void LayerCollectionCannotBeMutatedExternally()
        {
            ModelDefinition model = new ModelDefinition();
            Assert.Throws<NotSupportedException>(() =>
                ((IList<LayerDefinition>)model.Layers).Add(LayerDefinition.Flatten()));
        }

        [Test]
        public void InvalidSettingsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LayerDefinition.Dense(0, ActivationType.ReLU));
            Assert.Throws<ArgumentOutOfRangeException>(() => LayerDefinition.Conv2D(-1, 3, 3, ActivationType.ReLU));
            Assert.Throws<ArgumentOutOfRangeException>(() => LayerDefinition.Conv2D(8, 0, 3, ActivationType.ReLU));
            Assert.Throws<ArgumentOutOfRangeException>(() => LayerDefinition.MaxPooling2D(2, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => LayerDefinition.Dense(4, (ActivationType)99));
        }

        [TestCase("empty")]
        [TestCase("missingFlatten")]
        [TestCase("duplicateFlatten")]
        [TestCase("convAfterFlatten")]
        [TestCase("poolAfterFlatten")]
        [TestCase("denseBeforeFlatten")]
        [TestCase("wrongOutputUnits")]
        [TestCase("wrongOutputActivation")]
        [TestCase("missingOutput")]
        public void InvalidStructureReturnsErrorsAndNoJson(string scenario)
        {
            ModelDefinition model = new ModelDefinition();
            if (scenario == "missingFlatten")
            {
                model.AddLayer(LayerDefinition.Conv2D(8, 3, 3, ActivationType.ReLU));
            }
            if (scenario == "denseBeforeFlatten")
            {
                model.AddLayer(LayerDefinition.Dense(4, ActivationType.ReLU));
            }
            if (scenario != "empty" && scenario != "missingFlatten")
            {
                model.AddLayer(LayerDefinition.Flatten());
            }
            switch (scenario)
            {
                case "duplicateFlatten": model.AddLayer(LayerDefinition.Flatten()); break;
                case "convAfterFlatten": model.AddLayer(LayerDefinition.Conv2D(8, 3, 3, ActivationType.ReLU)); break;
                case "poolAfterFlatten": model.AddLayer(LayerDefinition.MaxPooling2D(2, 2)); break;
            }
            if (scenario == "wrongOutputUnits")
            {
                model.AddLayer(LayerDefinition.Dense(9, ActivationType.Softmax));
            }
            else if (scenario == "wrongOutputActivation")
            {
                model.AddLayer(LayerDefinition.Dense(10, ActivationType.ReLU));
            }
            else if (scenario != "empty" && scenario != "missingOutput")
            {
                model.AddLayer(LayerDefinition.Output());
            }
            Assert.IsFalse(ModelJsonSerializer.TrySerialize(model, out string json,
                out IReadOnlyList<ValidationIssue> issues));
            Assert.IsNull(json);
            Assert.IsNotEmpty(issues);
            StringAssert.StartsWith("/layers", issues[0].Path);
            Assert.Throws<InvalidOperationException>(() => ModelJsonSerializer.Serialize(model));
        }

        [Test]
        public void SerializationIsCultureIndependent()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                ModelDefinition model = new ModelDefinition();
                model.AddLayer(LayerDefinition.Flatten());
                model.AddLayer(LayerDefinition.Output());
                string expected = ModelJsonSerializer.Serialize(model);
                CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
                Assert.AreEqual(expected, ModelJsonSerializer.Serialize(model));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }
    }
}
