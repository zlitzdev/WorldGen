using System;
using System.Reflection;

using UnityEngine;
using UnityEditor;
using System.Collections;

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

        private static MethodInfo s_getAnnotationsMethod;
        private static MethodInfo s_setGizmoEnabledMethod;
        private static MethodInfo s_setIconEnabledMethod;

        private static PropertyInfo s_classIdProperty;
        private static PropertyInfo s_scriptClassProperty;

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
                Type annotationType        = Type.GetType("UnityEditor.Annotation, UnityEditor");
                Type annotationUtilityType = Type.GetType("UnityEditor.AnnotationUtility, UnityEditor");

                if (annotationUtilityType != null && annotationType != null)
                {
                    s_getAnnotationsMethod  = annotationUtilityType.GetMethod("GetAnnotations", BindingFlags.Static | BindingFlags.NonPublic);
                    s_setGizmoEnabledMethod = annotationUtilityType.GetMethod("SetGizmoEnabled", BindingFlags.Static | BindingFlags.NonPublic);
                    s_setIconEnabledMethod  = annotationUtilityType.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic);

                    s_classIdProperty     = annotationType.GetProperty("classID", BindingFlags.Public | BindingFlags.Instance);
                    s_scriptClassProperty = annotationType.GetProperty("scriptClass", BindingFlags.Public | BindingFlags.Instance);
                }
            }

            if (s_getAnnotationsMethod == null || s_classIdProperty == null || s_scriptClassProperty == null)
            {
                return;
            }

            IEnumerable annotations = (IEnumerable)s_getAnnotationsMethod.Invoke(null, null);
            if (annotations == null)
            {
                return;
            }

            foreach (object annotation in annotations)
            {
                int    classId     = (int)s_classIdProperty.GetValue(annotation, null);
                string scriptClass = (string)s_scriptClassProperty.GetValue(annotation, null);
            
                if (scriptClass == type.Name)
                {
                    s_setGizmoEnabledMethod?.Invoke(null, new object[] 
                    {
                        classId,
                        scriptClass,
                        on ? 1 : 0
                    });
                    s_setIconEnabledMethod?.Invoke(null, new object[]
                    {
                        classId,
                        scriptClass,
                        on ? 1 : 0
                    });
                }
            }
        }
    }
}
