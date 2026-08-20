using Inmobiliaria.Models;
using Microsoft.AspNetCore.Mvc;

namespace Inmobiliaria.Controllers;

public class InquilinosController : Controller
{
    private static readonly List<Inquilino> _inquilinos = new()
    {
        new Inquilino(1, "Ana", "Martínez", "30123456", "1133445566", "ana.martinez@mail.com"),
        new Inquilino(2, "Luis", "Ramírez", "28765432", "1155667788", "luis.ramirez@mail.com"),
        new Inquilino(3, "Sofía", "Torres", "32456789", "1199887766", "sofia.torres@mail.com")
    };

    private static int _nextId = _inquilinos.Count + 1;

    public IActionResult Index()
    {
        var inquilinos = _inquilinos
            .OrderBy(i => i.Apellido)
            .ThenBy(i => i.Nombre)
            .ToList();

        return View(inquilinos);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Inquilino inquilino)
    {
        if (!ModelState.IsValid)
        {
            return View(inquilino);
        }

        if (_inquilinos.Any(i => i.Email.Equals(inquilino.Email, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(Inquilino.Email), "Ya existe un inquilino con ese email.");
            return View(inquilino);
        }

        inquilino.Id = _nextId++;
        _inquilinos.Add(inquilino);

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var inquilino = _inquilinos.FirstOrDefault(i => i.Id == id);

        if (inquilino == null)
        {
            return NotFound();
        }

        return View(inquilino);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Inquilino inquilino)
    {
        if (id != inquilino.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(inquilino);
        }

        var inquilinoExistente = _inquilinos.FirstOrDefault(i => i.Id == id);

        if (inquilinoExistente == null)
        {
            return NotFound();
        }

        if (_inquilinos.Any(i => i.Id != id && i.Email.Equals(inquilino.Email, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(Inquilino.Email), "Ya existe otro inquilino con ese email.");
            return View(inquilino);
        }

        inquilinoExistente.Nombre = inquilino.Nombre;
        inquilinoExistente.Apellido = inquilino.Apellido;
        inquilinoExistente.Dni = inquilino.Dni;
        inquilinoExistente.Telefono = inquilino.Telefono;
        inquilinoExistente.Email = inquilino.Email;

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int id)
    {
        var inquilino = _inquilinos.FirstOrDefault(i => i.Id == id);

        if (inquilino == null)
        {
            return NotFound();
        }

        return View(inquilino);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var inquilino = _inquilinos.FirstOrDefault(i => i.Id == id);

        if (inquilino == null)
        {
            return NotFound();
        }

        _inquilinos.Remove(inquilino);
        return RedirectToAction(nameof(Index));
    }
}
