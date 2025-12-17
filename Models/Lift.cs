using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiDataSimulator.Models;


public class Lift
{
    public int Id { get; set; }
    public int VerticalDrop { get; set; }
    public string Name { get; set; }
    public int ResortId { get; set; }
}
