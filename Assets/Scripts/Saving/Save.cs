using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* This class contains total serialized data of a save. This includes state of all scenes
 * as well as playthrough information that we might want to track
 */
[Serializable]
public class Save
{
    public string currentScene;
    public PlayerData playerData;
    public List<SceneData> scenes = new List<SceneData>();
}
