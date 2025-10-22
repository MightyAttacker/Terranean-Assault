using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using Unity.VisualScripting;
#if UNITY_EDITOR
using UnityEditor.ShaderGraph; //Edit: Karl Martinez-Benham
#endif 
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine.SceneManagement;

public class Hotbar : MonoBehaviour
{
    //Author - Lachlan Klenk
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

    [Header("Regions")]
    [Header("Attacker")]
    [Tooltip("Allowed placement area for the attacker (bottom-left to top-right)")]
    public Vector2 attackerRegionMin = new Vector2(-10f, -5f);
    public Vector2 attackerRegionMax = new Vector2(-2f, 5f);

    [Header("Defender")]
    [Tooltip("Allowed placement area for the defender (bottom-left to top-right)")]
    public Vector2 defenderRegionMin = new Vector2(2f, -5f);
    public Vector2 defenderRegionMax = new Vector2(10f, 5f);

    [Header("Objective 1")]
    [Tooltip("Allowed placement area for the defender (bottom-left to top-right)")]
    public Vector2 objective1RegionMin = new Vector2(2f, -5f);
    public Vector2 objective1RegionMax = new Vector2(10f, 5f);

    [Header("Objective 2")]
    [Tooltip("Allowed placement area for the defender (bottom-left to top-right)")]
    public Vector2 objective2RegionMin = new Vector2(2f, -5f);
    public Vector2 objective2RegionMax = new Vector2(10f, 5f);

    [Header("Objective 3")]
    [Tooltip("Allowed placement area for the defender (bottom-left to top-right)")]
    public Vector2 objective3RegionMin = new Vector2(2f, -5f);
    public Vector2 objective3RegionMax = new Vector2(10f, 5f);

    [Header("Objective 4")]
    [Tooltip("Allowed placement area for the defender (bottom-left to top-right)")]
    public Vector2 objective4RegionMin = new Vector2(2f, -5f);
    public Vector2 objective4RegionMax = new Vector2(10f, 5f);

    [Header("Tracking Spawned Units")]
    [Tooltip("All spawned units for movement tracking.")]
    public List<GameObject> spawnedUnits = new List<GameObject>();


    [Header("Scoreboard Text References")]
    public TMP_Text[] attackerScoreTexts;
    public TMP_Text[] defenderScoreTexts;

    [Header("UI")]
    public Button toggleModeButton; // Button to switch between placement/movement mode
    public Button toggleObjectives;
    public Button toggleScoreboard;
    public Button toggleDZ;
    public Button Exit;
    public GameObject PhaseTracker;
    public GameObject hotbarPanel;
    public GameObject Exits;
    public GameObject AttackerDZ;
    public GameObject DefenderDZ;
    public GameObject Objective1;
    public GameObject Objective2;
    public GameObject Objective3;
    public GameObject Objective4;
    public GameObject ScoreboardObject;
    public GameObject WinBanner;
    public GameObject EndScreen;
    public GameObject LeftClick;
    public GameObject RightClick;

    private int ObjectiveToggle = 1;
    private int ScoreboardToggle = 0;
    private int DZToggle = 0;
    private int selectedSlot = -1;
    public int phase = 0;
    private int Score = 0;
    private int TotalScoreAttacker = 0;
    private int TotalScoreDefender = 0;

    private UIDragHandler dragHandler;
    private UIResizeHandler resizeHandler;
    private RectTransform scoreboardRect;

    private GameObject currentGhost;
    private Camera mainCam;

    private Dictionary<string, float> attackerObjectiveScores = new Dictionary<string, float>();
    private Dictionary<string, float> defenderObjectiveScores = new Dictionary<string, float>();
    private Dictionary<string, string> objectiveOwners = new Dictionary<string, string>();

    private string[] phaseTexts = new string[]
{
    //"(Defender) \n Deployment Phase", //0
    "(Defender) \n Deployment Phase", //1
    "(Attacker) \n 1st Movement Phase", //2
    "(Attacker) \n 1st Fight Phase", //3
    "(Defender) \n 1st Movement Phase", //4
    "(Defender) \n 1st Fight Phase", //5
    "(Attacker) \n 2nd Movement Phase", //6
    "(Attacker) \n 2nd Fight Phase", //7
    "(Defender) \n 2nd Movement Phase", //8
    "(Defender) \n 2nd Fight Phase", //9
    "(Attacker) \n 3rd Movement Phase", //10
    "(Attacker) \n 3rd Fight Phase", //11
    "(Defender) \n 3rd Movement Phase", //12
    "(Defender) \n 3rd Fight Phase", //13
    "(Attacker) \n 4th Movement Phase", //14
    "(Attacker) \n 4th Fight Phase", //15
    "(Defender) \n 4th Movement Phase", //16
    "(Defender) \n 4th Fight Phase", //17
    "(Attacker) \n 5th Movement Phase", //18
    "(Attacker) \n 5th Fight Phase", //19
    "(Defender) \n 5th Movement Phase", //20
    "(Defender) \n 5th Fight Phase" //21
};

    private string[] LeftClickTxts = new string[]
    {
    //"Left Click - \n Select/Place Unit", //0-1
    "Left Click - \nSelect/Move Unit", //2/4/6/8/10/12/14/16/18/20
    "Left Click - \nSelect/Shoot Unit", //3/5/7/9/11/13/15/17/19/21
    };

    private string[] RightClickTxts = new string[]
    {
    //"Right Click - \n Deployment Phase", //0-1
    "Right Click - \nUndo Move", //2/4/6/8/10/12/14/16/18/20
    "Right Click - \nPunch", //3/5/7/9/11/13/15/17/19/21
    };

    // Predefined units
    public GameObject Blank;
    public GameObject Engineer, HeavyWeapons, Medic, Scout, Sniper, Soldier, Tank, ATV;
    public GameObject AssaultLeader, AssaultSargent, AssaultSquad, AssaultTransport, ReconTeam, JumpPackBattlesuit, HeavyBattlesuit, EliteBattlesuit;
    void Start()
    {
        mainCam = Camera.main;
        ScoreboardObject.SetActive(false);
        DefenderDZ.SetActive(false);
        EndScreen.SetActive(false);
        WinBanner.SetActive(false);
        Exits.SetActive(false);

        scoreboardRect = ScoreboardObject.GetComponent<RectTransform>();
        dragHandler = ScoreboardObject.GetComponent<UIDragHandler>();
        resizeHandler = ScoreboardObject.GetComponentInChildren<UIResizeHandler>();

        attackerScoreTexts = ScoreboardObject.transform.Find("Attacker")
                .GetComponentsInChildren<TMP_Text>(true);
        defenderScoreTexts = ScoreboardObject.transform.Find("Defender")
            .GetComponentsInChildren<TMP_Text>(true);

        // Only keep the ones named exactly "Txt 1"
        attackerScoreTexts = System.Array.FindAll(attackerScoreTexts, t => t.name == "Txt 1");
        defenderScoreTexts = System.Array.FindAll(defenderScoreTexts, t => t.name == "Txt 1");

        // Initialize itemPrefabs
        itemPrefabs = new GameObject[Images.Length];
        itemPrefabs[0] = Engineer;
        itemPrefabs[1] = HeavyWeapons;
        itemPrefabs[2] = Medic;
        itemPrefabs[3] = Scout;
        itemPrefabs[4] = Sniper;
        itemPrefabs[5] = Soldier;
        itemPrefabs[6] = Tank;
        itemPrefabs[7] = ATV;
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
        CalculateObjectiveControl();
        UpdateObjectiveColors();

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

            Vector3 ghostPos = mouseWorld;
            CharacterPathfindingMovementHandler footprint = currentGhost.GetComponent<CharacterPathfindingMovementHandler>();
            if (footprint != null)
            {
                ghostPos.x += (footprint.width - 1) / 2f;
                ghostPos.y += (footprint.height - 1) / 2f;
            }

            currentGhost.transform.position = ghostPos;

            // Check for wall tile
            bool overWall = false;
            if (WallTilemap != null)
            {
                Vector3Int cellPos = WallTilemap.WorldToCell(mouseWorld);
                overWall = WallTilemap.GetTile(cellPos) != null;
            }

            // Check for overlapping units
            bool overUnit = false;
            if (footprint != null)
            {
                for (int dx = 0; dx < footprint.width; dx++)
                {
                    for (int dy = 0; dy < footprint.height; dy++)
                    {
                        Vector2 checkPos = new Vector2(mouseWorld.x - (footprint.width - 1) / 2f + dx,
                                                       mouseWorld.y - (footprint.height - 1) / 2f + dy);
                        Collider2D hit = Physics2D.OverlapCircle(checkPos, 0.4f);
                        if (hit != null && (hit.CompareTag(attackerTag) || hit.CompareTag(defenderTag)))
                        {
                            overUnit = true;
                            break;
                        }
                    }
                    if (overUnit) break;
                }
            }

            // === Region + wall + unit feedback ===
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

            // --- Full footprint invalid check ---
            bool invalidPlacement = false;
            if (footprint != null)
            {
                for (int dx = 0; dx < footprint.width; dx++)
                {
                    for (int dy = 0; dy < footprint.height; dy++)
                    {
                        Vector2 checkPos = new Vector2(
                            mouseWorld.x - (footprint.width - 1) / 2f + dx,
                            mouseWorld.y - (footprint.height - 1) / 2f + dy
                        );

                        // Wall check
                        if (WallTilemap != null)
                        {
                            Vector3Int cellPos = WallTilemap.WorldToCell(checkPos);
                            if (WallTilemap.GetTile(cellPos) != null)
                                invalidPlacement = true;
                        }

                        // Unit check (precise point check)
                        Collider2D hit = Physics2D.OverlapPoint(checkPos);
                        if (hit != null && (hit.CompareTag(attackerTag) || hit.CompareTag(defenderTag)))
                            invalidPlacement = true;

                        // Region check
                        bool inRegion = true;
                        if (phase == 0)
                        {
                            inRegion = checkPos.x >= attackerRegionMin.x && checkPos.x <= attackerRegionMax.x &&
                                       checkPos.y >= attackerRegionMin.y && checkPos.y <= attackerRegionMax.y;
                        }
                        else if (phase == 1)
                        {
                            inRegion = checkPos.x >= defenderRegionMin.x && checkPos.x <= defenderRegionMax.x &&
                                       checkPos.y >= defenderRegionMin.y && checkPos.y <= defenderRegionMax.y;
                        }
                        if (!inRegion) invalidPlacement = true;
                    }
                }
            }

            // Update ghost color
            SpriteRenderer[] renderers = currentGhost.GetComponentsInChildren<SpriteRenderer>();
            foreach (var r in renderers)
            {
                Color c = invalidPlacement ? Color.red : Color.white;
                c.a = 0.7f;
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
                    if (phase == 0)
                    {
                        if (clickedObj.CompareTag(attackerTag))
                        {
                            AddToHotbar(clickedObj);
                            spawnedUnits.Remove(clickedObj); // Remove from spawned list
                            Destroy(clickedObj);
                        }
                    }
                    else if (phase == 1)
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

    private void SetObjectiveColor(GameObject objectiveParent, Color color)
    {
        // Get all SpriteRenderer components in the children
        SpriteRenderer[] childSprites = objectiveParent.GetComponentsInChildren<SpriteRenderer>();

        foreach (SpriteRenderer sr in childSprites)
        {
            sr.color = color;
        }
    }

    private void UpdateObjectiveColors()
    {
        UpdateSingleObjectiveColor(Objective1, 1);
        UpdateSingleObjectiveColor(Objective2, 2);
        UpdateSingleObjectiveColor(Objective3, 3);
        UpdateSingleObjectiveColor(Objective4, 4);
    }

    private void UpdateSingleObjectiveColor(GameObject objectiveParent, int objectiveNumber)
    {
        string owner = GetObjectiveOwner(objectiveNumber);

        Color parentColor;
        Color childColor;

        if (owner == attackerTag)
        {
            parentColor = new Color(1f, 0f, 0f, 0.02f);   // Red transparent
            childColor = new Color(1f, 0f, 0f, 1f);       // Red opaque
        }
        else if (owner == defenderTag)
        {
            parentColor = new Color(0f, 0f, 1f, 0.02f);   // Blue transparent
            childColor = new Color(0f, 0f, 1f, 1f);       // Blue opaque
        }
        else
        {
            parentColor = new Color(1f, 0.8f, 0f, 0.02f); // Yellow transparent
            childColor = new Color(1f, 0.8f, 0f, 1f);    // Yellow opaque
        }

        // Set the parent objective sprite
        SpriteRenderer parentSR = objectiveParent.GetComponent<SpriteRenderer>();
        if (parentSR != null && parentSR.color != parentColor)
            parentSR.color = parentColor;

        // Set all child sprites
        SpriteRenderer[] childSprites = objectiveParent.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in childSprites)
        {
            if (sr.gameObject != objectiveParent && sr.color != childColor)
                sr.color = childColor;
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

    public void UpdateAttackerScore(int index, string value)
    {
        if (index >= 0 && index < attackerScoreTexts.Length)
            attackerScoreTexts[index].text = value;
    }

    public void UpdateDefenderScore(int index, string value)
    {
        if (index >= 0 && index < defenderScoreTexts.Length)
            defenderScoreTexts[index].text = value;
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

    private bool CanPlaceUnit(GameObject prefab, Vector3 position)
    {
        CharacterPathfindingMovementHandler footprint = prefab.GetComponent<CharacterPathfindingMovementHandler>();
        if (footprint == null) footprint = prefab.AddComponent<CharacterPathfindingMovementHandler>(); // default to 1x1 if missing

        for (int x = 0; x < footprint.width; x++)
        {
            for (int y = 0; y < footprint.height; y++)
            {
                Vector3 cellPos = new Vector3(
                    Mathf.Floor(position.x) + 0.5f + x,
                    Mathf.Floor(position.y) + 0.5f + y,
                    0f
                );

                // Check if tile is a wall
                if (WallTilemap != null)
                {
                    Vector3Int tile = WallTilemap.WorldToCell(cellPos);
                    if (WallTilemap.GetTile(tile) != null)
                        return false; // blocked by wall
                }

                // Check for overlapping units
                Collider2D hit = Physics2D.OverlapCircle(cellPos, 0.4f);
                if (hit != null && (hit.CompareTag(attackerTag) || hit.CompareTag(defenderTag)))
                    return false; // occupied by unit
            }
        }

        return true;
    }


    void PlaceItem(Vector3 position)
    {
        if (selectedSlot < 0 || itemPrefabs[selectedSlot] == null)
            return;

        GameObject prefab = itemPrefabs[selectedSlot];
        CharacterPathfindingMovementHandler footprint = prefab.GetComponent<CharacterPathfindingMovementHandler>();
        int width = footprint != null ? footprint.width : 1;
        int height = footprint != null ? footprint.height : 1;

        int baseX = Mathf.FloorToInt(position.x);
        int baseY = Mathf.FloorToInt(position.y);

        // --- Check each tile of the footprint ---
        for (int dx = 0; dx < width; dx++)
        {
            for (int dy = 0; dy < height; dy++)
            {
                Vector3 checkPos = new Vector3(baseX + dx + 0.5f, baseY + dy + 0.5f, 0f);

                // 1️⃣ Wall check
                if (WallTilemap != null)
                {
                    Vector3Int cellPos = WallTilemap.WorldToCell(checkPos);
                    TileBase tile = WallTilemap.GetTile(cellPos);
                    if (tile != null)
                    {
                        errorDisplay.ShowError("Cannot place unit here — wall in the way!");
                        return;
                    }
                }

                // 2️⃣ Region check
                bool inValidRegion = true;
                if (phase == 0) // Attacker placement
                {
                    inValidRegion =
                        checkPos.x >= attackerRegionMin.x && checkPos.x <= attackerRegionMax.x &&
                        checkPos.y >= attackerRegionMin.y && checkPos.y <= attackerRegionMax.y;
                    if (!inValidRegion)
                    {
                        errorDisplay.ShowError("Cannot place unit here — outside attacker region!");
                        return;
                    }
                }
                else if (phase == 1) // Defender placement
                {
                    inValidRegion =
                        checkPos.x >= defenderRegionMin.x && checkPos.x <= defenderRegionMax.x &&
                        checkPos.y >= defenderRegionMin.y && checkPos.y <= defenderRegionMax.y;
                    if (!inValidRegion)
                    {
                        errorDisplay.ShowError("Cannot place unit here — outside defender region!");
                        return;
                    }
                }

                // 3️⃣ Check for overlapping units
                Collider2D hit = Physics2D.OverlapCircle(checkPos, 0.4f);
                if (hit != null && (hit.CompareTag(attackerTag) || hit.CompareTag(defenderTag)))
                {
                    errorDisplay.ShowError("Cannot place unit here — space is blocked!");
                    return;
                }
            }
        }

        // --- Instantiate unit ---
        Vector3 spawnPos = position;
        if (footprint != null)
        {
            spawnPos.x += (footprint.width - 1) / 2f;
            spawnPos.y += (footprint.height - 1) / 2f;
        }

        GameObject newUnit = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Assign team tag
        newUnit.tag = phase == 0 ? attackerTag : defenderTag;

        // Attach identity for recovery later
        UnitIdentity id = newUnit.AddComponent<UnitIdentity>();
        id.sourcePrefab = prefab;

        spawnedUnits.Add(newUnit);

        // Clear hotbar slot
        Images[selectedSlot].sprite = null;
        Images[selectedSlot].color = new Color(0, 0, 0, 0f);
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
            c.a = 0.7f;
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
                Images[i].color = new Color(0, 0, 0, 0f);
            }
        }
    }

    public void Phases()
    {
        if (phase < 24)
        {
            if (!IsHotbarEmpty())
            {
                errorDisplay.ShowError("Please place all units first");
                return;
            }

            // Handle special logic for specific phases
            if (phase == 0)
            {
                DefenderDZ.SetActive(true);
                AttackerDZ.SetActive(false);

                itemPrefabs = new GameObject[Images.Length];
                itemPrefabs[0] = AssaultLeader;
                itemPrefabs[1] = AssaultSargent;
                itemPrefabs[2] = AssaultTransport;
                itemPrefabs[3] = AssaultTransport;
                itemPrefabs[4] = EliteBattlesuit;
                itemPrefabs[5] = HeavyBattlesuit;
                itemPrefabs[6] = JumpPackBattlesuit;
                itemPrefabs[7] = ReconTeam;
                itemPrefabs[8] = AssaultSquad;

                UpdateHotbarSprites();
            }
            else if (phase == 1)
            {
                DefenderDZ.SetActive(false);

                if (hotbarPanel != null)
                    hotbarPanel.SetActive(false);

                if (currentGhost != null)
                {
                    Destroy(currentGhost);
                    selectedSlot = -1;
                    HighlightSlot(-1);
                }
            }
            else if (phase == 5 || phase == 9 || phase == 13 || phase == 17)
            {
                int scoreboardIndex = (phase - 5) / 4; // Maps 5→0, 9→1, 13→2, 17→3
                int Score = 0;
                int Turn = scoreboardIndex + 2;

                for (int i = 1; i <= 4; i++)
                {
                    if (GetObjectiveOwner(i) == attackerTag)
                    {
                        Score += 5;
                        TotalScoreAttacker += 5;
                    }
                }
                UpdateAttackerScore(scoreboardIndex, "Round " + Turn + " -\n" + Score.ToString());
                UpdateAttackerScore(5, "Total -\n" + TotalScoreAttacker.ToString());

            }
            else if (phase == 7 || phase == 11 || phase == 15 || phase == 19)
            {
                int scoreboardIndex = (phase - 7) / 4; // Maps 5→0, 9→1, 13→2, 17→3
                int Score = 0;
                int Turn = scoreboardIndex + 2;

                for (int i = 1; i <= 4; i++)
                {
                    if (GetObjectiveOwner(i) == defenderTag)
                    {
                        Score += 5;
                        TotalScoreDefender += 5;
                    }
                }

                UpdateDefenderScore(scoreboardIndex, "Round " + Turn + " -\n" + Score.ToString());
                UpdateDefenderScore(5, "Total -\n" + TotalScoreDefender.ToString());
            }
            else if (phase == 21)
            {
                int ScoreD = 0;
                for (int i = 1; i <= 4; i++)
                {
                    if (GetObjectiveOwner(i) == defenderTag)
                    {
                        ScoreD += 5;
                        TotalScoreDefender += 5;
                    }
                }
                UpdateDefenderScore(4, "Game End -\n" + ScoreD.ToString());
                UpdateDefenderScore(5, "Total -\n" + TotalScoreDefender.ToString());

                int ScoreA = 0;
                for (int i = 1; i <= 4; i++)
                {
                    if (GetObjectiveOwner(i) == attackerTag)
                    {
                        ScoreA += 5;
                        TotalScoreAttacker += 5;
                    }
                }
                UpdateAttackerScore(4, "Game End -\n" + ScoreA.ToString());
                UpdateAttackerScore(5, "Total -\n" + TotalScoreAttacker.ToString());
            }

            // Set phase text via lookup table
            TMP_Text PhaseText = PhaseTracker.GetComponentInChildren<TMP_Text>();
            if (phase >= 0 && phase < phaseTexts.Length)
                PhaseText.text = phaseTexts[phase];


            TMP_Text LeftClickTxt = LeftClick.GetComponentInChildren<TMP_Text>();
            TMP_Text RightClickTxt = RightClick.GetComponentInChildren<TMP_Text>();

            if (phase == 0)
            {
                // Do nothing
            }
            else
            {
                // phase is even → index 1, odd → index 0
                int index = (phase % 2 == 0) ? 1 : 0;
                LeftClickTxt.text = LeftClickTxts[index];
                RightClickTxt.text = RightClickTxts[index];
            }

            if (phase == 21)
            {

                if (TotalScoreAttacker > TotalScoreDefender)
                {
                    TMP_Text WinBanners = WinBanner.GetComponentInChildren<TMP_Text>();
                    WinBanners.text = "Attacker Wins!";
                    WinBanners.color = Color.red;
                }
                else if (TotalScoreDefender > TotalScoreAttacker)
                {
                    TMP_Text WinBanners = WinBanner.GetComponentInChildren<TMP_Text>();
                    WinBanners.text = "Defender Wins!";
                    WinBanners.color = Color.blue;
                }
                else
                {
                    TMP_Text WinBanners = WinBanner.GetComponentInChildren<TMP_Text>();
                    WinBanners.text = "Draw!";
                    WinBanners.color = Color.gold;
                }


                AttackerDZ.SetActive(false);
                DefenderDZ.SetActive(false);
                Objective1.SetActive(false);
                Objective2.SetActive(false);
                Objective3.SetActive(false);
                Objective4.SetActive(false);
                ScoreboardObject.SetActive(true);
                EndScreen.SetActive(true);
                WinBanner.SetActive(true);
                Exits.SetActive(true);

                if (dragHandler != null) dragHandler.enabled = false;
                if (resizeHandler != null) resizeHandler.enabled = false;

                StartCoroutine(ExpandScoreboardSmooth());

            }

            phase++;
        }

    }

    IEnumerator ExpandScoreboardSmooth()
    {
        Vector2 startMin = scoreboardRect.anchorMin;
        Vector2 startMax = scoreboardRect.anchorMax;
        Vector2 targetMin = new Vector2(0.1f, 0.1f);
        Vector2 targetMax = new Vector2(0.9f, 0.9f);

        float duration = 0.5f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            scoreboardRect.anchorMin = Vector2.Lerp(startMin, targetMin, t);
            scoreboardRect.anchorMax = Vector2.Lerp(startMax, targetMax, t);
            yield return null;
        }

        // Snap exactly at the end
        scoreboardRect.anchorMin = targetMin;
        scoreboardRect.anchorMax = targetMax;
        scoreboardRect.offsetMin = Vector2.zero;
        scoreboardRect.offsetMax = Vector2.zero;
    }


    public void Objectives()
    {
        if (ObjectiveToggle == 0)
        {
            ObjectiveToggle = 1;
            TMP_Text ToggleObjectives = toggleObjectives.GetComponentInChildren<TMP_Text>();
            ToggleObjectives.text = "Objectives (Shown)";
            Objective1.SetActive(true);
            Objective2.SetActive(true);
            Objective3.SetActive(true);
            Objective4.SetActive(true);
        }
        else if (ObjectiveToggle == 1)
        {
            ObjectiveToggle = 0;
            TMP_Text ToggleObjectives = toggleObjectives.GetComponentInChildren<TMP_Text>();
            ToggleObjectives.text = "Objectives (Hidden)";
            Objective1.SetActive(false);
            Objective2.SetActive(false);
            Objective3.SetActive(false);
            Objective4.SetActive(false);
        }
    }

    public void Scoreboard()
    {
        if (ScoreboardToggle == 0)
        {
            ScoreboardToggle = 1;
            TMP_Text ToggleScoreboard = toggleScoreboard.transform.GetChild(0).GetComponent<TMP_Text>();
            ToggleScoreboard.text = "Scoreboard (Shown)";
            ScoreboardObject.SetActive(true);

        }
        else if (ScoreboardToggle == 1)
        {
            ScoreboardToggle = 0;
            TMP_Text ToggleScoreboard = toggleScoreboard.transform.GetChild(0).GetComponent<TMP_Text>();
            ToggleScoreboard.text = "Scoreboard (Hidden)";
            ScoreboardObject.SetActive(false);
        }
    }

    public void DZ()
    {
        if (phase == 0 || phase == 1)
        {
            errorDisplay.ShowError("Cannot Toggle Deployment Zones In Deployment Phase");
        }
        else
        {
            if (DZToggle == 0)
            {
                DZToggle = 1;
                TMP_Text ToggleDZ = toggleDZ.transform.GetChild(0).GetComponent<TMP_Text>();
                ToggleDZ.text = "Deployment Zones \n(Shown)";
                AttackerDZ.SetActive(true);
                DefenderDZ.SetActive(true);

            }
            else if (DZToggle == 1)
            {
                DZToggle = 0;
                TMP_Text ToggleDZ = toggleDZ.transform.GetChild(0).GetComponent<TMP_Text>();
                ToggleDZ.text = "Deployment Zones \n(Hidden)";
                AttackerDZ.SetActive(false);
                DefenderDZ.SetActive(false);
            }
        }

    }

    public void ExitPage()
    {
        SceneManager.LoadSceneAsync(1);
    }

    GameObject FindPrefabByName(string name)
    {
        foreach (var prefab in itemPrefabs)
            if (prefab != null && prefab.name == name) return prefab;

        GameObject[] allUnits = { Engineer, HeavyWeapons, Medic, Scout, Sniper, Soldier, Tank, ATV,
                                 AssaultLeader, AssaultSargent, AssaultSquad, AssaultTransport, HeavyBattlesuit, EliteBattlesuit, JumpPackBattlesuit, ReconTeam};

        foreach (var unit in allUnits)
            if (unit != null && unit.name == name) return unit;

        return null;
    }

    public void CalculateObjectiveControl()
    {
        attackerObjectiveScores.Clear();
        defenderObjectiveScores.Clear();
        objectiveOwners.Clear();

        for (int i = 1; i <= 4; i++)
        {
            attackerObjectiveScores["Objective" + i] = 0f;
            defenderObjectiveScores["Objective" + i] = 0f;
        }

        foreach (GameObject unit in spawnedUnits)
        {
            if (unit == null) continue;

            CharacterPathfindingMovementHandler handler = unit.GetComponent<CharacterPathfindingMovementHandler>();
            if (handler == null) continue;

            float value = handler.GetObjectiveControlValue();
            string tag = unit.tag;
            Vector3 pos = unit.transform.position;

            if (IsWithinRegion(pos, objective1RegionMin, objective1RegionMax))
                AddToTeamControl(tag, "Objective1", value, attackerObjectiveScores, defenderObjectiveScores);
            if (IsWithinRegion(pos, objective2RegionMin, objective2RegionMax))
                AddToTeamControl(tag, "Objective2", value, attackerObjectiveScores, defenderObjectiveScores);
            if (IsWithinRegion(pos, objective3RegionMin, objective3RegionMax))
                AddToTeamControl(tag, "Objective3", value, attackerObjectiveScores, defenderObjectiveScores);
            if (IsWithinRegion(pos, objective4RegionMin, objective4RegionMax))
                AddToTeamControl(tag, "Objective4", value, attackerObjectiveScores, defenderObjectiveScores);
        }

        for (int i = 1; i <= 4; i++)
        {
            string key = "Objective" + i;
            float atk = attackerObjectiveScores[key];
            float def = defenderObjectiveScores[key];

            string owner = (atk > def) ? attackerTag :
                           (def > atk) ? defenderTag :
                           "Contested";

            objectiveOwners[key] = owner;

            Debug.Log($"{key}: {attackerTag} = {atk}, {defenderTag} = {def} → {owner}");
        }
        UpdateObjectiveColors();
    }


    // Helper: checks if a position is inside a rectangular region
    private bool IsWithinRegion(Vector3 pos, Vector2 min, Vector2 max)
    {
        return pos.x >= min.x && pos.x <= max.x && pos.y >= min.y && pos.y <= max.y;
    }

    // Helper: adds control values to the right team
    private void AddToTeamControl(string tag, string objectiveKey, float value,
                                  Dictionary<string, float> atkDict,
                                  Dictionary<string, float> defDict)
    {
        if (tag == attackerTag)
            atkDict[objectiveKey] += value;
        else if (tag == defenderTag)
            defDict[objectiveKey] += value;
    }

    public float GetAttackerObjectiveScore(int objectiveNumber)
    {
        string key = "Objective" + objectiveNumber;
        return attackerObjectiveScores.ContainsKey(key) ? attackerObjectiveScores[key] : 0f;
    }

    public float GetDefenderObjectiveScore(int objectiveNumber)
    {
        string key = "Objective" + objectiveNumber;
        return defenderObjectiveScores.ContainsKey(key) ? defenderObjectiveScores[key] : 0f;
    }

    public string GetObjectiveOwner(int objectiveNumber)
    {
        string key = "Objective" + objectiveNumber;
        return objectiveOwners.ContainsKey(key) ? objectiveOwners[key] : "Unknown";
    }

}
