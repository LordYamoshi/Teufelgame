using UnityEngine;

public class GridManager : MonoBehaviour
{

  [Header("Grid Settings")]
    public int width = 10;
    public int depth = 10;
    public float cellSize = 1.0f;
    public Vector3 gridOrigin = Vector3.zero;

    [Header("Visualization")]
    public bool showDebugLines = true;
    public Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    public float lineDrawDuration = 0f;

    [Header("Grid Data")]
    private GameObject[,] gridData;

    void Start()
    {
        MoveGrid(new Vector3(0,0,0));
        InitializeGrid();;
    }

    void Update()
    {
        if (showDebugLines)
        {
            DrawDebugGrid();
        }
    }

    void InitializeGrid()
    {
        gridData = new GameObject[width, depth];
    }

    public void ResizeGrid(int newWidth, int newDepth)
    {
        width = newWidth;
        depth = newDepth;
        InitializeGrid();
    }

    public void MoveGrid(Vector3 newOrigin)
    {
        gridOrigin = newOrigin;
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

    public GameObject GetObjectAt(int x, int z)
    {
        if (x >= 0 && x < width && z >= 0 && z < depth)
        {
            return gridData[x, z];
        }
        return null;
    }

    public void SetObjectAt(int x, int z, GameObject obj)
    {
        if (x >= 0 && x < width && z >= 0 && z < depth)
        {
            gridData[x, z] = obj;

            if (obj != null)
            {
                obj.transform.position = GetWorldPosition(x, z);
            }
        }
    }

    private void DrawDebugGrid()
    {
        float offsetX = -width * cellSize / 2;
        float offsetZ = -depth * cellSize / 2;

        for (int x = 0; x <= width; x++)
        {
            Debug.DrawLine(
                new Vector3(offsetX + x * cellSize, 0, offsetZ) + gridOrigin,
                new Vector3(offsetX + x * cellSize, 0, offsetZ + depth * cellSize) + gridOrigin,
                gridColor,
                lineDrawDuration
            );
        }

        for (int z = 0; z <= depth; z++)
        {
            Debug.DrawLine(
                new Vector3(offsetX, 0, offsetZ + z * cellSize) + gridOrigin,
                new Vector3(offsetX + width * cellSize, 0, offsetZ + z * cellSize) + gridOrigin,
                gridColor,
                lineDrawDuration
            );
        }
    }
}