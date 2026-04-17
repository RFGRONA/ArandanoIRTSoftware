using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArandanoIRT.Web._0_Domain.Common
{
/// <summary>
/// Proporciona métodos de extensión para trabajar con enumeraciones.
/// </summary>
    public static class EnumExtensions
    {
    /// <summary>
    /// Obtiene el nombre de visualización de un valor de enumeración,
    /// utilizando el atributo [Display(Name = "...")].
    /// Si no se encuentra el atributo, devuelve el nombre del miembro de la enumeración.
    /// </summary>
    /// <param name="enumValue">El valor de la enumeración.</param>
    /// <returns>El nombre de visualización o el nombre del miembro.</returns>
        public static string GetDisplayName(this Enum enumValue)
        {
            return enumValue.GetType()
                .GetMember(enumValue.ToString())
                .First()
                .GetCustomAttribute<DisplayAttribute>()?
                .GetName() ?? enumValue.ToString();
        }
    }

/// <summary>
/// Proporciona métodos de extensión para generar listas de selección a partir de enumeraciones.
/// </summary>
    public static class EnumSelectListExtensions
    {
    /// <summary>
    /// Convierte una enumeración en una colección de objetos SelectListItem,
    /// que se puede utilizar para rellenar listas desplegables en vistas de Razor.
    /// </summary>
    /// <typeparam name="TEnum">El tipo de la enumeración.</typeparam>
    /// <returns>Una colección de SelectListItem.</returns>
        public static IEnumerable<SelectListItem> ToSelectList<TEnum>() where TEnum : Enum
        {
            return Enum.GetValues(typeof(TEnum))
                .Cast<TEnum>()
                .Select(e => new SelectListItem()
                {
                    Text = e.GetDisplayName(),
                    Value = e.ToString()
                }).ToList();
        }
    }
}
