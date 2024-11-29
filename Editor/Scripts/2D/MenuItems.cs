using UnityEditor;
using UnityEngine;

namespace Zlitz.Extra2D.WorldGen
{
    internal static class MenuItems
    {
        [MenuItem("GameObject/Zlitz/Extra2D/World Gen/Tilemap Module Set")]
        private static void CreateTilemapModuleSet()
        {
            string name = "New Tilemap Module Set";

            GameObject gameObject = new GameObject(name);
            gameObject.transform.parent = Selection.activeTransform;

            gameObject.AddComponent<Grid>();
            gameObject.AddComponent<TilemapModuleSet>();
        }

        [MenuItem("GameObject/Zlitz/Extra2D/World Gen/Tilemap Module (Prebuilt)")]
        private static void CreatePrebuiltTilemapModule()
        {
            string name = "New Prebuilt Tilemap Module";

            GameObject gameObject = new GameObject(name);
            gameObject.transform.parent = Selection.activeTransform;

            gameObject.AddComponent<PrebuiltTilemapModule>();
        }
    }
}
