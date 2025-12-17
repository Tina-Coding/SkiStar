using SkiDataSimulator.Models;
using SkiDataSimulator.Repositories;
using SkidataWpf.Models;

namespace SkiDataSimulator.Simulation
{
    public class SkiSimulator
    {
        private static readonly Random _random = new();
        private readonly DbRepository _dbRepository;

        public SkiSimulator(DbRepository dbRepository)
        {
            _dbRepository = dbRepository;
        }

        /// <summary>
        /// Simulerar en hel skidsäsong för en lista av skidpass.
        /// Genererar skiddagar och skidåkning för varje åkare under säsongen.
        /// </summary>
        public async Task<List<SkiRun>> SimulateSeasonAsync(List<SkiPass> skiPasses, DateTime dayInSeason)
        {
            try
            {
                Season? season = await _dbRepository.GetSeasonByDateAsync(dayInSeason);

                if (season == null)
                {
                    throw new Exception($"Ingen säsong hittades för datumet {dayInSeason.ToShortDateString()}.");
                }

                List<SkiRun> allRuns = new();

                foreach (var skiPass in skiPasses)
                {
                    var skiDays = GenerateSkiDays(season.StartDate, season.EndDate, GetRandomSkiDays());

                    foreach (var skiDay in skiDays)
                    {
                        var skiRuns = await SimulateDayForOneSkipassAsync(skiPass, skiDay, season);
                        allRuns.AddRange(skiRuns);
                    }
                }
                return allRuns;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fel vid simulering av skidsäsong: {ex.Message} ", ex);
            }
        }

        /// <summary>
        /// Simulerar en skiddag för en lista av skidåkare.
        /// </summary>
        public async Task<List<SkiRun>> SimulateDayForAllSkipassesAsync(List<SkiPass> skiPasses, DateTime date)
        {
            try
            {
                Season? season = await _dbRepository.GetSeasonByDateAsync(date);

                if (season == null)
                {
                    throw new Exception($"Ingen säsong hittades för datumet {date.ToShortDateString()}.");
                }

                var tasks = skiPasses.Select(skiPass => SimulateDayForOneSkipassAsync(skiPass, date, season));
                var skiRunsList = await Task.WhenAll(tasks);
                return skiRunsList.SelectMany(runs => runs).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Fel vid simulering av skiddag för alla skidåkare: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Simulerar en skiddag för en enskild skidåkare.
        /// </summary>
        private async Task<List<SkiRun>> SimulateDayForOneSkipassAsync(SkiPass skiPass, DateTime date, Season season)
        {
            try
            {
                int rideCount = GetRandomRidesPerDay();
                List<SkiRun> skiRuns = new();
                Resort? resort = await _dbRepository.GetRandomResortAsync();
                if (resort == null)
                {
                    throw new Exception($"Hittade ingen skidanläggning.");
                }

                for (int i = 0; i < rideCount; i++)
                {
                    Lift lift = await _dbRepository.GetRandomSkiLiftFromResortAsync(resort);
                    if (lift == null)
                    {
                        throw new Exception($"Hittade ingen lift.");
                    }
                    DateTime rideTime = GenerateRideTimestamp(date);
                    skiRuns.Add(new SkiRun(skiPass.Id, lift.Id, rideTime, season.Id));
                }
                return skiRuns;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fel vid simulering av enskild skidåkare: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Genererar ett slumpmässigt antal skiddagar för en skidåkare.
        /// </summary>
        private int GetRandomSkiDays()
        {
            int roll = _random.Next(100);
            return roll switch
            {
                < 5 => _random.Next(1, 4),
                < 25 => _random.Next(4, 7),
                < 70 => _random.Next(7, 13),
                < 90 => _random.Next(13, 21),
                _ => _random.Next(21, 51)
            };
        }

        /// <summary>
        /// Genererar ett slumpmässigt antal åk för en skidåkare.
        /// </summary>
        private int GetRandomRidesPerDay()
        {
            int roll = _random.Next(100);
            return roll switch
            {
                < 5 => _random.Next(1, 4),
                < 25 => _random.Next(4, 7),
                < 70 => _random.Next(5, 10),
                < 90 => _random.Next(10, 20),
                _ => _random.Next(20, 46)
            };
        }

        /// <summary>
        /// Genererar en lista med slumpmässiga skiddagar under en period.
        /// </summary>
        private List<DateTime> GenerateSkiDays(DateTime dateStart, DateTime dateEnd, int totalDays)
        {
            List<DateTime> possibleDays = Enumerable.Range(0, (dateEnd - dateStart).Days + 1)
                .Select(offset => dateStart.AddDays(offset))
                .ToList();

            return Enumerable.Range(0, totalDays)
                .Select(_ => WeightedRandomDate(possibleDays))
                .OrderBy(d => d)
                .ToList();
        }

        /// <summary>
        /// Väljer en slumpmässig dag med högre sannolikhet för populära skidveckor.
        /// </summary>
        private DateTime WeightedRandomDate(List<DateTime> possibleDays)
        {
            if (_random.NextDouble() < 0.2) // 20% chans att välja högsäsongsdagar
            {
                var highSeasonDays = possibleDays.Where(d =>
                    (d.Month == 12 && d.Day >= 15) || (d.Month == 1 && d.Day <= 5) ||
                    (d.Month >= 2 && d.Month <= 3) || (d.Month == 4 && d.Day >= 10))
                    .ToList();

                if (highSeasonDays.Any())
                {
                    return highSeasonDays[_random.Next(highSeasonDays.Count)];
                }
            }
            return possibleDays[_random.Next(possibleDays.Count)];
        }

        /// <summary>
        /// Genererar en slumpmässig tidpunkt för ett skidåk.
        /// </summary>
        private DateTime GenerateRideTimestamp(DateTime date)
        {
            return date.Date.AddHours(9).AddMinutes(_random.Next(0, 451)); // Mellan 09:00 och 16:30
        }
    }
}
