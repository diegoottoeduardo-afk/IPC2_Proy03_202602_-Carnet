using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DteFrontend.Pages.Ayuda
{
    public class EstudianteModel : PageModel
    {
        public string NombreCompleto { get; set; } = "Diego Otto Eduardo";
        public string Carnet { get; set; } = "202505251";
        public string Curso { get; set; } = "Introducción a la Programación y Computación 2";
        public string Carrera { get; set; } = "Ingeniería en Ciencias y Sistemas";
        public string Universidad { get; set; } = "Universidad de San Carlos de Guatemala";
        public string Facultad { get; set; } = "Facultad de Ingeniería";
        public string Semestre { get; set; } = "Segundo Semestre 2026";

        public void OnGet()
        {
        }
    }
}
