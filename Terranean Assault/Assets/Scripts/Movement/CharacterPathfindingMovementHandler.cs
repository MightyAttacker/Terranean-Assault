using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterPathfindingMovementHandler : MonoBehaviour
{
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
        pathVectorList = Pathfinding.Instance.FindPath(GetPosition(), targetPosition);

        if (pathVectorList != null && pathVectorList.Count > 1)
            pathVectorList.RemoveAt(0);
    }

    /// <summary>
    /// Attempt to move the unit for the current phase. Returns true if move starts.
    /// </summary>
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
