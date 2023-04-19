using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class DialogOption
{
    public delegate void Effect();

    public int ID = 0;
    public List<int> optionIDsResulting = new List<int>();
    public List<Effect> effects = new List<Effect>();
    public bool doClearDialog = true;

    public DialogOption(int iD, List<int> optionIDsResulting)
    {
        ID = iD;
        this.optionIDsResulting = optionIDsResulting;
    }

    public void DoEffect()
    {
        foreach (Effect effect in effects) effect();
    }

}
