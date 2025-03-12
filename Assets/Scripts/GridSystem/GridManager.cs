using System;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 10;
    public int depth = 10;
    public float cellSize = 1.0f;
    public Vector3 gridOrigin = Vector3.zero;

    [Header("Visualization")]
    public Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    public Color validPlacementColor = Color.green;
    public Color invalidPlacementColor = Color.red;

    [Header("Grid Data")]
    public GameObject[,] gridData;
    private GameObject[,] cellPrefabs;

    public GameObject cellPrefab;

    void Start()
    {
        InitializeGrid();
        gridOrigin = new Vector3(-1.29f, 4.3f, -3.8f);
    }

    private void Update()
    {
        DrawDebugGrid();
    }

    void InitializeGrid()
    {
        gridData = new GameObject[width, depth];
        cellPrefabs = new GameObject[width, depth];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector3 cellCenter = GetWorldPosition(x, z);
                cellPrefabs[x, z] = Instantiate(cellPrefab, cellCenter, Quaternion.identity, transform);
            }
        }
    }

    public Vector3 GetWorldPosition(int x, int z)
    {
        float offsetX = -width * cellSize / 2;
        float offsetZ = -depth * cellSize / 2;

        return new Vector3(
            offsetX + x * cellSize + cellSize / 2,
            0,
            offsetZ + z * cellSize + cellSize / 2
        ) + gridOrigin;
    }

    public (int x, int z) GetGridPosition(Vector3 worldPosition)
    {
        float offsetX = -width * cellSize / 2;
        float offsetZ = -depth * cellSize / 2;

        Vector3 localPosition = worldPosition - gridOrigin;

        int x = Mathf.FloorToInt((localPosition.x - offsetX) / cellSize);
        int z = Mathf.FloorToInt((localPosition.z - offsetZ) / cellSize);

        return (x, z);
    }

    public bool IsValidPlacement(int x, int z, int objWidth, int objDepth)
    {
        for (int dx = 0; dx < objWidth; dx++)
        {
            for (int dz = 0; dz < objDepth; dz++)
            {
                int checkX = x + dx;
                int checkZ = z + dz;

                if (checkX < 0 || checkX >= width || checkZ < 0 || checkZ >= depth || gridData[checkX, checkZ] != null)
                {
                    Debug.Log($"Invalid placement at ({checkX}, {checkZ})");
                    return false;
                }
            }
        }
        return true;
    }

    public void PlaceObject(GameObject obj, int x, int z, int objWidth, int objDepth)
    {
        for (int dx = 0; dx < objWidth; dx++)
        {
            for (int dz = 0; dz < objDepth; dz++)
            {
                gridData[x + dx, z + dz] = obj;
            }
        }

        Vector3 firstCellPosition = GetWorldPosition(x, z);
        obj.transform.position = firstCellPosition;
    }
    
    public void RemoveObject(GameObject obj, int x, int z, int objWidth, int objDepth)
    {
        for (int dx = 0; dx < objWidth; dx++)
        {
            for (int dz = 0; dz < objDepth; dz++)
            {
                int checkX = x + dx;
                int checkZ = z + dz;

                if (checkX >= 0 && checkX < width && checkZ >= 0 && checkZ < depth)
                {
                    if (gridData[checkX, checkZ] == obj)
                    {
                        gridData[checkX, checkZ] = null;
                    }
                }
            }
        }
    }

    public void DrawDebugGrid()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                bool isValidPlacement = IsValidPlacement(x, z, 1, 1);
                Color drawColor = isValidPlacement ? validPlacementColor : invalidPlacementColor;
                Vector3 cellCenter = GetWorldPosition(x, z);
                InstantiateCellPrefab(cellCenter, drawColor);
            }
        }
    }

    private void InstantiateCellPrefab(Vector3 position, Color color)
    {
        GameObject cell = Instantiate(cellPrefab, position, Quaternion.identity, transform);
        Renderer renderer = cell.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }
}