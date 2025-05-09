using Tutorial8.Models.DTOs;
namespace Tutorial8.Services;


public interface IClientsService
{
    Task<List<ClientTripDTO>> GetClientTrips(int clientId);
    Task<bool> ClientExists(int clientId);
    Task<int> CreateClient(ClientDTO clientDto);
    Task<AssignClientToTripResult> AssignClientToTrip(int clientId, int tripId);
    Task<bool> DeleteAssignment(int clientId, int tripId);
}