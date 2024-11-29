using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Zlitz.Extra2D.WorldGen
{
    [CustomPropertyDrawer(typeof(TilemapModulePool))]
    public class TilemapModulePoolDrawer : PropertyDrawer
    {
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

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedTilemapModulePool serializedTilemapModulePool = new SerializedTilemapModulePool(property);

            ListView listView = new ListView();
            listView.showAddRemoveFooter = true;
            listView.showBoundCollectionSize = true;
            listView.showFoldoutHeader = true;
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.reorderable = true;
            listView.reorderMode = ListViewReorderMode.Animated;
            listView.headerTitle = property.displayName;
            listView.style.marginLeft = -14.0f;

            listView.makeItem = () =>
            {
                PoolItem poolItem = new PoolItem(warningIcon);
                return poolItem;
            };

            listView.bindItem = (e, i) =>
            {
                if (i >= serializedTilemapModulePool.entriesProperty.arraySize)
                {
                    return;
                }

                SerializedProperty itemProperty = serializedTilemapModulePool.entriesProperty.GetArrayElementAtIndex(i);
                if (e is PoolItem poolItem)
                {
                    float totalWeight = 0.0f;
                    for (int j = 0; j < serializedTilemapModulePool.entriesProperty.arraySize; j++)
                    {
                        SerializedProperty itemProperty2 = serializedTilemapModulePool.entriesProperty.GetArrayElementAtIndex(j);

                        SerializedProperty weightProperty = itemProperty2.FindPropertyRelative("m_weight");
                        totalWeight += weightProperty.floatValue;
                    }

                    poolItem.Bind(itemProperty, serializedTilemapModulePool.placeInstruction, totalWeight);
                }
            };

            listView.itemsAdded += (indices) =>
            {
                listView.RefreshItems();
            };

            listView.itemsRemoved += (indices) =>
            {
                listView.RefreshItems();
            };

            listView.TrackPropertyValue(serializedTilemapModulePool.entriesProperty, p =>
            {
                listView.RefreshItems();
            });

            listView.BindProperty(serializedTilemapModulePool.entriesProperty);
            return listView;
        }

        private class PoolItem : VisualElement
        {
            private Texture2D m_warningIcon;

            private TextField     m_moduleIdField;
            private FloatField    m_weightField;
            private FloatField    m_totalWeightField;
            private Label         m_percentageLabel;
            private VisualElement m_conflictIcon;

            private TilemapModulePlaceInstruction m_placeInstruction;

            private SerializedProperty m_itemProperty;
            private SerializedProperty m_moduleIdProperty;
            private SerializedProperty m_weightProperty;

            public void UpdateTotalWeight(float totalWeight)
            {
                m_totalWeightField.value = totalWeight;

                float percentage = (float)(m_weightField.value) / (float)m_totalWeightField.value;
                m_percentageLabel.text = $"{percentage * 100.0f:0.##} %";
            }

            public void Bind(SerializedProperty itemProperty, TilemapModulePlaceInstruction placeInstruction, float totalWeight)
            {
                m_itemProperty     = itemProperty;
                m_moduleIdProperty = m_itemProperty?.FindPropertyRelative("m_moduleId");
                m_weightProperty   = m_itemProperty?.FindPropertyRelative("m_weight");

                m_placeInstruction = placeInstruction;

                m_conflictIcon.style.display = m_placeInstruction != null ? DisplayStyle.Flex : DisplayStyle.None;

                if (m_moduleIdProperty != null)
                {
                    m_moduleIdField.value = m_moduleIdProperty.stringValue;
                    if (m_placeInstruction != null && (m_placeInstruction.moduleSet == null || !m_placeInstruction.moduleSet.modules.ContainsKey(m_moduleIdProperty.stringValue)))
                    {
                        m_conflictIcon.style.backgroundImage = m_warningIcon;
                        m_conflictIcon.tooltip = "TilemapModulePlaceInstruction either has no TilemapModuleSet or its module set doesn't contain this module ID.";
                    }
                    else
                    {
                        m_conflictIcon.style.backgroundImage = null;
                        m_conflictIcon.tooltip = "";
                    }
                }
                if (m_weightProperty != null)
                {
                    m_weightField.value = m_weightProperty.floatValue;
                }

                UpdateTotalWeight(totalWeight);
            }

            public PoolItem(Texture2D warningIcon)
            {
                m_warningIcon = warningIcon;

                style.flexDirection = FlexDirection.Row;

                m_moduleIdField = new TextField();
                m_moduleIdField.style.minWidth = 140.0f;

                m_moduleIdField.RegisterValueChangedCallback(e =>
                {
                    if (m_moduleIdProperty == null)
                    {
                        return;
                    }

                    if (e.newValue != m_moduleIdProperty.stringValue)
                    {
                        m_moduleIdProperty.stringValue = e.newValue;
                        m_moduleIdProperty.serializedObject.ApplyModifiedProperties();

                        if (m_placeInstruction != null && (m_placeInstruction.moduleSet == null || !m_placeInstruction.moduleSet.modules.ContainsKey(m_moduleIdProperty.stringValue)))
                        {
                            m_conflictIcon.style.backgroundImage = m_warningIcon;
                            m_conflictIcon.tooltip = "TilemapModulePlaceInstruction either has no TilemapModuleSet or its module set doesn't contain this module ID.";
                        }
                        else
                        {
                            m_conflictIcon.style.backgroundImage = null;
                            m_conflictIcon.tooltip = "";
                        }
                    }

                });

                Add(m_moduleIdField);

                VisualElement weightContainer = new VisualElement();
                weightContainer.style.flexGrow = 1.0f;
                weightContainer.style.flexDirection = FlexDirection.Row;

                Add(weightContainer);

                m_weightField = new FloatField();
                m_weightField.style.width = Length.Percent(47.0f);

                m_weightField.RegisterValueChangedCallback(e =>
                {
                    float newWeight = e.newValue;
                    newWeight = Mathf.Max(0.0f, newWeight);

                    if (newWeight != m_weightField.value)
                    {
                        m_weightField.SetValueWithoutNotify(newWeight);
                    }

                    if (newWeight != m_weightProperty.floatValue)
                    {
                        m_weightProperty.floatValue = newWeight;
                        m_weightProperty.serializedObject.ApplyModifiedProperties();
                    }
                });

                weightContainer.Add(m_weightField);

                Label slashLabel = new Label();
                slashLabel.text = "  /";
                slashLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                slashLabel.style.width = Length.Percent(6.0f);
                weightContainer.Add(slashLabel);

                m_totalWeightField = new FloatField();
                m_totalWeightField.style.width = Length.Percent(47.0f);
                m_totalWeightField.SetEnabled(false);
                weightContainer.Add(m_totalWeightField);

                m_percentageLabel = new Label();
                m_percentageLabel.style.width = 60.0f;
                m_percentageLabel.style.unityTextAlign = TextAnchor.MiddleRight;
                Add(m_percentageLabel);

                m_conflictIcon = new VisualElement();
                m_conflictIcon.style.width = 16.0f;
                m_conflictIcon.style.height = 16.0f;
                m_conflictIcon.style.marginBottom = 2.0f;
                m_conflictIcon.style.marginTop = 2.0f;
                m_conflictIcon.style.marginLeft = 4.0f;
                m_conflictIcon.style.marginRight = 0.0f;
                m_conflictIcon.style.flexShrink = 0.0f;
                Add(m_conflictIcon);
            }
        }

        private struct SerializedTilemapModulePool
        {
            public TilemapModulePlaceInstruction placeInstruction { get; private set; }

            public SerializedObject serializedObject { get; private set; }

            public SerializedProperty entriesProperty { get; private set; }

            public SerializedTilemapModulePool(SerializedProperty property)
            {
                placeInstruction = property.serializedObject.targetObject as TilemapModulePlaceInstruction;

                serializedObject = property.serializedObject;

                entriesProperty = property.FindPropertyRelative("m_entries");
            }
        }
    }
}
