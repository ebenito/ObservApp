using System;
using System.Collections.Generic;
using System.Text;

namespace ObservApp.Shared.Models;

public record SignalItem(
    string Titulo,
    string Descripcion,
    string Url,
    string Fuente,
    DateTimeOffset FechaPublicacion,
    SeñalCategoria Categoria = SeñalCategoria.General
);

public enum SeñalCategoria
{
    General,
    Eclipse,
    PlanetaVisible,
    LluviaEstrellas,
    Cometa,
    Satelite,
    Espacial
}