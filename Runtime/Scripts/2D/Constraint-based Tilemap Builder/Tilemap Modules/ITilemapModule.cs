using System.Collections.Generic;
using UnityEngine;

using Rng = System.Random;

namespace Zlitz.Extra2D.WorldGen
{
    public interface ITilemapModule
    {
        string id { get; }

        bool isValid { get; }

        ITilemapModuleState CreateBaseState(Rng rng);

        bool NextBaseState(ITilemapModuleState state);

        GeneratedTilemapModule GenerateBase(object state);
    }

    public interface ITilemapModule<TState> : ITilemapModule where TState : class, ITilemapModuleState
    {
        ITilemapModuleState ITilemapModule.CreateBaseState(Rng rng) => CreateState(rng);

        bool ITilemapModule.NextBaseState(ITilemapModuleState state) => NextState(state as TState);

        GeneratedTilemapModule ITilemapModule.GenerateBase(object state) => Generate(state as TState);

        TState CreateState(Rng rng);

        bool NextState(TState state);

        GeneratedTilemapModule Generate(TState state);
    }

    public class GeneratedTilemapModule
    {
        public Vector2Int regionSize { get; private set; }

        private TilemapPlaceInfo[,] m_placeInfo;

        private List<KeyValuePair<Vector2Int, Vector2Int>> m_connections = new List<KeyValuePair<Vector2Int, Vector2Int>>();

        private List<KeyValuePair<Vector3, GameObject>> m_prefabs = new List<KeyValuePair<Vector3, GameObject>>();

        public IEnumerable<KeyValuePair<Vector2Int, Vector2Int>> connections => m_connections;

        public IEnumerable<KeyValuePair<Vector3, GameObject>> prefabs => m_prefabs;

        public TilemapPlaceInfo GetPlaceInfo(Vector2Int position)
        {
            return m_placeInfo[position.x, position.y];
        }

        public void SetPlaceInfo(Vector2Int position, TilemapPlaceInfo placeInfo)
        {
            m_placeInfo[position.x, position.y] = placeInfo;
        }

        public void SetConnection(Vector2Int position, Vector2Int direction)
        {
            for (int i = 0; i < m_connections.Count; i++)
            {
                if (m_connections[i].Key == position)
                {
                    m_connections[i] = new KeyValuePair<Vector2Int, Vector2Int>(position, direction);
                    return;
                }
            }

            m_connections.Add(new KeyValuePair<Vector2Int, Vector2Int>(position, direction));
        }

        public void AddPrefab(Vector3 position, GameObject prefab)
        {
            m_prefabs.Add(new KeyValuePair<Vector3, GameObject>(position, prefab));
        }

        public GeneratedTilemapModule(Vector2Int regionSize)
        {
            this.regionSize = regionSize;
            m_placeInfo = new TilemapPlaceInfo[regionSize.x, regionSize.y];
        }
    }
}
