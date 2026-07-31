using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Run
{
    // How well the player has done over the last few levels. The room graph's adaptive pass reads Standing.
    public sealed class RunPerformance
    {
        // How many levels of history are kept. Standing stays at 0 until this many have been recorded.
        public const int Window = 3;

        // How many levels must pass between one adaptive room and the next.
        public const int Cooldown = 2;

        // What one alarm takes off a level's score.
        private const float AlarmPenalty = 0.5f;

        private readonly Queue<LevelOutcome> _recent = new();

        private readonly struct LevelOutcome
        {
            public LevelOutcome(int hearts, int maxHearts, int alarms)
            {
                Hearts = hearts;
                MaxHearts = maxHearts;
                Alarms = alarms;
            }

            public int Hearts { get; }
            public int MaxHearts { get; }
            public int Alarms { get; }
        }

        // The level the last adaptive room was added at, or -1 if no room has been added yet.
        public int LastInjectedLevel { get; private set; } = -1;

        // Adds the level just finished. Once the window is full, the oldest level drops out.
        public void RecordLevel(int hearts, int maxHearts, int alarms)
        {
            _recent.Enqueue(new LevelOutcome(hearts, maxHearts, alarms));
            if (_recent.Count > Window) _recent.Dequeue();
        }

        public void RecordInjection(int level) => LastInjectedLevel = level;

        // -1 (struggling) to +1 (thriving), and 0 until the window is full.
        public float Standing
        {
            get
            {
                if (_recent.Count < Window) return 0f;

                var total = _recent.Sum(Score);
                return Mathf.Clamp(total / _recent.Count, -1f, 1f);
            }
        }

        // True when enough levels have been recorded and the cooldown since the last room has passed.

        public bool CanInject(int level) => _recent.Count >= Window &&
                                            (LastInjectedLevel < 0 || level == LastInjectedLevel ||
                                             level - LastInjectedLevel > Cooldown);

        // Scores one level: how much of the health bar was left, minus what the alarms cost.
        private static float Score(LevelOutcome outcome)
        {
            var maxHearts = Mathf.Max(1, outcome.MaxHearts);
            var hearts = Mathf.Clamp(outcome.Hearts, 0, maxHearts);
            var survived = maxHearts <= 1 ? hearts : (hearts - 1f) / (maxHearts - 1f);
            return Mathf.Clamp(Mathf.Lerp(-1f, 1f, survived) - AlarmPenalty * outcome.Alarms, -1f, 1f);
        }
    }
}