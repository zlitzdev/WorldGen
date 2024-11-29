using System;
using System.Reflection;

using UnityEngine;
using UnityEditor;

namespace Zlitz.Extra2D.WorldGen
{
    [InitializeOnLoad]
    internal static class ScriptIcons
    {
        private static readonly Type s_monoBehaviourType                 = typeof(MonoBehaviour);
        private static readonly Type s_tilemapModuleType                 = typeof(ITilemapModule);
        private static readonly Type s_tilemapModulePlaceInstructionType = typeof(TilemapModulePlaceInstruction);

        private static Texture2D s_tilemapModuleIcon;
        private static Texture2D s_tilemapModulePlaceInstructionIcon;

        private static MethodInfo s_setIconEnabledMethod;

        static ScriptIcons()
        {
            SetGizmoIconEnabled(typeof(TilemapModuleSet), false);
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsAbstract)
                    {
                        continue;
                    }

                    if (s_tilemapModuleType.IsAssignableFrom(type) && s_monoBehaviourType.IsAssignableFrom(type))
                    {
                        if (s_tilemapModuleIcon == null)
                        {
                            s_tilemapModuleIcon = Resources.Load<Texture2D>("Icon_TilemapModule");
                        }

                        if (s_tilemapModuleIcon != null)
                        {
                            GameObject temp = new GameObject();
                            MonoBehaviour component = temp.AddComponent(type) as MonoBehaviour;

                            MonoScript monoScript = MonoScript.FromMonoBehaviour(component);
                            if (monoScript != null)
                            {
                                MonoImporter monoImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(monoScript)) as MonoImporter;
                                if (monoImporter != null)
                                {
                                    SetGizmoIconEnabled(type, false);
                                    if (monoImporter.GetIcon() == null)
                                    {
                                        monoImporter.SetIcon(s_tilemapModuleIcon);
                                        monoImporter.SaveAndReimport();
                                    }
                                }
                            }

                            GameObject.DestroyImmediate(temp);
                        }
                    }

                    if (s_tilemapModulePlaceInstructionType.IsAssignableFrom(type))
                    {
                        if (s_tilemapModulePlaceInstructionIcon == null)
                        {
                            s_tilemapModulePlaceInstructionIcon = Resources.Load<Texture2D>("Icon_TilemapModulePlaceInstruction");
                        }

                        if (s_tilemapModulePlaceInstructionIcon != null)
                        {
                            GameObject temp = new GameObject();
                            MonoBehaviour component = temp.AddComponent(type) as MonoBehaviour;

                            MonoScript monoScript = MonoScript.FromMonoBehaviour(component);
                            if (monoScript != null)
                            {
                                MonoImporter monoImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(monoScript)) as MonoImporter;
                                if (monoImporter != null)
                                {
                                    SetGizmoIconEnabled(type, false);
                                    if (monoImporter.GetIcon() == null)
                                    {
                                        monoImporter.SetIcon(s_tilemapModulePlaceInstructionIcon);
                                        monoImporter.SaveAndReimport();
                                    }
                                }
                            }

                            GameObject.DestroyImmediate(temp);
                        }
                    }
                }
            }
        }
    
        private static void SetGizmoIconEnabled(Type type, bool on)
        {
            if (s_tilemapModuleIcon == null)
            {
                s_setIconEnabledMethod = Assembly.GetAssembly(typeof(Editor))?.GetType("UnityEditor.AnnotationUtility")?.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic);
            }

            if (s_setIconEnabledMethod == null)
            {
                return;
            }

            const int MONO_BEHAVIOR_CLASS_ID = 114;
            s_setIconEnabledMethod.Invoke(null, new object[] { MONO_BEHAVIOR_CLASS_ID, type.Name, on ? 1 : 0 });
        }
    }
}
