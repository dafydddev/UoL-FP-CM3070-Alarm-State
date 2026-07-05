using System;
using UnityEngine;
using Random = System.Random;

namespace Generation
{
    [CreateAssetMenu(menuName = "Generation/Difficulty Profile")]
    public class DifficultyProfile : ScriptableObject
    {
        public string label;

        [Serializable]
        public class Range
        {
            public float minFloor;
            public float minCeiling;
            public AnimationCurve minShape = AnimationCurve.Linear(0, 0, 1, 1); // 0-1 fraction from floor to ceiling

            public float maxFloor;
            public float maxCeiling;
            public AnimationCurve maxShape = AnimationCurve.Linear(0, 0, 1, 1); // 0-1 fraction from floor to ceiling
        }

        public Range secondaryObjectives;
        public Range extraExits;
        public Range lockChance;
        public Range guardChance;

        private static float Progress(int level, int totalLevels) =>
            totalLevels <= 1 ? 1f : Mathf.Clamp01((level - 1) / (float)(totalLevels - 1));

        private static float CurrentMin(Range range, float progress) =>
            range.minFloor + (range.minCeiling - range.minFloor) * range.minShape.Evaluate(progress);

        private static float CurrentMax(Range range, float progress) =>
            range.maxFloor + (range.maxCeiling - range.maxFloor) * range.maxShape.Evaluate(progress);

        public int SecondaryObjectiveCount(int level, int totalLevels, Random rng)
        {
            var p = Progress(level, totalLevels);
            var min = CurrentMin(secondaryObjectives, p);
            var max = Mathf.Max(CurrentMax(secondaryObjectives, p), min);
            return rng.Next(Mathf.RoundToInt(min), Mathf.RoundToInt(max) + 1);
        }

        public int ExtraExitCount(int level, int totalLevels, Random rng)
        {
            var p = Progress(level, totalLevels);
            var min = CurrentMin(extraExits, p);
            var max = Mathf.Max(CurrentMax(extraExits, p), min);
            return rng.Next(Mathf.RoundToInt(min), Mathf.RoundToInt(max) + 1);
        }

        public float LockChance(int level, int totalLevels, Random rng)
        {
            var p = Progress(level, totalLevels);
            var min = CurrentMin(lockChance, p);
            var max = Mathf.Max(CurrentMax(lockChance, p), min);
            return min + (float)rng.NextDouble() * (max - min);
        }

        public float GuardChance(int level, int totalLevels, Random rng)
        {
            var p = Progress(level, totalLevels);
            var min = CurrentMin(guardChance, p);
            var max = Mathf.Max(CurrentMax(guardChance, p), min);
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
