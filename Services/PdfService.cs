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
        public void GenerarReporteAsistencia(string filePath, DateTime fecha, List<Persona> asistentes)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            
            // Configurar cultura a español para nombres de meses y días
            var cultura = new CultureInfo("es-ES");
            string fechaFormateada = fecha.ToString("dd/MM/yyyy", cultura);
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.jpg");

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Verdana));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Reporte de Asistencia").FontSize(24).SemiBold().FontColor(Colors.Indigo.Medium);
                            col.Item().Text($"Fecha: {fechaFormateada}").FontSize(14).Italic();
                        });

                        // Insertar Logo si existe
                        if (File.Exists(logoPath))
                        {
                            row.ConstantItem(120).Image(logoPath);
                        }
                        else
                        {
                            row.ConstantItem(120).Height(60).Placeholder();
                        }
                    });

                    page.Content().PaddingVertical(20).Column(x =>
                    {
                        x.Spacing(15);

                        // Resumen Estadístico
                        x.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c => {
                                c.Item().Text("TOTAL ASISTENTES").FontSize(10).SemiBold();
                                c.Item().Text(asistentes.Count.ToString()).FontSize(20).Bold();
                            });
                            row.RelativeItem().Column(c => {
                                c.Item().Text("INVITADOS").FontSize(10).SemiBold();
                                c.Item().Text(asistentes.Count(a => a.Categoria == Categoria.Invitado).ToString()).FontSize(20).Bold();
                            });
                        });

                        // Tabla de Detalle
                        x.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("#");
                                header.Cell().Element(CellStyle).Text("Nombre y Apellido");
                                header.Cell().Element(CellStyle).Text("Categoría");
                                header.Cell().Element(CellStyle).Text("Tipo");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(2).BorderColor(Colors.Indigo.Medium);
                                }
                            });

                            int i = 1;
                            foreach (var item in asistentes)
                            {
                                table.Cell().Element(ItemStyle).Text(i++.ToString());
                                table.Cell().Element(ItemStyle).Text(item.Nombre);
                                table.Cell().Element(ItemStyle).Text(item.Categoria.ToString());
                                table.Cell().Element(ItemStyle).Text(item.EsInvitado ? item.TipoInvitado : "Miembro");

                                static IContainer ItemStyle(IContainer container)
                                {
                                    return container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                                }
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Generado automáticamente por Asistencia Pro - Página ");
                        x.CurrentPageNumber();
                    });
                });
            })
            .GeneratePdf(filePath);
        }
    }
}
