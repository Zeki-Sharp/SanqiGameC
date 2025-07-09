using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class TMPSetSortingLayer : MonoBehaviour
{
    public string sortingLayerName = "Default";
    public int sortingOrder = 5;

    void Start()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.sortingLayerName = sortingLayerName;
        renderer.sortingOrder = sortingOrder;
    }
}