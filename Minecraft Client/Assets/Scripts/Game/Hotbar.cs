using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Hotbar : MonoBehaviour
{
    [SerializeField] private Image[] itemSlots;
    [SerializeField] private Sprite[] itemIcons; //id always: -1 to get item icon
    [SerializeField] private GameObject highlight;

    private byte[] hotBarItems = new byte[9];
    private byte selectedSlot = 0;
    
    private void Start()
    {
        hotBarItems[0] = 10;
        hotBarItems[1] = 11;
        hotBarItems[2] = 3;
        hotBarItems[3] = 4;
        hotBarItems[4] = 5;
        hotBarItems[5] = 2;
        hotBarItems[6] = 7;
        hotBarItems[7] = 8;
        hotBarItems[8] = 9;
            
        for (int i = 0; i < 9; i++)
        {
            itemSlots[i].sprite = itemIcons[hotBarItems[i] - 1];
        }
    }

    public void SelectSlot(byte idx)
    {
        selectedSlot = idx;
        highlight.transform.position = itemSlots[idx].transform.position;
    }

    public byte GetSelectedSlot()
    {
        return selectedSlot;
    }

    public byte GetSelectedItemID()
    {
        return hotBarItems[selectedSlot];
    }
}
