using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CursorControl : MonoBehaviour {
    public Camera mainCamera;
    public Text infoText;
    public float verticalCursorShift = 0;

    private Vector3 previousPos;
    private bool cursorEnabled = true;

    // START
    void Start ()
    {
        previousPos = new Vector3(0, 10, 0);
    }
	
	// UPDATE
	void Update ()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            ChangeCursorState();

        Vector3 mousePos = Input.mousePosition;
        Vector3 worldPos;
        Ray mouseRay = mainCamera.ScreenPointToRay(mousePos);
        RaycastHit mouseRayResult;
        if (cursorEnabled && Physics.Raycast(mouseRay, out mouseRayResult))
        {
            worldPos = mouseRayResult.point + new Vector3(0, verticalCursorShift, 0);
            previousPos = worldPos;
        }
        else
            worldPos = previousPos;

        gameObject.transform.position = worldPos;
    }

    void ChangeCursorState()
    {
        if (cursorEnabled)
        {
            cursorEnabled = false;
            //Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            cursorEnabled = true;
            //Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
