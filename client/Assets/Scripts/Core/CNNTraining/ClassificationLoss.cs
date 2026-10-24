using System;

namespace CNNTraining
{
    internal static class ClassificationLoss
    {
        internal static double Calculate(float[] logits, int label)
        {
            double maximum = logits[0];
            for (int i = 1; i < logits.Length; i++)
            {
                maximum = Math.Max(maximum, logits[i]);
            }
            double sum = 0;
            for (int i = 0; i < logits.Length; i++)
            {
                sum += Math.Exp(logits[i] - maximum);
            }
            // Log-sum-exp avoids clipping the loss when the correct class is very unlikely.
            return maximum - logits[label] + Math.Log(sum);
        }

        internal static void Gradient(float[] probabilities, int label, float[] gradient)
        {
            Array.Copy(probabilities, gradient, probabilities.Length);
            gradient[label] -= 1;
        }

        internal static int Predict(float[] probabilities)
        {
            int prediction = 0;
            for (int i = 1; i < probabilities.Length; i++)
            {
                if (probabilities[i] > probabilities[prediction])
                {
                    prediction = i;
                }
            }
            return prediction;
        }
    }
}
