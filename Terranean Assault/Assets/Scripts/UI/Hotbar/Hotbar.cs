using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using Unity.VisualScripting;

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
    [Tooltip("Attacker Tag (e.g., 'LegionsImperius')")]
    public string attackerTag = "LegionsImperius";

    [Tooltip("Defender Tag (e.g., 'MechanisedCommonwealth')")]
    public string defenderTag = "MechanisedCommonwealth";

    [Header("Error Display")]
    public ErrorDisplay errorDisplay;

    [Header("Placement Regions")]
    [Tooltip("Allowed placement area for the attacker (bottom-left to top-right)")]
    public Vector2 attackerRegionMin = new Vector2(-10f, -5f);
    public Vector2 attackerRegionMax = new Vector2(-2f, 5f);

    [Tooltip("Allowed placement area for the defender (bottom-left to top-right)")]
    public Vector2 defenderRegionMin = new Vector2(2f, -5f);
    public Vector2 defenderRegionMax = new Vector2(10f, 5f);

    [Header("Tracking Spawned Units")]
    [Tooltip("All spawned units for movement tracking.")]
    public List<GameObject> spawnedUnits = new List<GameObject>();

    [Header("UI")]
    public Button toggleModeButton; // Button to switch between placement/movement mode
    public GameObject hotbarPanel;
    private int selectedSlot = -1;
    private GameObject currentGhost;
    private Camera mainCam;
    public int phase = 0;
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
    }

    void Update()
    {
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

            // === Region + wall feedback ===
            bool inValidRegion = true;

            if (phase == 0)
            {
                inValidRegion =
                    mouseWorld.x >= attackerRegionMin.x && mouseWorld.x <= attackerRegionMax.x &&
                    mouseWorld.y >= attackerRegionMin.y && mouseWorld.y <= attackerRegionMax.y;
            }
            else if (phase == 1)
            {
                inValidRegion =
                    mouseWorld.x >= defenderRegionMin.x && mouseWorld.x <= defenderRegionMax.x &&
                    mouseWorld.y >= defenderRegionMin.y && mouseWorld.y <= defenderRegionMax.y;
            }

            // Combined condition: red if over wall OR outside region
            bool invalidPlacement = overWall || !inValidRegion;

            SpriteRenderer[] renderers = currentGhost.GetComponentsInChildren<SpriteRenderer>();
            foreach (var r in renderers)
            {
                Color c = invalidPlacement ? Color.red : Color.white;
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
                    if (phase == 0 || phase == 2)
                    {
                        if (clickedObj.CompareTag(attackerTag))
                        {
                            AddToHotbar(clickedObj);
                            spawnedUnits.Remove(clickedObj); // Remove from spawned list
                            Destroy(clickedObj);
                        }
                    }
                    else if (phase == 1 || phase == 3)
                    {
                        if (clickedObj.CompareTag(defenderTag))
                        {
                            AddToHotbar(clickedObj);
                            spawnedUnits.Remove(clickedObj); // Remove from spawned list
                            Destroy(clickedObj);
                        }
                    }
                }
            }
        }
    }

    bool IsHotbarEmpty()
    {
        foreach (var prefab in itemPrefabs)
        {
            if (prefab != null)
                return false;
        }
        return true;
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
    if (selectedSlot < 0 || itemPrefabs[selectedSlot] == null)
        return;

    // --- Check for wall tile ---
    if (WallTilemap != null)
    {
        Vector3Int cellPos = WallTilemap.WorldToCell(position);
        TileBase tile = WallTilemap.GetTile(cellPos);
        if (tile != null)
        {
            errorDisplay.ShowError("Cannot place unit here — wall in the way!");
            return;
        }
    }

    // --- Check region by phase ---
    bool inValidRegion = true;

    if (phase == 0) // Attacker placement
    {
        inValidRegion =
            position.x >= attackerRegionMin.x && position.x <= attackerRegionMax.x &&
            position.y >= attackerRegionMin.y && position.y <= attackerRegionMax.y;

        if (!inValidRegion)
        {
            errorDisplay.ShowError("Cannot place unit here — outside attacker region!");
            return;
        }
    }
    else if (phase == 1) // Defender placement
    {
        inValidRegion =
            position.x >= defenderRegionMin.x && position.x <= defenderRegionMax.x &&
            position.y >= defenderRegionMin.y && position.y <= defenderRegionMax.y;

        if (!inValidRegion)
        {
            errorDisplay.ShowError("Cannot place unit here — outside defender region!");
            return;
        }
    }

    // --- Check for overlapping unit ---
    Collider2D hit = Physics2D.OverlapCircle(position, 0.4f);
    if (hit != null && (hit.CompareTag(attackerTag) || hit.CompareTag(defenderTag)))
    {
        errorDisplay.ShowError("Cannot place unit here — another unit is in the way!");
        return;
    }

    // --- Instantiate unit ---
    GameObject prefab = itemPrefabs[selectedSlot];
    GameObject newUnit = Instantiate(prefab, position, Quaternion.identity);

    // Assign team tag
    if (phase == 0 || phase == 2)
        newUnit.tag = attackerTag;
    else if (phase == 1 || phase == 3)
        newUnit.tag = defenderTag;

    // Attach identity for recovery later
    UnitIdentity id = newUnit.AddComponent<UnitIdentity>();
    id.sourcePrefab = prefab;

    spawnedUnits.Add(newUnit);

    // Clear hotbar slot
    Images[selectedSlot].sprite = null;
    Images[selectedSlot].color = new Color(1, 1, 1, 0f);
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

        errorDisplay.ShowError("Hotbar full – cannot pick up more units!");
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
                Images[i].color = new Color(1, 1, 1, 0.25f);
            }
        }
    }

    public void HideHotbar()
    {
        if (IsHotbarEmpty())
        {

            if (phase == 0)
            {
                phase = 1;
                TMP_Text buttonText = toggleModeButton.GetComponentInChildren<TMP_Text>();
                buttonText.text = "(Attacker) \n Movement Phase";

                itemPrefabs = new GameObject[Images.Length];
                itemPrefabs[0] = AssaultLeader;
                itemPrefabs[1] = AssaultSargent;
                itemPrefabs[2] = AssaultTransport;
                itemPrefabs[3] = AssaultTransport;
                itemPrefabs[4] = AssaultSquad;
                itemPrefabs[5] = AssaultSquad;
                itemPrefabs[6] = AssaultSquad;
                itemPrefabs[7] = AssaultSquad;
                itemPrefabs[8] = AssaultSquad;

                UpdateHotbarSprites();
            }
            else if (phase == 1)
            {
                phase = 2;
                TMP_Text buttonText = toggleModeButton.GetComponentInChildren<TMP_Text>();
                buttonText.text = "(Defender) \n Movement Phase";

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

        }
        else
        {
            errorDisplay.ShowError("Please place all units first");
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
