using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Preview_Click : MonoBehaviour
{
    public string previewShowName;

    
    private void FixedUpdate()
    {
        //点击检测
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("点击了");

            GameObject obj = BaseUtility.GetFirstPickGameObject(Input.mousePosition);
            if (obj != null)
            {
                if (obj.name == previewShowName && obj.TryGetComponent(out RawImageColorController rawImage))
                {
                    rawImage.OnPointerClick(new PointerEventData(EventSystem.current));
                }
            }
        }
    }
}