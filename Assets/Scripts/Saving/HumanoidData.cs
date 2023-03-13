using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static HumanoidAnimations;

/* Saved data for humanoid characters
 */
[Serializable]
public class HumanoidData : EntityData
{
    public HumanoidData(EntityData data)
    {
        this.id = data.id;
        this.location = data.location;
        this.rotation = data.rotation;
        this.scale = data.scale;
        this.velocity = data.velocity;
        this.speed = data.speed;
        this.alive = data.alive;
        this.health = data.health;
    }

    // Behaviour data
    public WeaponData weaponActive;

    // Graphic data
    public List<string> bodypartData;

    // Animation data
    public HumanoidAnimationData animationData;
}

[Serializable]
public class HumanoidAnimationData
{
    public handsState stateHands;
    public movementState stateMovement;
    public float[] facingVector;
    public float[] movementVector;
    public float[] aimingVector;
    public List<Tuple<int, float>> animatorsState;
    public List<float> jointsAngles;
}