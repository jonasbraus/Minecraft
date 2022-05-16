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
        //change later:
        for (int i = 0; i < 9; i++)
        {
            itemSlots[i].sprite = itemIcons[i];
            hotBarItems[i] = (byte)(i + 1);
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
