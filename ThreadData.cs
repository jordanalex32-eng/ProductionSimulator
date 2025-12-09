using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductionSimulator
{
     public class ThreadData
    {
        public int Id { get; set; }

        public string Name { get; set; } = "ThreadRun";

        public ProductionPallet pallet { get; set; } = new ProductionPallet(null,null);
    }
}
