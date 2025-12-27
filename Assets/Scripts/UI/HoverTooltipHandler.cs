using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverTextChanger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private string text1;
    [SerializeField] private string text2;
    [SerializeField] private bool showTitle = true;

    [SerializeField] private Button button;
    [SerializeField] private string ActionName;
    [SerializeField] private MonoBehaviour target;

    // FIX: Bind early to avoid Initialize() before Start() null refs
    private void Awake()
    {
        if (buttonText == null)
            buttonText = GetComponentInChildren<TMP_Text>(true); // include inactive children

        if (button == null)
            button = GetComponent<Button>();
    }

    private void Start()
    {
        if (buttonText != null)
        {
            buttonText.text = showTitle ? text1 : text2;
        }

        // FIX: Guard for null target or empty ActionName
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (target != null && !string.IsNullOrEmpty(ActionName))
                {
                    target.SendMessage(ActionName, SendMessageOptions.DontRequireReceiver);
                }
                else
                {
                    Debug.LogWarning("[HoverTextChanger] Click ignored: target or ActionName not set.", this);
                }
            });
        }
    }

    public void Initialize(string title, string content, bool showTitle)
    {
        // FIX: Lazy rebind in case Awake was skipped or refs lost
        if (buttonText == null)
            buttonText = GetComponentInChildren<TMP_Text>(true);

        this.text1 = title;
        this.text2 = content;
        this.showTitle = showTitle;

        if (buttonText == null)
        {
            Debug.LogError("[HoverTextChanger] buttonText is null in Initialize.", this);
            return;
        }

        buttonText.text = showTitle ? title : content;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buttonText == null) return;                   // FIX
        if (string.IsNullOrEmpty(text2)) return;          // FIX
        buttonText.text = text2;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (buttonText == null) return;                   // FIX
        if (string.IsNullOrEmpty(text1)) return;          // FIX
        buttonText.text = text1;
    }
}
