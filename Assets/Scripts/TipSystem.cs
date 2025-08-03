using System;
using TMPro;
using UnityEngine;

public class TowerMenuSystem : MonoBehaviour
{
    public GameObject towerMenu;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI gardeText;
    public TextMeshProUGUI infoText;
    private void Awake()
    {
        nameText = transform.Find("NameText").GetComponent<TextMeshProUGUI>();
        gardeText = transform.Find("GardeText").GetComponent<TextMeshProUGUI>();
        infoText = transform.Find("InfoText").GetComponent<TextMeshProUGUI>();
    }
    private void OnEnable()
    {
        EventBus.Instance.Subscribe<TowerMenuEventArgs>(OnShow);
        EventBus.Instance.Subscribe<TowerMenuEventArgs>(OnHide);
    }
    private void OnShow(TowerMenuEventArgs obj)
    {
        towerMenu.SetActive(true);
        nameText.text = obj.NameText;
        gardeText.text = obj.GradeText;
        infoText.text = obj.InfoText;
    }
    private void OnHide(TowerMenuEventArgs obj)
    {
        towerMenu.SetActive(false);
    }
    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<TowerMenuEventArgs>(OnShow);
        EventBus.Instance.Unsubscribe<TowerMenuEventArgs>(OnHide);
    }
    
}
internal class TowerMenuEventArgs : EventArgs
{
    public string NameText;
    public string GradeText;
    public string InfoText;

    public TowerMenuEventArgs(string nameText, string gradeText, string infoText)
    {
        NameText = nameText;
        GradeText = gradeText;
        InfoText = infoText;
    }
}
