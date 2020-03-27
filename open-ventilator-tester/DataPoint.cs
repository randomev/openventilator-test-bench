using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace open_ventilator_tester
{
    class DataPoint
    {
        public double y { get; set; }
        public long x { get; set; }
        public DataPoint(double value)
        {
            this.y = value;
            this.x = DateTime.Now.Ticks;
        }
    }
}
