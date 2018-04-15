using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraControl : MonoBehaviour {
    // PUBLIC
    public GameObject focusObject = null;
    public GameObject cameraCheckPoint;
    public GameObject cameraHolder;
    public GameObject cursor;
    public Text infoText;
    public camMode cameraType = camMode.AxiOnMiddle;
    public float xCamShift = 0;
    public float yCamShift = 0;
    public float zCamShift = 0;
    public float horrCamShift = 0;
    public float angleOfRotation = 0;
    public float angleOfDrop = 60;
    public float maxAngleOfDrop = 85;
    public float angleChange = 1f;
    public float minCamY = 10;
    public float maxCamY = 80;

    public enum camMode
    {
        AxiOnMiddle,
        AxiFixed
    }

    // PRIVATE
    private Vector3 cameraPos;
    private float xAngleChange;


    // START
    void Start ()
    {
        cameraHolder.transform.rotation = Quaternion.Euler(angleOfDrop, angleOfRotation, 0);
        cameraCheckPoint.transform.parent.rotation = Quaternion.Euler(angleOfDrop, angleOfRotation, 0);
    }


    // UPDATE
    void Update ()
    {
        switch (cameraType)
        {
            case camMode.AxiOnMiddle:
                /*gameObject.transform.eulerAngles = new Vector3(angleOfDrop, angleOfRotation, 0);
                Vector3 cursorPos = cursor.transform.position;
                cameraPos += cursorPos;
                cameraPos.Scale(new Vector3(0.5F, 0, 0.5F));
                float vecDiff = (cameraPos - FocusPos).magnitude;
                cameraPos.y = vecDiff + 10;
                cameraPos.z += -20 - vecDiff;*/
                break;
            case camMode.AxiFixed:
                if (Input.GetAxis("Mouse ScrollWheel") < 0f)
                {
                    gameObject.GetComponent<Camera>().fieldOfView += 1;
                }

                if (Input.GetAxis("Mouse ScrollWheel") > 0f)
                {
                    gameObject.GetComponent<Camera>().fieldOfView -= 1;
                }

                RaycastHit hitInfo;
                if (Physics.Raycast(cameraCheckPoint.transform.position, cameraCheckPoint.transform.forward, out hitInfo))
                {
                    Transform hitTrans = hitInfo.collider.transform;
                    // SEARCHING FOR TOP TRANSFORM
                    while (hitTrans.parent != null)
                    {
                        hitTrans = hitTrans.parent;
                    }

                    xAngleChange = 0;
                    // CHECKING IF IT IS FOCUSOBJECT
                    if (hitTrans.gameObject != focusObject)
                    {
                        // ROTATING CAMERA UP TO MAXDROPANGLE
                        if (cameraHolder.transform.rotation.eulerAngles.x < maxAngleOfDrop - angleChange)
                        {
                            xAngleChange = angleChange;
                        }                             
                    }
                    else
                    {
                        // ROTATING CAMERA BACK TO DROPANGLE
                        if (cameraHolder.transform.rotation.eulerAngles.x > angleOfDrop + angleChange)
                        {
                            xAngleChange = -angleChange;
                        }
                    }
                }

                break;
            /*case camMode.ThirdPerson:
                Vector3 playerAngles = playerMech.transform.eulerAngles;
                gameObject.transform.eulerAngles = new Vector3(angleOfDrop, playerAngles.x, 0);
                cameraPos = -playerMech.transform.forward.normalized * zCamShift;
                cameraPos.y += yCamShift;
                break;*/
        }
    }


    // FIXED UPDATE
    void FixedUpdate()
    {
        if (focusObject != null)
        {
            cameraHolder.transform.parent.position = focusObject.transform.position;
            if (xAngleChange != 0)
            {
                Vector3 tmpAngles = cameraHolder.transform.rotation.eulerAngles;
                tmpAngles.x += xAngleChange;
                cameraHolder.transform.rotation = Quaternion.Euler(tmpAngles);
            }
        }
    }
}
