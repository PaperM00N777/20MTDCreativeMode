using flanne;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Final
{
    public class GameAction
    {
        public string Name { get; set; } 
        public bool Complete { get; set; } = false;
        public Action action { get; set; }
        public GameAction(string name)
        {
            Name = name;
        }
        public void execute()
        {
            action?.Invoke();
        }

    }
}

