using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using IglesiaAsistencia.Models;

namespace IglesiaAsistencia.Services
{
    public class WhatsAppService
    {
        public void EnviarReporte(DateTime fecha, List<Persona> asistentes, List<Persona> cumpleañeros)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"📊 Reporte de Asistencia - {fecha:dddd dd/MM/yyyy}");
            sb.AppendLine();
            sb.AppendLine($"Total asistentes: {asistentes.Count}");
            sb.AppendLine($"Pastores: {asistentes.Count(a => a.Categoria == Categoria.Pastor)}");
            sb.AppendLine($"Diáconos: {asistentes.Count(a => a.Categoria == Categoria.Diacono)}");
            sb.AppendLine($"Miembros: {asistentes.Count(a => a.Categoria == Categoria.Miembro)}");
            sb.AppendLine($"Adolescentes: {asistentes.Count(a => a.Categoria == Categoria.Adolescente)}");
            sb.AppendLine($"Invitados: {asistentes.Count(a => a.Categoria == Categoria.Invitado)}");
            sb.AppendLine();

            if (cumpleañeros.Any())
            {
                sb.AppendLine("🎉 Cumpleaños:");
                foreach (var c in cumpleañeros)
                {
                    string timing = c.EsCumpleañosHoy ? "(hoy)" : "(esta semana)";
                    sb.AppendLine($"- {c.Nombre} {timing}");
                }
                sb.AppendLine();
            }

            var invitados = asistentes.Where(a => a.Categoria == Categoria.Invitado).ToList();
            if (invitados.Any())
            {
                sb.AppendLine("👋 Invitados:");
                sb.AppendLine();
                
                var nuevos = invitados.Where(i => i.ContadorVisitas <= 1).ToList();
                if (nuevos.Any())
                {
                    sb.AppendLine("🆕 Nuevos:");
                    foreach (var n in nuevos) sb.AppendLine($"- {n.Nombre}");
                }

                var recurrentes = invitados.Where(i => i.ContadorVisitas > 1).ToList();
                if (recurrentes.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine("🔁 Recurrentes:");
                    foreach (var r in recurrentes) sb.AppendLine($"- {r.Nombre} ({r.ContadorVisitas} visitas)");
                }
            }

            string mensaje = WebUtility.UrlEncode(sb.ToString());
            string url = $"https://wa.me/?text={mensaje}";
            
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}
