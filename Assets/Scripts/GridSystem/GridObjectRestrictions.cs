using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Extension class for applying restrictions to grid objects
/// </summary>
public static class GridObjectRestrictions
{
    /// <summary>
    /// Restriction types for grid objects
    /// </summary>
    public enum RestrictionType
    {
        None,           // No restrictions (movable and destructible)
        Immovable,      // Cannot be moved, but can be destroyed
        DestroyOnly,    // Cannot be moved, can only be destroyed
        Permanent       // Cannot be moved or destroyed
    }
    
    /// <summary>
    /// Applies a restriction type to a grid object
    /// </summary>
    /// <param name="gridObject">The grid object to restrict</param>
    /// <param name="restrictionType">Type of restriction to apply</param>
    public static void ApplyRestriction(GridObject gridObject, RestrictionType restrictionType)
    {
        if (gridObject == null) return;
        
        switch (restrictionType)
        {
            case RestrictionType.None:
                // Default - movable and destructible
                gridObject.isMovable = true;
                gridObject.isDestructible = true;
                break;
                
            case RestrictionType.Immovable:
                // Can't be moved, but can be destroyed
                gridObject.isMovable = false;
                gridObject.isDestructible = true;
                break;
                
            case RestrictionType.DestroyOnly:
                // Can only be destroyed, not moved
                gridObject.isMovable = false;
                gridObject.isDestructible = true;
                
                // Add a visual indicator to show it's destroy-only
                AddRestrictionVisual(gridObject.gameObject, Color.red);
                break;
                
            case RestrictionType.Permanent:
                // Can't be moved or destroyed
                gridObject.isMovable = false;
                gridObject.isDestructible = false;
                
                // Add a visual indicator to show it's permanent
                AddRestrictionVisual(gridObject.gameObject, Color.gray);
                break;
        }
        
        // Tag the object so we know it's a grid object
        gridObject.gameObject.tag = "GridObject";
    }
    
    /// <summary>
    /// Adds a visual indicator to show restriction status
    /// </summary>
    private static void AddRestrictionVisual(GameObject obj, Color color)
    {
        // Check if we already have a restriction visual
        Transform indicator = obj.transform.Find("RestrictionIndicator");
        if (indicator != null) return;
        
        // Create a small indicator object
        GameObject indicatorObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicatorObj.name = "RestrictionIndicator";
        indicatorObj.transform.SetParent(obj.transform);
        indicatorObj.transform.localPosition = Vector3.up * 0.5f;
        indicatorObj.transform.localScale = Vector3.one * 0.2f;
        
        // Add a material with the restriction color
        Renderer renderer = indicatorObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Metallic", 0.8f);
            mat.SetFloat("_Glossiness", 0.8f);
            renderer.material = mat;
        }
        
        // Remove collider to avoid interference
        Collider collider = indicatorObj.GetComponent<Collider>();
        if (collider != null)
        {
            Object.Destroy(collider);
        }
    }
    
    /// <summary>
    /// Batch applies restrictions to multiple grid objects
    /// </summary>
    /// <param name="gridObjects">List of grid objects to restrict</param>
    /// <param name="restrictionType">Type of restriction to apply</param>
    public static void BatchApplyRestrictions(List<GridObject> gridObjects, RestrictionType restrictionType)
    {
        if (gridObjects == null) return;
        
        foreach (var gridObject in gridObjects)
        {
            ApplyRestriction(gridObject, restrictionType);
        }
    }
    
    /// <summary>
    /// Gets all grid objects in the scene and applies a restriction to them
    /// </summary>
    /// <param name="restrictionType">Type of restriction to apply</param>
    public static void ApplyRestrictionToAllInScene(RestrictionType restrictionType)
    {
        GridObject[] allGridObjects = Object.FindObjectsOfType<GridObject>();
        foreach (var gridObject in allGridObjects)
        {
            ApplyRestriction(gridObject, restrictionType);
        }
    }
    
    /// <summary>
    /// Converts a restriction type to a user-friendly string
    /// </summary>
    public static string GetRestrictionTypeName(RestrictionType restrictionType)
    {
        switch (restrictionType)
        {
            case RestrictionType.None:
                return "Normal (Movable & Destructible)";
            case RestrictionType.Immovable:
                return "Immovable (Can Be Destroyed)";
            case RestrictionType.DestroyOnly:
                return "Destroy-Only (Cannot Move)";
            case RestrictionType.Permanent:
                return "Permanent (Cannot Move or Destroy)";
            default:
                return restrictionType.ToString();
        }
    }
}