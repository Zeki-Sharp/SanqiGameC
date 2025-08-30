using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverTextChanger  : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private string text1;
    [SerializeField] private string text2;
    [SerializeField] private bool showTitle = true;

    [SerializeField] private Button button;
    [SerializeField] private string ActionName;
    [SerializeField] private MonoBehaviour target;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (buttonText == null)
        {
           buttonText = GetComponentInChildren<TMP_Text>();
        }
        if (buttonText != null)
        {
           buttonText.text = showTitle ? text1 : text2;
        }
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() =>
            {
                target.SendMessage(ActionName);
            });
        }
    }
    
    public void Initialize(string title, string content,bool showTitle)
    {
        this.text1 = title;
        this.showTitle = showTitle;
        this.text2 = content;
        buttonText.text = showTitle ? title : content;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (showTitle)
        {
            return;
        }
        buttonText.text = text2;
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!showTitle)
        {
            return;
        }
       buttonText.text = text1;
    }
}