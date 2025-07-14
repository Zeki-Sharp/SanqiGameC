using UnityEngine;

public class SaveTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 获取服务实例
        var saveService = SaveService.Instance;

// 保存数据
        saveService.Save("123", "Save_Data_1.sav");

// 加载数据
        string data = saveService.Load("Save_Data_1.sav", "default");
        Debug.Log(data);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
