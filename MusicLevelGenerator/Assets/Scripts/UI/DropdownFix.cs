using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropdownFix : MonoBehaviour
{
    public string sortingLayer = "Visualiser";

    public void OnDropDownClicked()
    {
        Transform droplist = transform.Find("Dropdown List");

        if (droplist != null)
        {
            droplist.GetComponent<Canvas>().sortingLayerName = sortingLayer;
        }
    }
}
