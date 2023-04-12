using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class DialogOption
{
    public delegate void Effect();

    public ulong ID = 0;
    public List<ulong> optionIDsResulting = new List<ulong>();
    public List<Effect> effects = new List<Effect>();
    public bool doClearDialog = true;

    public DialogOption(ulong iD, List<ulong> optionIDsResulting)
    {
        ID = iD;
        this.optionIDsResulting = optionIDsResulting;
    }

    public void DoEffect()
    {
        foreach (Effect effect in effects) effect();
    }

}
