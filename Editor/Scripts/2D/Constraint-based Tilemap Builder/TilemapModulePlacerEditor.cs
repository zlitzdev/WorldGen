using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Zlitz.Extra2D.WorldGen
{
    [CustomEditor(typeof(TilemapModulePlacer))]
    public class TilemapModulePlacerEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            SerializedProperty layersProperty          = serializedObject.FindProperty("m_layers");
            SerializedProperty objectContainerProperty = serializedObject.FindProperty("m_objectContainer");
            SerializedProperty placedObjectsProperty   = serializedObject.FindProperty("m_placedObjects");

            VisualElement root = new VisualElement();

            PropertyField objectContainerField = new PropertyField();
            objectContainerField.BindProperty(objectContainerProperty);
            root.Add(objectContainerField);

            ListView layersListView = new ListView();
            layersListView.showFoldoutHeader = true;
            layersListView.showBoundCollectionSize = true;
            layersListView.SetEnabled(false);
            layersListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            layersListView.reorderable = true;
            layersListView.reorderMode = ListViewReorderMode.Animated;
            layersListView.headerTitle = "Layers";
            layersListView.style.marginLeft = -14.0f;

            layersListView.makeItem = () => new LayerEntryItem();

            layersListView.bindItem = (e, i) =>
            {
                if (e is LayerEntryItem layerItem)
                {
                    SerializedProperty itemProperty = layersProperty.GetArrayElementAtIndex(i);

                    SerializedProperty layerIdProperty = itemProperty.FindPropertyRelative("m_layerId");
                    SerializedProperty tilemapProperty = itemProperty.FindPropertyRelative("m_tilemap");

                    layerItem.keyField.value = layerIdProperty.stringValue;
                    layerItem.tilemapField.value = tilemapProperty.objectReferenceValue;
                }
            };

            layersListView.BindProperty(layersProperty);

            root.Add(layersListView);


            ListView placedObjectsListView = new ListView();
            placedObjectsListView.showFoldoutHeader = true;
            placedObjectsListView.showBoundCollectionSize = true;
            placedObjectsListView.SetEnabled(false);
            placedObjectsListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            placedObjectsListView.reorderable = true;
            placedObjectsListView.reorderMode = ListViewReorderMode.Animated;
            placedObjectsListView.headerTitle = "Placed Objects";
            placedObjectsListView.style.marginLeft = -14.0f;

            placedObjectsListView.makeItem = () => new PropertyField();

            placedObjectsListView.bindItem = (e, i) =>
            {
                if (e is PropertyField propertyField)
                {
                    SerializedProperty itemProperty = placedObjectsProperty.GetArrayElementAtIndex(i);
                    propertyField.BindProperty(itemProperty);
                }
            };

            placedObjectsListView.BindProperty(placedObjectsProperty);

            root.Add(placedObjectsListView);

            return root;
        }

        private class LayerEntryItem : VisualElement
        {
            public TextField keyField { get; private set; }

            public ObjectField tilemapField { get; private set; }

            public LayerEntryItem()
            {
                style.flexDirection = FlexDirection.Row;

                keyField = new TextField();
                keyField.style.minWidth = 140.0f;
                Add(keyField);

                tilemapField = new ObjectField();
                tilemapField.style.flexGrow = 1.0f;
                tilemapField.objectType = typeof(Tilemap);
                Add(tilemapField);
            }
        }
    }
}
