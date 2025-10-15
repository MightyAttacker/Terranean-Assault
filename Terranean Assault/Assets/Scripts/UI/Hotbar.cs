using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Needed for click detection

public class Hotbar : MonoBehaviour
{
    public Image[] slots;        // UI Image components for the slots
    public Sprite[] itemSprites; // Sprites from your project assets

    private int selectedSlot = 0;

    void Start()
    {
        // Add click listeners to each slot
        for (int i = 0; i < slots.Length; i++)
        {
            int index = i; // Capture the current index for the listener
            Button button = slots[i].GetComponent<Button>();
            if (button == null)
            {
                // Add a Button component if one isn't already present
                button = slots[i].gameObject.AddComponent<Button>();
            }
            button.onClick.AddListener(() => SelectSlot(index));
        }
    }

    void Update()
    {
        // Allow selecting slots with number keys (1–9)
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
            slots[i].color = (i == index) ? Color.cyan : Color.white;
        }
    }
}
