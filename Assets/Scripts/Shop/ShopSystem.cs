using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class ShopSystem : MonoBehaviour
{
   private int money;
   [ShowInInspector]public int Money
   {
       get { return money; }
   }
   [ShowInInspector] private TextMeshProUGUI moneyText;
   public void Awake()
   {
       // moneyText = GetComponent<TextMeshProUGUI>();
   }
   public void Initialize(MapConfig mapConfig,DifficultyLevel level)
   {
       MapData mapData = mapConfig.GetMapData(level);
         money = mapData.StartingMoney;
   }
   
   public bool CanAfford(int amount)
   {
       return money >= amount;
   }
   
   public void SpendMoney(int amount)
   {
       money -= amount;
   }

   public void AddMoney(int amount)
   {
       money += amount;
   }
   
}
