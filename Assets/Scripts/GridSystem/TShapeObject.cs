using UnityEngine;

public class TShapeObject : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GridObject gridObj = GetComponent<GridObject>();
        int[,] zigzagLayout = new int[3, 3] {
            { 1, 1, 1 },
            { 0, 1, 0 },
            { 0, 1, 0 }
        };
        gridObj.SetObjectLayout(zigzagLayout, new Vector2Int(1, 1)); 
    }
}
