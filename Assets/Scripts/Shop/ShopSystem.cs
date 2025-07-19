using System;
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
   [SerializeField] private TextMeshProUGUI moneyText;
   [SerializeField] private GameMap gameMap;
   public void Awake()
   {
       // moneyText = GetComponent<TextMeshProUGUI>();
   }
   public void Initialize(MapConfig mapConfig,DifficultyLevel level)
   {
       MapData mapData = mapConfig.GetMapData(level);
         money = mapData.StartingMoney;
   }

   private void Start()
   {
       gameMap = FindFirstObjectByType<GameMap>();
       money = gameMap.GetMapData().StartingMoney;
       moneyText.text = money.ToString();
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
