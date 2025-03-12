using UnityEngine;

public class GridDragAndDropManager : MonoBehaviour
{
    public Camera mainCamera;
    public Material highlightMaterial;
    public Material validPositionMaterial;
    public Material invalidPositionMaterial;
    public GridManager gridManager;

    private GameObject selectedObject;
    private GameObject previewObject;
    private Material originalMaterial;
    private bool isDragging = false;
    private Vector2Int originalGridPosition;
    private Vector2Int objectSize;

    void Update()
    {
        if (isDragging)
        {
            UpdateDraggingPosition();
            if (Input.GetMouseButtonUp(0))
            {
                FinalizePlacement();
            }
        }
        else
        {
            TrySelectObject();
        }
    }

    void TrySelectObject()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            int layerMask = LayerMask.GetMask("Objects");
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                selectedObject = hit.collider.gameObject;
                var gridPosition = gridManager.GetGridPosition(selectedObject.transform.position);
                originalGridPosition = new Vector2Int(gridPosition.x, gridPosition.z);
                GridObject gridObject = selectedObject.GetComponent<GridObject>();
                if (gridObject != null)
                {
                    objectSize = new Vector2Int(gridObject.width, gridObject.depth);
                    StartDragging();
                }
                else
                {
                    selectedObject = null;
                }
            }
        }
    }
    
    void StartDragging()
    {
        isDragging = true;

        Renderer renderer = selectedObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            originalMaterial = renderer.material;
            renderer.material = highlightMaterial;
        }

        previewObject = GameObject.Instantiate(selectedObject);
        previewObject.SetActive(true);

        Renderer previewRenderer = previewObject.GetComponent<Renderer>();
        if (previewRenderer != null)
        {
            Material previewMat = new Material(validPositionMaterial);
            previewRenderer.material = previewMat;
        }

        Collider previewCollider = previewObject.GetComponent<Collider>();
        if (previewCollider != null)
        {
            previewCollider.enabled = false;
        }

        gridManager.RemoveObject(selectedObject, originalGridPosition.x, originalGridPosition.y, objectSize.x, objectSize.y);
    }

    public void UpdateDraggingPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, gridManager.gridOrigin.y, 0));
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);
            var gridPosition = gridManager.GetGridPosition(worldPoint);
            int gridX = gridPosition.x;
            int gridZ = gridPosition.z;

            bool isValidPosition = gridManager.IsValidPlacement(gridX, gridZ, objectSize.x, objectSize.y);

            Renderer previewRenderer = previewObject.GetComponent<Renderer>();
            if (previewRenderer != null)
            {
                previewRenderer.material = isValidPosition ? validPositionMaterial : invalidPositionMaterial;
            }

            Vector3 centerPosition = CalculateCenterPosition(gridX, gridZ, objectSize);
            previewObject.transform.position = centerPosition;

            // Change color of objects being hovered over
            for (int dx = 0; dx < objectSize.x; dx++)
            {
                for (int dz = 0; dz < objectSize.y; dz++)
                {
                    int checkX = gridX + dx;
                    int checkZ = gridZ + dz;

                    if (checkX >= 0 && checkX < gridManager.width && checkZ >= 0 && checkZ < gridManager.depth)
                    {
                        GameObject hoveredObject = gridManager.gridData[checkX, checkZ];
                        if (hoveredObject != null)
                        {
                            Renderer hoveredRenderer = hoveredObject.GetComponent<Renderer>();
                            if (hoveredRenderer != null)
                            {
                                hoveredRenderer.material.color = Color.red;
                            }
                        }
                    }
                }
            }

            gridManager.DrawDebugGrid();
        }
    }

    void FinalizePlacement()
    {
        var gridPosition = gridManager.GetGridPosition(previewObject.transform.position);
        int gridX = gridPosition.x;
        int gridZ = gridPosition.z;

        if (gridManager.IsValidPlacement(gridX, gridZ, objectSize.x, objectSize.y))
        {
            gridManager.PlaceObject(selectedObject, gridX, gridZ, objectSize.x, objectSize.y);
        }
        else
        {
            gridManager.PlaceObject(selectedObject, originalGridPosition.x, originalGridPosition.y, objectSize.x, objectSize.y);
        }

        Renderer renderer = selectedObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = originalMaterial;
        }

        Destroy(previewObject);

        isDragging = false;
        selectedObject = null;
    }

    Vector3 CalculateCenterPosition(int gridX, int gridZ, Vector2Int size)
    {
        GridObject gridObject = selectedObject.GetComponent<GridObject>();
        Vector3 offset = gridObject.GetCenterOffset(gridManager.cellSize);
        Vector3 centerPosition = gridManager.GetWorldPosition(gridX, gridZ) + offset + new Vector3((size.x - 1) * gridManager.cellSize / 2, 0, (size.y - 1) * gridManager.cellSize / 2);
        return centerPosition;
    }
}