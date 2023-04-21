using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static ContentGenerator;
using static CreatureAI;
using static CreatureBehaviour;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;

/* This large class is responsible for all of AI in the game currently.
 * This should be split up into a smaller class structure that encompasses each enemy separately but I couldn't do it due to
 * time constraints.
 */
public class CreatureAI : MonoBehaviour, Saveable<CreatureAIData>
{
    public CreatureBehaviour behaviour; // Link to behaviour so we can control this creature

    [SerializeField] private Vector2? locationGoal = null;                      // If empty - indicates that we dont want to move. Otherwise location to which we want to go
    [SerializeField] private float detectionRange = 0.0f;                       // How far to scan for enemies when idle
    [SerializeField] private float detectionRangeCaution = 0.0f;                // How far to scan for enemies when in hightened attention states
    [SerializeField] private float attackDistance = 0.0f;                       // How far away this creature can start attacking
    [SerializeField] private float accuracyAllowance = 0.0f;                    // How many angles of deviation can there be from desired and current weapon aim for us to start shooting
    [SerializeField] private float preferredFightDistance = 0.0f;               // AI will attempt to maintain this distance when fighting
    [SerializeField] private float cautionTime = 0.0f;                          // Length in seconds of caution phase
    [SerializeField] private float searchTime = 0.0f;                           // Length in seconds of search phase
    [SerializeField] private float chaseTime = 0.0f;                            // Length in seconds of chase phase
    [SerializeField] private List<Vector2> idleRoutine = new List<Vector2>();   // Add points to this to make AI patrol a certain area
    [SerializeField] private float distanceMaxOffset = 2.0f;                    // How far away from preferred distance can enemy be before we start moving
    [SerializeField] private float locationMaxOffset = 0.5f;                    // How far away from location goal is considered to still be goal achieved
    [SerializeField] private Vector2 defaultFacing = Vector2.right;             // When idle and standing, the creature will default to this facing angle

    // Handled by detection system
    [HideInInspector] public ulong targetID = 0;    // If 0 - it means no target
    private GameObject target = null;               // Target game object
    private Vector2? targetLastPos = null;          // Last position of target - or null if no target

    private aiState state = aiState.idle;           // All actions are determined by current state
    private int routineIndex = 0;                   // Current index in routine array to go to

    private Coroutine timer = null;
    private float countdown = 0.0f;

    private CircleCollider2D detectorCollider;
    private Dictionary<GameObject, float> entities = new Dictionary<GameObject, float>();   // Entities in range to detect. They will still be scanned for line of sight
    private List<GameObject> entitiesSorted = null;                                         // List of objects from above dictionary, to be sorted by distance and line of sight
    private float detectionRangeCurrent = 0.0f;                                             // Detection range differs on which state we are in currently

    private bool coroutinesRunning = false;

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

    public void Awake()
    {
        SpawnDetectorObject();
    }

    public void Start()
    {
        StartCoroutine(StartCoroutines());
    }

    private void OnDestroy()
    {
        // Dont stop all coroutines - AI might be disabled separately from behaviour
        StopCoroutine(UpdateMovement());
        StopCoroutine(UpdateFacing());
        StopCoroutine(AIUpdate());
        if (timer != null) StopCoroutine(timer);
    }

    private void OnEnable()
    {
        StartCoroutine(StartCoroutines());
    }

    private void OnDisable()
    {
        coroutinesRunning = false;
    }

    public void ObtainTargetLocation()
    {      
        // If we have object but not id then grab ID
        if (target && (targetID == 0)) targetID = target.GetComponent<EntityBehaviour>().ID;
        // If we have ID but not object then grab object
        if (!target && (targetID != 0)) target = HelpFunc.FindEntityByID(targetID);
        // If object wasnt found then it must have been destroyed so we have no target
        if (!target) targetID = 0;
        // If we do have target then grab the position
        if (target) targetLastPos = target.transform.position;
    }

    // Run this if entity can be scanned for line of sight
    public void AddEntityDetected(GameObject entity)
    {
        CreatureBehaviour b = entity.GetComponent<CreatureBehaviour>();
        if (!b) return;
        bool add = ShouldAggro(behaviour.faction, b.faction);
        if (add)
        {
            entities[entity] = (entity.transform.position - transform.position).magnitude;
        }
    }

    public void RemoveEntityDetected(GameObject entity)
    {
        entities.Remove(entity);
    }

    // Notifies the AI that its being attacked
    public void NotifyTakingDamage(ulong aggressorID, FactionAllegiance fromFaction)
    {
        // Raise caution level
        if (state == aiState.idle)
        {
            state = aiState.caution;
            locationGoal = null;
            StartTimer(cautionTime);
        }
        // If player attacking town - turn hostile
        if (fromFaction == FactionAllegiance.player && behaviour.faction == FactionAllegiance.NPC)
        {
            // Set me aggressive
            GlobalControl.decisions.causedVillageTrouble = true;
            if (behaviour is NPCBehaviour) SetNPCAggressive(aggressorID, (NPCBehaviour)behaviour);
            // Call for help
            foreach (CreatureBehaviour creature in HelpFunc.GetCreaturesInRadiusByHitbox(transform.position, 10f))
            {
                if (creature is NPCBehaviour && ((NPCBehaviour)creature).faction == FactionAllegiance.NPC) SetNPCAggressive(aggressorID, (NPCBehaviour)creature);
            }
        }
        // Update detection range
        UpdateDetectionRadius();
        // Add entity to be scanned
        GameObject owner = HelpFunc.FindEntityByID(aggressorID);
        if (owner != null) entities[owner] = (owner.transform.position - transform.position).magnitude;
    }

    // Create detector game object with correct detection range and attach it to this entity
    private void SpawnDetectorObject()
    {
        GameObject detector = new GameObject("Detector");
        detector.transform.parent = transform;
        detector.transform.localPosition = Vector3.zero;
        detector.layer = 8;
        Detection detection = detector.AddComponent<Detection>();
        detection.behaviour = behaviour;
        detectorCollider = detector.AddComponent<CircleCollider2D>();
        detectorCollider.radius = detectionRange;
        detectorCollider.isTrigger = true;
    }

    // Updates friendly NPC to become aggressive and arm them with weapon
    private void SetNPCAggressive(ulong aggressorID, NPCBehaviour behaviour)
    {
        behaviour.faction = FactionAllegiance.NPCaggressive;
        behaviour.interactible = false;
        WeaponData weapon = GetRandomWeapon(new List<ItemTier>() { ItemTier.medium, ItemTier.strong });
        if (weapon != null)
        {
            behaviour.loot.Add(ContentGenerator.FabricateWeapon(weapon));
            weapon.unlimitedAmmo = true;
            weapon.pickable = false;
            behaviour.SetItemActive(weapon);
        }
        // Add entity to be scanned
        GameObject owner = HelpFunc.FindEntityByID(aggressorID);
        if (owner != null) entities[owner] = (owner.transform.position - transform.position).magnitude;
    }

    private IEnumerator StartCoroutines()
    {
        yield return null;
        if (!coroutinesRunning)
        {
            UpdateDetectionRadius();
            StartCoroutine(UpdateMovement());
            StartCoroutine(UpdateFacing());
            StartCoroutine(AIUpdate());
            StartTimer(countdown);
            coroutinesRunning = true;
        }
    }

    // Get distance to each entity in list and sort it by those distances
    private void UpdateEntitiesDistance()
    {
        Dictionary<GameObject, float> entitiesCopy = new Dictionary<GameObject, float>(entities);
        foreach (KeyValuePair<GameObject, float> pair in entitiesCopy)
        {
            float distance = (pair.Key.transform.position - transform.position).magnitude;
            entities[pair.Key] = distance;
        }
        entitiesSorted = entities.OrderBy(x => x.Value).Select(x => x.Key).ToList();
    }

    /* Causes entity to start moving towards target or stop entirely. This system should be upgraded using a proper pathfinding algorithm.
     * Currently the entities will simply move in straight line to target.
     */
    private IEnumerator UpdateMovement()
    {
        while (true)
        {
            // If there is a goal - start moving towards it
            if (GlobalControl.paused) yield return new WaitForSeconds(.1f);
            if (LocationGoalAchieved()) locationGoal = null;
            if (locationGoal != null)
            {
                Vector2 desiredDirection = locationGoal.Value - (Vector2)transform.position;
                behaviour.SetSpeed(behaviour.moveSpeed);
                behaviour.SetMoveVector(desiredDirection);
            }
            // Stop if no goal
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
            // We have a target - look at it
            if (GlobalControl.paused) yield return new WaitForSeconds(.1f);
            if (locationGoal.HasValue)
            {
                Vector2 desiredDirection = locationGoal.Value - (Vector2)transform.position;
                behaviour.SetFacingVector(desiredDirection);
                behaviour.SetAimingLocation((Vector2)transform.position + desiredDirection);
            }
            // Not target but we are moving. Look at desired location
            else if (targetID == 0)
            {
                behaviour.SetFacingVector(defaultFacing);
                behaviour.SetAimingLocation((Vector2)transform.position + defaultFacing);
            }
            // Default facing
            else
            {
                Vector2 desiredDirection = targetLastPos.Value - (Vector2)transform.position;
                behaviour.SetFacingVector(desiredDirection);
                behaviour.SetAimingLocation(targetLastPos.Value);
            }
            yield return new WaitForSeconds(.1f);
        }
    }

    // Causes actions to happen depending on current state
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
            // Search for current target line of sight
            if (HaveTarget())
            {
                ScanEntities(new List<GameObject>() { target });
            }
            // Search for a new target
            else
            {
                UpdateEntitiesDistance();
                ScanEntities(entitiesSorted);
            }
            UpdateAIState();
            yield return new WaitForSeconds(.5f);
        }
    }

    // Scan each potential enemy for line of sight starting with closest
    private void ScanEntities(List<GameObject> entityList)
    {
        bool targetKept = false;
        foreach (GameObject entity in entityList)
        {
            RaycastHit2D hit = RaycastEntity(entity);
            if (hit.collider == null) continue;
            Transform hitParentTransform = hit.collider.transform.parent;
            if (hitParentTransform == null) continue;
            if (hitParentTransform.gameObject == entity)
            {
                // Target acquired
                target = entity;
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

    // Update detector object to have radius matching to current detection radius
    private void UpdateDetectionRadius()
    {
        detectorCollider.radius = 0f;
        detectionRangeCurrent = 0f;
        switch (state)
        {
            case aiState.hold:
                {
                    detectorCollider.radius = 0f;
                    detectionRangeCurrent = 0f;
                    break;
                }
            case aiState.idle:
                {
                    detectorCollider.radius = detectionRange;
                    detectionRangeCurrent = detectionRange;
                    break;
                }
            default:
                detectorCollider.radius = detectionRangeCaution;
                detectionRangeCurrent = detectionRangeCaution;
                break;
        }
    }

    // Primary function of AI class. AI state logic is setup here
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
                    // cycle idle route
                    if (LocationGoalAchieved())
                    {
                        if (routineIndex >= idleRoutine.Count) routineIndex = 0;
                        if (idleRoutine.Count > 0) locationGoal = idleRoutine[routineIndex];
                        routineIndex++;
                    }
                    // target spotted - begin caution phase
                    if (HaveTarget())
                    {
                        state = aiState.caution;
                        UpdateDetectionRadius();
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
                            UpdateDetectionRadius();
                            StartTimer(chaseTime);
                            break;
                        }
                        else
                        {
                            // they got away - resume routine
                            StartTimer(0.0f);
                            state = aiState.idle;
                            locationGoal = null;
                            UpdateDetectionRadius();
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
                        UpdateDetectionRadius();
                        StartTimer(chaseTime);
                        break;
                    }
                    // they got away - resume routine
                    if (TimerUp())
                    {
                        state = aiState.idle;
                        locationGoal = null;
                        UpdateDetectionRadius();
                        routineIndex = 0;
                        break;
                    }
                    break;
                }
            case aiState.track:
                {
                    // check if detection still has target and if we are not tired of chasing
                    if (!HaveTarget())
                    {
                        // target lost, search for target
                        state = aiState.search;
                        UpdateDetectionRadius();
                        StartTimer(searchTime);
                        break;
                    }
                    if (TimerUp())
                    {
                        state = aiState.idle;
                        locationGoal = null;
                        UpdateDetectionRadius();
                        routineIndex = 0;
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
                        UpdateDetectionRadius();
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
                        UpdateDetectionRadius();
                        StartTimer(searchTime);
                        break;
                    }
                    // set attack target for weapons and execute the attack
                    ObtainTargetLocation();
                    behaviour.SetAttackTarget(target);
                    if (behaviour.AnyWeaponOnTarget(accuracyAllowance)) behaviour.Attack();
                    // path to preferred distance
                    Vector2 currLocation = transform.position;
                    float currDistance = (targetLastPos.Value - currLocation).magnitude;
                    if (currDistance > preferredFightDistance + distanceMaxOffset ||
                        currDistance < preferredFightDistance - distanceMaxOffset) 
                        locationGoal = HelpFunc.GetPointAtDistance(currLocation, targetLastPos.Value, preferredFightDistance);
                    break;
                }
            default:
                break;
        }
    }

    // True if location is in accepted bounds
    private bool LocationGoalAchieved()
    {
        if (locationGoal == null) return true;
        Vector2 currLocation = behaviour.transform.position;
        float currX = currLocation.x;
        float currY = currLocation.y;
        float goalX = locationGoal.Value.x;
        float goalY = locationGoal.Value.y;
        bool xMatching = Mathf.Abs(goalX - currX) < locationMaxOffset;
        bool yMatching = Mathf.Abs(goalY - currY) < locationMaxOffset;
        return xMatching && yMatching;
    }

    // Send a raycast on Vision layer to check for line of sight blocks
    private RaycastHit2D RaycastEntity(GameObject entity)
    {
        CreatureBehaviour b = entity.GetComponent<CreatureBehaviour>();
        Vector2 targetLoc = (Vector2)b.visionCollider.transform.position + b.visionCollider.offset;
        Vector2 originLoc = (Vector2)behaviour.visionCollider.transform.position + behaviour.visionCollider.offset;
        Vector2 targetVec = targetLoc - originLoc;
        originLoc = originLoc + targetVec.normalized * 0.2f;
        RaycastHit2D hit = Physics2D.Raycast(originLoc, targetVec, detectionRangeCurrent, 1 << 9);
        // Enable this for testing
        //Debug.DrawRay(originLoc, targetVec.normalized * detectionRangeCurrent, Color.yellow);
        return hit;
    }

    // Conditions for when we detect and begin aggression as AI
    private bool ShouldAggro(FactionAllegiance factionMe, FactionAllegiance factionOther)
    {
        if (factionMe == FactionAllegiance.berserk) return true;
        if (factionMe == FactionAllegiance.hostile && factionOther != FactionAllegiance.hostile) return true;
        if (factionMe == FactionAllegiance.NPC && factionOther == FactionAllegiance.hostile) return true;
        if (factionMe == FactionAllegiance.NPCaggressive && factionOther == FactionAllegiance.player) return true;
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
        timer = null;
    }

    public CreatureAIData Save()
    {
        CreatureAIData data = new CreatureAIData();
        data.locationGoal = locationGoal.HasValue ? HelpFunc.VectorToArray(locationGoal.Value) : null;
        data.detectionRange = detectionRange;
        data.detectionRangeCaution = detectionRangeCaution;
        data.detectionRangeCurrent = detectionRangeCurrent;
        data.attackDistance = attackDistance;
        data.preferredFightDistance = preferredFightDistance;
        data.cautionTime = cautionTime;
        data.searchTime = searchTime;
        data.chaseTime = chaseTime;
        data.idleRoutine = HelpFunc.VectorListToArrayList(idleRoutine);
        data.locationMaxOffset = locationMaxOffset;
        data.distanceMaxOffset = distanceMaxOffset;
        data.targetID = targetID;
        data.targetLastPos = targetLastPos.HasValue ? HelpFunc.VectorToArray(targetLastPos.Value) : null;
        data.state = state;
        data.routineIndex = routineIndex;
        data.countdown = countdown;
        data.defaultFacing = HelpFunc.VectorToArray(defaultFacing);
        return data;
    }

    public void Load(CreatureAIData data, bool loadTransform = true)
    {
        locationGoal = (data.locationGoal != null) ? HelpFunc.DataToVec2(data.locationGoal) : null;
        detectionRange = data.detectionRange;
        detectionRangeCaution = data.detectionRangeCaution;
        detectionRangeCurrent = data.detectionRangeCurrent;
        attackDistance = data.attackDistance;
        preferredFightDistance = data.preferredFightDistance;
        cautionTime = data.cautionTime;
        searchTime = data.searchTime;
        chaseTime = data.chaseTime;
        idleRoutine = HelpFunc.DataToListVec2(data.idleRoutine);
        locationMaxOffset = data.locationMaxOffset;
        distanceMaxOffset = data.distanceMaxOffset;
        targetID = data.targetID;
        if (targetID != 0) target = HelpFunc.FindEntityByID(targetID);
        targetLastPos = (data.targetLastPos != null) ? HelpFunc.DataToVec2(data.targetLastPos) : null;
        state = data.state;
        routineIndex = data.routineIndex;
        countdown = data.countdown;
        defaultFacing = HelpFunc.DataToVec2(data.defaultFacing);
        if (countdown > 0) StartTimer(countdown);
    }
}

[Serializable]
public class CreatureAIData
{
    public CreatureAIData() { }

    public float[] locationGoal;
    public float detectionRange;
    public float detectionRangeCaution;
    public float detectionRangeCurrent;
    public float attackDistance;
    public float preferredFightDistance;
    public float cautionTime;
    public float searchTime;
    public float chaseTime;
    public List<float[]> idleRoutine;
    public float locationMaxOffset;
    public float distanceMaxOffset;
    public ulong targetID;
    public float[] targetLastPos;
    public aiState state;
    public int routineIndex;
    public float countdown;
    public float[] defaultFacing;

}