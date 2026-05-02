using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using IglesiaAsistencia.Models;

namespace IglesiaAsistencia.Services
{
    public class PdfService
    {
        public void GenerarReporteDiarioDetallado(string filePath, DateTime fecha, List<Persona> asistentes, List<Persona> excusas, List<Persona> ausentes)
        {
           QuestPDF.Settings.License = LicenseType.Community;
            var cultura = new CultureInfo("es-ES");
            string rawFecha = fecha.ToString("dddd, dd 'de' MMMM 'de' yyyy", cultura);
            string fechaFormateada = char.ToUpper(rawFecha[0]) + rawFecha.Substring(1);
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_iglesia.jpeg");

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("REPORTE DIARIO DE ASISTENCIA").FontSize(24).ExtraBold().FontColor(Colors.Indigo.Medium);
                            col.Item().Text(fechaFormateada.ToUpper()).FontSize(12).SemiBold().FontColor(Colors.Grey.Medium);
                        });

                        if (File.Exists(logoPath))
                            row.ConstantItem(100).Image(logoPath);
                    });

                    page.Content().PaddingVertical(15).Column(x =>
                    {
                        x.Spacing(15);

                        // Resumen Visual
                        x.Item().Row(row =>
                        {
                            row.RelativeItem().Component(new StatCard("TOTAL PRESENTES", asistentes.Count.ToString(), Colors.Green.Medium));
                            row.Spacing(10);
                            row.RelativeItem().Component(new StatCard("EXCUSAS", excusas.Count.ToString(), Colors.Orange.Medium));
                            row.Spacing(10);
                            row.RelativeItem().Component(new StatCard("AUSENCIAS", ausentes.Count.ToString(), Colors.Red.Medium));
                        });

                        // 1. NUESTRAS VISITAS
                        var visitas = asistentes.Where(a => a.Categoria == Categoria.Visita).ToList();
                        if (visitas.Any())
                        {
                            x.Item().Column(col =>
                            {
                                col.Item().Text("NUESTRAS VISITAS").FontSize(14).Bold().FontColor(Colors.Indigo.Medium);
                                col.Item().PaddingBottom(5).BorderBottom(1).BorderColor(Colors.Indigo.Lighten3);
                                col.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(c => { c.ConstantColumn(30); c.RelativeColumn(2); c.RelativeColumn(1); c.RelativeColumn(1); });
                                    foreach (var v in visitas)
                                    {
                                        table.Cell().Text("-");
                                        table.Cell().Column(c => {
                                            c.Item().Text(v.Nombre).SemiBold();
                                            if (!string.IsNullOrEmpty(v.QuienLoInvito))
                                                c.Item().Text($"Invitado por: {v.QuienLoInvito}").FontSize(8).Italic();
                                        });
                                        table.Cell().Text(v.TipoVisita).Italic().FontSize(9);
                                        table.Cell().Text(v.Telefono ?? "").FontSize(9);
                                    }
                                });
                            });
                        }

                        // 2. AUSENTES CON COMPROMISO
                        if (ausentes.Any())
                        {
                            x.Item().Column(col =>
                            {
                                col.Item().Text("HERMANOS AUSENTES (CON COMPROMISO HOY)").FontSize(14).Bold().FontColor(Colors.Red.Medium);
                                col.Item().PaddingBottom(5).BorderBottom(1).BorderColor(Colors.Red.Lighten4);
                                col.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(c => { c.ConstantColumn(30); c.RelativeColumn(); c.RelativeColumn(); });
                                    foreach (var aus in ausentes)
                                    {
                                        table.Cell().Text("(Aus)");
                                        table.Cell().Text(aus.Nombre).SemiBold();
                                        table.Cell().Text(aus.Categoria.ToString()).FontSize(9);
                                    }
                                });
                            });
                        }

                        // 3. EXCUSAS
                        if (excusas.Any())
                        {
                            x.Item().Column(col =>
                            {
                                col.Item().Text("EXCUSAS PRESENTADAS").FontSize(14).Bold().FontColor(Colors.Orange.Medium);
                                col.Item().PaddingBottom(5).BorderBottom(1).BorderColor(Colors.Orange.Lighten4);
                                col.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(c => { c.ConstantColumn(30); c.RelativeColumn(2); c.RelativeColumn(3); });
                                    foreach (var exc in excusas)
                                    {
                                        table.Cell().Text("(Exc)");
                                        table.Cell().Text(exc.Nombre).SemiBold();
                                        table.Cell().Text(exc.CurrentNotaExcusa ?? "Sin descripción").Italic().FontSize(9);
                                    }
                                });
                            });
                        }

                        // 4. LISTA COMPLETA DE ASISTENTES (Opcional, pero útil para el registro)
                        var hermanosAsistentes = asistentes.Where(a => a.Categoria != Categoria.Visita).ToList();
                        if (hermanosAsistentes.Any())
                        {
                            x.Item().Column(col =>
                            {
                                col.Item().Text("MIEMBROS PRESENTES").FontSize(14).Bold().FontColor(Colors.Grey.Darken3);
                                col.Item().PaddingBottom(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                                col.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(c => { c.ConstantColumn(30); c.RelativeColumn(); c.RelativeColumn(); });
                                    foreach (var asis in hermanosAsistentes.OrderBy(a => a.Categoria))
                                    {
                                        table.Cell().Text("v");
                                        table.Cell().Text(asis.Nombre);
                                        table.Cell().Text(asis.Categoria.ToString()).FontSize(9);
                                    }
                                });
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                    });
                });
            }).GeneratePdf(filePath);
        }

        public void GenerarReporteAsistencia(string filePath, DateTime fecha, List<Persona> asistentes)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            
            var cultura = new CultureInfo("es-ES");
            string fechaFormateada = fecha.ToString("dd/MM/yyyy", cultura);
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_iglesia.jpeg");

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Reporte de Asistencia").FontSize(22).SemiBold().FontColor(Colors.Indigo.Medium);
                            col.Item().Text($"Fecha del Servicio: {fechaFormateada}").FontSize(12).Italic();
                        });

                        if (File.Exists(logoPath))
                            row.ConstantItem(100).Image(logoPath);
                        else
                            row.ConstantItem(100).Height(50).Placeholder();
                    });

                    page.Content().PaddingVertical(15).Column(x =>
                    {
                        x.Spacing(10);

                        // Resumen
                        x.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c => {
                                c.Item().Text("TOTAL ASISTENTES").FontSize(9).SemiBold();
                                c.Item().Text(asistentes.Count.ToString()).FontSize(18).Bold();
                            });
                            row.RelativeItem().Column(c => {
                                c.Item().Text("MIEMBROS").FontSize(9).SemiBold();
                                c.Item().Text(asistentes.Count(a => a.Categoria != Categoria.Visita).ToString()).FontSize(18).Bold();
                            });
                            row.RelativeItem().Column(c => {
                                c.Item().Text("INVITADOS").FontSize(9).SemiBold();
                                c.Item().Text(asistentes.Count(a => a.Categoria == Categoria.Visita).ToString()).FontSize(18).Bold();
                            });
                        });

                        // Agrupar por categoría
                        var grupos = asistentes.GroupBy(a => a.Categoria).OrderBy(g => g.Key);

                        foreach (var grupo in grupos)
                        {
                            x.Item().Column(col =>
                            {
                                col.Item().PaddingTop(10).Text(grupo.Key.ToString().ToUpper()).FontSize(12).SemiBold().FontColor(Colors.Indigo.Darken2);
                                col.Item().PaddingBottom(5).BorderBottom(1).BorderColor(Colors.Indigo.Lighten3);
                                
                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(30);
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("#");
                                        header.Cell().Element(CellStyle).Text("Nombre");
                                        header.Cell().Element(CellStyle).Text("Teléfono");
                                        header.Cell().Element(CellStyle).AlignRight().Text("Visitas");
                                        header.Cell().Element(CellStyle).AlignRight().Text("Estado");

                                        static IContainer CellStyle(IContainer c) => c.PaddingVertical(5).BorderBottom(1).DefaultTextStyle(s => s.SemiBold());
                                    });

                                    int i = 1;
                                    foreach (var item in grupo)
                                    {
                                        table.Cell().Element(ItemStyle).Text(i++.ToString());
                                        table.Cell().Element(ItemStyle).Text(item.Nombre);
                                        table.Cell().Element(ItemStyle).Text(item.Telefono ?? "-");
                                        table.Cell().Element(ItemStyle).AlignRight().Text(item.ContadorVisitas.ToString());
                                        table.Cell().Element(ItemStyle).AlignRight().Text(item.EsVisita ? item.TipoVisita : "Activo");
                                    }
                                });
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Asistencia Pro - Página ");
                        t.CurrentPageNumber();
                    });
                });
            })
            .GeneratePdf(filePath);
        }

        public void GenerarReporteDetallado(string filePath, string titulo, DateTime inicio, DateTime fin, List<PersonaReporteDto> datos)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var cultura = new CultureInfo("es-ES");
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_iglesia.jpeg");

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(titulo).FontSize(22).SemiBold().FontColor(Colors.Indigo.Medium);
                            col.Item().Text($"Periodo: {inicio:dd/MM/yyyy} al {fin:dd/MM/yyyy}").FontSize(12).Italic();
                        });

                        if (File.Exists(logoPath))
                            row.ConstantItem(100).Image(logoPath);
                    });

                    page.Content().PaddingVertical(15).Column(x =>
                    {
                        x.Spacing(15);

                        // Para Miembros: sub-agrupar por rango etario. Para el resto: agrupar por categoría.
                        var gruposParaReporte = datos
                            .SelectMany(d => new[] { new { Grupo = d.Categoria == Categoria.Miembro ? d.RangoEtario : d.Categoria.ToString(), EsVisita = d.Categoria == Categoria.Visita, Item = d } })
                            .GroupBy(x => x.Grupo)
                            .OrderBy(g => g.Key);

                        foreach (var grupo in gruposParaReporte)
                        {
                            bool esVisita = grupo.First().EsVisita;
                            x.Item().Column(col =>
                            {
                                col.Item().Text(grupo.Key.ToString().ToUpper()).FontSize(13).SemiBold().FontColor(Colors.Indigo.Darken2);
                                col.Item().PaddingBottom(5).BorderBottom(1).BorderColor(Colors.Indigo.Lighten3);
                                
                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(30);
                                        columns.RelativeColumn(esVisita ? 8 : 4);
                                        if (esVisita)
                                        {
                                            columns.RelativeColumn(2);
                                        }
                                        else
                                        {
                                            columns.RelativeColumn(2);
                                            columns.RelativeColumn(2);
                                            columns.RelativeColumn(2);
                                        }
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(HeaderStyle).Text("#");
                                        header.Cell().Element(HeaderStyle).Text("Nombre");
                                        
                                        if (esVisita)
                                        {
                                            header.Cell().Element(HeaderStyle).AlignRight().Text("Visitas Totales");
                                        }
                                        else
                                        {
                                            header.Cell().Element(HeaderStyle).AlignRight().Text("Asistencias");
                                            header.Cell().Element(HeaderStyle).AlignRight().Text("Total");
                                            header.Cell().Element(HeaderStyle).AlignRight().Text("% Asistencia");
                                        }

                                        static IContainer HeaderStyle(IContainer c) => c.PaddingVertical(5).BorderBottom(1).DefaultTextStyle(s => s.SemiBold());
                                    });

                                    int i = 1;
                                    var items = esVisita
                                        ? grupo.OrderByDescending(p => p.Item.VisitasHistoricas)
                                        : grupo.OrderByDescending(p => p.Item.Porcentaje);

                                    foreach (var entry in items)
                                    {
                                        var item = entry.Item;
                                        table.Cell().Element(ItemStyle).Text(i++.ToString());
                                        table.Cell().Element(ItemStyle).Text(item.Nombre);
                                        
                                        if (esVisita)
                                        {
                                            table.Cell().Element(ItemStyle).AlignRight().Text(item.VisitasHistoricas.ToString());
                                        }
                                        else
                                        {
                                            table.Cell().Element(ItemStyle).AlignRight().Text(item.AsistenciasRealizadas.ToString());
                                            table.Cell().Element(ItemStyle).AlignRight().Text(item.TotalServicios.ToString());
                                            
                                            var color = item.Porcentaje >= 80 ? Colors.Green.Medium : 
                                                        item.Porcentaje >= 50 ? Colors.Orange.Medium : Colors.Red.Medium;
                                            
                                            table.Cell().Element(ItemStyle).AlignRight().Text($"{item.Porcentaje:F1}%").FontColor(color).Bold();
                                        }
                                    }
                                });
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(t => {
                        t.Span("Reporte Detallado de Asistencia - Página ");
                        t.CurrentPageNumber();
                    });
                });
            })
            .GeneratePdf(filePath);
        }

        static IContainer ItemStyle(IContainer container)
        {
            return container.PaddingVertical(5).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3);
        }
    }

    public class PersonaReporteDto
    {
        public string Nombre { get; set; } = string.Empty;
        public Categoria Categoria { get; set; }
        public int AsistenciasRealizadas { get; set; }
        public int TotalServicios { get; set; }
        public int VisitasHistoricas { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public double Porcentaje => TotalServicios == 0 ? 0 : (double)AsistenciasRealizadas / TotalServicios * 100;
        
        public string RangoEtario
        {
            get
            {
                if (!FechaNacimiento.HasValue) return "Adultos"; // Por defecto

                var hoy = DateTime.Today;
                var edad = hoy.Year - FechaNacimiento.Value.Year;
                if (FechaNacimiento.Value.Date > hoy.AddYears(-edad)) edad--;

                if (edad <= 12) return "Niños";
                if (edad <= 30) return "Jóvenes";
                if (edad <= 59) return "Adultos";
                return "Adultos Mayores";
            }
        }
    }
    public class StatCard : IComponent
    {
        private string Title { get; }
        private string Value { get; }
        private string Color { get; }

        public StatCard(string title, string value, string color)
        {
            Title = title;
            Value = value;
            Color = color;
        }

        public void Compose(IContainer container)
        {
            container.Background(Colors.Grey.Lighten5).Border(1).BorderColor(Colors.Grey.Lighten3).Padding(10).Column(col =>
            {
                col.Item().Text(Title).FontSize(8).SemiBold().FontColor(Colors.Grey.Darken1);
                col.Item().Text(Value).FontSize(18).ExtraBold().FontColor(Color);
            });
        }
    }
}
