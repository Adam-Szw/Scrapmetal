using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class Decisions
{
    public bool welcomeShown = false;
    public bool c320GreetingDone = false;
    public bool elderGreetingDone = false;
    public bool elderQuestAccepted = false;
    public bool elderQuestFulfilled = false;
    public bool elderQuestCompleted = false;
    public bool causedVillageTrouble = false;
    public bool gyroGreetingDone = false;
    public bool gyroShopInventoryLoaded = false;
    public bool jesseGreetingDone = false;
    public bool jesseShopInventoryLoaded = false;
}
