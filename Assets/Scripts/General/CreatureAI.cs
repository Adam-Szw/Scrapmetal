using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static CreatureBehaviour;
using static UnityEngine.EventSystems.EventTrigger;

public class CreatureAI : MonoBehaviour
{
    public CreatureBehaviour behaviour;

    [SerializeField] private Vector2? locationGoal = null;
    [SerializeField] private float detectionDistance = 0.0f;
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
    private float countdown = 0.0f;

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

    public void Update()
    {
        if (GlobalControl.paused) return;
        // Disable yourself if dead
        if (!behaviour.GetAlive())
        {
            enabled = false;
            return;
        }
        DetectEnemies();
        UpdateAIState();
        UpdateMovement();
        UpdateFacing();
    }

    public void ObtainTargetLocation()
    {
        if (targetID == 0) return;
        if (target == null) target = HelpFunc.FindGameObjectByBehaviourID(targetID);
        targetLastPos = target.transform.position;
    }

    private void UpdateMovement()
    {
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
    }

    private void UpdateFacing()
    {
        if (targetID == 0)
        {
            behaviour.SetFacingVector(Vector2.right);
            behaviour.SetAimingVector((Vector2)transform.position + Vector2.right);
        }
        else
        {
            Vector2 desiredDirection = targetLastPos.Value - (Vector2)transform.position;
            behaviour.SetFacingVector(desiredDirection);
            behaviour.SetAimingVector(targetLastPos.Value);
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
                    if (LocationGoalAchieved()) countdown = idleTime;
                    // if timer finished, path to the next goal in routine or loop to start
                    if (countdown == 0.0f)
                    {
                        locationGoal = idleRoutine[routineIndex];
                        routineIndex++;
                        if (routineIndex >= idleRoutine.Count) routineIndex = 0;
                    }
                    // target spotted - begin caution phase
                    if (targetID != 0)
                    {
                        state = aiState.caution;
                        if (targetID != 0) countdown = cautionTime;
                        locationGoal = null;
                    }
                    // update timer
                    countdown = Mathf.Max(0.0f, countdown -= Time.deltaTime);
                    break;
                }
            case aiState.caution:
                {
                    // timer finished - check if we still have target
                    if (countdown == 0.0f)
                    {
                        if (targetID != 0)
                        {
                            // how dare you! time to fight
                            state = aiState.track;
                            break;
                        }
                        else
                        {
                            // they got away - resume routine
                            state = aiState.idle;
                            routineIndex = 0;
                            break;
                        }
                    }
                    // update timer
                    countdown = Mathf.Max(0.0f, countdown -= Time.deltaTime);
                    break;
                }
            case aiState.search:
                {
                    // path to last known location of target
                    locationGoal = targetLastPos;
                    if (targetID != 0)
                    {
                        // target reacquired - begin fighting again
                        state = aiState.track;
                        break;
                    }
                    if (countdown == 0.0f)
                    {
                        // they got away - resume routine
                        state = aiState.idle;
                        routineIndex = 0;
                        break;
                    }
                    // update timer
                    countdown = Mathf.Max(0.0f, countdown -= Time.deltaTime);
                    break;
                }
            case aiState.track:
                {
                    // check if detection still has target first
                    if (targetID == 0)
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
                        break;
                    }
                    break;
                }
            case aiState.attack:
                {
                    // check if detection still has target first
                    if (targetID == 0)
                    {
                        // target lost, search for target
                        state = aiState.search;
                        countdown = searchTime;
                        break;
                    }
                    // set attack target for weapons and execute the attack
                    ObtainTargetLocation();
                    behaviour.SetAttackTarget(target);
                    if (behaviour.AnyWeaponOnTarget()) behaviour.Attack();
                    // path to preferred distance
                    Vector2 currLocation = transform.position;
                    float currDistance = (targetLastPos.Value - currLocation).magnitude;
                    if(currDistance > preferredFightDistance + DISTANCE_MAX_OFFSET ||
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

    private void DetectEnemies()
    {
        target = null;
        targetID = 0;
        targetLastPos = null;

        // NOTE: THIS COULD BE OPTIMIZED!

        // Gather all entities in detection radius
        List<Tuple<GameObject, float>> toScan = new List<Tuple<GameObject, float>>();
        List<GameObject> entities = HelpFunc.GetCreaturesInRadius(transform.position, detectionDistance);
        foreach (GameObject entity in entities)
        {
            // Only attempt detection if we want to be hostile
            CreatureBehaviour behaviour = entity.GetComponent<CreatureBehaviour>();
            if (behaviour.ID == this.behaviour.ID) continue;
            bool detect = DetectAggro(this.behaviour.faction, behaviour.faction);
            if (detect)
            {
                float distance = (entity.transform.position - transform.position).magnitude;
                toScan.Add(new Tuple<GameObject, float>(entity, distance));
            }
        }
        // Sort entities to scan by distance to us
        toScan.Sort((x, y) => x.Item2.CompareTo(y.Item2));
        foreach (Tuple<GameObject, float> tpl in toScan)
        {
            // Scan each potential enemy for line of sight
            GameObject obj = tpl.Item1;
            CreatureBehaviour behaviour = obj.GetComponent<CreatureBehaviour>();
            RaycastHit2D hit = RaycastEntity(obj);
            if (hit.collider != null)
            {
                // Target acquired
                target = obj;
                EntityBehaviour b = obj.GetComponent<EntityBehaviour>();
                targetID = b.ID;
                targetLastPos = obj.transform.position;
                return;
            }
        }

    }

    private RaycastHit2D RaycastEntity(GameObject entity)
    {
        // Temporarily change layer of our vision blocker so it doesnt block the raycast
        behaviour.visionBlocker.gameObject.layer = 7;
        Vector2 targetLoc = entity.GetComponent<CreatureBehaviour>().visionBlocker.transform.position;
        Vector2 position = behaviour.visionBlocker.transform.position;
        Vector2 targetVec = targetLoc - position;
        RaycastHit2D hit = Physics2D.Raycast(position, targetVec, detectionDistance, 3);
        Debug.DrawRay(position, targetVec * detectionDistance, Color.yellow, 0.1f, false);
        behaviour.visionBlocker.gameObject.layer = 3;
        return hit;
    }

    // Conditions for when we detect and begin aggression as AI
    private bool DetectAggro(FactionAllegiance factionMe, FactionAllegiance factionOther)
    {
        if (factionMe == FactionAllegiance.berserk) return true;
        if (factionMe == FactionAllegiance.hostile && factionOther != FactionAllegiance.hostile) return true;
        if (factionMe == FactionAllegiance.NPC && factionOther == FactionAllegiance.hostile) return true;
        if (factionMe == FactionAllegiance.NPCaggro && factionOther == FactionAllegiance.player) return true;
        if (factionMe == FactionAllegiance.enemy && factionOther == FactionAllegiance.player) return true;
        return false;
    }

}
