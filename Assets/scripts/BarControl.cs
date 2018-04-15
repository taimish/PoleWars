using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BarControl : MonoBehaviour
{
    public Image backGround;
    public Image foreGround;
    public float maxValue = 1;
    public float currValue = 0.5f;
    public int barWidth = 50;
    public int barHeight = 8;
    public byte transparency = 200;
    public Color32 frameCol = new Color32(73, 73, 73, 200);
    public Color32 backGroundCol = new Color32(0, 22, 101, 200);
    public Color32 foreGroundCol = new Color32(0, 23, 255, 200);

	// START
	void Start ()
    {
        gameObject.GetComponent<Image>().color = frameCol;
        backGround.GetComponent<Image>().color = backGroundCol;
        foreGround.GetComponent<Image>().color = foreGroundCol;

        if (barHeight < 6)
            barHeight = 6;
	}
	
	// UPDATE
	void Update ()
    {
        foreGround.transform.localScale = new Vector3((currValue / maxValue), 1, 1);
	}
}
