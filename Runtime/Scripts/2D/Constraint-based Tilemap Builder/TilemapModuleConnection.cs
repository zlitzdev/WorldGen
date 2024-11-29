using UnityEngine;

namespace Zlitz.Extra2D.WorldGen
{
    public enum ConnectDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    public static class ConnectDirectionExtension
    {
        public static Vector2Int ToVector(this ConnectDirection direction)
        {
            switch (direction)
            {
                case ConnectDirection.Left : return new Vector2Int(-1,  0);
                case ConnectDirection.Right: return new Vector2Int( 1,  0);
                case ConnectDirection.Up   : return new Vector2Int( 0,  1);
                case ConnectDirection.Down : return new Vector2Int( 0, -1);
            }

            return Vector2Int.zero;
        }
    }
}
