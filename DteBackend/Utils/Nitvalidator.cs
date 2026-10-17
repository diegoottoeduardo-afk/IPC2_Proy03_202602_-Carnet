namespace DteBackend.Utils
{
    public static class NitValidator
    {
        public static bool EsNitValido(string nit)
        {
            if (string.IsNullOrWhiteSpace(nit))
                return false;

            nit = nit.Trim().ToUpper();

            if (nit.Length < 2 || nit.Length > 20)
                return false;

            char digitoVerificador = nit[nit.Length - 1];
            string baseNit = nit.Substring(0, nit.Length - 1);

            int suma = 0;
            int posicion = 1;

            for (int i = baseNit.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(baseNit[i]))
                    return false;

                int digito = int.Parse(baseNit[i].ToString());
                suma += digito * posicion;
                posicion++;
            }

            // Paso 3: Módulo 11 de la sumatoria
            int modulo = suma % 11;

            // Paso 4: A 11 réstale el resultado del paso 3
            int resta = 11 - modulo;

            // Paso 5: Módulo 11 del resultado del paso 4
            int resultado = resta % 11;

            char esperado;
            if (resultado == 10)
            {
                esperado = 'K';
            }
            else
            {
                esperado = resultado.ToString()[0];
            }

            return digitoVerificador == esperado;
        }
    }
}
