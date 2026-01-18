using System.ComponentModel.DataAnnotations;
namespace Gimnasio.Models;

public class Cliente
{
    [Key]
    public int Id { get; set; }
    [Required(ErrorMessage = "El campo Nombre es obligatorio")]
    [StringLength(100)]
    public string Nombre { get; set; }
    [StringLength(100)]
    [Required (ErrorMessage = "El campo Apellido es obligatorio")]
    public string Apellido { get; set; }
    [Required(ErrorMessage = "La Fecha de nacimiento es obligatoria para validar que sea mayor de 16")]
    public DateTime FechaNacimiento { get; set; }
    public string? Email { get; set; }
    [Required]
    [Phone]
    public string Telefono { get; set; }
    public string Direccion { get; set; }
    
}