using SkiDataSimulator.Models;

namespace SkidataWpf.Models;

public class SkiPass
{
    
    public int Id { get; set; }
    
    public string CardNumber { get; set; } // Till exempel e8e1761b-ef76-40f8-86e9-eed3c5865a2c
    // public int SkierId { get; set; }
    public int DestinationId { get; set; }
    public DateTime Start_date { get; set; }
    public DateTime End_date { get; set; }
    public int SkierId { get; set; } 

}
