using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;

namespace Zlitz.Extra2D.WorldGen
{
    [AddComponentMenu("Zlitz/Extra2D/World Gen/Tilemap Module Set")]
    [RequireComponent(typeof(Grid))]
    public sealed class TilemapModuleSet : MonoBehaviour
    {
        [SerializeField]
        private Tilemap m_mask;
        
        [SerializeField]
        private LayerEntry[] m_layers;

        private Grid m_grid;

        public Grid grid
        {
            get
            {
                if (m_grid == null)
                {
                    m_grid = GetComponent<Grid>();
                }
                return m_grid;
            }
        }

        public IReadOnlyDictionary<string, Tilemap> layers
        {
            get
            {
                Dictionary<string, Tilemap> results = new Dictionary<string, Tilemap>();

                foreach (LayerEntry layer in m_layers)
                {
                    if (!results.TryAdd(layer.layerId, layer.tilemap))
                    {
                        Debug.LogWarning($"Repeated layer ID detected: {layer.layerId}");
                    }
                    if (layer.tilemap == null)
                    {
                        Debug.LogWarning($"Layer {layer.layerId} has no tilemap");
                    }
                }

                return results;
            }
        }

        public IReadOnlyDictionary<string, ITilemapModule> modules
        {
            get
            {
                Dictionary<string, ITilemapModule> results = new Dictionary<string, ITilemapModule>();

                foreach (ITilemapModule module in GetComponentsInChildren<ITilemapModule>())
                {
                    if (!results.TryAdd(module.id, module))
                    {
                        Debug.LogWarning($"ITilemapModule {module} is ignored because of repeated id {module.id}.");
                    }
                }

                return results;
            }
        }

        public TilemapPlaceInfo GetPlaceInfo(Vector2Int position)
        {
            Vector3Int pos = new Vector3Int(position.x, position.y, 0);

            if (m_mask != null && m_mask.GetTile(pos) != null)
            {
                return null;
            }

            TilemapPlaceInfo placeInfo = new TilemapPlaceInfo();
            foreach (LayerEntry layerEntry in m_layers)
            {
                placeInfo.SetTile(layerEntry.layerId, layerEntry.tilemap?.GetTile(pos));
            }

            return placeInfo;
        }

        private void Awake()
        {
            m_grid = GetComponent<Grid>();
        }

        [Serializable]
        private struct LayerEntry
        {
            [SerializeField]
            private string m_layerId;

            [SerializeField]
            private Tilemap m_tilemap;

            public string layerId => m_layerId;

            public Tilemap tilemap => m_tilemap;
        }
    }
}
