using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/* Saved data for all entities
 */
[Serializable]
public class EntityData
{
    // Basic information
    public int id;
    public float[] location;
    public float[] rotation;
    public float[] scale;

    // Entity movement data
    public float[] velocity;
    public float speed;

    // Entity behaviour data
    public bool alive;
    public float health;

}
