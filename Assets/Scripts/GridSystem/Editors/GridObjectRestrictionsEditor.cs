#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor window for applying restrictions to grid objects
/// </summary>
public class GridObjectRestrictionsEditor : EditorWindow
{
    private GridObjectRestrictions.RestrictionType selectedRestriction = GridObjectRestrictions.RestrictionType.None;
    private Vector2 scrollPosition;
    private List<GridObject> selectedGridObjects = new List<GridObject>();
    private bool showSceneObjects = true;
    private bool showSelectedObjects = true;
    private string searchFilter = "";
    
    [MenuItem("Grid System/Object Restrictions Editor")]
    public static void ShowWindow()
    {
        GridObjectRestrictionsEditor window = GetWindow<GridObjectRestrictionsEditor>("Grid Object Restrictions");
        window.minSize = new Vector2(400, 300);
    }
    
    private void OnEnable()
    {
        // Update the list when window is opened or when selection changes
        UpdateSelectedObjectsList();
        Selection.selectionChanged += UpdateSelectedObjectsList;
    }
    
    private void OnDisable()
    {
        Selection.selectionChanged -= UpdateSelectedObjectsList;
    }
    
    private void UpdateSelectedObjectsList()
    {
        selectedGridObjects.Clear();
        
        foreach (GameObject obj in Selection.gameObjects)
        {
            GridObject gridObj = obj.GetComponent<GridObject>();
            if (gridObj != null)
            {
                selectedGridObjects.Add(gridObj);
            }
        }
        
        Repaint();
    }
    
    private void OnGUI()
    {
        DrawHeader();
        
        EditorGUILayout.Space(10);
        
        DrawRestrictionTypeSelector();
        
        EditorGUILayout.Space(10);
        
        DrawSelectedObjectsList();
        
        EditorGUILayout.Space(10);
        
        DrawSceneObjectsList();
        
        EditorGUILayout.Space(10);
        
        DrawApplyButtons();
    }
    
    private void DrawHeader()
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 16;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        
        EditorGUILayout.LabelField("Grid Object Restrictions Editor", headerStyle);
        
        EditorGUILayout.HelpBox("Use this tool to apply restriction types to grid objects.\n" +
                              "Restrictions control whether objects can be moved or destroyed during gameplay.", 
                              MessageType.Info);
    }
    
    private void DrawRestrictionTypeSelector()
    {
        EditorGUILayout.LabelField("Restriction Type", EditorStyles.boldLabel);
        
        // Use a fancy grid of buttons for restriction types
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fixedHeight = 50;
        buttonStyle.fontStyle = FontStyle.Bold;
        
        EditorGUILayout.BeginHorizontal();
        
        foreach (GridObjectRestrictions.RestrictionType restrictionType in System.Enum.GetValues(typeof(GridObjectRestrictions.RestrictionType)))
        {
            Color originalColor = GUI.backgroundColor;
            
            // Set button color based on restriction type
            switch (restrictionType)
            {
                case GridObjectRestrictions.RestrictionType.None:
                    GUI.backgroundColor = Color.green;
                    break;
                case GridObjectRestrictions.RestrictionType.Immovable:
                    GUI.backgroundColor = Color.yellow;
                    break;
                case GridObjectRestrictions.RestrictionType.DestroyOnly:
                    GUI.backgroundColor = Color.red;
                    break;
                case GridObjectRestrictions.RestrictionType.Permanent:
                    GUI.backgroundColor = Color.gray;
                    break;
            }
            
            // Highlight the selected type
            if (restrictionType == selectedRestriction)
            {
                GUI.backgroundColor = Color.Lerp(GUI.backgroundColor, Color.white, 0.5f);
            }
            
            if (GUILayout.Button(GridObjectRestrictions.GetRestrictionTypeName(restrictionType), buttonStyle))
            {
                selectedRestriction = restrictionType;
            }
            
            GUI.backgroundColor = originalColor;
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Description of the selected restriction
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Description:", EditorStyles.boldLabel);
        
        string description = "";
        switch (selectedRestriction)
        {
            case GridObjectRestrictions.RestrictionType.None:
                description = "Default object behavior - can be both moved and destroyed during gameplay.";
                break;
            case GridObjectRestrictions.RestrictionType.Immovable:
                description = "Object cannot be moved after placement, but can be destroyed.";
                break;
            case GridObjectRestrictions.RestrictionType.DestroyOnly:
                description = "Object cannot be moved after placement, only destroyed. Will be marked with a red indicator.";
                break;
            case GridObjectRestrictions.RestrictionType.Permanent:
                description = "Object cannot be moved or destroyed. Will be marked with a gray indicator.";
                break;
        }
        
        EditorGUILayout.HelpBox(description, MessageType.None);
    }
    
    private void DrawSelectedObjectsList()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        showSelectedObjects = EditorGUILayout.Foldout(showSelectedObjects, $"Selected Objects ({selectedGridObjects.Count})", true);
        
        if (showSelectedObjects)
        {
            if (selectedGridObjects.Count == 0)
            {
                EditorGUILayout.HelpBox("No grid objects currently selected. Select objects in the scene to apply restrictions.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply to Selected"))
                {
                    ApplyRestrictionToSelectedObjects();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                foreach (GridObject gridObj in selectedGridObjects)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    EditorGUILayout.ObjectField(gridObj.gameObject, typeof(GameObject), false);
                    
                    string status = gridObj.isMovable ? "Movable" : "Immovable";
                    status += gridObj.isDestructible ? ", Destructible" : ", Indestructible";
                    
                    EditorGUILayout.LabelField(status, GUILayout.Width(150));
                    
                    if (GUILayout.Button("Apply", GUILayout.Width(60)))
                    {
                        GridObjectRestrictions.ApplyRestriction(gridObj, selectedRestriction);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawSceneObjectsList()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        showSceneObjects = EditorGUILayout.Foldout(showSceneObjects, "All Grid Objects in Scene", true);
        
        if (showSceneObjects)
        {
            GridObject[] allGridObjects = FindObjectsOfType<GridObject>();
            
            EditorGUILayout.BeginHorizontal();
            
            searchFilter = EditorGUILayout.TextField("Search:", searchFilter);
            
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                searchFilter = "";
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            if (allGridObjects.Length == 0)
            {
                EditorGUILayout.HelpBox("No grid objects found in the scene.", MessageType.Info);
            }
            else
            {
                if (GUILayout.Button("Apply to All"))
                {
                    if (EditorUtility.DisplayDialog("Confirm",
                        $"Apply '{GridObjectRestrictions.GetRestrictionTypeName(selectedRestriction)}' to all grid objects in scene?",
                        "Apply", "Cancel"))
                    {
                        GridObjectRestrictions.ApplyRestrictionToAllInScene(selectedRestriction);
                    }
                }
                
                EditorGUILayout.Space(5);
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                
                foreach (GridObject gridObj in allGridObjects)
                {
                    // Apply search filter
                    if (!string.IsNullOrEmpty(searchFilter) && 
                        !gridObj.name.ToLower().Contains(searchFilter.ToLower()))
                    {
                        continue;
                    }
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = gridObj.gameObject;
                    }
                    
                    EditorGUILayout.ObjectField(gridObj.gameObject, typeof(GameObject), false);
                    
                    string status = gridObj.isMovable ? "Movable" : "Immovable";
                    status += gridObj.isDestructible ? ", Destructible" : ", Indestructible";
                    
                    EditorGUILayout.LabelField(status, GUILayout.Width(150));
                    
                    if (GUILayout.Button("Apply", GUILayout.Width(60)))
                    {
                        GridObjectRestrictions.ApplyRestriction(gridObj, selectedRestriction);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawApplyButtons()
    {
        GUILayout.FlexibleSpace();
        
        EditorGUILayout.BeginHorizontal();
        
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.fixedHeight = 30;
        
        if (GUILayout.Button("Apply to Selected", buttonStyle))
        {
            ApplyRestrictionToSelectedObjects();
        }
        
        if (GUILayout.Button("Apply to All in Scene", buttonStyle))
        {
            if (EditorUtility.DisplayDialog("Confirm",
                $"Apply '{GridObjectRestrictions.GetRestrictionTypeName(selectedRestriction)}' to all grid objects in scene?",
                "Apply", "Cancel"))
            {
                GridObjectRestrictions.ApplyRestrictionToAllInScene(selectedRestriction);
            }
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void ApplyRestrictionToSelectedObjects()
    {
        if (selectedGridObjects.Count == 0) return;
        
        Undo.RecordObjects(selectedGridObjects.ToArray(), "Apply Grid Object Restrictions");
        
        foreach (GridObject gridObj in selectedGridObjects)
        {
            GridObjectRestrictions.ApplyRestriction(gridObj, selectedRestriction);
        }
        
        EditorUtility.SetDirty(selectedGridObjects[0].gameObject);
    }
}
#endif