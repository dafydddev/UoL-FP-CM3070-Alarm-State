using System;
using UnityEngine;

namespace Generation
{
    [CreateAssetMenu(menuName = "Generation/Difficulty Profile")]
    public class DifficultyProfile : ScriptableObject
    {
        public string label; // e.g. "Easy", "Medium", "Hard" — purely descriptive

        [Serializable]
        public class Stat
        {
            public float baseValue; // starting value
            public float perLevel; // how the value changes between levels (e.g. +1, -1)
            public float min = 0;
            public float max = 1;
            public float Evaluate(int level) => Mathf.Clamp(baseValue + perLevel * level, min, max);
        }

        public Stat minSecondaryObjectives;
        public Stat maxSecondaryObjectives;
        public Stat exitCount;
        public Stat lockChance;
    }
}