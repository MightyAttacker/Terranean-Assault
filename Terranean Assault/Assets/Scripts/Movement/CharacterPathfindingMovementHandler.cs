using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterPathfindingMovementHandler : MonoBehaviour
{
    //Author - Lachlan Klenk
    public event System.Action OnMovementStarted;
    public event System.Action OnMovementStopped;

    private const float speed = 5f; // movement speed
    private int currentPathIndex;
    private List<Vector3> pathVectorList;
    private Rigidbody2D rb;
    private bool isMoving = false;
    private int lastMovedPhase = -1;
    private Vector3 lastPositionBeforeMove;
    public int LastMovedPhase => lastMovedPhase;
    [SerializeField] private float maxMoveDistance = 6f;
    [SerializeField] private float objectiveControl = 1;
    // [SerializeField] private float health = 2; // Redundant, health is in UnitHealth (Karl Martinez-Benham)
    [SerializeField] private float meleeDamage = 2;
    public float MeleeDamage => meleeDamage; //private float getter, Karl Martinez-Benham
    [SerializeField] private float rangedDamage = 1;
    public int width = 1;
    public int height = 1;

    public float GetObjectiveControlValue()
    {
        return objectiveControl;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (pathVectorList != null && currentPathIndex < pathVectorList.Count)
        {
            if (!isMoving)
            {
                isMoving = true;
                OnMovementStarted?.Invoke();
            }

            Vector3 targetPosition = pathVectorList[currentPathIndex];
            Vector3 currentPosition = transform.position;

            Vector3 moveDir = (targetPosition - currentPosition);
            float distanceToTarget = moveDir.magnitude;

            if (distanceToTarget > 0f)
            {
                moveDir /= distanceToTarget; // normalize
                float moveDistance = speed * Time.deltaTime;
                float distanceToMove = Mathf.Min(moveDistance, distanceToTarget);

                transform.position += moveDir * distanceToMove;

                if (distanceToMove >= distanceToTarget)
                {
                    currentPathIndex++;
                    if (currentPathIndex >= pathVectorList.Count)
                        StopMoving();
                }
            }
            else
            {
                currentPathIndex++;
                if (currentPathIndex >= pathVectorList.Count)
                    StopMoving();
            }
        }
    }

    private void StopMoving()
    {
        pathVectorList = null;
        if (isMoving)
        {
            isMoving = false;
            OnMovementStopped?.Invoke();
        }
    }


    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public float GetMaxMoveDistance()
    {
        return maxMoveDistance;
    }

    public void SetTargetPosition(Vector3 targetPosition)
    {
        currentPathIndex = 0;

        // Compute bottom-left of footprint
        float halfWidthOffset = (width - 1) * 0.5f;
        float halfHeightOffset = (height - 1) * 0.5f;

        // Adjust targetPosition to bottom-left before sending to pathfinding
        Vector3 pathfindingTarget = new Vector3(
            targetPosition.x - halfWidthOffset,
            targetPosition.y - halfHeightOffset,
            targetPosition.z
        );

        // Find path using the adjusted target
        pathVectorList = Pathfinding.Instance.FindPath(GetPosition(), pathfindingTarget);

        if (pathVectorList != null && pathVectorList.Count > 1)
            pathVectorList.RemoveAt(0);

        // Apply footprint offset to each step in the path
        if (pathVectorList != null)
        {
            for (int i = 0; i < pathVectorList.Count; i++)
            {
                pathVectorList[i] += new Vector3(halfWidthOffset, halfHeightOffset, 0f);
            }
        }
    }

    public bool TryMove(Vector3 targetPosition, int currentPhase)
    {
        if (lastMovedPhase == currentPhase)
        {
            Debug.Log($"{name} has already moved in phase {currentPhase}.");
            return false;
        }

        // Save current position to allow undo
        lastPositionBeforeMove = transform.position;

        SetTargetPosition(targetPosition);
        lastMovedPhase = currentPhase;
        return true;
    }


    // Existing method (no arguments)
    public void ResetMovementPhase()
    {
        ResetMovementPhase(false);
    }

    // New overload with argument
    public void ResetMovementPhase(bool restorePosition)
    {
        lastMovedPhase = -1;

        if (restorePosition)
        {
            transform.position = lastPositionBeforeMove;
            pathVectorList = null; // stop any current movement
            isMoving = false;
        }
    }


}
