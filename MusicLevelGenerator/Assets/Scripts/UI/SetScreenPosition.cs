using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetScreenPosition : MonoBehaviour
{
    [SerializeField] Canvas canvas;
    [SerializeField] Transform target;

    bool initialised = false;

    private void Start()
    {
        RectTransform rectTransform = this.GetComponent<RectTransform>();
        Vector2 position = this.GetComponent<RectTransform>().anchoredPosition;
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(target.position);

        position.y = screenPoint.y;

        rectTransform.anchoredPosition = position;

        initialised = true;
    }
}
