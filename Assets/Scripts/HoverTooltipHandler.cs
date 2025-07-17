using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverTextChanger  : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private string title;
    [SerializeField] private string content;
    [SerializeField] private bool showTitle = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (buttonText == null)
        {
           buttonText = GetComponentInChildren<TextMeshProUGUI>();
        }
        if (buttonText != null)
        {
           buttonText.text = showTitle ? title : content;
        }
    }
    
    public void Initialize(string title, string content,bool showTitle)
    {
        this.title = title;
        this.showTitle = showTitle;
        this.content = content;
        buttonText.text = showTitle ? title : content;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (showTitle)
        {
            return;
        }
        buttonText.text = content;
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (showTitle)
        {
            return;
        }
       buttonText.text = title;
    }
}