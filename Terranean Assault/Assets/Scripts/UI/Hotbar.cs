using UnityEngine;
using UnityEngine.UI;

public class Hotbar : MonoBehaviour
{
    public Image[] slots;        // UI Image components for the slots
    public Sprite[] itemSprites; // Sprites from your project assets

    private int selectedSlot = 0;

    void Start()
    {
        // Example: fill slots with sprites from your assets
        for (int i = 0; i < slots.Length && i < itemSprites.Length; i++)
        {
            slots[i].sprite = itemSprites[i];
        }

        HighlightSlot(selectedSlot);
    }

    void Update()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSlot(i);
            }
        }
    }

    void SelectSlot(int index)
    {
        selectedSlot = index;
        HighlightSlot(index);
        Debug.Log("Selected slot: " + (index + 1));
    }

    void HighlightSlot(int index)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].color = (i == index) ? Color.yellow : Color.white;
        }
    }
}
