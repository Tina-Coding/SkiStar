using Npgsql;
using SkiDataSimulator.Models;
using SkidataWpf.Models;
using System.Windows.Controls;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Windows.Documents;

namespace SkiDataSimulator.Repositories;

public class DbRepository
{
    private readonly string _connectionString;
    public DbRepository()
    {
        var config = new ConfigurationBuilder()
                     .AddUserSecrets<DbRepository>()
                     .Build();

        _connectionString = config.GetConnectionString("DefaultConnection"); // Hämtar hemlig information från jsonfil

    }

    /// <summary>
    /// Funktion som skapar och hämtar connection från databasen
    /// </summary>
    /// <returns></returns>
    private async Task<NpgsqlConnection> CreateAndOpenConnection()
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }

    /// <summary>
    /// Metod som lägger till ny skidåkare. Tar in förnamn och efternamn från gränssnittet för att skapa en skidåkare
    /// </summary>
    /// <param name="skier"></param>
    /// <returns></returns>
    public async Task CreateNewSkier(Skier skier)
    {
        try
        {
            using NpgsqlConnection conn = await CreateAndOpenConnection();

            using var command = new NpgsqlCommand("insert into skier(firstname, lastname) values(@firstname, @lastname)", conn);
            command.Parameters.AddWithValue("firstname", skier.Firstname);
            command.Parameters.AddWithValue("lastname", skier.Lastname);

            var result = await command.ExecuteNonQueryAsync();

        }
        catch (Exception)
        {

            throw;
        }


    }
    
    /// <summary>
    /// Metod som tar bort skidåkare och liftkort om skidåkaren inte har några åk registrerade
    /// </summary>
    /// <param name="firstName"></param>
    /// <param name="lastName"></param>
    /// <returns></returns>
    public async Task<bool> DeleteSkier(Skier skier)
    {
        using NpgsqlConnection conn = await CreateAndOpenConnection();

        using var transaction = await conn.BeginTransactionAsync();
        try
        {

            var command = new NpgsqlCommand("delete from ski_pass where skier_id = @id", conn, transaction);
            command.Parameters.AddWithValue("id", skier.Id);
            
            await command.ExecuteNonQueryAsync();  


            var command1 = new NpgsqlCommand("delete from skier where id = @id", conn, transaction);
            command1.Parameters.AddWithValue("id", skier.Id);
            
            await command1.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Funktion som tar bort alla skidåk för en vald skidåkare om skidåkaren har registrerade skidåk
    /// </summary>
    /// <param name="skier"></param>
    /// <returns></returns>
    public async Task<bool> DeleteAllSkiRuns(Skier skier)
    {
        try
        {

            using NpgsqlConnection conn = await CreateAndOpenConnection();
            using var command = new NpgsqlCommand("delete from ski_run using ski_pass where ski_run.skipass_id = ski_pass.id AND ski_pass.skier_id = @skierId;", conn);

            command.Parameters.AddWithValue("skierId", skier.Id);
            return await command.ExecuteNonQueryAsync() > 0;

        }
        catch (Exception)
        {
            throw;
        }
    

    }

    /// <summary>
    /// Funktion som valderar om den valda skidåkaren från gränssnittet har registrerade skidåk och returnerar en bool
    /// </summary>
    /// <param name="skier"></param>
    /// <returns></returns>
    public async Task<bool> ValidateSkiRun(Skier skier)
    {
        try
        {

            using NpgsqlConnection conn = await CreateAndOpenConnection();

            using var command = new NpgsqlCommand("select exists(select ski_run.id, ski_run.skipass_id, ski_run.season_id, ski_run.timestamp, " +
                                                   "ski_pass.skier_id from ski_run join ski_pass ON ski_pass.id = ski_run.skipass_id where ski_pass.skier_id=@skierId)", conn);

            command.Parameters.AddWithValue("skierId", skier.Id);

            var skierHasSkiRuns = (bool?)await command.ExecuteScalarAsync();
            if (skierHasSkiRuns == true)
            {
                return (bool)skierHasSkiRuns;
            }
            return false;

        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Funktion som tar emot den valda skidåkaren och skapar ett liftkort från information i gränssnittet
    /// </summary>
    /// <param name="skier"></param>
    /// <param name="skipass"></param>
    /// <returns></returns>
    public async Task<bool> BuySkipass(Skier skier, SkiPass skipass)
    {
        try
        {

            using NpgsqlConnection conn = await CreateAndOpenConnection();

            using var command = new NpgsqlCommand("insert into ski_pass(card_number, destination_id, skier_id, start_date, end_date) " +
                                                  "values(@card_number, @destination_id, @skier_id, @start_date, @end_date)", conn);

            command.Parameters.AddWithValue("skier_id", skier.Id);
            command.Parameters.AddWithValue("card_number", skipass.CardNumber);
            command.Parameters.AddWithValue("destination_id", skipass.DestinationId);
            command.Parameters.AddWithValue("start_date", skipass.Start_date);
            command.Parameters.AddWithValue("end_date", skipass.End_date);

            return await command.ExecuteNonQueryAsync() > 0;

        }
        catch (Exception ex)
        {
            throw;
        }

    }

    /// <summary>
    /// Funktion som kollar om åkare har en giltligt liftkort. Returnerar en bekräftelse (bool)
    /// </summary>
    /// <param name="resort_id"></param>
    /// <param name="cardNumber"></param>
    /// <param name="date"></param>
    /// <returns></returns>
    public async Task<bool> ValidateSkipass(int resort_id, string cardNumber, DateTime date)
    {
        try
        {
            using NpgsqlConnection conn = await CreateAndOpenConnection();

            using var command = new NpgsqlCommand("select EXISTS (select ski_pass.card_number from ski_pass join resort on ski_pass.destination_id = resort.destination_id " +
            "where resort.id = @resort.id and ski_pass.card_number = @card_number and @date between start_date and end_date)", conn);
            
            command.Parameters.AddWithValue("resort.id", resort_id);
            command.Parameters.AddWithValue("card_number", cardNumber);
            command.Parameters.AddWithValue("date", date);

            var isValidSkiPass = (bool?)await command.ExecuteScalarAsync();

            if (isValidSkiPass != null)
            {
                return (bool)isValidSkiPass;
            }

            return false;
        }

        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Funktion som hämtar liftkortsId och liftId från gränssnittet och registrerar ett nytt åk
    /// </summary>
    /// <param name="skipassId"></param>
    /// <param name="liftId"></param>
    /// <returns></returns>
    public async Task<bool> RegisterNewSkirun(int skipassId, int liftId)
    {
        try
        {
            using NpgsqlConnection conn = await CreateAndOpenConnection();

            using var command = new NpgsqlCommand("insert into ski_run(skipass_id, lift_id) " +
                                                  "values(@skipass_id, @lift_id)", conn);

            command.Parameters.AddWithValue("skipass_id", skipassId);
            command.Parameters.AddWithValue("lift_id", liftId);

            return await command.ExecuteNonQueryAsync() > 0;
  
        }
        catch (Exception ex)
        {

            throw;
        }
    }

    /// <summary>
    /// Funktion som hämtar information om utgångsdatum för liftkort, totalt antal skiddagar för säsong och totalt antal länder
    /// som skidåkaren har haft
    /// </summary>
    /// <param name="skier"></param>
    /// <returns></returns>
    public async Task<(DateTime, long, long)> GetAllInfoSkier(Skier skier)
    {

        var endDate = DateTime.Parse("2025-10-01");
        long totalDays = 0;
        long totalCountries = 0;
        try
        {

            using NpgsqlConnection conn = await CreateAndOpenConnection();

            using var command = new NpgsqlCommand("select skier.id, skier.firstname, skier.lastname, ski_pass.skier_id, ski_pass.end_date " +
                                                 "from skier JOIN ski_pass ON skier.id = ski_pass.skier_id where skier.id=@SkierId " +
                                                 "order by ski_pass.end_date DESC limit 1;", conn); // sqlfråga för när datum slutar gälla och hämtar förnamn och efternamn


            using var command2 = new NpgsqlCommand("select sum(ski_pass.end_date - ski_pass.start_date) " + 
                                                    "AS total_days from skier join ski_pass ON skier.id = ski_pass.skier_id " +
                                                    "join season on ski_pass.end_date between season.start_date and season.end_date " +
                                                    "where skier.id=@SkierId and season.id = 1;", conn); //sqlfråga för antal skiddagar totalt för innevarande säsong

            using var command3 = new NpgsqlCommand("select count(distinct destination.country_id) AS country_count " +
                                                  "from skier join ski_pass ON skier.id = ski_pass.skier_id " +
                                                  "join destination ON ski_pass.destination_id = destination.id where skier.id=@SkierId; ", conn); //sqlfråga för antal länder som skidåkaren har besökt (åkt skidor på) genom alla tider

            command.Parameters.AddWithValue("SkierId", skier.Id);
            command2.Parameters.AddWithValue("SkierId", skier.Id);
            command3.Parameters.AddWithValue("SkierId", skier.Id);


            // Läser av svaret i databasen från sql-koden tills det inte finns någon mer data att läsa
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {

                    SkiPass skipass = new SkiPass
                    {
                        End_date = (DateTime)reader["end_date"]
                    };
                    endDate = skipass.End_date;
                }
            }
            using (var reader = command2.ExecuteReader())
            {
                while (reader.Read())
                {

                   totalDays = (long)reader["total_days"];

                }
  
            }
            using (var reader = command3.ExecuteReader())
            {
                while (reader.Read())
                {

                    totalCountries = (long)reader["country_count"];

                }
            }
            return (endDate, totalDays, totalCountries);

        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    /// <summary>
    /// Funktion som utifrån liftkortsnummer hämtar kortId
    /// </summary>
    /// <param name="cardNumber"></param>
    /// <returns></returns>
    public async Task<int> GetSkiPassId(string cardNumber)
    {
        try
        {
            int skiPassId = 0;
            SkiPass skipass = null;
            using NpgsqlConnection conn = await CreateAndOpenConnection();

            using var command = new NpgsqlCommand("select id, card_number " +
                                                    "from ski_pass " +
                                                    "where " +
                                                    "card_number=@card_number", conn);

            command.Parameters.AddWithValue("card_number", cardNumber);


            // Läser av svaret i databasen från sql-koden tills det inte finns någon mer data att läsa
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    skipass = new SkiPass
                    {
                        Id = (int)reader["id"],

                    };
                    skiPassId = skipass.Id;

                }
            }
            return skiPassId;

        }
        catch (Exception)
        {
            throw;
        }

    }

    /// <summary>
    /// Funktion som hämtar skidåkare från databas utifrån namn och returnerar en skidåkare som objekt, inkluderat id
    /// </summary>
    /// <param name="firstName"></param>
    /// <param name="lastName"></param>
    /// <returns></returns>
    public async Task<List<Skier>> GetSKierByNameAsync(string firstName, string lastName)
    {
        try
        {

            List<Skier> skierByName = new List<Skier>();
            Skier skier = null;
            using NpgsqlConnection conn = await CreateAndOpenConnection();

            using var command = new NpgsqlCommand("select id, firstname, lastname " +
                                                    "from skier " +
                                                    "where firstname=@firstname or " +
                                                    "lastname=@lastname", conn);

            command.Parameters.AddWithValue("firstname", firstName);
            command.Parameters.AddWithValue("lastname", lastName);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    skier = new Skier
                    {
                        Id = (int)reader["id"],
                        Firstname = reader["firstname"].ToString(),
                        Lastname = reader["lastname"].ToString()

                    };
                    skierByName.Add(skier);
                }
            }

            return skierByName;

        }
        catch (Exception)
        {
            throw;
        }

    }
  
 /// <summary>
 /// Funktion som hämtar alla skiddestinationer
 /// </summary>
 /// <returns></returns>
     public async Task<List<Destination>> GetAllDestinations()
    {

        try
        {
            List<Destination> destinations = new List<Destination>();
            using NpgsqlConnection conn = await CreateAndOpenConnection();

            using var command = new NpgsqlCommand("select id, destination_name from destination", conn);


            using (var reader = command.ExecuteReader())
            {

                while (reader.Read())
                {
                    Destination destination = new Destination
                    {
                        Id = (int)reader["id"],
                        Name = reader["destination_name"].ToString(),

                    };

                    destinations.Add(destination);
                }

                return destinations;

            }

        }
        catch (Exception)
        {

            throw;
        }

    }

    /// <summary>
    /// Funktion som hämtar alla skidresorter
    /// </summary>
    /// <returns></returns>
    public async Task<List<Resort>> GetAllResorts()
    {

        try
        {
            List<Resort> resorts = new List<Resort>();
            using NpgsqlConnection conn = await CreateAndOpenConnection();

            using var command = new NpgsqlCommand("select id, resort_name from resort", conn);


            using (var reader = command.ExecuteReader())
            {

                while (reader.Read())
                {
                    Resort resort = new Resort
                    {
                        Id = (int)reader["id"],
                        Name = reader["resort_name"].ToString(),

                    };

                    resorts.Add(resort);
                }

                return resorts;

            }

        }
        catch (Exception)
        {

            throw;
        }

    }

    /// <summary>
    /// Funktion som hämtar alla skidresorter utifrån vald destination från gränssnittet
    /// </summary>
    /// <param name="destinationId"></param>
    /// <returns></returns>
    public async Task<List<Resort>> GetAllResortsFiltered(int destinationId)
    {

        try
        {
            List<Resort> resorts = new List<Resort>();
            using NpgsqlConnection conn = await CreateAndOpenConnection();

            using var command = new NpgsqlCommand("select id, resort_name from resort where destination_id=@destination_id", conn);

            command.Parameters.AddWithValue("destination_id", destinationId);

            using (var reader = command.ExecuteReader())
            {

                while (reader.Read())
                {
                    Resort resort = new Resort
                    {
                        Id = (int)reader["id"],
                        Name = reader["resort_name"].ToString(),

                    };

                    resorts.Add(resort);
                }

                return resorts;

            }

        }
        catch (Exception)
        {

            throw;
        }

    }

    /// <summary>
    /// Funktion som hämtar alla skidliftar
    /// </summary>
    /// <returns></returns>
    public async Task<List<Lift>> GetAllLifts()
    {

        try
        {
            List<Lift> lifts = new List<Lift>();
            using NpgsqlConnection conn = await CreateAndOpenConnection();

            using var command = new NpgsqlCommand("select id, lift_name from lift", conn);


            using (var reader = command.ExecuteReader())
            {

                while (reader.Read())
                {
                    Lift lift = new Lift
                    {
                        Id = (int)reader["id"],
                        Name = reader["lift_name"].ToString(),

                    };

                    lifts.Add(lift);
                }

                return lifts;

            }

        }
        catch (Exception)
        {

            throw;
        }

    }

    /// <summary>
    /// Funktion som hämtar alla skidliftar utifrån vald resort från gränssnittet
    /// </summary>
    /// <param name="resortId"></param>
    /// <returns></returns>
    public async Task<List<Lift>> GetAllLiftsFiltered(int resortId)
    {

        try
        {
            List<Lift> lifts = new List<Lift>();
            using NpgsqlConnection conn = await CreateAndOpenConnection();

            using var command = new NpgsqlCommand("select id, lift_name from lift where resort_id=@resort_id", conn);
            command.Parameters.AddWithValue("resort_id", resortId);

            using (var reader = command.ExecuteReader())
            {

                while (reader.Read())
                {
                    Lift lift = new Lift
                    {
                        Id = (int)reader["id"],
                        Name = reader["lift_name"].ToString(),

                    };

                    lifts.Add(lift);
                }

                return lifts;

            }

        }
        catch (Exception)
        {

            throw;
        }

    }

    /// <summary>
    /// Funktion som hämtar random liftar från specifik resort och returnerar en random lift
    /// </summary>
    /// <param name="resort"></param>
    /// <returns></returns>
    public async Task<Lift> GetRandomSkiLiftFromResortAsync(Resort resort)
    {
        try
        {
            Lift lift = null;
            List<Lift> lifts = new List<Lift>();

            using NpgsqlConnection conn = await CreateAndOpenConnection();

            using var command = new NpgsqlCommand("select id from lift where resort_id=@resort_id order by random()", conn);


            command.Parameters.AddWithValue("resort_id", resort.Id);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    lift = new Lift
                    {
                        Id = (int)reader["id"],
                    };
                    lifts.Add(lift);
                }

            }

            return lift;

        }
        catch (Exception)
        {

            throw;
        }

    }

    /// <summary>
    /// Funktion som hämtar random liftkort ur databas och returnerar en lista med dessa
    /// </summary>
    /// <param name="numberOfSkipasses"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<List<SkiPass>> GetRandomSkiPassesAsync(int numberOfSkipasses)
    {
        try
        {
            List<SkiPass> skiPassIds = new List<SkiPass>();
            using NpgsqlConnection conn = await CreateAndOpenConnection();

            using var command = new NpgsqlCommand("select id, card_number from ski_pass order by random()", conn);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    SkiPass skipass = new SkiPass
                    {
                        Id = (int)reader["id"],
                        CardNumber = reader["card_number"].ToString()

                    };
                    skiPassIds.Add(skipass);
                }
                return skiPassIds;
            }

        }
        catch (Exception)
        {

            throw;
        }

    }

    /// <summary>
    /// Funktion som hämtar random resort ur databas och returnerar ett random resortobjekt
    /// </summary>
    /// <returns></returns>
    public async Task<Resort> GetRandomResortAsync()
    {
        try
        {
            Resort resort = null;
            using NpgsqlConnection conn = await CreateAndOpenConnection();

            using var command = new NpgsqlCommand("select id, resort_name from resort order by random() limit 1", conn);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    resort = new Resort
                    {
                        Id = (int)reader["id"],
                        Name = reader["resort_name"].ToString()
                    };

                }
            }
            return resort;

        }
        catch (Exception)
        {

            throw;
        }


       

    }
    
    /// <summary>
    /// Funktion som kollar vilken säsong som ett datum tillhör och returnerar en säsong i ett objekt
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public async Task<Season> GetSeasonByDateAsync(DateTime date)
    {

        try
        {
            using NpgsqlConnection conn = await CreateAndOpenConnection();

            Season season = null;
            using var command = new NpgsqlCommand("select id, name, start_date, end_date from season where @date between start_date and end_date", conn);

            command.Parameters.AddWithValue("date", date);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    season = new Season
                    {
                        Id = (int)reader["id"],
                        Name = reader["name"].ToString(),
                        StartDate = (DateTime)reader["start_date"],
                        EndDate = (DateTime)reader["end_date"]
                    };

                }
            }
            return season;

        }
        catch (Exception)
        {

            throw;
        }

        
    }
    
    /// <summary>
     /// Funktion som lägger in alla skidåk från simulatorn i databasen
     /// </summary>
     /// <param name="skiRuns"></param>
     /// <returns></returns>
    public async Task SaveSkiRunsAsync(List<SkiRun> skiRuns)
    {
        try
        {
            using NpgsqlConnection conn = await CreateAndOpenConnection();

            using var command = new NpgsqlCommand("insert into ski_run(skipass_id, season_id, lift_id, timestamp) " +
                  "values(@skipass_id, @season_id, @lift_id, @timestamp)", conn);

            foreach (SkiRun skirun in skiRuns)
            {

                command.Parameters.Clear();

                command.Parameters.AddWithValue("skipass_id", skirun.SkipassId);
                command.Parameters.AddWithValue("season_id", skirun.SeasonId);
                command.Parameters.AddWithValue("lift_id", skirun.LiftId);
                command.Parameters.AddWithValue("timestamp", skirun.Timestamp);
                var result = await command.ExecuteNonQueryAsync();
            }

        }
        catch (Exception)
        {

            throw;
        }
 

    }

    private static T? ConvertFromDBVal<T>(object obj)  //Om det blir problem med nullable från databas
    {
        if (obj == null || obj == DBNull.Value)
        {
            return default;
        }
        return (T)obj;

    }

    private static object ConvertToDBVal<T>(object obj)
    {
        if (obj == null || obj == string.Empty)
        {
            return DBNull.Value;
        }
        return (T)obj;
    }



}
