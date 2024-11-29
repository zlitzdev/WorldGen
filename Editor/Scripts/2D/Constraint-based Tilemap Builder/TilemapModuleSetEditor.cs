using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Reflection;
using System.IO;

namespace Zlitz.Extra2D.WorldGen
{
    [CustomEditor(typeof(TilemapModuleSet))]
    public class TilemapModuleSetEditor : Editor
    {
        private static GUIStyle s_textStyle;

        private static Texture2D s_warningIcon;

        private static Texture2D warningIcon
        {
            get
            {
                if (s_warningIcon == null)
                {
                    s_warningIcon = EditorGUIUtility.IconContent("Warning@2x").image as Texture2D;
                }
                return s_warningIcon;
            }
        }

        private bool m_previousToolsHidden;

        private SerializedTilemapModuleSet m_serializedTilemapModuleSet;

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
            m_serializedTilemapModuleSet = new SerializedTilemapModuleSet(serializedObject);

            VisualElement root = new VisualElement();

            Foldout tilemapFoldout = new Foldout();
            tilemapFoldout.text = "Tilemaps";
            root.Add(tilemapFoldout);

            PropertyField maskField = new PropertyField();
            maskField.style.flexGrow = 1.0f;
            maskField.BindProperty(m_serializedTilemapModuleSet.maskProperty);
            tilemapFoldout.Add(maskField);

            ListView layersListView = new ListView();
            layersListView.style.flexGrow = 1.0f;
            layersListView.style.marginLeft = -14.0f;
            layersListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            layersListView.showAddRemoveFooter = true;
            layersListView.showFoldoutHeader = true;
            layersListView.reorderable = true;
            layersListView.reorderMode = ListViewReorderMode.Animated;
            layersListView.headerTitle = "Layers";
            layersListView.BindProperty(m_serializedTilemapModuleSet.layersProperty);

            layersListView.makeItem = () =>
            {
                LayerEntryItem item = new LayerEntryItem();

                layersListView.TrackPropertyValue(m_serializedTilemapModuleSet.layersProperty, p =>
                {
                    SerializedProperty currentEntryProperty = item.index >= m_serializedTilemapModuleSet.layersProperty.arraySize ? null : m_serializedTilemapModuleSet.layersProperty.GetArrayElementAtIndex(item.index);
                    if (currentEntryProperty == null)
                    {
                        return;
                    }

                    SerializedProperty layerIdProperty = currentEntryProperty.FindPropertyRelative("m_layerId");
                    string currentLayerId = layerIdProperty.stringValue;

                    item.conflictIcon.style.backgroundImage = null;
                    item.conflictIcon.tooltip = "";
                    for (int j = 0; j < m_serializedTilemapModuleSet.layersProperty.arraySize; j++)
                    {
                        if (j == item.index)
                        {
                            continue;
                        }

                        SerializedProperty otherEntryProperty = m_serializedTilemapModuleSet.layersProperty.GetArrayElementAtIndex(j);
                        SerializedProperty otherLayerIdProperty = otherEntryProperty.FindPropertyRelative("m_layerId");
                        string otherLayerId = otherLayerIdProperty.stringValue;

                        if (currentLayerId == otherLayerId)
                        {
                            item.conflictIcon.style.backgroundImage = warningIcon;
                            item.conflictIcon.tooltip = "Duplicated value will be ignored";
                            break;
                        }
                    }
                });

                return item;
            };

            layersListView.bindItem = (e, i) =>
            {
                if (e is LayerEntryItem layerItem)
                {
                    layerItem.index = i;

                    SerializedProperty layerEntryProperty = m_serializedTilemapModuleSet.layersProperty.GetArrayElementAtIndex(i);

                    SerializedProperty keyProperty = layerEntryProperty.FindPropertyRelative("m_layerId");
                    layerItem.keyField.BindProperty(keyProperty);

                    SerializedProperty tilemapProperty = layerEntryProperty.FindPropertyRelative("m_tilemap");
                    layerItem.tilemapField.BindProperty(tilemapProperty);

                    string currentLayerId = keyProperty.stringValue;

                    layerItem.conflictIcon.style.backgroundImage = null;
                    layerItem.conflictIcon.tooltip = "";
                    for (int j = 0; j < m_serializedTilemapModuleSet.layersProperty.arraySize; j++)
                    {
                        if (j == layerItem.index)
                        {
                            continue;
                        }

                        SerializedProperty otherEntryProperty = m_serializedTilemapModuleSet.layersProperty.GetArrayElementAtIndex(j);
                        SerializedProperty otherLayerIdProperty = otherEntryProperty.FindPropertyRelative("m_layerId");
                        string otherLayerId = otherLayerIdProperty.stringValue;

                        if (currentLayerId == otherLayerId)
                        {
                            layerItem.conflictIcon.style.backgroundImage = warningIcon;
                            layerItem.conflictIcon.tooltip = "Duplicated value will be ignored";
                            break;
                        }
                    }
                }
            };

            tilemapFoldout.Add(layersListView);

            return root;
        }

        internal static int DrawModuleSceneUI(SerializedObject serializedObject, int handleId = 0, bool interactable = true)
        {
            SerializedPrebuiltTilemapModule serializedPrebuiltTilemapModule = new SerializedPrebuiltTilemapModule(serializedObject);

            Color handlerColor = Handles.color;

            TilemapModuleSet moduleSet = serializedPrebuiltTilemapModule.prebuiltTilemapModule.moduleSet;

            Vector2Int regionSize = serializedPrebuiltTilemapModule.regionSizeProperty.vector2IntValue;

            Vector3 currentPosition = serializedPrebuiltTilemapModule.prebuiltTilemapModule.transform.localPosition;

            Vector3 position;
            position.x = Mathf.Round(currentPosition.x);
            position.y = Mathf.Round(currentPosition.y);
            position.z = 0.0f;

            Vector3 size = new Vector3(regionSize.x, regionSize.y, 0.0f);

            Matrix4x4 handleMatrix = Handles.matrix;
            if (moduleSet != null)
            {
                Handles.matrix = serializedPrebuiltTilemapModule.prebuiltTilemapModule.moduleSet.transform.localToWorldMatrix;
            }

            float x1 = position.x;
            float y1 = position.y;
            float x2 = x1 + regionSize.x;
            float y2 = y1 + regionSize.y;

            bool isSelected = Selection.gameObjects.Contains((serializedObject.targetObject as Component)?.gameObject ?? null);

            Handles.DrawSolidRectangleWithOutline(new Vector3[]
            {
                position,
                position + new Vector3(size.x, 0.0f, 0.0f),
                position + size,
                position + new Vector3(0.0f, size.y, 0.0f),
            }, new Color(0.0f, 0.0f, 0.0f, 0.0f), isSelected ? Color.yellow : Color.magenta);

            for (int i = 0; i < serializedPrebuiltTilemapModule.connectionsProperty.arraySize; i++)
            {
                SerializedProperty connectionDataProperty = serializedPrebuiltTilemapModule.connectionsProperty.GetArrayElementAtIndex(i);

                SerializedProperty connectionPositionProperty = connectionDataProperty.FindPropertyRelative("m_position");
                SerializedProperty connectionDirectionProperty = connectionDataProperty.FindPropertyRelative("m_direction");

                Vector2Int connectPosition = connectionPositionProperty.vector2IntValue;

                Vector3 arrowOrigin = new Vector3(Mathf.Min(x1, x2), Mathf.Min(y1, y2));
                arrowOrigin.x += 0.5f + connectPosition.x;
                arrowOrigin.y += 0.5f + connectPosition.y;

                Vector2 direction = ((ConnectDirection)connectionDirectionProperty.enumValueIndex).ToVector();
                DrawArrow(arrowOrigin, direction);
            }

            if (interactable)
            {
                Vector3 bottomLeft = new Vector3(x1, y1, 0.0f);
                bottomLeft = DrawCornerHandle(ref handleId, bottomLeft);
                x1 = bottomLeft.x;
                y1 = bottomLeft.y;

                Vector3 bottomRight = new Vector3(x2, y1, 0.0f);
                bottomRight = DrawCornerHandle(ref handleId, bottomRight);
                x2 = bottomRight.x;
                y1 = bottomRight.y;

                Vector3 topLeft = new Vector3(x1, y2, 0.0f);
                topLeft = DrawCornerHandle(ref handleId, topLeft);
                x1 = topLeft.x;
                y2 = topLeft.y;

                Vector3 topRight = new Vector3(x2, y2, 0.0f);
                topRight = DrawCornerHandle(ref handleId, topRight);
                x2 = topRight.x;
                y2 = topRight.y;
            }

            int ix1 = Mathf.FloorToInt(x1);
            int iy1 = Mathf.FloorToInt(y1);
            int ix2 = Mathf.CeilToInt(x2);
            int iy2 = Mathf.CeilToInt(y2);

            if (s_textStyle == null)
            {
                s_textStyle = new GUIStyle()
                {
                    fontSize = 14,
                    alignment = TextAnchor.UpperLeft
                };
            }
            s_textStyle.normal.textColor = isSelected ? Color.yellow : Color.magenta;

            Handles.Label(new Vector3(Mathf.Min(ix1, ix2), Mathf.Min(iy1, iy2)), serializedPrebuiltTilemapModule.idProperty.stringValue, s_textStyle);

            if (Event.current.type == EventType.MouseDown)
            {
                Vector2 mousePos = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin;
                if (serializedPrebuiltTilemapModule.prebuiltTilemapModule.moduleSet != null)
                {
                    mousePos = serializedPrebuiltTilemapModule.prebuiltTilemapModule.moduleSet.transform.InverseTransformPoint(mousePos);
                }
                mousePos -= new Vector2(Mathf.Min(ix1, ix2), Mathf.Min(iy1, iy2));

                if (mousePos.x > 0.0f && mousePos.x < Mathf.Abs(ix2 - ix1) && mousePos.y >= 0.0f && mousePos.y < Mathf.Abs(iy2 - iy1))
                {
                    Vector2Int mouseTilePosition = new Vector2Int(Mathf.FloorToInt(mousePos.x), Mathf.FloorToInt(mousePos.y));

                    if (interactable && Event.current.button == 1)
                    {
                        Event.current.Use();

                        bool found = false;
                        for (int i = 0; i < serializedPrebuiltTilemapModule.connectionsProperty.arraySize; i++)
                        {
                            SerializedProperty connectionDataProperty = serializedPrebuiltTilemapModule.connectionsProperty.GetArrayElementAtIndex(i);

                            SerializedProperty connectionPositionProperty = connectionDataProperty.FindPropertyRelative("m_position");
                            SerializedProperty connectionDirectionProperty = connectionDataProperty.FindPropertyRelative("m_direction");

                            if (connectionPositionProperty.vector2IntValue == mouseTilePosition)
                            {
                                ConnectDirection currentDirection = (ConnectDirection)connectionDirectionProperty.enumValueIndex;
                                switch (currentDirection)
                                {
                                    case ConnectDirection.Left:
                                        {
                                            connectionDirectionProperty.enumValueIndex = (int)ConnectDirection.Right;
                                            break;
                                        }
                                    case ConnectDirection.Right:
                                        {
                                            connectionDirectionProperty.enumValueIndex = (int)ConnectDirection.Up;
                                            break;
                                        }
                                    case ConnectDirection.Up:
                                        {
                                            connectionDirectionProperty.enumValueIndex = (int)ConnectDirection.Down;
                                            break;
                                        }
                                    case ConnectDirection.Down:
                                        {
                                            serializedPrebuiltTilemapModule.connectionsProperty.DeleteArrayElementAtIndex(i);
                                            break;
                                        }
                                }

                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            int index = serializedPrebuiltTilemapModule.connectionsProperty.arraySize;
                            serializedPrebuiltTilemapModule.connectionsProperty.InsertArrayElementAtIndex(index);

                            SerializedProperty newConnectionProperty = serializedPrebuiltTilemapModule.connectionsProperty.GetArrayElementAtIndex(index);

                            SerializedProperty newConnectionPositionProperty = newConnectionProperty.FindPropertyRelative("m_position");
                            SerializedProperty newConnectionDirectionProperty = newConnectionProperty.FindPropertyRelative("m_direction");

                            newConnectionPositionProperty.vector2IntValue = mouseTilePosition;
                            newConnectionDirectionProperty.enumValueIndex = (int)ConnectDirection.Left;
                        }

                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            Vector2Int newRegionSize = new Vector2Int(ix2 - ix1, iy2 - iy1);

            for (int i = serializedPrebuiltTilemapModule.connectionsProperty.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty connectionDataProperty = serializedPrebuiltTilemapModule.connectionsProperty.GetArrayElementAtIndex(i);
                SerializedProperty connectionPositionProperty = connectionDataProperty.FindPropertyRelative("m_position");

                if (newRegionSize.x != 0 && newRegionSize.y != 0)
                {
                    Vector2Int connectionPosition = connectionPositionProperty.vector2IntValue;
                    connectionPosition.x = Mathf.Clamp(connectionPosition.x, 0, Mathf.Abs(newRegionSize.x) - 1);
                    connectionPosition.y = Mathf.Clamp(connectionPosition.y, 0, Mathf.Abs(newRegionSize.y) - 1);

                    connectionPositionProperty.vector2IntValue = connectionPosition;
                }
                else
                {
                    serializedPrebuiltTilemapModule.connectionsProperty.DeleteArrayElementAtIndex(i);
                }
            }

            for (int i = serializedPrebuiltTilemapModule.connectionsProperty.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty connectionDataProperty = serializedPrebuiltTilemapModule.connectionsProperty.GetArrayElementAtIndex(i);
                SerializedProperty connectionPositionProperty = connectionDataProperty.FindPropertyRelative("m_position");

                for (int j = 0; j < i; j++)
                {
                    SerializedProperty connectionDataProperty2 = serializedPrebuiltTilemapModule.connectionsProperty.GetArrayElementAtIndex(j);
                    SerializedProperty connectionPositionProperty2 = connectionDataProperty2.FindPropertyRelative("m_position");

                    if (connectionPositionProperty.vector2IntValue == connectionPositionProperty2.vector2IntValue)
                    {
                        serializedPrebuiltTilemapModule.connectionsProperty.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
            }

            serializedPrebuiltTilemapModule.prebuiltTilemapModule.transform.localPosition = new Vector3(ix1, iy1, 0.0f);
            serializedPrebuiltTilemapModule.regionSizeProperty.vector2IntValue = newRegionSize;
            serializedObject.ApplyModifiedProperties();

            Handles.matrix = handleMatrix;
            Handles.color = handlerColor;

            return handleId;
        }

        private static Vector3 DrawCornerHandle(ref int handleId, Vector3 position)
        {
            int controlId = GUIUtility.GetControlID(handleId++, FocusType.Passive);

            Vector3 newPosition = Handles.FreeMoveHandle(
                controlId,
                position,
                0.1f,
                Vector3.zero,
                Handles.DotHandleCap
            );

            if (GUIUtility.hotControl == controlId)
            {
                return newPosition;
            }

            return position;
        }

        private static void DrawArrow(Vector3 origin, Vector3 direction)
        {
            Color handleColor = Handles.color;

            Vector3 arrowHead = origin + 0.4f * direction;

            Vector3 leftWing = Quaternion.Euler(0.0f, 0.0f, 30.0f) * direction * 0.6f;
            Vector3 rightWing = Quaternion.Euler(0.0f, 0.0f, -30.0f) * direction * 0.6f;

            arrowHead -= 0.06f * direction;
            Vector3 p1 = arrowHead - leftWing;
            Vector3 p2 = arrowHead - rightWing;

            Handles.color = Color.black;
            Handles.DrawAAConvexPolygon(arrowHead, p1, p2);

            arrowHead = origin + 0.8f * (arrowHead - origin);
            p1 = origin + 0.8f * (p1 - origin);
            p2 = origin + 0.8f * (p2 - origin);

            Handles.color = Color.red;
            Handles.DrawAAConvexPolygon(arrowHead, p1, p2);

            Handles.color = handleColor;
        }

        private struct SerializedTilemapModuleSet
        {
            public TilemapModuleSet tilemapModuleSet { get; private set; }

            public SerializedProperty maskProperty { get; private set; }

            public SerializedProperty layersProperty { get; private set; }

            public SerializedTilemapModuleSet(SerializedObject serializedTilemapModuleSet)
            {
                tilemapModuleSet = serializedTilemapModuleSet.targetObject as TilemapModuleSet;

                maskProperty   = serializedTilemapModuleSet.FindProperty("m_mask");
                layersProperty = serializedTilemapModuleSet.FindProperty("m_layers");
            }
        }

        private struct SerializedPrebuiltTilemapModule
        {
            public PrebuiltTilemapModule prebuiltTilemapModule { get; private set; }

            public SerializedProperty idProperty { get; private set; }

            public SerializedProperty regionSizeProperty { get; private set; }

            public SerializedProperty connectionsProperty { get; private set; }

            public SerializedPrebuiltTilemapModule(SerializedObject serializedPrebuiltTilemapModule)
            {
                prebuiltTilemapModule = serializedPrebuiltTilemapModule.targetObject as PrebuiltTilemapModule;

                idProperty = serializedPrebuiltTilemapModule.FindProperty("m_id");
                regionSizeProperty = serializedPrebuiltTilemapModule.FindProperty("m_regionSize");
                connectionsProperty = serializedPrebuiltTilemapModule.FindProperty("m_connections");
            }
        }

        private class LayerEntryItem : VisualElement 
        {
            public int index;

            public TextField keyField { get; private set; }

            public PropertyField tilemapField { get; private set; }

            public VisualElement conflictIcon { get; private set; }

            public LayerEntryItem()
            {
                style.flexDirection = FlexDirection.Row;

                keyField = new TextField();
                keyField.style.minWidth = 140.0f;
                Add(keyField);

                tilemapField = new PropertyField();
                tilemapField.label = "";
                tilemapField.style.flexGrow = 1.0f;
                Add(tilemapField);

                conflictIcon = new VisualElement();
                conflictIcon.style.width = 16.0f;
                conflictIcon.style.height = 16.0f;
                conflictIcon.style.marginBottom = 2.0f;
                conflictIcon.style.marginTop = 2.0f;
                conflictIcon.style.marginLeft = 4.0f;
                conflictIcon.style.marginRight = 0.0f;
                conflictIcon.style.flexShrink = 0.0f;
                Add(conflictIcon);
            }
        }
    }

    [InitializeOnLoad]
    internal static class TilemapModuleSetSceneView
    {
        static TilemapModuleSetSceneView()
        {
            SceneView.duringSceneGui -= OnSceneView;
            SceneView.duringSceneGui += OnSceneView;
        }

        private static void OnSceneView(SceneView sceneView)
        {
            HashSet<TilemapModuleSet> tilemapModuleSets = new HashSet<TilemapModuleSet>();
            int handleId = 0;

            foreach (GameObject gameObject in Selection.gameObjects)
            {
                Transform transform = gameObject.transform;
                while (transform != null)
                {
                    if (transform.TryGetComponent(out TilemapModuleSet tilemapModuleSet))
                    {
                        if (tilemapModuleSets.Add(tilemapModuleSet))
                        {
                            PrebuiltTilemapModule[] prebuiltTilemapModules = tilemapModuleSet.GetComponentsInChildren<PrebuiltTilemapModule>();
                            foreach (PrebuiltTilemapModule prebuiltTilemapModule in prebuiltTilemapModules)
                            {
                                SerializedObject serializedPrebuiltTilemapModule = new SerializedObject(prebuiltTilemapModule);
                                handleId = TilemapModuleSetEditor.DrawModuleSceneUI(serializedPrebuiltTilemapModule, handleId, true);
                            }
                        }
                        break;
                    }
                    transform = transform.parent;
                }
            }
        }
    }
}
