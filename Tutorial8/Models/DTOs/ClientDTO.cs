namespace Tutorial8.Models.DTOs;
using System.ComponentModel.DataAnnotations;

public class ClientTripDTO
{
    public TripDTO Trip { get; set; }
    public int RegisteredAt { get; set; }
    public int? PaymentDate { get; set; }
}

public class ClientDTO
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(120)]
    public string FirstName { get; set; }
    
    [Required]
    [StringLength(120)]
    public string LastName { get; set; }
    
    [Required]
    [StringLength(120)]
    public string Email { get; set; }
    
    [Required]
    [StringLength(120)]
    public string Telephone { get; set; }
    
    [Required]
    [StringLength(120)]
    public string Pesel { get; set; }
}

public enum AssignClientToTripResult 
{
    Success,
    TripNotFound,
    ClientNotFound,
    TripFull,
    AlreadyAssigned
}

