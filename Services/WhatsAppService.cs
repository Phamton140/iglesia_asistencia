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
            sb.AppendLine($"En Seguimiento: {asistentes.Count(a => a.Categoria == Categoria.Seguimiento)}");
            sb.AppendLine($"Visitas: {asistentes.Count(a => a.Categoria == Categoria.Visita)}");
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

            var visitas = asistentes.Where(a => a.Categoria == Categoria.Visita).ToList();
            if (visitas.Any())
            {
                sb.AppendLine("👋 Nuestras Visitas:");
                
                foreach (var v in visitas)
                {
                    string infoInvito = !string.IsNullOrEmpty(v.QuienLoInvito) ? $" (Inv. por: {v.QuienLoInvito})" : "";
                    string status = v.ContadorVisitas <= 1 ? "🆕" : "🔁";
                    sb.AppendLine($"{status} {v.Nombre} ({v.ContadorVisitas}){infoInvito}");
                }
            }

            string mensaje = WebUtility.UrlEncode(sb.ToString());
            string url = $"https://wa.me/?text={mensaje}";
            
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}
