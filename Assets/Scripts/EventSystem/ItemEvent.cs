using System;
using UnityEngine;

public class ItemEvent : EventArgs
{
    private Item item;
    
    public ItemEvent(Item item)
    {
        this.item = item;
    }
    
    public ItemConfig ItemConfig => item?.ItemConfig;
}