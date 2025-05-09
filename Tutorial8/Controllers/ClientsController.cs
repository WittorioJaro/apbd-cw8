using Microsoft.AspNetCore.Mvc;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClientsController : ControllerBase
{
    private readonly IClientsService _clientsService;
    
    public ClientsController(IClientsService clientsService)
    {
        _clientsService = clientsService;
    }
    
    [HttpGet("{clientId}/trips")]
    public async Task<IActionResult> GetClientTrips(int clientId)
    {
        if (!await _clientsService.ClientExists(clientId))
            return NotFound($"Client with id: {clientId} not found");
        var trips = await _clientsService.GetClientTrips(clientId);
        
        return Ok(trips);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] ClientDTO clientDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var clientId = await _clientsService.CreateClient(clientDto);
        return CreatedAtAction(nameof(GetClientTrips), new { clientId }, new { clientId });
    }
    
    [HttpPut("{id}/trips/{tripId}")]
    public async Task<IActionResult> AddClientToTrip(int id, int tripId)
    {
        var result = await _clientsService.AssignClientToTrip(id, tripId);
        return result switch
        {
            AssignClientToTripResult.ClientNotFound => NotFound($"Client with id: {id} not found"),
            AssignClientToTripResult.TripNotFound => NotFound($"Trip with id: {tripId} not found"),
            AssignClientToTripResult.AlreadyAssigned => BadRequest("Client already assigned to this trip"),
            AssignClientToTripResult.TripFull => BadRequest($"Trip with id: {tripId} is full"),
            AssignClientToTripResult.Success => Created(),
            _ => StatusCode(500)
        };
    }
    
    [HttpDelete("{id}/trips/{tripId}")]
    public async Task<IActionResult> DeleteClientTripAssignment(int id, int tripId)
    {
        var deleted = await _clientsService.DeleteAssignment(id, tripId);
        if (!deleted)
            return NotFound("Assignment not found");
        return NoContent();
    }
}