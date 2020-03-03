using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetScreenPosition : MonoBehaviour
{
    [SerializeField] Canvas canvas;
    [SerializeField] Transform target;

    private void Start()
    {
        RectTransform rectTransform = this.GetComponent<RectTransform>();
        Vector3 position = this.GetComponent<RectTransform>().position;
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(target.position);

        position.y *= canvas.pixelRect.height / (float)Camera.main.pixelHeight;

        rectTransform.position = position;
    }
}
