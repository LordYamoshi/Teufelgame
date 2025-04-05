#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(PreplacedGridObjectsManager))]
public class PreplacedGridObjectsEditor : Editor
{
    private PreplacedGridObjectsManager manager;
    private GridManager gridManager;
    
    private GameObject selectedPrefab;
    private Vector2Int gridPosition = Vector2Int.zero;
    private int rotationIndex = 0;
    private bool isMovable = false;
    private bool isDestructible = true;
    private bool useCustomShape = false;
    private string customShapeName = "";
    
    private List<string> availableShapes = new List<string>();
    private int customShapeIndex = -1;
    
    private bool showPlacementTools = true;
    private bool showPreplacedObjects = true;
    private bool previewMode = false;
    private Vector2 scrollPosition;
    
    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;
    
    private void OnEnable()
    {
        manager = (PreplacedGridObjectsManager)target;
        
        if (manager.gridManager == null)
        {
            manager.gridManager = FindObjectOfType<GridManager>();
        }
        
        gridManager = manager.gridManager;
        
        // Load available shapes if possible
        LoadAvailableShapes();
        
        // Create custom styles
        headerStyle = new GUIStyle();
        buttonStyle = new GUIStyle();
    }
    
    public override void OnInspectorGUI()
    {
        if (headerStyle.normal.textColor == Color.clear)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontStyle = FontStyle.Bold;
        }
        
        serializedObject.Update();
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("gridManager"));
        
        if (manager.gridManager == null)
        {
            EditorGUILayout.HelpBox("Please assign a GridManager reference.", MessageType.Warning);
            serializedObject.ApplyModifiedProperties();
            return;
        }
        
        gridManager = manager.gridManager;
        
        EditorGUILayout.Space(10);
        
        // Display placement tools
        DrawPlacementTools();
        
        EditorGUILayout.Space(10);
        
        // Display list of preplaced objects
        DrawPreplacedObjectsList();
        
        serializedObject.ApplyModifiedProperties();
        
        // Update preview in scene view
        if (previewMode && selectedPrefab != null)
        {
            SceneView.RepaintAll();
        }
    }
    
    private void DrawPlacementTools()
    {
        EditorGUILayout.LabelField("Placement Tools", headerStyle);
        
        showPlacementTools = EditorGUILayout.Foldout(showPlacementTools, "Placement Tools", true);
        
        if (!showPlacementTools) return;
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // Prefab selection
        EditorGUI.BeginChangeCheck();
        selectedPrefab = EditorGUILayout.ObjectField("Prefab", selectedPrefab, typeof(GameObject), false) as GameObject;
        if (EditorGUI.EndChangeCheck())
        {
            // Validate that the prefab has a GridObject component
            if (selectedPrefab != null && selectedPrefab.GetComponent<GridObject>() == null)
            {
                EditorUtility.DisplayDialog("Invalid Prefab", 
                    "The selected prefab does not have a GridObject component.", "OK");
                selectedPrefab = null;
            }
        }
        
        // Grid position
        gridPosition = EditorGUILayout.Vector2IntField("Grid Position", gridPosition);
        
        // Rotation
        rotationIndex = EditorGUILayout.IntSlider("Rotation (90° steps)", rotationIndex, 0, 3);
        
        // Object properties
        EditorGUILayout.LabelField("Object Properties:", EditorStyles.boldLabel);
        
        isMovable = EditorGUILayout.Toggle("Is Movable", isMovable);
        isDestructible = EditorGUILayout.Toggle("Is Destructible", isDestructible);
        
        // Custom Shape Options
        useCustomShape = EditorGUILayout.Toggle("Use Custom Shape", useCustomShape);
        
        if (useCustomShape)
        {
            if (availableShapes.Count > 0)
            {
                customShapeIndex = EditorGUILayout.Popup("Shape", customShapeIndex, availableShapes.ToArray());
                if (customShapeIndex >= 0 && customShapeIndex < availableShapes.Count)
                {
                    customShapeName = availableShapes[customShapeIndex];
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No shapes available. Create shapes using the GridObject editor.", MessageType.Info);
                customShapeName = EditorGUILayout.TextField("Shape Name", customShapeName);
            }
        }
        
        EditorGUILayout.Space(5);
        
        // Preview mode
        EditorGUILayout.BeginHorizontal();
        previewMode = EditorGUILayout.Toggle("Preview in Scene", previewMode);
        
        if (previewMode && GUILayout.Button("Snap to Grid"))
        {
            // Find the grid position under the mouse in scene view
            SnapToMousePosition();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Add button
        GUI.enabled = selectedPrefab != null;
        if (GUILayout.Button("Add Pre-placed Object", buttonStyle, GUILayout.Height(30)))
        {
            manager.AddPreplacedObject(selectedPrefab, gridPosition, rotationIndex, 
                isMovable, isDestructible, useCustomShape, customShapeName);
            
            SceneView.RepaintAll();
        }
        GUI.enabled = true;
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawPreplacedObjectsList()
    {
        EditorGUILayout.LabelField("Pre-placed Objects", headerStyle);
        
        showPreplacedObjects = EditorGUILayout.Foldout(showPreplacedObjects, $"Pre-placed Objects ({manager.preplacedObjects.Count})", true);
        
        if (!showPreplacedObjects) return;
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        if (manager.preplacedObjects.Count == 0)
        {
            EditorGUILayout.HelpBox("No pre-placed objects added yet.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear All", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("Confirm Clear", 
                    "Are you sure you want to remove all pre-placed objects?", "Yes", "Cancel"))
                {
                    manager.ClearPreplacedObjects();
                    SceneView.RepaintAll();
                }
            }
            
            if (GUILayout.Button("Place All in Scene", GUILayout.Width(150)))
            {
                PlaceObjectsInScene();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Draw the list
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            
            SerializedProperty objectsProperty = serializedObject.FindProperty("preplacedObjects");
            
            for (int i = 0; i < objectsProperty.arraySize; i++)
            {
                SerializedProperty objectProperty = objectsProperty.GetArrayElementAtIndex(i);
                SerializedProperty prefabProperty = objectProperty.FindPropertyRelative("prefab");
                SerializedProperty positionProperty = objectProperty.FindPropertyRelative("gridPosition");
                SerializedProperty rotationProperty = objectProperty.FindPropertyRelative("rotationIndex");
                SerializedProperty movableProperty = objectProperty.FindPropertyRelative("isMovable");
                SerializedProperty destructibleProperty = objectProperty.FindPropertyRelative("isDestructible");
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Object {i+1}: {prefabProperty.objectReferenceValue?.name ?? "NULL"}", EditorStyles.boldLabel);
                
                if (GUILayout.Button("Remove", GUILayout.Width(80)))
                {
                    manager.RemovePreplacedObject(i);
                    GUIUtility.ExitGUI();
                }
                
                if (GUILayout.Button("Preview", GUILayout.Width(80)))
                {
                    // Preview this object in the scene
                    GameObject prefab = prefabProperty.objectReferenceValue as GameObject;
                    Vector2Int pos = positionProperty.vector2IntValue;
                    int rot = rotationProperty.intValue;
                    
                    if (prefab != null)
                    {
                        selectedPrefab = prefab;
                        gridPosition = pos;
                        rotationIndex = rot;
                        previewMode = true;
                        
                        SceneView.RepaintAll();
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(prefabProperty);
                EditorGUILayout.PropertyField(positionProperty);
                EditorGUILayout.PropertyField(rotationProperty);
                EditorGUILayout.PropertyField(movableProperty);
                EditorGUILayout.PropertyField(destructibleProperty);
                
                SerializedProperty useCustomShapeProperty = objectProperty.FindPropertyRelative("useCustomShape");
                EditorGUILayout.PropertyField(useCustomShapeProperty);
                
                if (useCustomShapeProperty.boolValue)
                {
                    EditorGUILayout.PropertyField(objectProperty.FindPropertyRelative("customShapeName"));
                }
                
                EditorGUI.indentLevel--;
                
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(2);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void OnSceneGUI()
    {
        if (!previewMode || selectedPrefab == null || gridManager == null) return;
        
        // Draw grid position preview
        Handles.color = Color.green;
        Vector3 worldPos = gridManager.GetWorldPosition(gridPosition);
        
        // Draw a square at the grid position
        float cellSize = gridManager.cellSize;
        Vector3 p1 = worldPos + new Vector3(-cellSize/2, 0.01f, -cellSize/2);
        Vector3 p2 = worldPos + new Vector3(cellSize/2, 0.01f, -cellSize/2);
        Vector3 p3 = worldPos + new Vector3(cellSize/2, 0.01f, cellSize/2);
        Vector3 p4 = worldPos + new Vector3(-cellSize/2, 0.01f, cellSize/2);
        
        Handles.DrawLine(p1, p2);
        Handles.DrawLine(p2, p3);
        Handles.DrawLine(p3, p4);
        Handles.DrawLine(p4, p1);
        
        // Draw position label
        Handles.Label(worldPos + Vector3.up * 0.5f, $"Position: {gridPosition}");
        
        // Draw rotation indicator
        Vector3 direction = new Vector3(
            Mathf.Sin(rotationIndex * Mathf.PI / 2),
            0,
            Mathf.Cos(rotationIndex * Mathf.PI / 2)
        );
        
        Handles.color = Color.blue;
        Handles.DrawLine(worldPos, worldPos + direction * cellSize * 0.8f);
        
        // Draw position handle
        Vector3 newPos = Handles.PositionHandle(worldPos, Quaternion.identity);
        if (newPos != worldPos)
        {
            // Update grid position
            gridPosition = gridManager.GetGridPosition(newPos);
            Repaint();
        }
        
        // Preview the object on the grid
        manager.PreviewPreplacedObject(selectedPrefab, gridPosition, rotationIndex);
        
        // Check for key presses to rotate
        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            bool handled = false;
            
            if (e.keyCode == KeyCode.R)
            {
                // Rotate clockwise
                rotationIndex = (rotationIndex + 1) % 4;
                handled = true;
            }
            else if (e.keyCode == KeyCode.E)
            {
                // Rotate counter-clockwise
                rotationIndex = (rotationIndex + 3) % 4;
                handled = true;
            }
            
            if (handled)
            {
                e.Use();
                Repaint();
                SceneView.RepaintAll();
            }
        }
    }
    
    private void SnapToMousePosition()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null) return;
        
        Vector3 mousePosition = Event.current.mousePosition;
        mousePosition.y = sceneView.camera.pixelHeight - mousePosition.y;
        
        Ray ray = sceneView.camera.ScreenPointToRay(mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);
            gridPosition = gridManager.GetGridPosition(worldPoint);
            Repaint();
        }
    }
    
    private void LoadAvailableShapes()
    {
        availableShapes.Clear();
        customShapeIndex = -1;
        
        // This is just a placeholder - you'll need to implement a way to get the saved shapes
        // For example, you might use a helper class that accesses the saved shapes from GridObject
        
        // If we have a GridManager, try to look for existing shapes
        if (gridManager != null)
        {
            // Check if the GridManager has a method to get saved layouts
            System.Reflection.MethodInfo method = gridManager.GetType().GetMethod("GetSavedGridLayouts");
            if (method != null)
            {
                string[] layouts = method.Invoke(gridManager, null) as string[];
                if (layouts != null && layouts.Length > 0)
                {
                    availableShapes.AddRange(layouts);
                }
            }
        }
    }
    
    private void PlaceObjectsInScene()
    {
        // Clear any existing preview
        gridManager.ResetAllCellVisuals();
        
        // Remove existing placed objects in scene
        GameObject[] existingObjects = GameObject.FindGameObjectsWithTag("GridObject");
        foreach (GameObject obj in existingObjects)
        {
            DestroyImmediate(obj);
        }
        
        // Place all objects from the list
        manager.PlacePreoccupyingObjects();
        
        // Mark the scene as dirty
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
#endif