using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static CreatureAI;
using static CreatureBehaviour;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;

public class CreatureAI : MonoBehaviour, Saveable<CreatureAIData>
{
    public CreatureBehaviour behaviour;

    [SerializeField] private Vector2? locationGoal = null;
    [SerializeField] private float attackDistance = 0.0f;
    [SerializeField] private float preferredFightDistance = 0.0f;
    [SerializeField] private float cautionTime = 0.0f;
    [SerializeField] private float searchTime = 0.0f;
    [SerializeField] private float idleTime = 0.0f;
    [SerializeField] private List<Vector2> idleRoutine = new List<Vector2>();
    [SerializeField] private float LOCATION_MAX_OFFSET = 0.1f;
    [SerializeField] private float DISTANCE_MAX_OFFSET = 2.0f;

    // Handled by detection system
    [HideInInspector] public ulong targetID = 0;
    private GameObject target = null;
    private Vector2? targetLastPos = null;

    private aiState state = aiState.idle;
    private int routineIndex = 0;

    private Coroutine timer = null;
    private float countdown = 0.0f;

    private List<GameObject> entitiesInDetectionRange = new List<GameObject>();

    /* hold - stand in place
     * idle - play idle cycle (for example patrol an area)
     * caution - play warning and/or run timer
     * search - locate the enemy
     * track - get to maximum attack distance to enemy
     * attack - use weapons and maintain preferred distance
     */
    public enum aiState
    {
        hold, idle, caution, search, track, attack
    }

    public void Start()
    {
        StartCoroutine(UpdateMovement());
        StartCoroutine(UpdateFacing());
        StartCoroutine(AIUpdate());
    }

    private void OnDestroy()
    {
        StopCoroutine(UpdateMovement());
        StopCoroutine(UpdateFacing());
        StopCoroutine(AIUpdate());
        if (timer != null) StopCoroutine(timer);
    }

    public void ObtainTargetLocation()
    {      
        // If we have object but not id then grab ID
        if (target && (targetID == 0)) targetID = target.GetComponent<EntityBehaviour>().ID;
        // If we have ID but not object then grab object
        if (!target && (targetID != 0)) target = HelpFunc.FindGameObjectByBehaviourID(targetID);
        // If object wasnt found then it must have been destroyed so we have no target
        if (!target) targetID = 0;
        // If we do have target then grab the position
        if (target) targetLastPos = target.transform.position;
    }

    public void AddEntityDetected(GameObject entity)
    {
        CreatureBehaviour behaviour = entity.GetComponent<CreatureBehaviour>();
        if (!behaviour) return;
        if (!entitiesInDetectionRange.Contains(entity))
        {
            entitiesInDetectionRange.Add(entity);
        }
    }

    public void RemoveEntityDetected(GameObject entity)
    {
        if (entitiesInDetectionRange.Contains(entity))
        {
            entitiesInDetectionRange.Remove(entity);
        }
    }

    private IEnumerator UpdateMovement()
    {
        while (true)
        {
            if (GlobalControl.paused) yield return new WaitForSeconds(.1f);
            if (LocationGoalAchieved()) locationGoal = null;
            if (locationGoal != null)
            {
                Vector2 desiredDirection = locationGoal.Value - (Vector2)transform.position;
                behaviour.SetSpeed(behaviour.moveSpeed);
                behaviour.SetMoveVector(desiredDirection);
            }
            else
            {
                behaviour.SetSpeed(0);
                if (targetID == 0) behaviour.SetMoveVector(Vector2.zero);
                else
                {
                    Vector2 desiredDirection = targetLastPos.Value - (Vector2)transform.position;
                    behaviour.SetMoveVector(desiredDirection);
                }
            }
            yield return new WaitForSeconds(.1f);
        }
    }

    private IEnumerator UpdateFacing()
    {
        while (true)
        {
            if (GlobalControl.paused) yield return new WaitForSeconds(.1f);
            if (targetID == 0)
            {
                behaviour.SetFacingVector(Vector2.right);
                behaviour.SetAimingLocation((Vector2)transform.position + Vector2.right);
            }
            else
            {
                Vector2 desiredDirection = targetLastPos.Value - (Vector2)transform.position;
                behaviour.SetFacingVector(desiredDirection);
                behaviour.SetAimingLocation(targetLastPos.Value);
            }
            yield return new WaitForSeconds(.1f);
        }
    }

    private IEnumerator AIUpdate()
    {
        while (true)
        {
            if (GlobalControl.paused) yield return new WaitForSeconds(.1f);
            // Disable yourself if dead
            if (!behaviour.GetAlive())
            {
                StopCoroutine(UpdateMovement());
                StopCoroutine(UpdateFacing());
                enabled = false;
                break;
            }
            ScanEntities();
            UpdateAIState();
            yield return new WaitForSeconds(.5f);
        }
    }

    private void ScanEntities()
    {
        List<Tuple<GameObject, float>> toScan = new List<Tuple<GameObject, float>>();
        // First check if we still have line of sight on target
        ObtainTargetLocation();
        // We dont have target - scan everything in list of entities
        if (!target)
        {
            // Make a list with distances to potential targets
            foreach (GameObject entity in entitiesInDetectionRange)
            {
                // Get rid of any deleted objects
                if (!entity) { entitiesInDetectionRange.Remove(entity); continue; }
                CreatureBehaviour b = entity.GetComponent<CreatureBehaviour>();
                bool detect = ShouldAggro(behaviour.faction, b.faction);
                if (detect)
                {
                    float distance = (entity.transform.position - transform.position).magnitude;
                    toScan.Add(new Tuple<GameObject, float>(entity, distance));
                }
            }
        }
        // We have target - only scan to maintain the target
        else
        {
            toScan.Add(new Tuple<GameObject, float>(target, 0f));
        }
        // Sort entities to scan by distance to us
        toScan.Sort((x, y) => x.Item2.CompareTo(y.Item2));
        // Scan each potential enemy for line of sight starting with closest
        bool targetKept = false;
        foreach (Tuple<GameObject, float> tpl in toScan)
        {
            RaycastHit2D hit = RaycastEntity(tpl.Item1);
            if (hit.collider != null)
            {
                // Target acquired
                target = tpl.Item1;
                targetKept = true;
                break;
            }
        }
        // Mark that we failed to find target
        if (!targetKept)
        {
            target = null;
            targetID = 0;
        }
    }

    private void UpdateAIState()
    {
        switch (state)
        {
            case aiState.hold:
                {
                    locationGoal = null;
                    break;
                }
            case aiState.idle:
                {
                    // set a timer if reached location
                    if (LocationGoalAchieved()) StartTimer(idleTime);
                    // if timer finished, path to the next goal in routine or loop to start
                    if (TimerUp())
                    {
                        locationGoal = idleRoutine[routineIndex];
                        routineIndex++;
                        if (routineIndex >= idleRoutine.Count) routineIndex = 0;
                    }
                    // target spotted - begin caution phase
                    if (HaveTarget())
                    {
                        state = aiState.caution;
                        locationGoal = null;
                        StartTimer(cautionTime);
                    }
                    break;
                }
            case aiState.caution:
                {
                    // timer finished - check if we still have target
                    if (TimerUp())
                    {
                        if (HaveTarget())
                        {
                            // how dare you! time to fight
                            state = aiState.track;
                            break;
                        }
                        else
                        {
                            // they got away - resume routine
                            StartTimer(0.0f);
                            state = aiState.idle;
                            routineIndex = 0;
                            break;
                        }
                    }
                    break;
                }
            case aiState.search:
                {
                    // path to last known location of target
                    locationGoal = targetLastPos;
                    if (HaveTarget())
                    {
                        // target reacquired - begin fighting again
                        state = aiState.track;
                        break;
                    }
                    if (TimerUp())
                    {
                        // they got away - resume routine
                        state = aiState.idle;
                        routineIndex = 0;
                        break;
                    }
                    break;
                }
            case aiState.track:
                {
                    // check if detection still has target first
                    if (!HaveTarget())
                    {
                        // target lost, search for target
                        state = aiState.search;
                        countdown = searchTime;
                        break;
                    }
                    // path to the target to get in weapon range
                    ObtainTargetLocation();
                    locationGoal = targetLastPos;
                    // check if range is achieved to begin attacking
                    Vector2 currLocation = transform.position;
                    if (HelpFunc.PositionInRange(currLocation, targetLastPos.Value, attackDistance))
                    {
                        // in engage range, attack
                        state = aiState.attack;
                        locationGoal = null;
                        break;
                    }
                    break;
                }
            case aiState.attack:
                {
                    // check if detection still has target first
                    if (!HaveTarget())
                    {
                        // target lost, search for target
                        state = aiState.search;
                        StartTimer(searchTime);
                        break;
                    }
                    // set attack target for weapons and execute the attack
                    ObtainTargetLocation();
                    behaviour.SetAttackTarget(target);
                    if (behaviour.AnyWeaponOnTarget()) behaviour.Attack();
                    // path to preferred distance
                    Vector2 currLocation = transform.position;
                    float currDistance = (targetLastPos.Value - currLocation).magnitude;
                    if (currDistance > preferredFightDistance + DISTANCE_MAX_OFFSET ||
                        currDistance < preferredFightDistance - DISTANCE_MAX_OFFSET) 
                        locationGoal = HelpFunc.GetPointAtDistance(currLocation, targetLastPos.Value, preferredFightDistance);
                    break;
                }
            default:
                break;
        }
    }

    private bool LocationGoalAchieved()
    {
        if (locationGoal == null) return true;
        Vector2 currLocation = behaviour.transform.position;
        float currX = currLocation.x;
        float currY = currLocation.y;
        float goalX = locationGoal.Value.x;
        float goalY = locationGoal.Value.y;
        bool xMatching = Mathf.Abs(goalX - currX) < LOCATION_MAX_OFFSET;
        bool yMatching = Mathf.Abs(goalY - currY) < LOCATION_MAX_OFFSET;
        return xMatching && yMatching;
    }

    private RaycastHit2D RaycastEntity(GameObject entity)
    {
        // Temporarily change layer of our vision blocker so it doesnt block the raycast
        Collider2D targetCollider = entity.GetComponent<Collider2D>();
        Vector2 targetLoc = ((Vector2)entity.transform.position) + targetCollider.offset;
        Vector2 position = transform.position;
        Vector2 targetVec = targetLoc - position;
        RaycastHit2D hit = Physics2D.Raycast(position, targetVec, targetVec.magnitude, 1 << 0);
        Debug.DrawRay(position, targetVec, Color.yellow, 2f);
        return hit;
    }

    // Conditions for when we detect and begin aggression as AI
    private bool ShouldAggro(FactionAllegiance factionMe, FactionAllegiance factionOther)
    {
        if (factionMe == FactionAllegiance.berserk) return true;
        if (factionMe == FactionAllegiance.hostile && factionOther != FactionAllegiance.hostile) return true;
        if (factionMe == FactionAllegiance.NPC && factionOther == FactionAllegiance.hostile) return true;
        if (factionMe == FactionAllegiance.NPCaggro && factionOther == FactionAllegiance.player) return true;
        if (factionMe == FactionAllegiance.enemy && factionOther == FactionAllegiance.player) return true;
        return false;
    }

    private bool HaveTarget() {
        ObtainTargetLocation();
        return targetID != 0; 
    }

    private bool TimerUp() { return countdown <= 0.0f; }

    private void StartTimer(float time)
    {
        if (timer != null) StopCoroutine(timer);
        timer = StartCoroutine(StartCountdown(time));
    }

    private IEnumerator StartCountdown(float time)
    {
        countdown = time;
        while (countdown > 0)
        {
            countdown -= Time.deltaTime;
            yield return null;
        }
    }

    public CreatureAIData Save()
    {
        CreatureAIData data = new CreatureAIData();
        data.locationGoal = locationGoal.HasValue ? HelpFunc.VectorToArray(locationGoal.Value) : null;
        data.attackDistance = attackDistance;
        data.preferredFightDistance = preferredFightDistance;
        data.cautionTime = cautionTime;
        data.searchTime = searchTime;
        data.idleTime = idleTime;
        data.idleRoutine = HelpFunc.VectorListToArrayList(idleRoutine);
        data.LOCATION_MAX_OFFSET = LOCATION_MAX_OFFSET;
        data.DISTANCE_MAX_OFFSET = DISTANCE_MAX_OFFSET;
        data.targetID = targetID;
        data.targetLastPos = targetLastPos.HasValue ? HelpFunc.VectorToArray(targetLastPos.Value) : null;
        data.state = state;
        data.routineIndex = routineIndex;
        data.countdown = countdown;
        return data;
    }

    public void Load(CreatureAIData data, bool loadTransform = true)
    {
        locationGoal = (data.locationGoal != null) ? HelpFunc.DataToVec2(data.locationGoal) : null;
        attackDistance = data.attackDistance;
        preferredFightDistance = data.preferredFightDistance;
        cautionTime = data.cautionTime;
        searchTime = data.searchTime;
        idleTime = data.idleTime;
        idleRoutine = HelpFunc.DataToListVec2(data.idleRoutine);
        LOCATION_MAX_OFFSET = data.LOCATION_MAX_OFFSET;
        DISTANCE_MAX_OFFSET = data.DISTANCE_MAX_OFFSET;
        targetID = data.targetID;
        if (targetID != 0) target = HelpFunc.FindGameObjectByBehaviourID(targetID);
        targetLastPos = (data.targetLastPos != null) ? HelpFunc.DataToVec2(data.targetLastPos) : null;
        state = data.state;
        routineIndex = data.routineIndex;
        countdown = data.countdown;
        if (countdown > 0) StartTimer(countdown);
    }
}

[Serializable]
public class CreatureAIData
{
    public CreatureAIData() { }

    public float[] locationGoal;
    public float attackDistance;
    public float preferredFightDistance;
    public float cautionTime;
    public float searchTime;
    public float idleTime;
    public List<float[]> idleRoutine;
    public float LOCATION_MAX_OFFSET;
    public float DISTANCE_MAX_OFFSET;
    public ulong targetID;
    public float[] targetLastPos;
    public aiState state;
    public int routineIndex;
    public float countdown;

}