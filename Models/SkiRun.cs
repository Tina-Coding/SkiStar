namespace SkidataWpf.Models;

public class SkiRun
{
    public int SkipassId { get; set; }
    public int LiftId { get; set; }
    public int SeasonId { get; set; }
    public DateTime Timestamp { get; set; }

    public SkiRun(int skipassId, int liftId, DateTime timestamp, int seasonId)
    {
        SkipassId = skipassId;
        LiftId = liftId;
        Timestamp = timestamp;
        SeasonId = seasonId;
    }
    public SkiRun()
    {

    }
}
