using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

#if UNITY_EDITOR
[CustomEditor(typeof(GridObject))]
public class GridObjectEditor : Editor
{
    private bool showLayoutPreview = true;
    private bool showShapePresets = true;
    private bool showCustomEditor = true;
    private bool showPivotOptions = true;
    private bool showSaveLoadSection = true;
    
    // Custom grid editor
    private int customGridWidth = 4;
    private int customGridHeight = 4;
    private int[,] customGridData;
    private Vector2Int customPivot = Vector2Int.zero;
    private float cellSize = 25f;
    
    // Shape preview cache
    private int[,] previewLayout;
    
    // Edited shape flag
    private bool hasEditedCustomShape = false;
    
    // Save/Load
    private string saveShapeName = "MyShape";
    private List<string> savedShapes = new List<string>();
    private int selectedSavedShapeIndex = -1;
    private Vector2 savedShapesScrollPosition;
    
    // Serialized properties
    private SerializedProperty currentShapeTypeProperty;
    private SerializedProperty shapeSizeProperty;
    private SerializedProperty autoUpdateShapeProperty;
    private SerializedProperty validPlacementMaterialProperty;
    private SerializedProperty invalidPlacementMaterialProperty;
    private SerializedProperty selectedMaterialProperty;
    private SerializedProperty loadShapeOnStartProperty;
    private SerializedProperty saveFolderProperty;
    private SerializedProperty serializedShapeDataProperty;
    
    private void OnEnable()
    {
        // Get serialized properties
        currentShapeTypeProperty = serializedObject.FindProperty("currentShapeType");
        shapeSizeProperty = serializedObject.FindProperty("shapeSize");
        autoUpdateShapeProperty = serializedObject.FindProperty("autoUpdateShape");
        validPlacementMaterialProperty = serializedObject.FindProperty("validPlacementMaterial");
        invalidPlacementMaterialProperty = serializedObject.FindProperty("invalidPlacementMaterial");
        selectedMaterialProperty = serializedObject.FindProperty("selectedMaterial");
        loadShapeOnStartProperty = serializedObject.FindProperty("loadShapeOnStart");
        saveFolderProperty = serializedObject.FindProperty("saveFolder");
        serializedShapeDataProperty = serializedObject.FindProperty("serializedShapeData");
        
        // Initialize custom grid data
        InitializeCustomGridData();
        
        // Initialize preview from current shape type
        GridObject gridObject = (GridObject)target;
        previewLayout = GridObject.CreateShape(gridObject.currentShapeType, gridObject.shapeSize);
        
        // Refresh saved shapes list
        RefreshSavedShapesList(gridObject);
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        GridObject gridObject = (GridObject)target;
        
        EditorGUILayout.Space(10);
        
        // Shape presets section
        DrawShapePresetsSection(gridObject);
        
        EditorGUILayout.Space(10);
        
        // Custom shape editor section
        DrawCustomShapeEditor(gridObject);
        
        EditorGUILayout.Space(10);
        
        // Save/Load section
        DrawSaveLoadSection(gridObject);
        
        EditorGUILayout.Space(10);
        
        // Pivot settings
        DrawPivotSettings();
        
        EditorGUILayout.Space(10);
        
        // Materials section
        EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(validPlacementMaterialProperty);
        EditorGUILayout.PropertyField(invalidPlacementMaterialProperty);
        EditorGUILayout.PropertyField(selectedMaterialProperty);
        
        EditorGUILayout.Space(5);
        
        // Runtime settings
        EditorGUILayout.PropertyField(autoUpdateShapeProperty, new GUIContent("Auto-Update Shape in Play Mode"));
        
        EditorGUILayout.Space(10);
        
        // Apply buttons
        DrawApplyButtons(gridObject);
        
        EditorGUILayout.Space(5);
        
        // Preview toggle
        showLayoutPreview = EditorGUILayout.Toggle("Show Shape Preview", showLayoutPreview);
        
        // Draw the preview if enabled
        if (showLayoutPreview)
        {
            DrawLayoutPreview();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void InitializeCustomGridData()
    {
        // Create an empty grid
        customGridData = new int[customGridWidth, customGridHeight];
        
        // Initialize with all zeros (empty cells)
        for (int x = 0; x < customGridWidth; x++)
        {
            for (int y = 0; y < customGridHeight; y++)
            {
                customGridData[x, y] = 0;
            }
        }
        
        // Default pivot at center
        customPivot = new Vector2Int(customGridWidth / 2, customGridHeight / 2);
    }
    
    private void DrawShapePresetsSection(GridObject gridObject)
    {
        showShapePresets = EditorGUILayout.Foldout(showShapePresets, "Shape Presets", true);
        
        if (showShapePresets)
        {
            EditorGUI.indentLevel++;
            
            // Shape type selection
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(currentShapeTypeProperty, new GUIContent("Shape Type"));
            
            // Shape size slider
            EditorGUILayout.PropertyField(shapeSizeProperty, new GUIContent("Shape Size"));
            
            if (EditorGUI.EndChangeCheck())
            {
                // Regenerate the preview layout when shape type or size changes
                GridObject.ShapeType shapeType = (GridObject.ShapeType)currentShapeTypeProperty.enumValueIndex;
                int shapeSize = shapeSizeProperty.intValue;
                
                // Only update if not custom shape
                if (shapeType != GridObject.ShapeType.Custom)
                {
                    previewLayout = GridObject.CreateShape(shapeType, shapeSize);
                    hasEditedCustomShape = false;
                }
            }
            
            if (GUILayout.Button("Use This Preset"))
            {
                GridObject.ShapeType shapeType = (GridObject.ShapeType)currentShapeTypeProperty.enumValueIndex;
                int shapeSize = shapeSizeProperty.intValue;
                
                previewLayout = GridObject.CreateShape(shapeType, shapeSize);
                
                // Update custom grid to match the preset
                UpdateCustomGridFromPreset();
                
                hasEditedCustomShape = false;
            }
            
            EditorGUI.indentLevel--;
        }
    }
    
    private void UpdateCustomGridFromPreset()
    {
        if (previewLayout == null) return;
        
        int width = previewLayout.GetLength(0);
        int height = previewLayout.GetLength(1);
        
        // If preset is larger than our custom grid, resize the custom grid
        if (width > customGridWidth || height > customGridHeight)
        {
            customGridWidth = Mathf.Max(customGridWidth, width);
            customGridHeight = Mathf.Max(customGridHeight, height);
            
            int[,] newGrid = new int[customGridWidth, customGridHeight];
            
            // Initialize with zeros
            for (int x = 0; x < customGridWidth; x++)
            {
                for (int y = 0; y < customGridHeight; y++)
                {
                    newGrid[x, y] = 0;
                }
            }
            
            // Copy data from old grid if applicable
            if (customGridData != null)
            {
                int oldWidth = customGridData.GetLength(0);
                int oldHeight = customGridData.GetLength(1);
                
                for (int x = 0; x < oldWidth && x < customGridWidth; x++)
                {
                    for (int y = 0; y < oldHeight && y < customGridHeight; y++)
                    {
                        newGrid[x, y] = customGridData[x, y];
                    }
                }
            }
            
            customGridData = newGrid;
        }
        
        // Copy preset layout into custom grid
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x < customGridWidth && y < customGridHeight)
                {
                    customGridData[x, y] = previewLayout[x, y];
                }
            }
        }
        
        // Update pivot to match the center of the preset
        customPivot = new Vector2Int(width / 2, height / 2);
    }
    
    private void DrawCustomShapeEditor(GridObject gridObject)
    {
        showCustomEditor = EditorGUILayout.Foldout(showCustomEditor, "Custom Shape Editor", true);
        
        if (!showCustomEditor) return;
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("Create your own shape by clicking cells in the grid", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // Grid size controls
        EditorGUILayout.BeginHorizontal();
        
        // Width slider
        EditorGUI.BeginChangeCheck();
        int newWidth = EditorGUILayout.IntSlider("Width", customGridWidth, 1, 8);
        if (EditorGUI.EndChangeCheck() && newWidth != customGridWidth)
        {
            ResizeCustomGrid(newWidth, customGridHeight);
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        // Height slider
        EditorGUI.BeginChangeCheck();
        int newHeight = EditorGUILayout.IntSlider("Height", customGridHeight, 1, 8);
        if (EditorGUI.EndChangeCheck() && newHeight != customGridHeight)
        {
            ResizeCustomGrid(customGridWidth, newHeight);
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Cell size for editing
        cellSize = EditorGUILayout.Slider("Cell Size", cellSize, 15f, 40f);
        
        EditorGUILayout.Space(5);
        
        // Grid editing tools
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Fill All"))
        {
            for (int x = 0; x < customGridWidth; x++)
            {
                for (int y = 0; y < customGridHeight; y++)
                {
                    customGridData[x, y] = 1;
                }
            }
            
            UpdatePreviewFromCustomGrid();
            hasEditedCustomShape = true;
            currentShapeTypeProperty.enumValueIndex = (int)GridObject.ShapeType.Custom;
        }
        
        if (GUILayout.Button("Clear All"))
        {
            for (int x = 0; x < customGridWidth; x++)
            {
                for (int y = 0; y < customGridHeight; y++)
                {
                    customGridData[x, y] = 0;
                }
            }
            
            UpdatePreviewFromCustomGrid();
            hasEditedCustomShape = true;
            currentShapeTypeProperty.enumValueIndex = (int)GridObject.ShapeType.Custom;
        }
        
        if (GUILayout.Button("Invert"))
        {
            for (int x = 0; x < customGridWidth; x++)
            {
                for (int y = 0; y < customGridHeight; y++)
                {
                    customGridData[x, y] = customGridData[x, y] == 0 ? 1 : 0;
                }
            }
            
            UpdatePreviewFromCustomGrid();
            hasEditedCustomShape = true;
            currentShapeTypeProperty.enumValueIndex = (int)GridObject.ShapeType.Custom;
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Editable grid view
        DrawEditableGrid();
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawSaveLoadSection(GridObject gridObject)
    {
        showSaveLoadSection = EditorGUILayout.Foldout(showSaveLoadSection, "Shape Save & Load", true);
        
        if (!showSaveLoadSection) return;
        
        // Save shape section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Save Current Shape", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        saveShapeName = EditorGUILayout.TextField("Shape Name:", saveShapeName);
        
        if (GUILayout.Button("Save", GUILayout.Width(100)))
        {
            if (string.IsNullOrEmpty(saveShapeName))
            {
                EditorUtility.DisplayDialog("Invalid Name", "Please enter a valid name for the shape", "OK");
            }
            else
            {
                if (gridObject.SaveShape(saveShapeName))
                {
                    RefreshSavedShapesList(gridObject);
                    EditorUtility.SetDirty(gridObject);
                    EditorUtility.DisplayDialog("Shape Saved", $"Shape '{saveShapeName}' saved successfully", "OK");
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        
        // Load shape section
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Load Shape", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Refresh Shape List"))
        {
            RefreshSavedShapesList(gridObject);
        }
        
        // Display list of saved shapes
        EditorGUILayout.LabelField("Available Shapes:");
        savedShapesScrollPosition = EditorGUILayout.BeginScrollView(savedShapesScrollPosition, GUILayout.Height(100));
        
        // Show list of saved shapes
        if (savedShapes.Count == 0)
        {
            EditorGUILayout.HelpBox("No saved shapes found.", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < savedShapes.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Toggle(selectedSavedShapeIndex == i, "", GUILayout.Width(20)))
                {
                    selectedSavedShapeIndex = i;
                    saveShapeName = savedShapes[i];
                }
                
                EditorGUILayout.LabelField(savedShapes[i]);
                
                if (GUILayout.Button("Load", GUILayout.Width(80)))
                {
                    if (gridObject.LoadShape(savedShapes[i]))
                    {
                        // Update editor state
                        GridObject.ShapeType shapeType;
                        if (Enum.TryParse(gridObject.currentShapeType.ToString(), out shapeType))
                        {
                            currentShapeTypeProperty.enumValueIndex = (int)shapeType;
                        }
                        else
                        {
                            currentShapeTypeProperty.enumValueIndex = (int)GridObject.ShapeType.Custom;
                        }
                        
                        // Update preview and custom grid
                        previewLayout = gridObject.GetCurrentLayout();
                        UpdateCustomGridFromGridObject(gridObject);
                        
                        // Mark as dirty
                        EditorUtility.SetDirty(gridObject);
                        serializedObject.Update();
                        
                        EditorUtility.DisplayDialog("Shape Loaded", $"Shape '{savedShapes[i]}' loaded successfully", "OK");
                    }
                }
                
                if (GUILayout.Button("Delete", GUILayout.Width(80)))
                {
                    if (EditorUtility.DisplayDialog("Delete Shape", 
                        $"Are you sure you want to delete the shape '{savedShapes[i]}'?", "Delete", "Cancel"))
                    {
                        DeleteSavedShape(gridObject, savedShapes[i]);
                        RefreshSavedShapesList(gridObject);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndScrollView();
        
        // Set as load on start
        if (selectedSavedShapeIndex >= 0 && selectedSavedShapeIndex < savedShapes.Count)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Load on Start");
            
            if (GUILayout.Button("Set as Default Shape"))
            {
                string selectedShape = savedShapes[selectedSavedShapeIndex];
                loadShapeOnStartProperty.stringValue = selectedShape;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(gridObject);
                EditorUtility.DisplayDialog("Default Set", 
                    $"'{selectedShape}' set as default shape to load on game start", "OK");
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void ResizeCustomGrid(int newWidth, int newHeight)
    {
        int[,] newGrid = new int[newWidth, newHeight];
        
        // Initialize with zeros
        for (int x = 0; x < newWidth; x++)
        {
            for (int y = 0; y < newHeight; y++)
            {
                newGrid[x, y] = 0;
            }
        }
        
        // Copy data from old grid if applicable
        if (customGridData != null)
        {
            int oldWidth = customGridData.GetLength(0);
            int oldHeight = customGridData.GetLength(1);
            
            for (int x = 0; x < Mathf.Min(oldWidth, newWidth); x++)
            {
                for (int y = 0; y < Mathf.Min(oldHeight, newHeight); y++)
                {
                    newGrid[x, y] = customGridData[x, y];
                }
            }
        }
        
        customGridWidth = newWidth;
        customGridHeight = newHeight;
        customGridData = newGrid;
        
        // Adjust pivot if necessary
        customPivot.x = Mathf.Min(customPivot.x, newWidth - 1);
        customPivot.y = Mathf.Min(customPivot.y, newHeight - 1);
        
        // Update preview
        UpdatePreviewFromCustomGrid();
        hasEditedCustomShape = true;
        currentShapeTypeProperty.enumValueIndex = (int)GridObject.ShapeType.Custom;
    }
    
    private void DrawEditableGrid()
    {
        if (customGridData == null) return;
        
        float totalWidth = cellSize * customGridWidth;
        float totalHeight = cellSize * customGridHeight;
        
        Rect gridRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);
        
        // Draw the background
        EditorGUI.DrawRect(gridRect, new Color(0.2f, 0.2f, 0.2f));
        
        // Handle mouse input for editing
        Event e = Event.current;
        if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
        {
            if (gridRect.Contains(e.mousePosition))
            {
                int x = Mathf.FloorToInt((e.mousePosition.x - gridRect.x) / cellSize);
                int y = Mathf.FloorToInt((e.mousePosition.y - gridRect.y) / cellSize);
                
                if (x >= 0 && x < customGridWidth && y >= 0 && y < customGridHeight)
                {
                    // Left click to set cell, right click to clear
                    customGridData[x, y] = e.button == 0 ? 1 : 0;
                    
                    // Update preview based on custom grid
                    UpdatePreviewFromCustomGrid();
                    
                    hasEditedCustomShape = true;
                    currentShapeTypeProperty.enumValueIndex = (int)GridObject.ShapeType.Custom;
                    e.Use();
                    Repaint();
                }
            }
        }
        
        // Draw the grid cells
        for (int x = 0; x < customGridWidth; x++)
        {
            for (int y = 0; y < customGridHeight; y++)
            {
                Rect cellRect = new Rect(
                    gridRect.x + x * cellSize,
                    gridRect.y + y * cellSize,
                    cellSize,
                    cellSize
                );
                
                // Determine cell color
                Color cellColor;
                
                if (x == customPivot.x && y == customPivot.y)
                {
                    // Pivot cell
                    cellColor = new Color(1f, 0.6f, 0f); // Orange
                }
                else if (customGridData[x, y] == 1)
                {
                    // Occupied cell
                    cellColor = new Color(0.2f, 0.7f, 0.2f); // Green
                }
                else
                {
                    // Empty cell
                    cellColor = new Color(0.3f, 0.3f, 0.3f); // Dark gray
                }
                
                // Draw cell
                EditorGUI.DrawRect(cellRect, cellColor);
                
                // Draw cell border
                EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 1), Color.black);
                EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1, cellRect.height), Color.black);
                EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y + cellRect.height - 1, cellRect.width, 1), Color.black);
                EditorGUI.DrawRect(new Rect(cellRect.x + cellRect.width - 1, cellRect.y, 1, cellRect.height), Color.black);
                
                // Coordinates
                GUI.Label(cellRect, $"{x},{y}", new GUIStyle
                {
                    normal = { textColor = Color.white },
                    fontSize = Mathf.FloorToInt(cellSize / 4),
                    alignment = TextAnchor.MiddleCenter
                });
            }
        }
        
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox("Left-click to add cells, right-click to remove cells.", MessageType.Info);
    }
    
        private void UpdatePreviewFromCustomGrid()
        {
            if (customGridData == null) return;
            
            // Create a copy of the custom grid for preview
            previewLayout = new int[customGridWidth, customGridHeight];
            
            for (int x = 0; x < customGridWidth; x++)
            {
                for (int y = 0; y < customGridHeight; y++)
                {
                    previewLayout[x, y] = customGridData[x, y];
                }
            }
        }
    
    private void UpdateCustomGridFromGridObject(GridObject gridObject)
    {
        if (gridObject == null) return;
        
        int[,] objectLayout = gridObject.GetCurrentLayout();
        Vector2Int pivot = gridObject.GetCurrentPivot();
        
        int width = objectLayout.GetLength(0);
        int height = objectLayout.GetLength(1);
        
        // Resize custom grid to match
        customGridWidth = width;
        customGridHeight = height;
        customGridData = new int[width, height];
        
        // Copy layout
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                customGridData[x, y] = objectLayout[x, y];
            }
        }
        
        // Set pivot
        customPivot = pivot;
        
        // Update preview
        UpdatePreviewFromCustomGrid();
    }
    
    private void DrawPivotSettings()
    {
        showPivotOptions = EditorGUILayout.Foldout(showPivotOptions, "Pivot Options", true);
        
        if (!showPivotOptions) return;
        
        EditorGUI.indentLevel++;
        
        // Display current pivot
        EditorGUILayout.LabelField($"Current Pivot: ({customPivot.x}, {customPivot.y})");
        
        // Max values depend on the active shape (custom or preset)
        int maxX = customGridWidth - 1;
        int maxY = customGridHeight - 1;
        
        // Pivot coordinate fields with range validation
        EditorGUI.BeginChangeCheck();
        customPivot.x = EditorGUILayout.IntSlider("Pivot X", customPivot.x, 0, maxX);
        customPivot.y = EditorGUILayout.IntSlider("Pivot Y", customPivot.y, 0, maxY);
        
        if (EditorGUI.EndChangeCheck())
        {
            // Make sure pivot is valid
            customPivot.x = Mathf.Clamp(customPivot.x, 0, maxX);
            customPivot.y = Mathf.Clamp(customPivot.y, 0, maxY);
        }
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Center Pivot"))
        {
            customPivot = new Vector2Int(customGridWidth / 2, customGridHeight / 2);
        }
        
        if (GUILayout.Button("Corner Pivot (0,0)"))
        {
            customPivot = Vector2Int.zero;
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUI.indentLevel--;
    }
    
    private void DrawApplyButtons(GridObject gridObject)
    {
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Apply Shape", GUILayout.Height(30)))
        {
            ApplyShapeToObject(gridObject);
        }
        
        if (hasEditedCustomShape && GUILayout.Button("Apply as Custom Shape", GUILayout.Height(30)))
        {
            // Switch to custom shape type
            currentShapeTypeProperty.enumValueIndex = (int)GridObject.ShapeType.Custom;
            ApplyShapeToObject(gridObject);
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawLayoutPreview()
    {
        if (previewLayout == null) return;
        
        EditorGUILayout.LabelField("Final Shape Preview", EditorStyles.boldLabel);
        
        int width = previewLayout.GetLength(0);
        int height = previewLayout.GetLength(1);
        
        // Calculate occupied cells for description
        int occupiedCells = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (previewLayout[x, y] == 1)
                {
                    occupiedCells++;
                }
            }
        }
        
        GridObject.ShapeType shapeType = (GridObject.ShapeType)currentShapeTypeProperty.enumValueIndex;
        string description = hasEditedCustomShape 
            ? $"Custom Shape ({width}x{height}, {occupiedCells} occupied cells)"
            : $"{shapeType} Shape ({width}x{height}, {occupiedCells} occupied cells)";
        
        EditorGUILayout.LabelField(description);
        
        // Draw the preview similar to custom grid but smaller and read-only
        float previewCellSize = 20f;
        float totalWidth = previewCellSize * width;
        float totalHeight = previewCellSize * height;
        
        Rect previewRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);
        
        // Draw the background
        EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f));
        
        // Draw the grid cells
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Rect cellRect = new Rect(
                    previewRect.x + x * previewCellSize,
                    previewRect.y + y * previewCellSize,
                    previewCellSize,
                    previewCellSize
                );
                
                // Determine cell color
                Color cellColor;
                
                if (x == customPivot.x && y == customPivot.y)
                {
                    // Pivot cell
                    cellColor = new Color(1f, 0.6f, 0f); // Orange
                }
                else if (previewLayout[x, y] == 1)
                {
                    // Occupied cell
                    cellColor = new Color(0.2f, 0.7f, 0.2f); // Green
                }
                else
                {
                    // Empty cell
                    cellColor = new Color(0.3f, 0.3f, 0.3f); // Dark gray
                }
                
                // Draw cell
                EditorGUI.DrawRect(cellRect, cellColor);
                
                // Draw cell border
                EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 1), Color.black);
                EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1, cellRect.height), Color.black);
                EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y + cellRect.height - 1, cellRect.width, 1), Color.black);
                EditorGUI.DrawRect(new Rect(cellRect.x + cellRect.width - 1, cellRect.y, 1, cellRect.height), Color.black);
            }
        }
        
        // Display legend
        EditorGUILayout.BeginHorizontal();
        
        EditorGUILayout.LabelField("Legend:", GUILayout.Width(50));
        
        Rect pivotLegendRect = GUILayoutUtility.GetRect(15, 15);
        EditorGUI.DrawRect(pivotLegendRect, new Color(1f, 0.6f, 0f));
        EditorGUILayout.LabelField("Pivot", GUILayout.Width(40));
        
        Rect occupiedLegendRect = GUILayoutUtility.GetRect(15, 15);
        EditorGUI.DrawRect(occupiedLegendRect, new Color(0.2f, 0.7f, 0.2f));
        EditorGUILayout.LabelField("Occupied", GUILayout.Width(70));
        
        Rect emptyLegendRect = GUILayoutUtility.GetRect(15, 15);
        EditorGUI.DrawRect(emptyLegendRect, new Color(0.3f, 0.3f, 0.3f));
        EditorGUILayout.LabelField("Empty");
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void ApplyShapeToObject(GridObject gridObject)
    {
        if (previewLayout == null) return;
        
        Undo.RecordObject(gridObject, "Set Grid Object Layout");
        
        // Apply the layout and pivot
        gridObject.SetObjectLayout(previewLayout, customPivot);
        
        // Update the shape type in the object if needed
        if (hasEditedCustomShape)
        {
            currentShapeTypeProperty.enumValueIndex = (int)GridObject.ShapeType.Custom;
        }
        
        EditorUtility.SetDirty(gridObject);
        
        // Count occupied cells for feedback
        int width = previewLayout.GetLength(0);
        int height = previewLayout.GetLength(1);
        int occupiedCells = 0;
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (previewLayout[x, y] == 1)
                {
                    occupiedCells++;
                }
            }
        }
        
        GridObject.ShapeType shapeType = (GridObject.ShapeType)currentShapeTypeProperty.enumValueIndex;
        string shapeDescription = hasEditedCustomShape ? "custom shape" : shapeType.ToString();
        
        Debug.Log($"Applied {shapeDescription} ({width}x{height}) with {occupiedCells} occupied cells to {gridObject.name}. " +
                 $"Pivot set to ({customPivot.x}, {customPivot.y})");
    }
    
    private void RefreshSavedShapesList(GridObject gridObject)
    {
        savedShapes.Clear();
        
        string[] shapes = gridObject.GetSavedShapes();
        savedShapes.AddRange(shapes);
        
        // If we have a selected shape and it's no longer in the list, reset the selection
        if (selectedSavedShapeIndex >= savedShapes.Count)
        {
            selectedSavedShapeIndex = -1;
        }
    }
    
    private void DeleteSavedShape(GridObject gridObject, string shapeName)
    {
        if (string.IsNullOrEmpty(shapeName))
            return;
            
        try
        {
            string savePath = Path.Combine(Application.persistentDataPath, gridObject.saveFolder);
            string filePath = Path.Combine(savePath, $"{shapeName}.json");
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"Deleted shape: {shapeName}");
                
                // If this was the default shape to load, clear that setting
                if (gridObject.loadShapeOnStart == shapeName)
                {
                    loadShapeOnStartProperty.stringValue = "";
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(gridObject);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error deleting shape: {e.Message}");
        }
    }
    
    public void OnSceneGUI()
    {
        GridObject gridObject = (GridObject)target;
        
        if (showLayoutPreview && previewLayout != null)
        {
            // Draw shape preview in scene view
            Vector3 objectPosition = gridObject.transform.position;
            float cellSize = 1.0f; // Should match your grid cell size
            
            // Loop through the shape layout
            int width = previewLayout.GetLength(0);
            int height = previewLayout.GetLength(1);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Only visualize occupied cells
                    if (previewLayout[x, y] == 1)
                    {
                        // Calculate position relative to pivot
                        Vector2Int relativePos = new Vector2Int(x, y) - customPivot;
                        Vector3 cellWorldPos = objectPosition + new Vector3(relativePos.x * cellSize, 0, relativePos.y * cellSize);
                        
                        // Draw cell
                        Handles.color = new Color(0.2f, 0.7f, 0.2f, 0.3f);
                        Handles.DrawWireCube(cellWorldPos, new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));
                    }
                }
            }
            
            // Draw pivot
            Handles.color = new Color(1f, 0.6f, 0f, 0.8f);
            Handles.SphereHandleCap(0,objectPosition, Quaternion.identity, cellSize * 0.2f, EventType.Repaint);
        }
        
        // Handle drag and drop of other GridObjects
        Event evt = Event.current;
        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            // Set the visual mode
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                
                foreach (var draggedObject in DragAndDrop.objectReferences)
                {
                    if (draggedObject is GameObject go)
                    {
                        GridObject sourceObj = go.GetComponent<GridObject>();
                        if (sourceObj != null && sourceObj != gridObject)
                        {
                            Undo.RecordObject(gridObject, "Copy Grid Object Shape");
                            gridObject.CopyLayoutFrom(sourceObj);
                            
                            // Update editor state
                            previewLayout = gridObject.GetCurrentLayout();
                            customPivot = gridObject.GetCurrentPivot();
                            
                            // Update UI
                            UpdateCustomGridFromGridObject(gridObject);
                            Repaint();
                            
                            EditorUtility.SetDirty(gridObject);
                        }
                    }
                }
            }
            
            evt.Use();
        }
    }
}
#endif