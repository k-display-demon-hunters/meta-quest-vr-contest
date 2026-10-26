using System;

namespace ModelDesignScene.Execution.Engine
{
    internal static class ActivationFunctions
    {
        internal static void Forward(float[] logits, float[] output, CnnActivation activation, int channels)
        {
            if (activation == CnnActivation.Softmax)
            {
                for (int offset = 0; offset < logits.Length; offset += channels)
                {
                    float maximum = logits[offset];
                    for (int c = 1; c < channels; c++)
                    {
                        maximum = Math.Max(maximum, logits[offset + c]);
                    }
                    double sum = 0;
                    for (int c = 0; c < channels; c++)
                    {
                        float value = (float)Math.Exp(logits[offset + c] - maximum);
                        output[offset + c] = value;
                        sum += value;
                    }
                    for (int c = 0; c < channels; c++)
                    {
                        output[offset + c] /= (float)sum;
                    }
                }
            }
            else
            {
                for (int i = 0; i < logits.Length; i++)
                {
                    float value = logits[i];
                    switch (activation)
                    {
                        case CnnActivation.ReLU:
                            output[i] = Math.Max(0, value);
                            break;
                        case CnnActivation.Sigmoid:
                            double exponential = Math.Exp(-Math.Abs(value));
                            output[i] = (float)(value >= 0 ? 1 / (1 + exponential) : exponential / (1 + exponential));
                            break;
                        case CnnActivation.Tanh:
                            output[i] = (float)Math.Tanh(value);
                            break;
                        default:
                            output[i] = value;
                            break;
                    }
                }
            }
            EnsureFinite(output);
        }

        internal static void Backward(float[] output, float[] incoming, float[] gradient, CnnActivation activation, int channels)
        {
            if (activation == CnnActivation.Softmax)
            {
                // Jacobian-vector product per spatial location; channels are contiguous in HWC.
                for (int offset = 0; offset < output.Length; offset += channels)
                {
                    double dot = 0;
                    for (int c = 0; c < channels; c++)
                    {
                        dot += output[offset + c] * incoming[offset + c];
                    }
                    for (int c = 0; c < channels; c++)
                    {
                        gradient[offset + c] = output[offset + c] * (incoming[offset + c] - (float)dot);
                    }
                }
                return;
            }
            for (int i = 0; i < output.Length; i++)
            {
                float value = output[i];
                switch (activation)
                {
                    case CnnActivation.ReLU:
                        gradient[i] = value > 0 ? incoming[i] : 0;
                        break;
                    case CnnActivation.Sigmoid:
                        gradient[i] = incoming[i] * value * (1 - value);
                        break;
                    case CnnActivation.Tanh:
                        gradient[i] = incoming[i] * (1 - value * value);
                        break;
                    default:
                        gradient[i] = incoming[i];
                        break;
                }
            }
        }

        internal static void EnsureFinite(float[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (float.IsNaN(values[i]) || float.IsInfinity(values[i]))
                {
                    throw new ArithmeticException("Training produced a non-finite value. Reduce the learning rate or model size.");
                }
            }
        }
    }
}
