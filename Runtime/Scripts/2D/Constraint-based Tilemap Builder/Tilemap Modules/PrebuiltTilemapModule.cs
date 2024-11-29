using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using Rng = System.Random;

namespace Zlitz.Extra2D.WorldGen
{
    [AddComponentMenu("Zlitz/Extra2D/World Gen/Tilemap Module (Prebuilt)")]
    public sealed class PrebuiltTilemapModule : MonoBehaviour, ITilemapModule<PrebuiltTilemapModule.State>
    {
        [SerializeField]
        private string m_id;

        [SerializeField]
        private Vector2Int m_regionSize = Vector2Int.one;

        [SerializeField]
        private ConnectionData[] m_connections;

        [SerializeField]
        private Symmetry m_symmetry;

        private TilemapModuleSet m_moduleSet;

        public TilemapModuleSet moduleSet
        {
            get
            {
                ValidateContainerSet();
                return m_moduleSet;
            }
        }

        private bool ValidateContainerSet()
        {
            if (m_moduleSet == null)
            {
                Transform parentTransform = transform.parent;
                while (parentTransform != null)
                {
                    if (parentTransform.TryGetComponent(out TilemapModuleSet moduleSet))
                    {
                        m_moduleSet = moduleSet;
                        break;
                    }
                    parentTransform = parentTransform.parent;
                }
            }
            
            return m_moduleSet != null;
        }

        [Serializable]
        private struct ConnectionData
        {
            [SerializeField]
            private Vector2Int m_position;

            [SerializeField]
            private ConnectDirection m_direction;

            public Vector2Int position => m_position;

            public ConnectDirection direction => m_direction;
        }

        [Flags]
        public enum Symmetry
        {
            Horizontal = 1,
            Vertical   = 2,
        }

        #region ITilemapModule

        public string id => m_id;

        public bool isValid => ValidateContainerSet() && m_regionSize.x != 0 && m_regionSize.y != 0;

        public State CreateState(Rng rng)
        {
            List<Symmetry> symmetryOptions = new List<Symmetry>
            {
                default
            };

            if ((m_symmetry & Symmetry.Horizontal) == Symmetry.Horizontal)
            {
                symmetryOptions.Add(Symmetry.Horizontal);
            }

            if ((m_symmetry & Symmetry.Vertical) == Symmetry.Vertical)
            {
                symmetryOptions.Add(Symmetry.Vertical);

                if ((m_symmetry & Symmetry.Horizontal) == Symmetry.Horizontal)
                {
                    symmetryOptions.Add(Symmetry.Horizontal | Symmetry.Vertical);
                }
            }

            return new State(symmetryOptions.ToArray(), rng);
        }

        public bool NextState(State state)
        {
            return state.Next();
        }

        public GeneratedTilemapModule Generate(State state)
        {
            Debug.Assert(ValidateContainerSet(), "This module does not belong to a TilemapModuleSet");

            Vector3 position = transform.localPosition;

            Vector2Int min = Vector2Int.zero;
            min.x = Mathf.FloorToInt(position.x);
            min.y = Mathf.FloorToInt(position.y);

            Vector2Int regionSize = m_regionSize;

            bool mirrorX = false;
            if (regionSize.x < 0)
            {
                mirrorX = true;
                min.x += regionSize.x;
                regionSize.x = -regionSize.x;
            }
            bool mirrorY = false;
            if (regionSize.y < 0)
            {
                mirrorY = true;
                min.y += regionSize.y;
                regionSize.y = -regionSize.y;
            }

            bool flipX = (state.currentOption & Symmetry.Horizontal) == Symmetry.Horizontal;
            bool flipY = (state.currentOption & Symmetry.Vertical)   == Symmetry.Vertical;

            GeneratedTilemapModule generatedModule = new GeneratedTilemapModule(regionSize);

            for (int dx = 0; dx < regionSize.x; dx++)
            {
                for (int dy = 0; dy < regionSize.y; dy++)
                {
                    Vector2Int pos = min;
                    pos.x += flipX ? (regionSize.x - 1 - dx) : dx;
                    pos.y += flipY ? (regionSize.y - 1 - dy) : dy;

                    Vector2Int offset = new Vector2Int(dx, dy);
                    generatedModule.SetPlaceInfo(offset, m_moduleSet.GetPlaceInfo(pos));
                }
            }

            foreach (ConnectionData connection in m_connections)
            {
                Vector2Int pos = Vector2Int.zero;
                pos.x = flipX ? (regionSize.x - 1 - connection.position.x) : connection.position.x;
                pos.y = flipY ? (regionSize.y - 1 - connection.position.y) : connection.position.y;

                Vector2Int dir = connection.direction.ToVector();
                dir.x = flipX ? -dir.x : dir.x;
                dir.y = flipY ? -dir.y : dir.y;

                generatedModule.SetConnection(pos, dir);
            }

            Transform tr = transform;
            for (int i = 0; i < tr.childCount; i++)
            {
                Transform childTransform = tr.GetChild(i);

                GameObject prefab = childTransform.gameObject;
                Vector3 pos = childTransform.localPosition;
                pos.x = (flipX != mirrorX) ? regionSize.x - pos.x : pos.x;
                pos.y = (flipY != mirrorY) ? regionSize.y - pos.y : pos.y;

                generatedModule.AddPrefab(pos, prefab);
            }

            return generatedModule;
        }

        public sealed class State : ITilemapModuleState
        {
            private Symmetry[] m_symmetryOptions;

            private int m_currentIndex;

            public bool Next()
            {
                m_currentIndex++;
                return m_currentIndex < m_symmetryOptions.Length;
            }

            public Symmetry currentOption => m_symmetryOptions[m_currentIndex];

            public State(Symmetry[] symmetryOptions, Rng rng)
            {
                m_symmetryOptions = symmetryOptions.OrderBy(e => rng.Next()).ToArray();
                m_currentIndex = 0;
            }
        }

        #endregion
    }


}
