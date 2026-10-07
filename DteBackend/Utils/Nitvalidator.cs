using System.Text.RegularExpressions;

namespace DteBackend.Utils
{
    public static class NitValidator
    {
        public static bool EsNitValido(string nit)
        {
            if (string.IsNullOrWhiteSpace(nit)) return false;

            // Extraer usando expresión regular: letras, números o guiones.
            // Para la validación, quitaremos guiones si los tiene y lo pasamos a mayúsculas
            nit = Regex.Replace(nit.ToUpper(), "[^0-9A-Z]", "");

            if (nit.Length < 2 || nit.Length > 20) return false;

            // El último carácter es el dígito verificador
            char digitoVerificador = nit[nit.Length - 1];
            string numeroBase = nit.Substring(0, nit.Length - 1);

            int suma = 0;
            // Multiplicar cada carácter por su posición de derecha a izquierda (iniciando en 2 para el cálculo real)
            // Según el PDF: Posición 1 es el dígito verificador (que omitimos en la suma). 
            // Así que las posiciones a multiplicar son 2, 3, 4... etc.
            int posicion = 2;

            for (int i = numeroBase.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(numeroBase[i])) return false; // La base deben ser puros números

                int valor = int.Parse(numeroBase[i].ToString());
                suma += (valor * posicion);
                posicion++;
            }

            int modulo = suma % 11;
            int resultado = 11 - modulo;

            // Si el resultado es 11, suele usarse 0. Si es 10, es K.
            char verificadorEsperado = resultado switch
            {
                10 => 'K',
                11 => '0',
                _ => resultado.ToString()[0]
            };

            return digitoVerificador == verificadorEsperado;
        }
    }
}
