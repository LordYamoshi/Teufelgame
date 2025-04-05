#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

/// <summary>
/// Editor tool for creating and editing grid layouts
/// </summary>
[CustomEditor(typeof(GridManager))]
public class GridEditor : Editor
{
    // Grid editing state
    private bool[,] _gridData;
    private int _width = 10;
    private int _height = 10;
    private bool _isEditingGrid = false;
    private Color _cellOnColor = Color.green;
    private Color _cellOffColor = Color.red;
    private Vector2 _scrollPosition;
    private float _cellSize = 20f;
    
    // Shape testing 
    private bool _showShapeTestingSection = false;
    private GridObject.ShapeType _selectedShapeType = GridObject.ShapeType.T;
    private int _selectedShapeSize = 3;
    private int _selectedRotation = 0;
    private List<Vector2Int> _highlightedCells = new List<Vector2Int>();
    
    // Debug options
    private bool _showDebugSettings = false;
    private bool _showGridCoordinates = true;
    private bool _showGridLines = true;
    
    // Save/Load
    private bool _showSaveLoadSection = false;
    private string _saveFileName = "GridLayout";
    
    // Applied grid tracking
    private bool[,] _lastAppliedGrid;
    private int _lastAppliedWidth;
    private int _lastAppliedHeight;
    
    // Serialized properties
    private SerializedProperty _maxWidthProperty;
    private SerializedProperty _maxDepthProperty;
    private SerializedProperty _cellSizeProperty;
    private SerializedProperty _gridOriginProperty;
    private SerializedProperty _cellVisualPrefabProperty;
    private SerializedProperty _cellContainerProperty;
    private SerializedProperty _requireEdgeConnectivityProperty;
    private SerializedProperty _showOnlyUsedTilesProperty;
    private SerializedProperty _defaultGridLayoutProperty;
    
    private void OnEnable()
    {
        // Get serialized properties
        _maxWidthProperty = serializedObject.FindProperty("maxWidth");
        _maxDepthProperty = serializedObject.FindProperty("maxDepth");
        _cellSizeProperty = serializedObject.FindProperty("cellSize");
        _gridOriginProperty = serializedObject.FindProperty("gridOrigin");
        _cellVisualPrefabProperty = serializedObject.FindProperty("cellVisualPrefab");
        _cellContainerProperty = serializedObject.FindProperty("cellContainer");
        _requireEdgeConnectivityProperty = serializedObject.FindProperty("requireEdgeConnectivity");
        _showOnlyUsedTilesProperty = serializedObject.FindProperty("showOnlyUsedTiles");
        _defaultGridLayoutProperty = serializedObject.FindProperty("defaultGridLayout");

        // Initialize grid data with default values
        GridManager gridManager = (GridManager)target;
        _width = gridManager.maxWidth;
        _height = gridManager.maxDepth;
        InitializeGridData();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        GridManager gridManager = (GridManager)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Grid Manager Properties", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(_maxWidthProperty);
        EditorGUILayout.PropertyField(_maxDepthProperty);
        EditorGUILayout.PropertyField(_cellSizeProperty);
        EditorGUILayout.PropertyField(_gridOriginProperty);
        EditorGUILayout.PropertyField(_cellVisualPrefabProperty);
        EditorGUILayout.PropertyField(_cellContainerProperty);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Gameplay Rules", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_requireEdgeConnectivityProperty, new GUIContent("Require Edge Connectivity", "Objects must have at least one edge connected to an existing object"));
        EditorGUILayout.PropertyField(_showOnlyUsedTilesProperty, new GUIContent("Show Only Used Tiles", "Only highlight tiles that will be used by the object (Clash of Clans style)"));
        EditorGUILayout.PropertyField(_defaultGridLayoutProperty, new GUIContent("Default Grid Layout", "Grid layout to load on startup"));

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(20);
        DrawGridEditorSection(gridManager);
        
        EditorGUILayout.Space(10);
        DrawSaveLoadSection(gridManager);
    }

    private void DrawGridEditorSection(GridManager gridManager)
    {
        EditorGUILayout.LabelField("Grid Editor Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(_isEditingGrid ? "Exit Grid Editor" : "Open Grid Editor", GUILayout.Height(30)))
        {
            _isEditingGrid = !_isEditingGrid;
            if (_isEditingGrid)
            {
                _width = gridManager.maxWidth;
                _height = gridManager.maxDepth;
                InitializeGridData();
            }
            else
            {
                // Clear highlighted cells when exiting
                _highlightedCells.Clear();
            }
        }

        if (GUILayout.Button("Create Rectangular Grid", GUILayout.Height(30)))
        {
            gridManager.CreateRectangularGrid(gridManager.maxWidth, gridManager.maxDepth);
            _highlightedCells.Clear();
        }
        
        EditorGUILayout.EndHorizontal();

        if (!_isEditingGrid)
            return;

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();

        // Grid dimensions input
        EditorGUILayout.BeginVertical(GUILayout.Width(150));
        EditorGUILayout.LabelField("Grid Dimensions");

        EditorGUI.BeginChangeCheck();
        _width = EditorGUILayout.IntSlider("Width", _width, 1, 40);
        _height = EditorGUILayout.IntSlider("Height", _height, 1, 40);
        if (EditorGUI.EndChangeCheck())
        {
            InitializeGridData();
            _highlightedCells.Clear();
        }

        EditorGUILayout.Space(10);
        _cellSize = EditorGUILayout.Slider("Cell Size", _cellSize, 10f, 40f);
        
        // Display options
        _showDebugSettings = EditorGUILayout.Foldout(_showDebugSettings, "Display Options", true);
        if (_showDebugSettings)
        {
            EditorGUI.indentLevel++;
            _showGridCoordinates = EditorGUILayout.Toggle("Show Coordinates", _showGridCoordinates);
            _showGridLines = EditorGUILayout.Toggle("Show Grid Lines", _showGridLines);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space(10);

        // Grid operations
        if (GUILayout.Button("Fill All"))
        {
            for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
                _gridData[x, y] = true;
            Repaint();
        }

        if (GUILayout.Button("Clear All"))
        {
            for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
                _gridData[x, y] = false;
            _highlightedCells.Clear();
            Repaint();
        }

        if (GUILayout.Button("Invert All"))
        {
            for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
                _gridData[x, y] = !_gridData[x, y];
            _highlightedCells.Clear();
            Repaint();
        }
        
        EditorGUILayout.Space(5);
        if (GUILayout.Button("Create Border"))
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _gridData[x, y] = (x == 0 || x == _width - 1 || y == 0 || y == _height - 1);
                }
            }
            Repaint();
        }

        EditorGUILayout.Space(10);

        // Apply/Delete buttons
        if (GUILayout.Button("Apply Grid", GUILayout.Height(30)))
        {
            ApplyGridToManager(gridManager);
            _highlightedCells.Clear();
        }

        if (GUILayout.Button("Delete Applied Grid", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Delete Applied Grid", "Are you sure you want to delete the most recently applied custom grid?", "Delete", "Cancel"))
            {
                DeleteAppliedGrid(gridManager);
                _highlightedCells.Clear();
            }
        }
        
        // Shape testing section
        EditorGUILayout.Space(15);
        _showShapeTestingSection = EditorGUILayout.Foldout(_showShapeTestingSection, "Shape Placement Testing", true);
        
        if (_showShapeTestingSection)
        {
            EditorGUILayout.Space(5);
            _selectedShapeType = (GridObject.ShapeType)EditorGUILayout.EnumPopup("Shape Type:", _selectedShapeType);
            _selectedShapeSize = EditorGUILayout.IntSlider("Shape Size:", _selectedShapeSize, 2, 5);
            _selectedRotation = EditorGUILayout.IntSlider("Rotation (90° increments):", _selectedRotation, 0, 3);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Shape Placement"))
            {
                TestShapePlacement(gridManager);
            }
            
            if (GUILayout.Button("Clear Highlights"))
            {
                _highlightedCells.Clear();
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();

        // Draw the grid
        DrawGridView(gridManager);

        EditorGUILayout.EndHorizontal();
        
        // Help text
        EditorGUILayout.HelpBox("Left-click to paint cells, right-click to erase. Use the shape testing section to preview how buildings will fit on your grid.", MessageType.Info);
    }

    private void DrawGridView(GridManager gridManager)
    {
        var gridRect = EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        var totalWidth = _cellSize * _width;
        var totalHeight = _cellSize * _height;

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition,
            GUILayout.Height(Mathf.Min(totalHeight + 20, 500)),
            GUILayout.Width(Mathf.Min(totalWidth + 20, 800)));

        Rect rect = GUILayoutUtility.GetRect(totalWidth, totalHeight);

        // Handle mouse input
        Event e = Event.current;
        if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
        {
            HandleGridMouseInput(e, rect);
        }

        // Draw the grid cells
        if (Event.current.type == EventType.Repaint)
        {
            DrawGridCells(rect);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }
    
    private void HandleGridMouseInput(Event e, Rect rect)
    {
        if (rect.Contains(e.mousePosition))
        {
            int x = Mathf.FloorToInt((e.mousePosition.x - rect.x) / _cellSize);
            int y = Mathf.FloorToInt((e.mousePosition.y - rect.y) / _cellSize);

            if (x >= 0 && x < _width && y >= 0 && y < _height)
            {
                _gridData[x, y] = e.button == 0; // Left button for true, right for false
                Repaint();
                e.Use();
            }
        }
    }
    
    private void DrawGridCells(Rect rect)
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                Rect cellRect = new Rect(
                    rect.x + x * _cellSize,
                    rect.y + y * _cellSize,
                    _cellSize,
                    _cellSize
                );

                // Check if this cell is highlighted for shape placement testing
                bool isHighlighted = _highlightedCells.Contains(new Vector2Int(x, y));
                Color cellColor;
                
                if (isHighlighted)
                {
                    cellColor = new Color(0.2f, 0.6f, 1f); // Blue highlight color
                }
                else
                {
                    cellColor = _gridData[x, y] ? _cellOnColor : _cellOffColor;
                }
                
                EditorGUI.DrawRect(cellRect, cellColor);

                // Draw cell border if enabled
                if (_showGridLines)
                {
                    EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 1), Color.black);
                    EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1, cellRect.height), Color.black);
                    EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y + cellRect.height - 1, cellRect.width, 1),
                        Color.black);
                    EditorGUI.DrawRect(new Rect(cellRect.x + cellRect.width - 1, cellRect.y, 1, cellRect.height),
                        Color.black);
                }

                // Draw coordinates in cell for better usability
                if (_showGridCoordinates)
                {
                    GUI.Label(cellRect, $"{x},{y}", new GUIStyle
                    {
                        normal = { textColor = isHighlighted ? Color.white : (_gridData[x, y] ? Color.black : Color.white) },
                        fontSize = Mathf.FloorToInt(_cellSize / 4),
                        alignment = TextAnchor.MiddleCenter
                    });
                }
            }
        }
    }
    
    private void DrawSaveLoadSection(GridManager gridManager)
    {
        _showSaveLoadSection = EditorGUILayout.Foldout(_showSaveLoadSection, "Grid File Operations", true);
        
        if (!_showSaveLoadSection) return;
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // File name input
        _saveFileName = EditorGUILayout.TextField("File Name", _saveFileName);
        
        EditorGUILayout.BeginHorizontal();
        
        // Save button
        if (GUILayout.Button("Save Grid Layout"))
        {
            SaveGridLayout(_saveFileName, gridManager);
        }
        
        // Load button
        if (GUILayout.Button("Load Grid Layout"))
        {
            LoadGridLayout(_saveFileName, gridManager);
        }
        
        EditorGUILayout.EndHorizontal();
        
        // List saved layouts
        string[] savedLayouts = gridManager.GetSavedGridLayouts();
        if (savedLayouts.Length > 0)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Available layouts:", EditorStyles.boldLabel);
            
            foreach (string layout in savedLayouts)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(layout);
                
                if (GUILayout.Button("Load", GUILayout.Width(80)))
                {
                    LoadGridLayout(layout, gridManager);
                }
                
                if (GUILayout.Button("Set as Default", GUILayout.Width(100)))
                {
                    _defaultGridLayoutProperty.stringValue = layout;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(gridManager);
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndVertical();
    }

    private void InitializeGridData()
    {
        bool[,] newData = new bool[_width, _height];

        // Copy existing data if possible
        if (_gridData != null)
        {
            int copyWidth = Mathf.Min(_width, _gridData.GetLength(0));
            int copyHeight = Mathf.Min(_height, _gridData.GetLength(1));

            for (int x = 0; x < copyWidth; x++)
            {
                for (int y = 0; y < copyHeight; y++)
                {
                    newData[x, y] = _gridData[x, y];
                }
            }
        }

        _gridData = newData;
    }
    
    /// <summary>
    /// Tests shape placement with the specified shape type
    /// </summary>
    private void TestShapePlacement(GridManager gridManager)
    {
        if (_gridData == null) return;
        
        // Get the shape layout
        int[,] layout = GridObject.CreateShape(_selectedShapeType, _selectedShapeSize);
        
        // Debug the shape layout
        DebugShapeLayout(layout);
        
        // Find a suitable placement location in the grid
        Vector2Int placementPos = FindSuitablePlacementPosition(layout);
        
        if (placementPos == new Vector2Int(-1, -1))
        {
            EditorUtility.DisplayDialog("Placement Failed", 
                $"Could not find a suitable position to place a {_selectedShapeType} shape on the grid. " +
                "Ensure your grid has enough connected cells.", "OK");
            return;
        }
        
        // Calculate occupied cells
        _highlightedCells = GetOccupiedCellsForShape(placementPos, layout);
        
        Debug.Log($"{_selectedShapeType} shape size {_selectedShapeSize} with rotation {_selectedRotation} can be placed at grid position {placementPos}. " +
                 $"It would occupy {_highlightedCells.Count} cells.");
        
        Repaint();
    }
    
    /// <summary>
    /// Debug helper to log the shape layout
    /// </summary>
    private void DebugShapeLayout(int[,] layout)
    {
        int width = layout.GetLength(0);
        int height = layout.GetLength(1);
        
        string layoutStr = $"{_selectedShapeType} shape ({width}x{height}) with rotation {_selectedRotation}:\n";
        
        // Create a copy of the layout to apply rotation for visualization
        int[,] rotatedLayout = new int[width, height];
        Vector2Int pivot = new Vector2Int(width / 2, height / 2);
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int rotatedPos = RotatePosition(new Vector2Int(x, y), pivot, _selectedRotation);
                
                // Check if rotated position is in bounds
                if (rotatedPos.x >= 0 && rotatedPos.x < width && rotatedPos.y >= 0 && rotatedPos.y < height)
                {
                    try
                    {
                        rotatedLayout[x, y] = layout[rotatedPos.x, rotatedPos.y];
                    }
                    catch (System.IndexOutOfRangeException)
                    {
                        // This can happen with rotation - just use 0
                        rotatedLayout[x, y] = 0;
                    }
                }
            }
        }
        
        // Display the rotated layout
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                layoutStr += rotatedLayout[x, y] == 1 ? "■ " : "□ ";
            }
            layoutStr += "\n";
        }
        
        Debug.Log(layoutStr);
    }
    
    /// <summary>
    /// Gets the list of occupied cells for a shape at the specified position
    /// </summary>
    private List<Vector2Int> GetOccupiedCellsForShape(Vector2Int position, int[,] layout)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        
        int shapeWidth = layout.GetLength(0);
        int shapeHeight = layout.GetLength(1);
        
        // Calculate pivot (center of shape)
        Vector2Int pivot = new Vector2Int(shapeWidth / 2, shapeHeight / 2);
        
        for (int x = 0; x < shapeWidth; x++)
        {
            for (int y = 0; y < shapeHeight; y++)
            {
                if (layout[x, y] == 1)
                {
                    // Calculate relative position and apply rotation
                    Vector2Int relativePos = new Vector2Int(x, y) - pivot;
                    relativePos = RotatePosition(relativePos, _selectedRotation);
                    
                    // Calculate final grid position
                    Vector2Int gridPos = position + relativePos;
                    
                    // Adjust for editor grid orientation (y-axis is flipped in editor grid)
                    gridPos.y = _height - 1 - gridPos.y;
                    
                    // Add to occupied cells if in bounds
                    if (gridPos.x >= 0 && gridPos.x < _width && gridPos.y >= 0 && gridPos.y < _height)
                    {
                        cells.Add(gridPos);
                    }
                }
            }
        }
        
        return cells;
    }
    
    /// <summary>
    /// Rotates a position vector by the given number of 90° increments
    /// </summary>
    private Vector2Int RotatePosition(Vector2Int pos, int rotations)
    {
        Vector2Int rotated = pos;
        
        // Apply rotation (90-degree increments)
        for (int i = 0; i < rotations; i++)
        {
            // 90-degree rotation: (x, y) -> (-y, x)
            rotated = new Vector2Int(-rotated.y, rotated.x);
        }
        
        return rotated;
    }
    
    /// <summary>
    /// Rotates a position around a pivot by the given number of 90° increments
    /// </summary>
    private Vector2Int RotatePosition(Vector2Int pos, Vector2Int pivot, int rotations)
    {
        // Convert to relative position from pivot
        Vector2Int relative = pos - pivot;
        
        // Apply rotation
        relative = RotatePosition(relative, rotations);
        
        // Convert back to absolute position
        return relative + pivot;
    }
    
    /// <summary>
    /// Finds a suitable position to place a shape
    /// </summary>
    private Vector2Int FindSuitablePlacementPosition(int[,] layout)
    {
        int shapeWidth = layout.GetLength(0);
        int shapeHeight = layout.GetLength(1);
        
        // Calculate pivot
        Vector2Int pivot = new Vector2Int(shapeWidth / 2, shapeHeight / 2);
        
        // Check each cell in the grid
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                // Convert editor grid y coordinate to GridManager's coordinate system
                int gridY = _height - 1 - y;
                Vector2Int placementPos = new Vector2Int(x, gridY);
                
                // Check if all required cells for the shape are valid
                bool canPlace = true;
                
                for (int sx = 0; sx < shapeWidth; sx++)
                {
                    for (int sy = 0; sy < shapeHeight; sy++)
                    {
                        if (layout[sx, sy] == 1)
                        {
                            // Calculate relative position and apply rotation
                            Vector2Int relativePos = new Vector2Int(sx, sy) - pivot;
                            relativePos = RotatePosition(relativePos, _selectedRotation);
                            
                            // Calculate the grid position for this part of the shape
                            Vector2Int checkPos = placementPos + relativePos;
                            
                            // Convert back to editor grid coordinates for checking gridData
                            int editorCheckY = _height - 1 - checkPos.y;
                            
                            // Check if this position is within grid bounds and exists in our grid
                            if (checkPos.x < 0 || checkPos.x >= _width || 
                                checkPos.y < 0 || checkPos.y >= _height ||
                                editorCheckY < 0 || editorCheckY >= _height ||
                                !_gridData[checkPos.x, editorCheckY])
                            {
                                canPlace = false;
                                break;
                            }
                        }
                    }
                    
                    if (!canPlace) break;
                }
                
                if (canPlace)
                {
                    return placementPos;
                }
            }
        }
        
        // No suitable position found
        return new Vector2Int(-1, -1);
    }

    private void ApplyGridToManager(GridManager gridManager)
    {
        if (_gridData == null)
            return;
        
        Undo.RecordObject(gridManager, "Apply Custom Grid");
        
        // Adjust grid manager properties if needed
        if (gridManager.maxWidth != _width)
        {
            gridManager.maxWidth = _width;
        }
        
        if (gridManager.maxDepth != _height)
        {
            gridManager.maxDepth = _height;
        }
        
        // Apply the grid
        bool[,] flippedGrid = new bool[_width, _height];
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                // Flip Y axis for Unity's coordinate system
                flippedGrid[x, y] = _gridData[x, _height - 1 - y];
            }
        }
        
        // Store this grid as the last applied
        _lastAppliedGrid = flippedGrid;
        _lastAppliedWidth = _width;
        _lastAppliedHeight = _height;
        
        gridManager.CreateCustomGrid(flippedGrid);
        
        // Clear highlighted cells after applying
        _highlightedCells.Clear();
        
        EditorUtility.SetDirty(gridManager);
        
        Debug.Log($"Applied custom grid of size {_width}x{_height} to GridManager");
    }
    
    private void DeleteAppliedGrid(GridManager gridManager)
    {
        if (_lastAppliedGrid == null)
        {
            EditorUtility.DisplayDialog("No Grid to Delete", "There is no previously applied custom grid to delete.", "OK");
            return;
        }
        
        Undo.RecordObject(gridManager, "Delete Applied Grid");
        
        // Find the cell container in the scene
        Transform cellContainer = null;
        
        if (gridManager.transform.Find("Cell Container") != null)
        {
            cellContainer = gridManager.transform.Find("Cell Container");
        }
        else if (gridManager.cellContainer != null)
        {
            cellContainer = gridManager.cellContainer;
        }
        
        if (cellContainer != null)
        {
            // Get a list of all child GameObjects to destroy
            List<GameObject> objectsToDestroy = new List<GameObject>();
            for (int i = cellContainer.childCount - 1; i >= 0; i--)
            {
                objectsToDestroy.Add(cellContainer.GetChild(i).gameObject);
            }
            
            // Destroy all the cell GameObjects
            foreach (GameObject obj in objectsToDestroy)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }
        
        // Clear the grid data structure
        gridManager.ClearGrid();
        
        // Reset the last applied grid
        _lastAppliedGrid = null;
        
        // Force scene update
        EditorUtility.SetDirty(gridManager);
        SceneView.RepaintAll();
        
        Debug.Log("Deleted applied grid from GridManager");
    }
    
    private void SaveGridLayout(string fileName, GridManager gridManager)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            EditorUtility.DisplayDialog("Invalid Filename", "Please enter a valid filename", "OK");
            return;
        }
        
        if (gridManager.SaveGridLayout(fileName))
        {
            EditorUtility.DisplayDialog("Grid Saved", $"Grid layout saved as '{fileName}'", "OK");
        }
    }
    
    private void LoadGridLayout(string fileName, GridManager gridManager)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            EditorUtility.DisplayDialog("Invalid Filename", "Please enter a valid filename", "OK");
            return;
        }
        
        if (gridManager.LoadGridLayout(fileName))
        {
            EditorUtility.DisplayDialog("Grid Loaded", $"Grid layout '{fileName}' loaded successfully", "OK");
            // Update width and height to match loaded grid
            _width = gridManager.maxWidth;
            _height = gridManager.maxDepth;
            Repaint();
        }
    }
}
#endif