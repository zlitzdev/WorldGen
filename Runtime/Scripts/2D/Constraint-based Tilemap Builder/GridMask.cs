using System.Collections.Generic;

using UnityEngine;

namespace Zlitz.Extra2D.WorldGen
{
    internal sealed class GridMask
    {
        private readonly Dictionary<Vector2Int, ulong> m_chunks = new Dictionary<Vector2Int, ulong>();

        public bool Add(Vector2Int position)
        {
            (Vector2Int chunkId, int index) = ConvertPosition(position);

            if (!m_chunks.TryGetValue(chunkId, out ulong bits))
            {
                bits = 0;
            }

            ulong mask = 1ul << index;

            bool currentBit = (bits & mask) != 0;
            if (currentBit)
            {
                return false;
            }

            bits |= mask;
            m_chunks[chunkId] = bits;
            return true;
        }

        public bool Remove(Vector2Int position)
        {
            (Vector2Int chunkId, int index) = ConvertPosition(position);

            if (!m_chunks.TryGetValue(chunkId, out ulong bits))
            {
                return false;
            }

            ulong mask = 1ul << index;

            bool currentBit = (bits & mask) != 0;
            if (!currentBit)
            {
                return false;
            }

            bits &= ~mask;
            m_chunks[chunkId] = bits;
            return true;
        }

        public bool Contains(Vector2Int position)
        {
            (Vector2Int chunkId, int index) = ConvertPosition(position);

            if (!m_chunks.TryGetValue(chunkId, out ulong bits))
            {
                return false;
            }

            ulong mask = 1ul << index;
            return (bits & mask) != 0;
        }

        public GridMask()
        {
        }

        public GridMask(GridMask other)
        {
            m_chunks.Clear();
            foreach (KeyValuePair<Vector2Int, ulong> chunk in other.m_chunks)
            {
                m_chunks.Add(chunk.Key, chunk.Value);
            }
        }

        private (Vector2Int, int) ConvertPosition(Vector2Int position)
        {
            Vector2Int chunkId = Vector2Int.zero;

            chunkId.x = Mathf.FloorToInt(position.x * 0.125f);
            chunkId.y = Mathf.FloorToInt(position.y * 0.125f);

            int ix = position.x - chunkId.x * 8;
            int iy = position.y - chunkId.y * 8;
        
            int index = iy * 8 + ix;

            return (chunkId, index);
        }

        private Vector2Int CombinePosition(Vector2Int chunkid, int index)
        {
            int ix = index % 8;
            int iy = index / 8;

            chunkid.x = chunkid.x * 8 + ix;
            chunkid.y = chunkid.y * 8 + iy;

            return chunkid;
        }
    }
}
