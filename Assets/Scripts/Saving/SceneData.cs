using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* Contains state of all entities, items etc. in the scene
 */
[Serializable]
public class SceneData
{
    public string name;
    public CameraData cameraData;
    public List<HumanoidData> humanoids = new List<HumanoidData>();
    public List<BulletData> bullets = new List<BulletData>();
}
