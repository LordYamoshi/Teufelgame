#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BuildingIncomeGenerator))]
public class BuildingIncomeGeneratorEditor : Editor
{
    private SerializedProperty canGenerateIncomeProperty;
    private SerializedProperty incomeAmountProperty;
    private SerializedProperty incomeIntervalProperty;
    private SerializedProperty incomeEffectPrefabProperty;
    private SerializedProperty onIncomeGeneratedProperty;

    void OnEnable()
    {
        canGenerateIncomeProperty = serializedObject.FindProperty("canGenerateIncome");
        incomeAmountProperty = serializedObject.FindProperty("incomeAmount");
        incomeIntervalProperty = serializedObject.FindProperty("incomeInterval");
        incomeEffectPrefabProperty = serializedObject.FindProperty("incomeEffectPrefab");
        onIncomeGeneratedProperty = serializedObject.FindProperty("OnIncomeGenerated");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(canGenerateIncomeProperty, new GUIContent("Can Generate Income"));

        // Only show other properties if canGenerateIncome is true
        if (canGenerateIncomeProperty.boolValue)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(incomeAmountProperty, new GUIContent("Income Amount"));
            
            EditorGUILayout.PropertyField(incomeIntervalProperty, new GUIContent("Income Interval (seconds)"));
            
            // Show time conversion for better usability
            float interval = incomeIntervalProperty.floatValue;
            EditorGUILayout.LabelField($"Generation Rate: {incomeAmountProperty.intValue} every {FormatTimeInterval(interval)}");
            
            float perMinute = (60f / interval) * incomeAmountProperty.intValue;
            EditorGUILayout.LabelField($"Income Per Minute: {perMinute:F1}");
            
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(incomeEffectPrefabProperty, new GUIContent("Income Effect Prefab"));
            
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.PropertyField(onIncomeGeneratedProperty);

        // Test button during play mode
        if (Application.isPlaying)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Testing", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Generate Income Now"))
            {
                BuildingIncomeGenerator generator = (BuildingIncomeGenerator)target;
                generator.TriggerIncomeGeneration();
            }
            
            if (GUILayout.Button("Toggle Income Generation"))
            {
                BuildingIncomeGenerator generator = (BuildingIncomeGenerator)target;
                generator.ToggleIncomeGeneration();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
    
    private string FormatTimeInterval(float seconds)
    {
        if (seconds < 60)
        {
            return $"{seconds:F1} seconds";
        }
        else if (seconds < 3600)
        {
            float minutes = seconds / 60f;
            return $"{minutes:F1} minutes";
        }
        else
        {
            float hours = seconds / 3600f;
            return $"{hours:F1} hours";
        }
    }
}
#endif