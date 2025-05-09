using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class ClientsService : IClientsService
{
    private readonly string _connectionString = "Server=localhost,1433;Database=master;User Id=sa;Password=yourStrong(!)Password;TrustServerCertificate=True;";

    public async Task<List<ClientTripDTO>> GetClientTrips(int clientId)
    {
        var clientTrips = new List<ClientTripDTO>();
        string command = @"
        SELECT t.*, c.Name as CountryName, ct.RegisteredAt, ct.PaymentDate 
        FROM Trip t
        LEFT JOIN Country_Trip ctr ON t.IdTrip = ctr.IdTrip
        LEFT JOIN Country c ON ctr.IdCountry = c.IdCountry
        INNER JOIN Client_Trip ct ON t.IdTrip = ct.IdTrip
        WHERE ct.IdClient = @ClientId";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@ClientId", clientId);
            await conn.OpenAsync();
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                int idTripOrdinal = reader.GetOrdinal("IdTrip");
                int nameOrdinal = reader.GetOrdinal("Name");
                int descriptionOrdinal = reader.GetOrdinal("Description");
                int dateFromOrdinal = reader.GetOrdinal("DateFrom");
                int dateToOrdinal = reader.GetOrdinal("DateTo");
                int max = reader.GetOrdinal("MaxPeople");
                int countryNameOrdinal = reader.GetOrdinal("CountryName");
                int registeredAtOrdinal = reader.GetOrdinal("RegisteredAt");
                int paymentDateOrdinal = reader.GetOrdinal("PaymentDate");

                while (await reader.ReadAsync())
                {
                    var tripId = reader.GetInt32(idTripOrdinal);
                    var existingTrip = clientTrips.FirstOrDefault(ct => ct.Trip.Id == tripId);
                    
                    if (existingTrip == null)
                    {
                        var tripDto = new TripDTO
                        {
                            Id = tripId,
                            Name = reader.GetString(nameOrdinal),
                            Description = reader.GetString(descriptionOrdinal),
                            DateFrom = reader.GetDateTime(dateFromOrdinal),
                            DateTo = reader.GetDateTime(dateToOrdinal),
                            MaxPeople = reader.GetInt32(max),
                            Countries = new List<CountryDTO>()
                        };

                        if (!reader.IsDBNull(countryNameOrdinal))
                        {
                            tripDto.Countries.Add(new CountryDTO
                            {
                                Name = reader.GetString(countryNameOrdinal)
                            });
                        }

                        clientTrips.Add(new ClientTripDTO
                        {
                            Trip = tripDto,
                            RegisteredAt = reader.GetInt32(registeredAtOrdinal),
                            PaymentDate = reader.IsDBNull(paymentDateOrdinal) ? 
                                null : reader.GetInt32(paymentDateOrdinal)
                        });
                    }
                    else if (!reader.IsDBNull(countryNameOrdinal))
                    {
                        existingTrip.Trip.Countries.Add(new CountryDTO
                        {
                            Name = reader.GetString(countryNameOrdinal)
                        });
                    }
                }
            }
        }

        return clientTrips;
    }
    
    public async Task<bool> ClientExists(int clientId)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand("SELECT 1 FROM Client WHERE IdClient = @ClientId", conn))
        {
            cmd.Parameters.AddWithValue("@ClientId", clientId);
            await conn.OpenAsync();
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                return await reader.ReadAsync();
            }
        }
    }

    public async Task<int> CreateClient(ClientDTO clientDto)
    {
        string command = @"INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
          OUTPUT INSERTED.IdClient
          VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)";
        
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@FirstName", clientDto.FirstName);
            cmd.Parameters.AddWithValue("@LastName", clientDto.LastName);
            cmd.Parameters.AddWithValue("@Email", clientDto.Email);
            cmd.Parameters.AddWithValue("@Telephone", clientDto.Telephone);
            cmd.Parameters.AddWithValue("@Pesel", clientDto.Pesel);

            await conn.OpenAsync();
            var newId = await cmd.ExecuteScalarAsync();
            return (int) newId;
        }
    }

    public async Task<bool> DeleteAssignment(int clientId, int tripId)
    {
        string command = @"DELETE FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId";
        using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(command, conn))
            {
                cmd.Parameters.AddWithValue("@ClientId", clientId);
                cmd.Parameters.AddWithValue("@TripId", tripId);
                await conn.OpenAsync();
                var result = await cmd.ExecuteNonQueryAsync();
                return result > 0;
            }
    }
    

    public async Task<AssignClientToTripResult> AssignClientToTrip(int clientId, int tripId)
    {
        if (!await ClientExists(clientId))
            return AssignClientToTripResult.ClientNotFound;
        
        int? maxPeople = null;
        string maxCommand = @"SELECT MaxPeople FROM Trip WHERE IdTrip = @TripId";
        string checkAssign = @"SELECT 1 FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId";
        string checkFull = @"SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @TripId";
        string assignCommand = @"INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt) 
            VALUES (@ClientId, @TripId, @Now)";
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            using (SqlCommand cmd = new SqlCommand(maxCommand, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", tripId);
                var result = await cmd.ExecuteScalarAsync();
                if (result == null)
                    return AssignClientToTripResult.TripNotFound;
                maxPeople = (int)result;
            }
            using (SqlCommand cmd = new SqlCommand(checkAssign, conn))
            {
                cmd.Parameters.AddWithValue("@ClientId", clientId);
                cmd.Parameters.AddWithValue("@TripId", tripId);
                var alreadyAssigned = await cmd.ExecuteScalarAsync();
                if (alreadyAssigned != null)
                    return AssignClientToTripResult.AlreadyAssigned;
            }
            using (SqlCommand cmd = new SqlCommand(checkFull, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", tripId);
                var count = await cmd.ExecuteScalarAsync();
                if (count != null && (int)count >= maxPeople)
                    return AssignClientToTripResult.TripFull;
            }
            using (SqlCommand cmd = new SqlCommand(assignCommand, conn))
            {
                cmd.Parameters.AddWithValue("@ClientId", clientId);
                cmd.Parameters.AddWithValue("@TripId", tripId);
                cmd.Parameters.AddWithValue("@Now", (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                await cmd.ExecuteNonQueryAsync();
                return AssignClientToTripResult.Success;
            }
        }
    }

}