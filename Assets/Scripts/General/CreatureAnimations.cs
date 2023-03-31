using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static CreatureAnimations;
using static HumanoidAnimations;
using movementState = CreatureAnimations.movementState;

public class CreatureAnimations
{
    protected Transform transform;
    protected List<Animator> animators;

    /* Joints that can be controlled by scripts. they get a suffix _Parent
     * The reasoning here is that we have separate joints for animation frames and for script control
     */
    public struct Joint
    {
        public GameObject obj;
        public string name;
    }
    protected List<Joint> joints;

    // All creatures use idle and walk by default. Some creatures (such as humanoids) can use run
    public enum movementState
    {
        idle, walk, run
    }

    // Flags that control animations of child classes. They dont always have to be used.
    protected bool alive = true;
    protected movementState stateMovement = movementState.idle;
    protected Vector2 facingVector = Vector2.zero;
    protected Vector2 movementVector = Vector2.zero;

    // Useful flags for calculations. Readonly for outside of this class
    private enum Direction
    {
        neutral, left, right, up, down
    }
    private Direction horizontalDirection = Direction.neutral;
    private Direction verticalDirection = Direction.neutral;
    private Direction facingHorizontalDirection = Direction.neutral;
    private Direction facingVerticalDirection = Direction.neutral;
    private bool movingBackward = false;        // these 2 are not the same. backwards refers only to horizontal
    private bool movingAgainstFacing = false;   // misalignment of running and facing direction

    public CreatureAnimations(Transform transform, List<Animator> animators, List<string> bodypartNames)
    {
        this.transform = transform;
        this.animators = animators;
        ListJoints(bodypartNames);
    }

    public void SetVectors(Vector2 movementVector, Vector2 facingVector)
    {
        if (!alive) return;
        this.movementVector = movementVector;
        this.facingVector = facingVector;

        // Update direction flags of movement and facing
        UpdateDirections(movementVector, facingVector);

        // Apply transformations based on facing direction
        switch (facingHorizontalDirection)
        {
            case Direction.right:
                FlipSprite(true);
                break;
            case Direction.left:
                FlipSprite(false);
                break;
        }
    }

    // Rotate a script-controlled joint to a certain angle. Actual rotation in any given frame is split
    // into steps calculated from completion time so call this every frame to complete the rotation
    public void RotateJoint(Joint joint, float targetAngle, float step, bool local = true)
    {
        if (!joint.obj) return;
        Transform transform = joint.obj.transform;
        Quaternion targetRotation = Quaternion.Euler(0.0f, 0.0f, targetAngle);
        if (local) transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetRotation, Time.deltaTime * step);
        else transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Time.deltaTime * step);
    }

    // Resets script controlled joints to default (0 degrees) angle
    public void ResetJoints()
    {
        foreach(Joint joint in joints) if (joint.obj) joint.obj.transform.localEulerAngles = Vector3.zero;
    }

    public void FlipSprite(bool right)
    {
        Vector3 scale = transform.localScale;
        Vector3 newScale = scale;
        newScale.x = Mathf.Abs(newScale.x) * (right ? 1.0f : -1.0f);
        transform.localScale = newScale;
    }

    public movementState GetStateMovement() { return stateMovement; }

    public Vector2 GetFacingVector() { return facingVector; }

    public Vector2 GetMovementVector() { return movementVector; }

    public bool IsMovingBackwards() { return movingBackward; }

    public bool IsMovingAgainstFacing() { return movingAgainstFacing; }

    // Update state and cause animation update
    public void SetStateMovement(movementState state)
    {
        stateMovement = state;
        UpdateAnimators();
    }

    // Update alive flag and change state of animation based on that
    public void SetAlive(bool alive)
    {
        this.alive = alive;
        UpdateAnimators();
        if (!alive) ResetJoints();
    }

    virtual public void PlayFlinch() { }

    public virtual void UpdateRotations() { }

    /* This is where animators are set based on whether or not the character holds weapons, is running etc.
    * Should be called whenever changes are made to the animations state
    */
    protected void UpdateAnimators()
    {
        foreach (Animator animator in animators) animator.SetBool("Alive", alive);
        if (stateMovement == movementState.idle)
            foreach (Animator animator in animators) animator.SetInteger("MovementState", 0);

        if (stateMovement == movementState.walk)
            foreach (Animator animator in animators) animator.SetInteger("MovementState", !IsMovingBackwards() ? 1 : 2);

        if (stateMovement == movementState.run)
            foreach (Animator animator in animators) animator.SetInteger("MovementState", 3);
    }

    protected Joint? GetJointByName(string name)
    {
        foreach (Joint j in joints) if (j.name.Equals(name)) return j;
        return null;
    }

    // This will update the list of joints available to the script
    private void ListJoints(List<string> bodypartNames)
    {
        joints = new List<Joint>();
        foreach (string bodypartName in bodypartNames)
        {
            string jointName = bodypartName + "_Parent";
            Joint joint = new Joint();
            joint.name = jointName;
            joint.obj = HelpFunc.RecursiveFindChild(transform.gameObject, jointName);
            joints.Add(joint);
        }
    }

    private void UpdateDirections(Vector3 velocityVector, Vector3 facingVector)
    {
        horizontalDirection = Direction.neutral;
        if (velocityVector.x > 0.0f) horizontalDirection = Direction.right;
        if (velocityVector.x < 0.0f) horizontalDirection = Direction.left;
        verticalDirection = Direction.neutral;
        if (velocityVector.y > 0.0f) verticalDirection = Direction.up;
        if (velocityVector.y < 0.0f) verticalDirection = Direction.down;
        facingHorizontalDirection = Direction.neutral;
        if (facingVector.x > 0.0f) facingHorizontalDirection = Direction.right;
        if (facingVector.x < 0.0f) facingHorizontalDirection = Direction.left;
        facingVerticalDirection = Direction.neutral;
        if (facingVector.y > 0.0f) facingVerticalDirection = Direction.up;
        if (facingVector.y < 0.0f) facingVerticalDirection = Direction.down;

        movingBackward = false;
        if (horizontalDirection != Direction.neutral &&
            facingHorizontalDirection != Direction.neutral &&
            horizontalDirection != facingHorizontalDirection) movingBackward = true;

        movingAgainstFacing = false;
        if (horizontalDirection != Direction.neutral && horizontalDirection != facingHorizontalDirection) movingAgainstFacing = true;
        if (verticalDirection != Direction.neutral && verticalDirection != facingVerticalDirection) movingAgainstFacing = true;
    }

    private List<Tuple<int, float>> GetState()
    {
        List<Tuple<int, float>> ret = new List<Tuple<int, float>>();
        foreach(Animator animator in animators)
        {
            int state = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
            float progress = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            ret.Add(new Tuple<int, float>(state, progress));
        }
        return ret;
    }

    private List<float[]> GetJointsAngles()
    {
        List<float[]> jointsAngles = new List<float[]>();
        foreach(Joint joint in joints)
        {
            jointsAngles.Add(HelpFunc.VectorToArray(joint.obj.transform.localEulerAngles));
        }
        return jointsAngles;
    }

    public CreatureAnimationData Save()
    {
        CreatureAnimationData data = new CreatureAnimationData();
        data.stateMovement = GetStateMovement();
        data.facingVector = HelpFunc.VectorToArray(GetFacingVector());
        data.movementVector = HelpFunc.VectorToArray(GetMovementVector());
        data.animatorsState = GetState();
        data.jointsAngles = GetJointsAngles();
        return data;
    }

    public void Load(CreatureAnimationData data)
    {
        SetStateMovement(data.stateMovement);
        SetVectors(HelpFunc.DataToVec2(data.facingVector), HelpFunc.DataToVec2(data.movementVector));
        List<Tuple<int, float>> state = data.animatorsState;
        for(int i = 0; i < state.Count; i++)
        {
            animators[i].Play(state[i].Item1, 0, state[i].Item2);
        }
        for (int i = 0; i < data.jointsAngles.Count; i++)
        {
            RotateJoint(joints[i], HelpFunc.DataToVec3(data.jointsAngles[i]).z, 360.0f);
        }
    }

}

[Serializable]
public class CreatureAnimationData
{
    public CreatureAnimationData() { }

    public movementState stateMovement;
    public float[] facingVector;
    public float[] movementVector;
    public List<Tuple<int, float>> animatorsState;
    public List<float[]> jointsAngles;
}
