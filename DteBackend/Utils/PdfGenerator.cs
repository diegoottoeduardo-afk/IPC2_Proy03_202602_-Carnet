using System.Text;

namespace DteBackend.Utils
{
    public static class PdfGenerator
    {
        public static byte[] GenerarPdf(string titulo, List<string> lineas)
        {
            var sbContenido = new StringBuilder();
            sbContenido.AppendLine("BT");
            sbContenido.AppendLine("/F1 16 Tf");
            sbContenido.AppendLine("50 750 Td");
            sbContenido.AppendLine($"({Sanitizar(titulo)}) Tj");
            sbContenido.AppendLine("ET");

            int y = 710;
            sbContenido.AppendLine("BT");
            sbContenido.AppendLine("/F1 10 Tf");

            foreach (var linea in lineas)
            {
                if (y < 50)
                {
                    // Evitar salirse de la página
                    break;
                }

                sbContenido.AppendLine($"1 0 0 1 50 {y} Tm");
                sbContenido.AppendLine($"({Sanitizar(linea)}) Tj");
                y -= 14;
            }

            sbContenido.AppendLine("ET");

            string streamContent = sbContenido.ToString();
            byte[] streamBytes = Encoding.Latin1.GetBytes(streamContent);

            var sb = new StringBuilder();
            sb.Append("%PDF-1.4\n");

            var offsets = new List<int>();

            // Obj 1: Catalog
            offsets.Add(Encoding.Latin1.GetByteCount(sb.ToString()));
            sb.Append("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            // Obj 2: Pages
            offsets.Add(Encoding.Latin1.GetByteCount(sb.ToString()));
            sb.Append("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

            // Obj 3: Page
            offsets.Add(Encoding.Latin1.GetByteCount(sb.ToString()));
            sb.Append("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>\nendobj\n");

            // Obj 4: Font
            offsets.Add(Encoding.Latin1.GetByteCount(sb.ToString()));
            sb.Append("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");

            // Obj 5: Stream
            offsets.Add(Encoding.Latin1.GetByteCount(sb.ToString()));
            sb.Append($"5 0 obj\n<< /Length {streamBytes.Length} >>\nstream\n");
            sb.Append(streamContent);
            sb.Append("\nendstream\nendobj\n");

            // Xref
            int startXref = Encoding.Latin1.GetByteCount(sb.ToString());
            sb.Append("xref\n0 6\n");
            sb.Append("0000000000 65535 f \n");
            foreach (var off in offsets)
            {
                sb.Append($"{off:D10} 00000 n \n");
            }

            // Trailer
            sb.Append("trailer\n<< /Size 6 /Root 1 0 R >>\n");
            sb.Append($"startxref\n{startXref}\n%%EOF\n");

            return Encoding.Latin1.GetBytes(sb.ToString());
        }

        private static string Sanitizar(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return "";
            // Reemplazar caracteres especiales para PDF estándar
            return texto
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)")
                .Replace("á", "a")
                .Replace("é", "e")
                .Replace("í", "i")
                .Replace("ó", "o")
                .Replace("ú", "u")
                .Replace("Á", "A")
                .Replace("É", "E")
                .Replace("Í", "I")
                .Replace("Ó", "O")
                .Replace("Ú", "U")
                .Replace("ñ", "n")
                .Replace("Ñ", "N");
        }
    }
}
