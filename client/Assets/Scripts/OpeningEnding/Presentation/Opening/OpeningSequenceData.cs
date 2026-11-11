using System;

namespace Cinematic
{
    [Serializable]
    public sealed class OpeningSequenceData
    {
        public double QuestionDuration = 3.5;
        public double RewindDuration = 3.5;
        public double PostOfficeDuration = 5.6;
        public double WindowZoomDuration = 1.4;
        public double StampDuration = 2.8;
        public float StampContactOffset = 1.4f;
        public double EnvelopeDuration = 4;
        public double DigitDuration = 1.2;
        public double PixelDuration = 8.6;
        public float PixelOrbitOffset = 2.2f;
        public float PixelGatherOffset = 7.8f;
        public double FactoryRevealDuration = .4;
        public double DoorsDuration = 4;

        public float Rewind => (float)QuestionDuration;
        public float Arrival => (float)ArrivalTime;
        public float Zoom => (float)ZoomTime;
        public float StampShot => (float)StampTime;
        public float Envelope => (float)EnvelopeTime;
        public float Digit => (float)DigitTime;
        public float Pixels => (float)PixelTime;
        public float Factory => (float)FactoryTime;
        public float Doors => (float)DoorsTime;
        public float Duration => (float)(DoorsTime + DoorsDuration);

        internal double ArrivalTime => QuestionDuration + RewindDuration;
        private double ZoomTime => ArrivalTime + PostOfficeDuration;
        private double StampTime => ZoomTime + WindowZoomDuration;
        internal double EnvelopeTime => StampTime + StampDuration;
        private double DigitTime => EnvelopeTime + EnvelopeDuration;
        internal double PixelTime => DigitTime + DigitDuration;
        private double FactoryTime => PixelTime + PixelDuration;
        internal double DoorsTime => FactoryTime + FactoryRevealDuration;
    }
}
