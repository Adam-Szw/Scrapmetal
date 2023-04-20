using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCBehaviour : HumanoidBehaviour, Saveable<NPCData>, Spawnable<NPCData>
{

    public bool interactible = false;
    public int dialogStartID = 0;
    public string NPCname = "No Name";

    protected new void Start()
    {
        base.Start();
        if (interactible)
        {
            AddInteractionCollider();
            interactionEnterEffect = (CreatureBehaviour user) => { InteractionEnter(user); };
            interactionUseEffect = (CreatureBehaviour user) => { InteractionUse(); };
        }
    }

    private void InteractionUse()
    {
        // Open dialog
        UIControl.showDialog(DialogLibrary.GetDialogConditionedID(dialogStartID), NPCname, this);
    }

    private void InteractionEnter(CreatureBehaviour user)
    {
        // Do nothing if the user is not player
        if (user is not PlayerBehaviour) return;
        StartCoroutine(SpawnInteractionTextCoroutine("Press (E) to talk", PlayerBehaviour.interactibleInteravalTime, 0f));
    }

    new public NPCData Save()
    {
        NPCData data = new NPCData(base.Save());
        data.interactible = interactible;
        data.dialogStartID = dialogStartID;
        data.NPCname = NPCname;
        return data;
    }

    public void Load(NPCData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        interactible = data.interactible;
        dialogStartID = data.dialogStartID;
        NPCname = data.NPCname;
    }

    public static GameObject Spawn(NPCData data, Vector2 position, Quaternion rotation, Vector2 scale, Transform parent = null)
    {
        GameObject obj = HumanoidBehaviour.Spawn(data, position, rotation, scale, parent);
        obj.GetComponent<NPCBehaviour>().Load(data, false);
        return obj;
    }

    public static GameObject Spawn(NPCData data, Transform parent = null)
    {
        GameObject obj = HumanoidBehaviour.Spawn(data, parent);
        obj.GetComponent<NPCBehaviour>().Load(data);
        return obj;
    }
}

[Serializable]
public class NPCData : HumanoidData
{
    public NPCData() { }

    public NPCData(HumanoidData data) : base(data)
    {
        itemActive = data.itemActive;
        bodypartData = data.bodypartData;
        animationData = data.animationData;
        randomizeParts = data.randomizeParts;
        bodypartsGenerated = data.bodypartsGenerated;
    }

    public bool interactible;
    public int dialogStartID;
    public string NPCname;

}

