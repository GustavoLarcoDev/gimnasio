using Gimnasio.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using Gimnasio.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace Gimnasio.Controllers
{
    public class GimnasiosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GimnasiosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Gimnasios
        public IActionResult Index()
        {
            return View();
        }
        
        // GET: Gimnasios/GetGimnasios
        [HttpGet]
        public async Task<IActionResult> GetGimnasios()
        {
            try
            {
                var gimnasios = await _context.Gimnasios
                    .Select(g => new
                    {
                        g.GimnasioId,
                        g.GimnasioNombre,
                        g.DuenoGimnasio,
                        g.Telefono,
                        g.Email,
                        g.IsActive,
                        g.EsPrueba,
                        g.FechaCreacion,
                        TotalClientes = g.Clientes.Count
                    })
                    .OrderByDescending(g => g.FechaCreacion)
                    .ToListAsync();

                return Ok(gimnasios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: Gimnasios/GetGimnasio/5
        [HttpGet]
        public async Task<IActionResult> GetGimnasio(Guid id)
        {
            try
            {
                var gimnasio = await _context.Gimnasios.FindAsync(id);

                if (gimnasio == null)
                {
                    return NotFound(new { success = false, message = "Gimnasio no encontrado" });
                }

                return Ok(new
                {
                    gimnasio.GimnasioId,
                    gimnasio.GimnasioNombre,
                    gimnasio.DuenoGimnasio,
                    gimnasio.Telefono,
                    gimnasio.Email,
                    gimnasio.Password,
                    gimnasio.IsActive,
                    gimnasio.EsPrueba
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: Gimnasios/Crear
        [HttpGet]
        public IActionResult Crear()
        {
            return View("Create");
        }

        // POST: Gimnasios/Create
        [HttpPost]
        public async Task<IActionResult> Create(string NombreGimnasio, string duenoGimnasio, string telefono, string EmailGimnasio, 
                                                string passwordGimnasio, bool isActive, bool esPrueba)
        {
            try
            {
                // Validación: No pueden ser ambos true o ambos false
                if (isActive && esPrueba)
                {
                    return BadRequest("Un gimnasio no puede ser de pago y de prueba al mismo tiempo");
                }

                if (!isActive && !esPrueba)
                {
                    return BadRequest("Debe seleccionar si el gimnasio es de pago o de prueba");
                }

                // Validaciones existentes
                if (string.IsNullOrWhiteSpace(EmailGimnasio))
                {
                    return BadRequest("Correo del Gimnasio es necesario");
                }

                bool existing = await _context.Gimnasios.AnyAsync(c => c.Email == EmailGimnasio);
                if (existing)
                {
                    return BadRequest("Gimnasio con este email ya existe");
                }

                if (string.IsNullOrWhiteSpace(NombreGimnasio))
                {
                    return BadRequest("Nombre del Gimnasio es necesario");
                }

                if (string.IsNullOrWhiteSpace(duenoGimnasio))
                {
                    return BadRequest("Dueño Gimnasio es necesario");
                }

                if (string.IsNullOrWhiteSpace(telefono))
                {
                    return BadRequest("Teléfono es necesario");
                }

                if (string.IsNullOrWhiteSpace(passwordGimnasio))
                {
                    return BadRequest("Password es necesario");
                }

                // Crear el gimnasio con la lógica correcta
                var gimnasio = new Gym
                {
                    GimnasioId = Guid.NewGuid(),
                    GimnasioNombre = NombreGimnasio,
                    DuenoGimnasio = duenoGimnasio,
                    Telefono = telefono,
                    Email = EmailGimnasio,
                    Password = passwordGimnasio,
                    IsActive = isActive,      // true si es de pago, false si es prueba
                    EsPrueba = esPrueba,      // true si es prueba, false si es de pago
                    FechaCreacion = DateTime.Now,
                    FechaDeActualizacion = DateTime.Now,
                };

                _context.Gimnasios.Add(gimnasio);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Gimnasio creado exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // PUT: Gimnasios/Editar
        [HttpPost]
        public async Task<IActionResult> Editar([FromForm] Gym gimnasio)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Datos inválidos" });
                }

                // Validación: No pueden ser ambos true o ambos false
                if (gimnasio.IsActive && gimnasio.EsPrueba)
                {
                    return BadRequest(new { success = false, message = "Un gimnasio no puede ser de pago y de prueba al mismo tiempo" });
                }

                if (!gimnasio.IsActive && !gimnasio.EsPrueba)
                {
                    return BadRequest(new { success = false, message = "Debe seleccionar si el gimnasio es de pago o de prueba" });
                }

                var gimnasioExistente = await _context.Gimnasios.FindAsync(gimnasio.GimnasioId);

                if (gimnasioExistente == null)
                {
                    return NotFound(new { success = false, message = "Gimnasio no encontrado" });
                }

                // Verificar si el email ya existe en otro gimnasio
                var existeEmail = await _context.Gimnasios
                    .AnyAsync(g => g.Email == gimnasio.Email && g.GimnasioId != gimnasio.GimnasioId);

                if (existeEmail)
                {
                    return BadRequest(new { success = false, message = "Ya existe otro gimnasio con ese email" });
                }

                gimnasioExistente.GimnasioNombre = gimnasio.GimnasioNombre;
                gimnasioExistente.DuenoGimnasio = gimnasio.DuenoGimnasio;
                gimnasioExistente.Telefono = gimnasio.Telefono;
                gimnasioExistente.Email = gimnasio.Email;
                gimnasioExistente.IsActive = gimnasio.IsActive;
                gimnasioExistente.EsPrueba = gimnasio.EsPrueba;
                gimnasioExistente.FechaDeActualizacion = DateTime.Now;

                // Solo actualizar password si se proporcionó uno nuevo
                if (!string.IsNullOrEmpty(gimnasio.Password))
                {
                    gimnasioExistente.Password = gimnasio.Password;
                }

                _context.Update(gimnasioExistente);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Gimnasio actualizado exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // DELETE: Gimnasios/Eliminar/5
        [HttpPost]
        public async Task<IActionResult> Eliminar(Guid id)
        {
            try
            {
                var gimnasio = await _context.Gimnasios
                    .Include(g => g.Clientes)
                    .FirstOrDefaultAsync(g => g.GimnasioId == id);

                if (gimnasio == null)
                {
                    return NotFound(new { success = false, message = "Gimnasio no encontrado" });
                }

                // Verificar si tiene clientes
                if (gimnasio.Clientes.Any())
                {
                    return BadRequest(new 
                    { 
                        success = false, 
                        message = $"No se puede eliminar. El gimnasio tiene {gimnasio.Clientes.Count} cliente(s) registrado(s)" 
                    });
                }

                _context.Gimnasios.Remove(gimnasio);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Gimnasio eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: Gimnasios/CambiarEstado
        [HttpPost]
        public async Task<IActionResult> CambiarEstado(Guid id)
        {
            try
            {
                var gimnasio = await _context.Gimnasios.FindAsync(id);

                if (gimnasio == null)
                {
                    return NotFound(new { success = false, message = "Gimnasio no encontrado" });
                }

                // LÓGICA CORRECTA: Toggle entre Activo/Pago y Prueba
                // Si está activo (pago), cambiarlo a prueba
                // Si está en prueba, cambiarlo a activo (pago)
        
                if (gimnasio.IsActive)
                {
                    // Era de pago (activo), ahora será de prueba
                    gimnasio.IsActive = false;
                    gimnasio.EsPrueba = true;
                }
                else
                {
                    // Era de prueba, ahora será de pago (activo)
                    gimnasio.IsActive = true;
                    gimnasio.EsPrueba = false;
                }

                gimnasio.FechaDeActualizacion = DateTime.Now;

                _context.Update(gimnasio);
                await _context.SaveChangesAsync();

                string tipoActual = gimnasio.IsActive ? "Pago (Activo)" : "Prueba";
        
                return Ok(new 
                { 
                    success = true, 
                    message = $"Gimnasio cambiado a modo {tipoActual} exitosamente",
                    isActive = gimnasio.IsActive,
                    esPrueba = gimnasio.EsPrueba
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: Gimnasios/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                // Si el usuario ya está autenticado, redirigir al Dashboard
                var gimnasioIdClaim = User.Claims.FirstOrDefault(c => c.Type == "GimnasioId");
                if (gimnasioIdClaim != null)
                {
                   return RedirectToAction("Dashboard", new { id = gimnasioIdClaim.Value });
                }
            }
            return View();
        }

        // POST: Gimnasios/Login
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            try 
            {
                
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    ViewBag.Error = "Email y contraseña son obligatorios";
                    return View();
                }

                var gimnasio = await _context.Gimnasios
                    .FirstOrDefaultAsync(g => g.Email == email && g.Password == password);

                if (gimnasio == null)
                {
                    ViewBag.Error = "Credenciales inválidas";
                    return View();
                }

                if (!gimnasio.IsActive && !gimnasio.EsPrueba) // Validación extra por si acaso
                {
                    ViewBag.Error = "Su cuenta no está activa. Contacte al administrador.";
                    return View();
                }

                // Crear Claims
                var claims = new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, gimnasio.GimnasioNombre),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, gimnasio.Email),
                    new System.Security.Claims.Claim("GimnasioId", gimnasio.GimnasioId.ToString())
                };

                var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                {
                    IsPersistent = true,
                };

                await HttpContext.SignInAsync(
                    Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                    new System.Security.Claims.ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Dashboard", new { id = gimnasio.GimnasioId });
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Ocurrió un error al intentar iniciar sesión: " + ex.Message;
                return View();
            }
        }

        // GET: Gimnasios/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
            
            return RedirectToAction("Login");
        }

        // GET: Gimnasios/Dashboard/5
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("Gimnasios/{id}/Dashboard")]
        public async Task<IActionResult> Dashboard(Guid id)
        {
            try
            {
                // Verificar que el usuario logueado sea el dueño del dashboard
                var gimnasioIdClaim = User.Claims.FirstOrDefault(c => c.Type == "GimnasioId");
                if (gimnasioIdClaim == null || gimnasioIdClaim.Value != id.ToString())
                {
                    return Forbid(); // O redirigir a Login/Home con mensaje de error
                }

                var gimnasio = await _context.Gimnasios
                    .Include(g => g.Clientes)
                    .FirstOrDefaultAsync(g => g.GimnasioId == id);

                if (gimnasio == null)
                {
                    return NotFound();
                }

                return View(gimnasio);
            }
            catch (Exception ex)
            {
                 return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: Gimnasios/CreateClient
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateClient([FromForm] Cliente cliente)
        {
            try
            {
                // Verificar autorización
                var gimnasioIdClaim = User.Claims.FirstOrDefault(c => c.Type == "GimnasioId");
                if (gimnasioIdClaim == null || cliente.GimnasioId.ToString() != gimnasioIdClaim.Value)
                {
                     return Forbid();
                }

                if (!ModelState.IsValid)
                {
                    // Si falla, podríamos retornar al Dashboard con errores, pero es una petición POST.
                    // Idealmente esto se maneja con AJAX o retornando la vista Dashboard con el modelo inválido.
                     return BadRequest(new { success = false, message = "Datos inválidos" });
                }
                
                // Asegurar IDs
                cliente.ClienteId = Guid.NewGuid();
                cliente.FechaDeCreacion = DateTime.Now;
                cliente.FechaDeActualizacion = DateTime.Now;
                // FechaQueTermina se calcula base a Dias? Asumiremos que el usuario lo envía o se calcula aquí.
                if (cliente.Dias > 0 && cliente.FechaQueTermina == default)
                {
                     cliente.FechaQueTermina = DateTime.Now.AddDays(cliente.Dias);
                }

                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();

                return RedirectToAction("Dashboard", new { id = cliente.GimnasioId });
            }
             catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: Gimnasios/ExportExcel
        public async Task<IActionResult> ExportExcel()
        {
            try
            {
                var gimnasios = await _context.Gimnasios
                    .Include(g => g.Clientes)
                    .OrderByDescending(g => g.FechaCreacion)
                    .ToListAsync();

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Gimnasios");

                // Encabezados
                worksheet.Cell(1, 1).Value = "Nombre del Gimnasio";
                worksheet.Cell(1, 2).Value = "Dueño";
                worksheet.Cell(1, 3).Value = "Email";
                worksheet.Cell(1, 4).Value = "Teléfono";
                worksheet.Cell(1, 5).Value = "Estado";
                worksheet.Cell(1, 6).Value = "Es Prueba";
                worksheet.Cell(1, 7).Value = "Total Clientes";
                worksheet.Cell(1, 8).Value = "Fecha Creación";

                // Estilo encabezados
                var headerRange = worksheet.Range(1, 1, 1, 8);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#3b82f6");
                headerRange.Style.Font.FontColor = XLColor.White;

                // Datos
                int row = 2;
                foreach (var gimnasio in gimnasios)
                {
                    worksheet.Cell(row, 1).Value = gimnasio.GimnasioNombre;
                    worksheet.Cell(row, 2).Value = gimnasio.DuenoGimnasio;
                    worksheet.Cell(row, 3).Value = gimnasio.Email;
                    worksheet.Cell(row, 4).Value = gimnasio.Telefono;
                    worksheet.Cell(row, 5).Value = gimnasio.IsActive ? "Activo" : "Inactivo";
                    worksheet.Cell(row, 6).Value = gimnasio.EsPrueba ? "Sí" : "No";
                    worksheet.Cell(row, 7).Value = gimnasio.Clientes.Count;
                    worksheet.Cell(row, 8).Value = gimnasio.FechaCreacion.ToString("dd/MM/yyyy");
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var content = stream.ToArray();

                return File(
                    content,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Gimnasios_{DateTime.Now:yyyyMMdd}.xlsx"
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}