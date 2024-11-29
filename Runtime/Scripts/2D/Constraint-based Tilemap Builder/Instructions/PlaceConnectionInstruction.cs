using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using Rng = System.Random;

namespace Zlitz.Extra2D.WorldGen
{
    internal sealed class PlaceConnectionInstruction : TilemapModulePlaceInstruction.Instruction
    {
        private TilemapModulePool m_modulePool;
        private Func<int, string[]> m_connectionTag;
        private Action<IEnumerable<TilemapModulePlaceInstruction.Connection>, int, Rng> m_newConnectionCallback;

        private string[] m_modules;
        private int m_moduleIndex;

        private ITilemapModule m_module;
        private ITilemapModuleState m_state;
        private GeneratedTilemapModule m_generated;

        private TilemapModulePlaceInstruction.Connection[] m_connections;
        private int m_connectionIndex;

        private KeyValuePair<Vector2Int, Vector2Int>[] m_generatedConnections;
        private int m_generatedConnectionIndex;

        public override string debugName => m_module?.id ?? null;

        public PlaceConnectionInstruction(TilemapModulePool modulePool, Func<int, string[]> connectionTag, Action<IEnumerable<TilemapModulePlaceInstruction.Connection>, int, Rng> newConnectionCallback)
        {
            m_modulePool = modulePool;
            m_connectionTag = connectionTag;
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

            m_connections = states.currentState.GetConnections(m_connectionTag?.Invoke(repeatIndex)).OrderBy(c => rng.Next()).ToArray();
            m_connectionIndex = 0;

            m_generatedConnections = m_generated?.connections.OrderBy(c => rng.Next()).ToArray();
            m_generatedConnectionIndex = 0;
        }

        public override bool Execute(TilemapModuleSet moduleSet, TilemapModulePlaceInstruction.LayoutStateStack states, Rng rng)
        {
            if (m_generated == null ||
                m_connections == null || m_connections.Length <= 0 ||
                m_generatedConnections == null || m_generatedConnections.Length <= 0)
            {
                return false;
            }

            TilemapModulePlaceInstruction.LayoutState currentState = states.currentState;

            TilemapModulePlaceInstruction.Connection connectTo = m_connections[m_connectionIndex];

            KeyValuePair<Vector2Int, Vector2Int> generatedConnection = m_generatedConnections[m_generatedConnectionIndex];

            if (generatedConnection.Value + connectTo.direction != Vector2Int.zero)
            {
                return false;
            }

            Vector2Int placemenetPosition = connectTo.position + connectTo.direction - generatedConnection.Key;
            if (!currentState.TryPlace(m_generated, placemenetPosition))
            {
                return false;
            }

            currentState.AddPlacement(m_module, m_generated, placemenetPosition, (c, r) => m_newConnectionCallback?.Invoke(c, repeatIndex, r), rng, connectTo.position + connectTo.direction);
            return true;
        }

        public override bool Next(TilemapModuleSet moduleSet, Rng rng)
        {
            m_generatedConnectionIndex++;
            if (m_generatedConnectionIndex >= m_generatedConnections.Length)
            {
                m_generatedConnectionIndex = 0;

                m_connectionIndex++;
                if (m_connectionIndex >= m_connections.Length)
                {
                    m_connectionIndex = 0;
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

                        m_generatedConnections = m_generated?.connections.OrderBy(c => rng.Next()).ToArray();
                        m_generatedConnectionIndex = 0;

                        return true;
                    }

                    return false;
                }
            }

            return true;
        }

        protected override TilemapModulePlaceInstruction.Instruction Copy()
        {
            return new PlaceConnectionInstruction(m_modulePool, m_connectionTag, m_newConnectionCallback);
        }
    }
}
