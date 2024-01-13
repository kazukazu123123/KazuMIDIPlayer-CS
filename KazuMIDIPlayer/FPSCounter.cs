using System.Diagnostics;

namespace KazuMIDIPlayer
{
    internal class FPSCounter
    {
        private readonly Stopwatch stopwatch;
        private int frameCount;
        private double lastFPS;

        public FPSCounter()
        {
            stopwatch = new Stopwatch();
            frameCount = 0;
            lastFPS = 0.0;
        }

        public void Start()
        {
            stopwatch.Start();
        }

        public void Update()
        {
            frameCount++;

            if (stopwatch.Elapsed.TotalSeconds >= 1.0)
            {
                lastFPS = frameCount / stopwatch.Elapsed.TotalSeconds;
                frameCount = 0;
                stopwatch.Restart();
            }
        }

        public double GetLastFPS()
        {
            return lastFPS;
        }
    }
}
