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
    public List<EntityData> entities;
    public List<CellData> cells;

    public SceneData(string name, CameraData cameraData, List<EntityData> entities, List<CellData> cells)
    {
        this.name = name;
        this.cameraData = cameraData;
        this.entities = entities;
        this.cells = cells;
    }
}
