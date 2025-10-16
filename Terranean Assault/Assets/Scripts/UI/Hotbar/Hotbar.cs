using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class Hotbar : MonoBehaviour
{
    [Header("Images")]
    public Image[] Images;

    [Header("Tilemaps")]
    public Tilemap WallTilemap;

    [Header("Current Units")]
    public GameObject[] itemPrefabs;

    [Header("Slots")]
    public Image[] slots;

    [Header("Team Settings")]
    [Tooltip("Tag that marks units you are allowed to pick up (e.g., 'LegionsImperius')")]
    public string friendlyTag = "LegionsImperius";

    [Header("Tracking Spawned Units")]
    [Tooltip("All spawned units for movement tracking.")]
    public List<GameObject> spawnedUnits = new List<GameObject>();

    [Header("UI")]
    public Button toggleModeButton; // Button to switch between placement/movement mode
    public GameObject hotbarPanel;

    private int selectedSlot = -1;
    private GameObject currentGhost;
    private Camera mainCam;

    // Predefined units
    public GameObject Blank;
    public GameObject Engineer, HeavyWeapons, Medic, Scout, Sniper, Soldier, Tank;
    public GameObject AssaultLeader, AssaultSargent, AssaultSquad, AssaultTransport;

    [HideInInspector]
    public bool placementMode = true; // true = placing units, false = moving units

    void Start()
    {
        mainCam = Camera.main;

        // Initialize itemPrefabs
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

        UpdateHotbarSprites();

        // Add click listeners to images
        for (int i = 0; i < Images.Length; i++)
        {
            int index = i;
            Button button = Images[i].GetComponent<Button>();
            if (button == null)
                button = Images[i].gameObject.AddComponent<Button>();

            button.onClick.AddListener(() => SelectSlot(index));
        }

        // Add toggle mode button listener
        if (toggleModeButton != null)
            toggleModeButton.onClick.AddListener(TogglePlacementMode);

        HighlightSlot(-1);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H)) // press H to toggle hotbar
        TogglePlacementMode();
        
        if (!placementMode) return; // disable placement updates when in movement mode

        Vector3 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        // Number keys select hotbar
        for (int i = 0; i < Images.Length; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                SelectSlot(i);

        // === If ghost active (placing) ===
        if (currentGhost != null)
        {
            // Snap to grid center
            mouseWorld.x = Mathf.Floor(mouseWorld.x) + 0.5f;
            mouseWorld.y = Mathf.Floor(mouseWorld.y) + 0.5f;
            currentGhost.transform.position = mouseWorld;

            // Check for wall tile
            bool overWall = false;
            if (WallTilemap != null)
            {
                Vector3Int cellPos = WallTilemap.WorldToCell(mouseWorld);
                overWall = WallTilemap.GetTile(cellPos) != null;
            }

            // Ghost color feedback
            SpriteRenderer[] renderers = currentGhost.GetComponentsInChildren<SpriteRenderer>();
            foreach (var r in renderers)
            {
                Color c = overWall ? Color.red : Color.white;
                c.a = 0.5f;
                r.color = c;
            }

            // Left click = place
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() && !overWall)
                PlaceItem(mouseWorld);

            // Right click = cancel
            if (Input.GetMouseButtonDown(1))
                DeselectSlot();
        }
        // === Right click to pick up units while placing ===
        else
        {
            if (Input.GetMouseButtonDown(1) && !EventSystem.current.IsPointerOverGameObject())
            {
                Vector2 mousePos2D = mainCam.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

                if (hit.collider != null)
                {
                    GameObject clickedObj = hit.collider.gameObject;
                    if (clickedObj.CompareTag(friendlyTag))
                    {
                        AddToHotbar(clickedObj);
                        spawnedUnits.Remove(clickedObj); // Remove from spawned list
                        Destroy(clickedObj);
                    }
                }
            }
        }
    }

    void SelectSlot(int index)
    {
        if (selectedSlot == index)
        {
            DeselectSlot();
            return;
        }

        selectedSlot = index;
        HighlightSlot(index);

        if (currentGhost != null)
            Destroy(currentGhost);

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
            slots[i].color = (i == index) ? Color.cyan : Color.white;
    }

    void PlaceItem(Vector3 position)
    {
        if (selectedSlot < 0 || itemPrefabs[selectedSlot] == null) return;

        if (WallTilemap != null)
        {
            Vector3Int cellPos = WallTilemap.WorldToCell(position);
            TileBase tile = WallTilemap.GetTile(cellPos);
            if (tile != null)
            {
                Debug.Log("Cannot place on wall tile!");
                return;
            }
        }

        GameObject prefab = itemPrefabs[selectedSlot];
        GameObject newUnit = Instantiate(prefab, position, Quaternion.identity);
        newUnit.tag = friendlyTag;

        // Attach identity so we can recover the prefab later
        UnitIdentity id = newUnit.AddComponent<UnitIdentity>();
        id.sourcePrefab = prefab;

        // Add to spawned units list
        spawnedUnits.Add(newUnit);

        // Clear hotbar slot
        Images[selectedSlot].sprite = null;
        Images[selectedSlot].color = Color.white;
        itemPrefabs[selectedSlot] = null;

        // Destroy ghost & deselect
        Destroy(currentGhost);
        DeselectSlot();
    }

    void AddToHotbar(GameObject obj)
    {
        GameObject prefabToAdd = null;
        UnitIdentity id = obj.GetComponent<UnitIdentity>();
        if (id != null && id.sourcePrefab != null)
            prefabToAdd = id.sourcePrefab;

        if (prefabToAdd == null) return;

        // Find first empty slot
        for (int i = 0; i < itemPrefabs.Length; i++)
        {
            if (itemPrefabs[i] == null)
            {
                itemPrefabs[i] = prefabToAdd;
                SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    Images[i].sprite = sr.sprite;
                    Images[i].color = Color.white;
                }
                return;
            }
        }

        Debug.Log("Hotbar full – cannot pick up more units!");
    }

    void SetGhostVisual(GameObject obj, bool isGhost)
    {
        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers)
        {
            Color c = r.color;
            c.a = 0.5f;
            r.color = c;
        }

        Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>();
        foreach (var c in colliders)
            c.enabled = !isGhost;
    }

    void UpdateHotbarSprites()
    {
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

public void TogglePlacementMode()
{
    placementMode = false; // switch to movement mode
    Debug.Log("Hotbar hidden, placementMode: " + placementMode);

    // Hide the panel (hotbar + all its child slots)
    if (hotbarPanel != null)
        hotbarPanel.SetActive(false);

    // Destroy ghost & deselect
    if (currentGhost != null)
    {
        Destroy(currentGhost);
        selectedSlot = -1;
        HighlightSlot(-1);
    }
}

    GameObject FindPrefabByName(string name)
    {
        foreach (var prefab in itemPrefabs)
            if (prefab != null && prefab.name == name) return prefab;

        GameObject[] allUnits = { Engineer, HeavyWeapons, Medic, Scout, Sniper, Soldier, Tank,
                                 AssaultLeader, AssaultSargent, AssaultSquad, AssaultTransport };

        foreach (var unit in allUnits)
            if (unit != null && unit.name == name) return unit;

        return null;
    }
}
