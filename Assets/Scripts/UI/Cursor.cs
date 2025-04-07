using UnityEngine;
using UnityEngine.UI;

public class CustomCursor : MonoBehaviour
{
    public enum CursorAction
    {
        Standard,
        Destructable
        //Grabbable,
        //Grabbed,
    }

    //Vector2 position_offset = new Vector2(40f, -40f);

    [SerializeField] Sprite standard;
    [SerializeField] Sprite destructable;
    //[SerializeField] Sprite grabbable;
    //[SerializeField] Sprite grabbed;

    [SerializeField] Image image;

    public RectTransform uiElement;  // Reference to the UI element's RectTransform
    private void Awake()
    {
        Cursor.visible = false;
    }

    void Start()
    {
        //image.sprite = destructable;
        image.sprite = standard;
    }

    void LateUpdate()
    {
        // Get the mouse position in screen space (pixel coordinates)
        Vector3 mousePosition = Input.mousePosition;

        // Convert the screen position to a canvas-local position
        Vector2 localPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            uiElement.parent.GetComponent<RectTransform>(),
            mousePosition,
            null,
            out localPosition);
        
        // Set the UI element's position to the mouse position
        uiElement.localPosition = localPosition; // + position_offset;
    }
    
    public void SetCursorAction(CursorAction cursorAction)
    {
        if (cursorAction == CursorAction.Standard)
        {
            image.sprite = standard;
        }
        else if (cursorAction == CursorAction.Destructable)
        {
            image.sprite = destructable;
        }
        //if (cursorAction == CursorAction.Grabbable)
        //{
        //    image.sprite = grabbable;
        //}
        //if (cursorAction == CursorAction.Grabbed)
        //{
        //    image.sprite = grabbed;
        //}
    }
}
