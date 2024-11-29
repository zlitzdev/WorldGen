using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;

namespace Zlitz.Extra2D.WorldGen
{
    [AddComponentMenu("Zlitz/Extra2D/World Gen/Tilemap Module Placer")]
    [RequireComponent(typeof(Grid))]
    public sealed class TilemapModulePlacer : MonoBehaviour
    {
        [SerializeField]
        private Transform m_objectContainer;
        
        [SerializeField]
        private List<LayerEntry> m_layers = new List<LayerEntry>();

        [SerializeField]
        private List<GameObject> m_placedObjects = new List<GameObject>();

        private Grid m_grid;

        private Grid grid
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

        public void Clear()
        {
            ResetLayers();
            foreach (GameObject gameObject in m_placedObjects)
            {
                SafeDestroy(gameObject);
            }
            m_placedObjects.Clear();
        }

        public bool Begin(TilemapModuleSet moduleSet)
        {
            if (moduleSet == null)
            {
                Debug.LogWarning("ModuleSet must not be null.");
                return false;
            }

            if (!CopyGrid(moduleSet))
            {
                Debug.LogWarning("ModuleSet does not have a Grid component.");
                return false;
            }

            return CopyLayers(moduleSet);
        }

        public void Place(GeneratedTilemapModule generatedModule, Vector2Int position)
        {
            for (int dx = 0; dx < generatedModule.regionSize.x; dx++)
            {
                for (int dy = 0; dy < generatedModule.regionSize.y; dy++)
                {
                    Vector2Int offset = new Vector2Int(dx, dy);

                    TilemapPlaceInfo placeInfo = generatedModule.GetPlaceInfo(offset);
                    if (placeInfo != null)
                    {
                        Place(placeInfo, position + offset);
                    }
                }
            }

            foreach (KeyValuePair<Vector3, GameObject> prefab in generatedModule.prefabs)
            {
                Vector3 pos = prefab.Key;
                pos.x += position.x;
                pos.y += position.y;
                AddObject(Instantiate(prefab.Value), pos);
            }
        }

        public async Task PlaceAsync(GeneratedTilemapModule generatedModule, Vector2Int position, CancellationToken ct)
        {
            List<Task> placeTasks = new List<Task>();
            for (int dx = 0; dx < generatedModule.regionSize.x; dx++)
            {
                for (int dy = 0; dy < generatedModule.regionSize.y; dy++)
                {
                    if (ct.IsCancellationRequested)
                    {
                        await Task.WhenAll(placeTasks);
                        return;
                    }

                    Vector2Int offset = new Vector2Int(dx, dy);

                    TilemapPlaceInfo placeInfo = generatedModule.GetPlaceInfo(offset);
                    if (placeInfo != null)
                    {
                        placeTasks.Add(PlaceAsync(placeInfo, position + offset, ct));
                    }
                }
            }

            AsyncInstantiateOperation<GameObject>[] instantiateOperations = new AsyncInstantiateOperation<GameObject>[generatedModule.prefabs.Count()];
            int index = 0;
            foreach (KeyValuePair<Vector3, GameObject> prefab in generatedModule.prefabs)
            {
                if (ct.IsCancellationRequested)
                {
                    await Task.WhenAll(placeTasks);
                    for (int i = 0; i < index; i++)
                    {
                        instantiateOperations[i].WaitForCompletion();
                    }
                    return;
                }

                Vector3 pos = prefab.Key;
                pos.x += position.x;
                pos.y += position.y;

                int k = index;

                instantiateOperations[index] = InstantiateAsync(prefab.Value);
                instantiateOperations[index].completed += o =>
                {
                    AddObject(instantiateOperations[k].Result[0], pos);
                };

                index++;
                await Task.Yield();
            }

            await Task.WhenAll(placeTasks);
            foreach (AsyncInstantiateOperation<GameObject> operation in instantiateOperations)
            {
                operation.WaitForCompletion();
            }
        }

        private void Place(TilemapPlaceInfo placeInfo, Vector2Int position)
        {
            foreach (KeyValuePair<string, TileBase> tile in placeInfo.tiles)
            {
                foreach (LayerEntry layer in m_layers)
                {
                    if (layer.layerId == tile.Key)
                    {
                        layer.tilemap?.SetTile(new Vector3Int(position.x, position.y, 0), tile.Value);
                    }
                }
            }
        }

        private async Task PlaceAsync(TilemapPlaceInfo placeInfo, Vector2Int position, CancellationToken ct)
        {
            foreach (KeyValuePair<string, TileBase> tile in placeInfo.tiles)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }
                foreach (LayerEntry layer in m_layers)
                {
                    if (layer.layerId == tile.Key)
                    {
                        layer.tilemap?.SetTile(new Vector3Int(position.x, position.y, 0), tile.Value);
                    }
                }
                await Task.Yield();
            }
        }

        private void AddObject(GameObject gameObject, Vector3 position)
        {
            gameObject.transform.parent = m_objectContainer ?? transform;
            gameObject.transform.localPosition = position;
            m_placedObjects.Add(gameObject);
        }

        private bool CopyGrid(TilemapModuleSet moduleSet)
        {
            if (moduleSet.grid == null)
            {
                return false;
            }

            grid.enabled     = moduleSet.grid.enabled;
            grid.cellSize    = moduleSet.grid.cellSize;
            grid.cellGap     = moduleSet.grid.cellGap;
            grid.cellLayout  = moduleSet.grid.cellLayout;
            grid.cellSwizzle = moduleSet.grid.cellSwizzle;

            return true;
        }

        private bool CopyLayers(TilemapModuleSet moduleSet)
        {
            if (moduleSet == null)
            {
                return false;
            }

            ResetLayers();

            foreach (KeyValuePair<string, Tilemap> layer in moduleSet.layers)
            {
                m_layers.Add(new LayerEntry(layer.Key, CopyTilemap(layer.Value, $"Layer ({layer.Key})", transform)));
            }

            return true;
        }

        private void ResetLayers()
        {
            foreach (LayerEntry layerEntry in m_layers)
            {
                SafeDestroy(layerEntry.tilemap.gameObject);
            }
            m_layers.Clear();
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

            public LayerEntry(string layerId, Tilemap tilemap)
            {
                m_layerId = layerId;
                m_tilemap = tilemap;
            }
        }

        #region Helper methods

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (!Application.isPlaying)
            {
                DestroyImmediate(obj);
            }
            else
            {
                Destroy(obj);
            }
        }

        private static Tilemap CopyTilemap(Tilemap sourceTilemap, string name, Transform parent)
        {
            Debug.Assert(sourceTilemap != null, "Source tilemap must not be null");

            Tilemap tilemap = Instantiate(sourceTilemap, parent);
            tilemap.ClearAllTiles();

            GameObject gameObject = tilemap.gameObject;
            gameObject.name = name;

            Transform transform = tilemap.transform;
            transform.localPosition = Vector3.zero;
            transform.rotation      = Quaternion.identity;
            transform.localScale    = Vector3.one;

            Component[] components = gameObject.GetComponents<Component>();
            foreach (Component component in components)
            {
                if ((component.hideFlags & HideFlags.DontSave) != 0)
                {
                    SafeDestroy(component);
                }
            }

            for (int i = gameObject.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = gameObject.transform.GetChild(i).gameObject;
                if ((child.hideFlags & HideFlags.DontSave) != 0)
                {
                    SafeDestroy(child);
                }
            }

            return tilemap;
        }

        #endregion
    }

    public class TilemapPlaceInfo
    {
        private List<KeyValuePair<string, TileBase>> m_tiles = new List<KeyValuePair<string, TileBase>>();

        public IEnumerable<KeyValuePair<string, TileBase>> tiles => m_tiles;

        public void SetTile(string layerId,  TileBase tile)
        {
            for (int i = 0; i < m_tiles.Count; i++)
            {
                if (m_tiles[i].Key == layerId)
                {
                    m_tiles[i] = new KeyValuePair<string, TileBase>(layerId, tile);
                    return;
                }
            }

            m_tiles.Add(new KeyValuePair<string, TileBase>(layerId, tile));
        }
    }
}
