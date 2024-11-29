using System;
using System.Collections.Generic;

using UnityEngine;

using Rng = System.Random;

namespace Zlitz.Extra2D.WorldGen
{
    internal sealed class PlacePositionInstruction : TilemapModulePlaceInstruction.Instruction
    {
        private TilemapModulePool m_modulePool;
        private Vector2Int m_position;
        private Action<IEnumerable<TilemapModulePlaceInstruction.Connection>, int, Rng> m_newConnectionCallback;

        private string[] m_modules;
        private int m_moduleIndex;

        private ITilemapModule m_module;
        private ITilemapModuleState m_state;
        private GeneratedTilemapModule m_generated;

        public override string debugName => m_module?.id ?? null;

        public PlacePositionInstruction(TilemapModulePool modulePool, Vector2Int position, Action<IEnumerable<TilemapModulePlaceInstruction.Connection>, int, Rng> newConnectionCallback)
        {
            m_modulePool = modulePool;
            m_position = position;
            m_newConnectionCallback = newConnectionCallback;
        }

        public override void Reset(TilemapModuleSet moduleSet, TilemapModulePlaceInstruction.LayoutStateStack states, Rng rng)
        {
            m_modules = m_modulePool.ShuffleByWeight(rng);
            m_moduleIndex = 0;

            if (m_moduleIndex < m_modules.Length)
            {
                string moduleId = m_modules[m_moduleIndex];

                m_module = null;
                if (moduleSet.modules.TryGetValue(moduleId, out ITilemapModule m))
                {
                    m_module = m;
                }

                m_state = m_module?.CreateBaseState(rng);
                m_generated = m_state == null ? null : m_module?.GenerateBase(m_state);
            }
        }

        public override bool Execute(TilemapModuleSet moduleSet, TilemapModulePlaceInstruction.LayoutStateStack states, Rng rng)
        {
            if (m_generated == null)
            {
                return false;
            }

            TilemapModulePlaceInstruction.LayoutState currentState = states.currentState;
            if (!currentState.TryPlace(m_generated, m_position))
            {
                return false;
            }

            currentState.AddPlacement(m_module, m_generated, m_position, (c, r) => m_newConnectionCallback?.Invoke(c, repeatIndex, r), rng);
            return true;
        }

        public override bool Next(TilemapModuleSet moduleSet, Rng rng)
        {
            if (m_module != null && m_state != null && m_module.NextBaseState(m_state))
            {
                m_generated = m_module.GenerateBase(m_state);
                return true;
            }

            m_moduleIndex++;
            if (m_moduleIndex < m_modules.Length)
            {
                string moduleId = m_modules[m_moduleIndex];

                m_module = null;
                if (moduleSet.modules.TryGetValue(moduleId, out ITilemapModule m))
                {
                    m_module = m;
                }

                m_state = m_module?.CreateBaseState(rng);
                m_generated = m_state == null ? null : m_module?.GenerateBase(m_state);

                return true;
            }

            return false;
        }

        protected override TilemapModulePlaceInstruction.Instruction Copy()
        {
            return new PlacePositionInstruction(m_modulePool, m_position, m_newConnectionCallback);
        }
    }
}
