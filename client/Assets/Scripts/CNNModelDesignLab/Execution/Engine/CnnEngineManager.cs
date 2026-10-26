using System;
using System.Threading.Tasks;

namespace ModelDesignScene.Execution.Engine
{
    public sealed class CnnEngineManager : IDisposable
    {
        private readonly CnnTrainingSession _session = new CnnTrainingSession();

        public bool HasTrainedModel => _session.HasTrainedModel;

        public Task<TrainingResult> TrainAsync(CnnTrainingRequest request, Action<TrainingProgress> progress)
        {
            return _session.TrainAsync(request, progress);
        }

        public Task<InferenceResult> InferAsync(float[] input, string imageId)
        {
            return _session.InferAsync(input, imageId);
        }

        public void Cancel()
        {
            _session.Cancel();
        }

        public void Dispose()
        {
            _session.Dispose();
        }
    }
}
