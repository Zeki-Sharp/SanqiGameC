using UnityEngine;

public class ItemManage : MonoBehaviour
{
    public GameMap map;
    public ItemGeneratorConfig itemGenerator;
    public GameObject itemPrefab;
    public GameObject itemArea;
    private int itemLimitCount = 5;
    public void GenerateItem()
    {
        ItemConfig itemConfig = itemGenerator.GetRandomItem();
        if (itemConfig == null)
        {
            return;
        }

        if (itemArea !=null)
        {
            GameObject item = Instantiate(itemPrefab, itemArea.transform);
            item.GetComponent<Item>().SetItem(itemConfig);
            itemConfig.Init();
        }
    }
    void Awake()
    {
        if (itemPrefab == null)
        {
            itemPrefab = Resources.Load<GameObject>("Prefab/Item/Item");
        }

        if (map == null)
        {
            map = GameMap.instance;
        }
    }

    void Start()
    {
        itemGenerator = map.GetMapConfig().itemGeneratorConfig;
        itemLimitCount = map.GetMapData().itemLimitCount;
        ShowItem();
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void ShowItem()
    {
        for (int i = itemArea.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(itemArea.transform.GetChild(i).gameObject);
        }
        for (int i = 0; i < itemLimitCount; i++)
        {
            GenerateItem();
        }
    }
}