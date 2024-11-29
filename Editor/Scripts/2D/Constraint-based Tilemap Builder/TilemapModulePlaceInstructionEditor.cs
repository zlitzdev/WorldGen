using System;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Zlitz.Extra2D.WorldGen
{
    [CustomEditor(typeof(TilemapModulePlaceInstruction), true)]
    public class TilemapModulePlaceInstructionEditor : Editor
    {
        private static readonly Type s_baseType = typeof(TilemapModulePlaceInstruction);

        private List<Type> m_targetTypes;

        private void OnEnable()
        {
            m_targetTypes = new List<Type>();

            Type type = target.GetType();
            while (type != s_baseType)
            {
                m_targetTypes.Insert(0, type);
                type = type.BaseType;
            }
            m_targetTypes.Insert(0, s_baseType);
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            SerializedProperty buildingProperty = serializedObject.FindProperty("m_building");
            root.SetEnabled(!buildingProperty.boolValue);
            root.TrackPropertyValue(buildingProperty, p =>
            {
                root.SetEnabled(!buildingProperty.boolValue);
            });

            HashSet<string> drawnProperties = new HashSet<string>();

            foreach (Type type in m_targetTypes)
            {
                Foldout foldout = null;

                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (FieldInfo field in fields)
                {
                    if (!ShouldDrawField(field))
                    {
                        continue;
                    }

                    if (!drawnProperties.Add(field.Name))
                    {
                        continue;
                    }

                    if (foldout == null)
                    {
                        foldout = new Foldout();
                        foldout.text = ObjectNames.NicifyVariableName(type.Name);
                        root.Add(foldout);
                    }

                    SerializedProperty property = serializedObject.FindProperty(field.Name);

                    PropertyField propertyField = new PropertyField(property);
                    foldout.Add(propertyField);
                }
            }

            return root;
        }

        private static bool ShouldDrawField(FieldInfo field)
        {
            if (field.IsPrivate && field.GetCustomAttribute<SerializeField>() == null)
            {
                return false;
            }

            if (field.IsNotSerialized || field.GetCustomAttribute<HideInInspector>() != null)
            {
                return false;
            }

            return true;
        }
    }
}
