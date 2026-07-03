using Mical.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mical.Areas.Admin.Controllers;

/// <summary>
/// Base de todos los controladores del área Admin. Centraliza el área y la
/// exigencia del rol Administrador: cualquier controlador que herede de aquí
/// queda protegido, sin poder olvidarse el atributo en cada uno.
/// </summary>
[Area("Admin")]
[Authorize(Policy = Policies.AdminOnly)]
public abstract class AdminBaseController : Controller
{
}
