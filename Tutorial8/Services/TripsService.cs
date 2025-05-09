using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString = "Server=localhost,1433;Database=master;User Id=sa;Password=yourStrong(!)Password;TrustServerCertificate=True;";
    
    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new List<TripDTO>();
        
        // Get all trips with countries
        string command = @"
        SELECT t.*, c.Name as CountryName 
        FROM Trip t
        LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
        LEFT JOIN Country c ON ct.IdCountry = c.IdCountry";
        

        
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            await conn.OpenAsync();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                int idTripOrdinal = reader.GetOrdinal("IdTrip");
                int max = reader.GetOrdinal("MaxPeople");
                int countryNameOrdinal = reader.GetOrdinal("CountryName");

                while (await reader.ReadAsync())
                {
                    var tripId = reader.GetInt32(idTripOrdinal);
                    
                    
                    trips.Add(new TripDTO()
                    {
                        Id = tripId,
                        Name = reader.GetString(1),
                        Description = reader.GetString(2),
                        DateFrom = reader.GetDateTime(3),
                        DateTo = reader.GetDateTime(4),
                        MaxPeople = reader.GetInt32(max),
                        Countries = new List<CountryDTO>()
                    });
                    
                    // Add country to the trip's country list
                    foreach (var trip in trips)
                    {
                        if (trip.Id == tripId)
                        {
                            var countryName = reader.GetString(countryNameOrdinal);
                            trip.Countries.Add(new CountryDTO()
                            {
                                Name = countryName
                            });
                        }
                    }
                }
                    
            }
        }
        

        return trips;
    }
}