using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace open_ventilator_tester
{
    public class DataField
    {
        public string Name { get; set; }
        public int Index { get; set; }

        public DataField(string name, int index)
        {

            this.Name = name;
            this.Index = index;

        }
    }
}
