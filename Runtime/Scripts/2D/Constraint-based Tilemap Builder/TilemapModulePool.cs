using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using Rng = System.Random;

namespace Zlitz.Extra2D.WorldGen
{
    [Serializable]
    public class TilemapModulePool
    {
        [SerializeField]
        private List<Entry> m_entries = new List<Entry>();

        public TilemapModulePool Add(string moduleId, float weight)
        {
            moduleId ??= "";
            weight = Mathf.Max(0.0f, weight);
            m_entries.Add(new Entry(moduleId, weight));
            return this;
        }

        public TilemapModulePool()
        {
            m_entries.Clear();
        }

        public TilemapModulePool(TilemapModulePool other)
        {
            m_entries.Clear();
            m_entries.AddRange(other.m_entries);
        }

        internal string[] ShuffleByWeight(Rng rng)
        {
            return m_entries.OrderByDescending(e => e.weight * (float)rng.NextDouble()).Select(e => e.moduleId).ToArray();
        }

        [Serializable]
        private struct Entry
        {
            [SerializeField]
            private string m_moduleId;
            
            [SerializeField]
            private float m_weight;

            public string moduleId => m_moduleId;

            public float weight => m_weight;

            public Entry(string moduleId, float weight)
            {
                m_moduleId = moduleId;
                m_weight = weight;
            }
        }
    }
}
