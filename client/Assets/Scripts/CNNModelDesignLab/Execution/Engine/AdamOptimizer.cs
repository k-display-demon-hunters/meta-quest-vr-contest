using System;

namespace ModelDesignScene.Execution.Engine
{
    /// <summary>A parameter vector owns its accumulated gradients and Adam moments.</summary>
    internal sealed class TrainableParameters
    {
        internal readonly float[] Values;
        internal readonly float[] Gradients;
        internal readonly float[] FirstMoment;
        internal readonly float[] SecondMoment;

        internal TrainableParameters(int count)
        {
            Values = new float[count];
            Gradients = new float[count];
            FirstMoment = new float[count];
            SecondMoment = new float[count];
        }

        internal void Initialize(Random random, int fanIn, int fanOut, CnnActivation activation)
        {
            double limit = activation == CnnActivation.ReLU ? Math.Sqrt(6.0 / fanIn) : Math.Sqrt(6.0 / (fanIn + fanOut));
            for (int i = 0; i < Values.Length; i++)
            {
                Values[i] = (float)((random.NextDouble() * 2 - 1) * limit);
            }
        }
    }

    internal sealed class AdamOptimizer
    {
        private const double Beta1 = 0.9;
        private const double Beta2 = 0.999;
        private const double Epsilon = 1e-7;
        private readonly float _learningRate;
        private double _beta1Power = 1;
        private double _beta2Power = 1;
        private double _firstCorrection;
        private double _secondCorrection;
        private int _batchCount;

        internal AdamOptimizer(float learningRate)
        {
            _learningRate = learningRate;
        }

        internal void BeginStep(int batchCount)
        {
            _batchCount = batchCount;
            _beta1Power *= Beta1;
            _beta2Power *= Beta2;
            _firstCorrection = 1 - _beta1Power;
            _secondCorrection = 1 - _beta2Power;
        }

        internal void Update(TrainableParameters parameters)
        {
            for (int i = 0; i < parameters.Values.Length; i++)
            {
                double gradient = parameters.Gradients[i] / (double)_batchCount;
                double first = Beta1 * parameters.FirstMoment[i] + (1 - Beta1) * gradient;
                double second = Beta2 * parameters.SecondMoment[i] + (1 - Beta2) * gradient * gradient;
                double next = parameters.Values[i] - _learningRate * (first / _firstCorrection) /
                    (Math.Sqrt(second / _secondCorrection) + Epsilon);
                if (double.IsNaN(next) || double.IsInfinity(next) || double.IsNaN(second) ||
                    double.IsInfinity(second) || second > float.MaxValue || Math.Abs(next) > float.MaxValue)
                {
                    throw new ArithmeticException("Adam update diverged. Reduce the learning rate.");
                }
                parameters.Values[i] = (float)next;
                parameters.FirstMoment[i] = (float)first;
                parameters.SecondMoment[i] = (float)second;
                parameters.Gradients[i] = 0;
            }
        }
    }
}
