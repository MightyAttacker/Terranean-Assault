using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Hotbar : MonoBehaviour
{
    [Header("Images")]
    public Image[] Images;

    [Header("Current Units")]
    public GameObject[] itemPrefabs;

    [Header("Slots")]
    public Image[] slots;

    private int selectedSlot = -1; // No slot selected at start
    private GameObject currentGhost; // The ghost following the cursor
    private Camera mainCam;

    //Blank Object
    public GameObject Blank;

    //Legions Imperius Units
    public GameObject Engineer;
    public GameObject HeavyWeapons;
    public GameObject Medic;
    public GameObject Scout;
    public GameObject Sniper;
    public GameObject Soldier;
    public GameObject Tank;

    //Mechanised Commonwealth
    public GameObject AssaultLeader;
    public GameObject AssaultSargent;
    public GameObject AssaultSquad;
    public GameObject AssaultTransport;

    void Start()
    {
        mainCam = Camera.main;

        itemPrefabs = new GameObject[Images.Length];
        itemPrefabs[0] = Engineer;
        itemPrefabs[1] = HeavyWeapons;
        itemPrefabs[2] = Medic;
        itemPrefabs[3] = Scout;
        itemPrefabs[4] = Sniper;
        itemPrefabs[5] = Soldier;
        itemPrefabs[6] = Tank;
        itemPrefabs[7] = Tank;
        itemPrefabs[8] = Blank;

        // Update UI to show the prefab sprites
        UpdateHotbarSprites();

        // Add click listeners for each slot
        for (int i = 0; i < Images.Length; i++)
        {
            int index = i;
            Button button = Images[i].GetComponent<Button>();
            if (button == null)
                button = Images[i].gameObject.AddComponent<Button>();

            button.onClick.AddListener(() => SelectSlot(index));
        }

        HighlightSlot(-1);
    }

    void Update()
    {
        // Select slot with number keys (1–9)
        for (int i = 0; i < Images.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSlot(i);
            }
        }

        // Update ghost position
        if (currentGhost != null)
        {
            Vector3 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;

            // Snap to grid
            mouseWorld.x = Mathf.Round(mouseWorld.x);
            mouseWorld.y = Mathf.Round(mouseWorld.y);

            currentGhost.transform.position = mouseWorld;

            // Place prefab on left click (ignoring UI)
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                PlaceItem(mouseWorld);
            }

            // Right-click to cancel placement
            if (Input.GetMouseButtonDown(1))
            {
                DeselectSlot();
            }
        }
    }

    void SelectSlot(int index)
    {
        // Deselect if same slot clicked again
        if (selectedSlot == index)
        {
            DeselectSlot();
            return;
        }

        selectedSlot = index;
        HighlightSlot(index);

        // Remove old ghost
        if (currentGhost != null)
            Destroy(currentGhost);

        // Spawn new ghost if prefab exists
        if (itemPrefabs != null && index < itemPrefabs.Length && itemPrefabs[index] != null)
        {
            currentGhost = Instantiate(itemPrefabs[index]);
            SetGhostVisual(currentGhost, true);
        }
    }

    void DeselectSlot()
    {
        selectedSlot = -1;
        HighlightSlot(-1);
        if (currentGhost != null)
            Destroy(currentGhost);
    }

    void HighlightSlot(int index)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].color = (i == index) ? Color.cyan : Color.white;
        }
    }

    void PlaceItem(Vector3 position)
    {
        if (selectedSlot < 0 || itemPrefabs[selectedSlot] == null) return;

        // Spawn real prefab
        Instantiate(itemPrefabs[selectedSlot], position, Quaternion.identity);

        // Remove from hotbar visually
        Images[selectedSlot].sprite = null;
        Images[selectedSlot].color = Color.white;
        itemPrefabs[selectedSlot] = null;

        // Remove ghost
        Destroy(currentGhost);
        DeselectSlot();
    }

    void SetGhostVisual(GameObject obj, bool isGhost)
    {
        // Make ghost semi-transparent (for 2D sprites)
        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers)
        {
            Color c = r.color;
            c.a = 0.5f;
            r.color = c;
        }

        // Disable colliders
        Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>();
        foreach (var c in colliders)
        {
            c.enabled = !isGhost;
        }
    }

    void UpdateHotbarSprites()
    {
        // Loop through all slots and assign the sprite from prefab
        for (int i = 0; i < Images.Length; i++)
        {
            if (i < itemPrefabs.Length && itemPrefabs[i] != null)
            {
                SpriteRenderer sr = itemPrefabs[i].GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    Images[i].sprite = sr.sprite;
                    Images[i].color = Color.white;
                }
            }
            else
            {
                Images[i].sprite = null;
                Images[i].color = Color.white;
            }
        }
    }
}
