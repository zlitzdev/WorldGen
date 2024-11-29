using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Zlitz.Extra2D.WorldGen
{
    [CustomEditor(typeof(PrebuiltTilemapModule))]
    public class PrebuiltTilemapModuleEditor : Editor
    {
        private bool m_previousToolsHidden;

        private void OnEnable()
        {
            m_previousToolsHidden = Tools.hidden;
            Tools.hidden = true;
        }

        private void OnDisable()
        {
            Tools.hidden = m_previousToolsHidden;
        }

        public override VisualElement CreateInspectorGUI()
        {
            SerializedProperty idProperty          = serializedObject.FindProperty("m_id");
            SerializedProperty symmetryProperty    = serializedObject.FindProperty("m_symmetry");
            SerializedProperty connectionsProperty = serializedObject.FindProperty("m_connections");
            SerializedProperty regionSizeProperty  = serializedObject.FindProperty("m_regionSize");

            VisualElement root = new VisualElement();

            PropertyField idField = new PropertyField();
            idField.label = "Module Id";
            idField.BindProperty(idProperty);
            root.Add(idField);

            PropertyField symmetryField = new PropertyField();
            symmetryField.BindProperty(symmetryProperty);
            root.Add(symmetryField);

            Foldout infoFoldout = new Foldout();
            infoFoldout.text = "Info";
            root.Add(infoFoldout);

            PropertyField sizeInfo = new PropertyField();
            sizeInfo.style.flexGrow = 1.0f;
            sizeInfo.label = "Region Size";
            sizeInfo.SetEnabled(false);
            sizeInfo.BindProperty(regionSizeProperty);
            infoFoldout.Add(sizeInfo);

            IntegerField connectionInfo = new IntegerField();
            connectionInfo.AddToClassList("unity-base-field__aligned");
            connectionInfo.label = "Connections";
            connectionInfo.value = connectionsProperty.arraySize;
            connectionInfo.SetEnabled(false);
            connectionInfo.TrackPropertyValue(connectionsProperty, p =>
            {
                connectionInfo.value = connectionsProperty.arraySize;
            });
            infoFoldout.Add(connectionInfo);

            return root;
        }
    }
}
