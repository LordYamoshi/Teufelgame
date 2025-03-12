using UnityEngine;

public class GridObject : MonoBehaviour
{
    [Header("Grid Properties")]
    [Tooltip("Width of the object in grid cells")]
    public int width = 1;
    
    [Tooltip("Depth of the object in grid cells")]
    public int depth = 1;
    
    [Header("Placement Visualization")]
    [Tooltip("Material to show when the object can be validly placed")]
    public Material validPlacementMaterial;
    
    [Tooltip("Material to show when the object cannot be placed here")]
    public Material invalidPlacementMaterial;
    
    [Tooltip("Material used when the object is selected")]
    public Material selectedMaterial;
    
    private Material[] originalMaterials;
    private Renderer[] objectRenderers;
    
    
    void Awake()
    {
        objectRenderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[objectRenderers.Length];
        
        for (int i = 0; i < objectRenderers.Length; i++)
        {
            originalMaterials[i] = objectRenderers[i].material;
        }

    }
    
    public Vector3 GetCenterOffset(float cellSize)
    {
        float xOffset = (width - 1) * cellSize / 2f;
        float zOffset = (depth - 1) * cellSize / 2f;
        return new Vector3(xOffset, 0, zOffset);
    }
    
}