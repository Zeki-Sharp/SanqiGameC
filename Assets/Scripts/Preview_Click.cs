using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Preview_Click : MonoBehaviour
{
    [SerializeField]private string previewShowName;
    [SerializeField]private GameObject previewArea;
    [SerializeField]private bool hasClick = false;
    [SerializeField]private GameMap gameMap;
    
    void Start()
    {
        gameMap = GameObject.Find("GameMap").GetComponent<GameMap>();
    }
    
    private void Update()
    {
        //点击检测
        if (Input.GetMouseButtonDown(0) && !hasClick)
        {
            Debug.Log("点击了");

            GameObject obj = BaseUtility.GetFirstPickGameObject(Input.mousePosition);
            if (obj != null)
            {
                if (obj.name == previewShowName && obj.TryGetComponent(out RawImageColorController rawImage))
                {
                    rawImage.OnPointerClick(new PointerEventData(EventSystem.current));
                    hasClick = true;
                }
            }
        }

        if (hasClick)
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mousePos);
            mouseWorldPos.z = 0;
            
        }
    }
}