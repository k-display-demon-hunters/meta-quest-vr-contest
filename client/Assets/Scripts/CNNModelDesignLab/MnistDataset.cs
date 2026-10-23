using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CNNTraining;
using UnityEngine;

namespace ModelDesignScene
{
    // Scene adapter: the engine only receives normalized arrays and integer labels.
    public sealed class MnistDataset
    {
        private const int ImageSize = 28 * 28;
        private const uint Magic = 0x544E4D43; // CMNT in little-endian byte order.
        private const string TestImagePrefix = "mnist-test-";
        private readonly Dictionary<int, int> _testPositions;
        private readonly float[] _testImages;
        private readonly int[] _testLabels;

        public TrainingData Data { get; }

        private MnistDataset(TrainingData data, DecodedDataset test)
        {
            Data = data;
            _testPositions = test.Positions;
            _testImages = test.Images;
            _testLabels = test.Labels;
        }

        public static MnistDataset Load(TextAsset trainingAsset, TextAsset testAsset)
        {
            if (trainingAsset == null || testAsset == null)
            {
                throw new InvalidOperationException("로컬 MNIST 학습·평가 데이터 에셋을 연결해 주세요.");
            }

            DecodedDataset source = Decode(trainingAsset.bytes, expectedSplit: 0);
            DecodedDataset test = Decode(testAsset.bytes, expectedSplit: 1);
            List<int> trainingRows = new List<int>();
            List<int> validationRows = new List<int>();
            int[] classCounts = new int[10];
            // Every tenth example of each class is held out, independent of shuffle seed.
            for (int row = 0; row < source.Labels.Length; row++)
            {
                int label = source.Labels[row];
                classCounts[label]++;
                if (classCounts[label] % 10 == 0)
                {
                    validationRows.Add(row);
                }
                else
                {
                    trainingRows.Add(row);
                }
            }
            for (int label = 0; label < classCounts.Length; label++)
            {
                if (classCounts[label] < 10)
                {
                    throw new InvalidDataException("MNIST 학습 에셋에는 숫자별로 최소 10개의 이미지가 필요합니다.");
                }
            }

            TrainingData data = new TrainingData(SelectRows(source, trainingRows),
                SelectRows(source, validationRows),
                new TrainingDataset(test.Images, test.Labels, ImageSize));
            return new MnistDataset(data, test);
        }

        public bool TryGetTestImage(string imageId, out float[] pixels, out int label)
        {
            pixels = null;
            label = -1;
            if (imageId == null || !imageId.StartsWith(TestImagePrefix, StringComparison.Ordinal))
            {
                return false;
            }
            string suffix = imageId.Substring(TestImagePrefix.Length);
            if (!int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out int sourceIndex)
                || sourceIndex.ToString(CultureInfo.InvariantCulture) != suffix
                || !_testPositions.TryGetValue(sourceIndex, out int row))
            {
                return false;
            }

            // Never expose the stored dataset array to a caller or an asynchronous operation.
            pixels = new float[ImageSize];
            Array.Copy(_testImages, row * ImageSize, pixels, 0, ImageSize);
            label = _testLabels[row];
            return true;
        }

        private static TrainingDataset SelectRows(DecodedDataset source, List<int> rows)
        {
            float[] images = new float[rows.Count * ImageSize];
            int[] labels = new int[rows.Count];
            for (int index = 0; index < rows.Count; index++)
            {
                int row = rows[index];
                Array.Copy(source.Images, row * ImageSize, images, index * ImageSize, ImageSize);
                labels[index] = source.Labels[row];
            }
            return new TrainingDataset(images, labels, ImageSize);
        }

        private static DecodedDataset Decode(byte[] bytes, int expectedSplit)
        {
            const int HeaderSize = 24;
            const int RecordSize = sizeof(int) + sizeof(byte) + ImageSize;
            if (bytes.Length < HeaderSize)
            {
                throw new InvalidDataException("MNIST 데이터 헤더가 없습니다.");
            }
            using (MemoryStream stream = new MemoryStream(bytes, false))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                if (reader.ReadUInt32() != Magic || reader.ReadInt32() != 1
                    || reader.ReadInt32() != expectedSplit
                    || reader.ReadInt32() != 28 || reader.ReadInt32() != 28)
                {
                    throw new InvalidDataException("지원하지 않는 MNIST 데이터 형식입니다.");
                }
                int count = reader.ReadInt32();
                int maxCount = expectedSplit == 0 ? 60000 : 10000;
                if (count < 1 || count > maxCount || bytes.Length != HeaderSize + (long)count * RecordSize)
                {
                    throw new InvalidDataException("MNIST 데이터 개수 또는 파일 길이가 올바르지 않습니다.");
                }
                DecodedDataset data = new DecodedDataset(count);
                for (int row = 0; row < count; row++)
                {
                    int sourceIndex = reader.ReadInt32();
                    int label = reader.ReadByte();
                    if (sourceIndex < 0 || sourceIndex >= maxCount || label > 9
                        || data.Positions.ContainsKey(sourceIndex))
                    {
                        throw new InvalidDataException("MNIST 원본 인덱스 또는 정답이 올바르지 않습니다.");
                    }
                    data.Positions.Add(sourceIndex, row);
                    data.Labels[row] = label;
                    int offset = row * ImageSize;
                    for (int pixel = 0; pixel < ImageSize; pixel++)
                    {
                        data.Images[offset + pixel] = reader.ReadByte() / 255f;
                    }
                }
                return data;
            }
        }

        private sealed class DecodedDataset
        {
            public float[] Images { get; }
            public int[] Labels { get; }
            public Dictionary<int, int> Positions { get; }

            public DecodedDataset(int count)
            {
                Images = new float[count * ImageSize];
                Labels = new int[count];
                Positions = new Dictionary<int, int>(count);
            }
        }
    }
}
