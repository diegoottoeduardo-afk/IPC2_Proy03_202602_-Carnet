namespace DteBackend.Utils
{
    public static class NitValidator
    {
        public static bool EsNitValido(string nit)
        {
            if (string.IsNullOrWhiteSpace(nit))
                return false;

            nit = nit.Trim().ToUpper();

            // Hasta 20 dígitos numéricos base + 1 dígito verificador = 21 máximo
            if (nit.Length < 2 || nit.Length > 21)
                return false;

            char digitoVerificador = nit[nit.Length - 1];
            string baseNit = nit.Substring(0, nit.Length - 1);

            int suma = 0;
            int factor = 2; // Inicia en 2 porque la posición 1 es el verificador que se exceptúa

            for (int i = baseNit.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(baseNit[i]))
                    return false;

                int digito = baseNit[i] - '0';
                suma += digito * factor;
                factor++;
            }

            // Paso 3: Módulo 11 de la sumatoria
            int modulo = suma % 11;

            // Paso 4: A 11 réstale el resultado del paso 3
            int resta = 11 - modulo;

            // Paso 5: Módulo 11 del resultado del paso 4
            int resultado = resta % 11;

            char esperado = (resultado == 10) ? 'K' : (char)('0' + resultado);

            return digitoVerificador == esperado;
        }
    }
}
