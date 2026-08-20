using Inmobiliaria.Models;
using Microsoft.AspNetCore.Mvc;

namespace Inmobiliaria.Controllers;

public class PropietariosController : Controller
{
    private static readonly List<Propietario> _propietarios = new()
    {
        new Propietario(1, "Lautaro", "Cadelago", "1122334455", "lautaro.cadelago@mail.com"),
        new Propietario(2, "Maria", "Gomez", "1144556677", "maria.gomez@mail.com"),
        new Propietario(3, "Carlos", "López", "1188997766", "carlos.lopez@mail.com")
    };

    private static int _nextId = _propietarios.Count + 1;

    public IActionResult Index()
    {
        var propietarios = _propietarios
            .OrderBy(p => p.Apellido)
            .ThenBy(p => p.Nombre)
            .ToList();

        return View(propietarios);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Propietario propietario)
    {
        if (!ModelState.IsValid)
        {
            return View(propietario);
        }

        if (_propietarios.Any(p => p.Email.Equals(propietario.Email, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(Propietario.Email), "Ya existe un propietario con ese email.");
            return View(propietario);
        }

        propietario.Id = _nextId++;
        _propietarios.Add(propietario);

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var propietario = _propietarios.FirstOrDefault(p => p.Id == id);

        if (propietario == null)
        {
            return NotFound();
        }

        return View(propietario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Propietario propietario)
    {
        if (id != propietario.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(propietario);
        }

        var propietarioExistente = _propietarios.FirstOrDefault(p => p.Id == id);

        if (propietarioExistente == null)
        {
            return NotFound();
        }

        if (_propietarios.Any(p => p.Id != id && p.Email.Equals(propietario.Email, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(Propietario.Email), "Ya existe otro propietario con ese email.");
            return View(propietario);
        }

        propietarioExistente.Nombre = propietario.Nombre;
        propietarioExistente.Apellido = propietario.Apellido;
        propietarioExistente.Telefono = propietario.Telefono;
        propietarioExistente.Email = propietario.Email;

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int id)
    {
        var propietario = _propietarios.FirstOrDefault(p => p.Id == id);

        if (propietario == null)
        {
            return NotFound();
        }

        return View(propietario);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var propietario = _propietarios.FirstOrDefault(p => p.Id == id);

        if (propietario == null)
        {
            return NotFound();
        }

        _propietarios.Remove(propietario);
        return RedirectToAction(nameof(Index));
    }
}
