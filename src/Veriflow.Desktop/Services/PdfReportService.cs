using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Veriflow.Desktop.Models;

namespace Veriflow.Desktop.Services
{
    public class PdfReportService
    {
        public PdfReportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public void GeneratePdf(string filePath, ReportHeader header, IEnumerable<ReportItem> items, bool isVideo)
        {
            // Calculate Total Size
            long totalBytes = 0;
            foreach (var item in items)
            {
                try
                {
                   if (File.Exists(item.OriginalMedia?.FullName))
                   {
                       totalBytes += new FileInfo(item.OriginalMedia.FullName).Length;
                   }
                }
                catch { /* Ignore access errors */ }
            }
            string totalSizeStr = FormatBytes(totalBytes);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    // Portrait is usually better for lists unless many columns.
                    // 7 Columns -> Landscape might be safer, but let's stick to Portrait default or user choice.
                    // Given the columns (Filename 3, Scene 1, Take 1, TC 80, Dur 80, Notes 3), it fits A4 Portrait tightly.
                    // Let's go Landscape to be safe and "Pro".
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial));

                    page.Header().Element(compose => ComposeHeader(compose, header, isVideo, totalSizeStr));
                    page.Content().Element(compose => ComposeContent(compose, items));
                    page.Footer().Element(ComposeFooter);
                });
            })
            .GeneratePdf(filePath);
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n2} {1}", number, suffixes[counter]);
        }

        private void ComposeHeader(IContainer container, ReportHeader header, bool isVideo, string totalSize)
        {
            // Title Logic
            string title = isVideo ? "CAMERA REPORT" : "SOUND REPORT";
            string brandingColor = isVideo ? "#1A4CB1" : "#D32F2F"; // Blue or Red accent

            // Logo Path
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "veriflow.ico");
            // Fix path if running from bin/Debug/... to point to src if needed, or rely on CopyToOutput.
            // Assuming Assets are copied to output. If not, fallback to source path for dev.
            if (!File.Exists(logoPath))
            {
                 // Fallback to strict source path since we are in dev environment
                 logoPath = @"d:\ELEMENT\VERIFLOW\src\Veriflow.Desktop\Assets\veriflow.ico";
            }

            container.Column(column =>
            {
                // Banner / Title
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text(title).FontSize(24).ExtraBold().FontColor(brandingColor);
                    
                    // Logo Integration
                    if (File.Exists(logoPath))
                    {
                        // Use AutoItem to allow width to adjust based on aspect ratio
                        // constrain height explicitly to 40 units
                        row.AutoItem().Height(40).AlignRight().Image(logoPath).FitHeight();
                    }
                    else
                    {
                         row.RelativeItem().AlignRight().Text("VERIFLOW").FontSize(14).SemiBold().FontColor(Colors.Grey.Medium);
                    }
                });

                column.Item().PaddingTop(10).LineHorizontal(2).LineColor(brandingColor);

                // Metadata Grid
                column.Item().PaddingTop(10).Row(row =>
                {
                    // LEFT COLUMN
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(t => { t.Span("Project: ").SemiBold(); t.Span(header.ProjectName); });
                        c.Item().Text(t => { t.Span("Date: ").SemiBold(); t.Span(header.ReportDate); });
                        
                        if (isVideo)
                        {
                             c.Item().Text(t => { t.Span("Operator: ").SemiBold(); t.Span(header.OperatorName); });
                        }
                        else
                        {
                             c.Item().Text(t => { t.Span("Location: ").SemiBold(); t.Span(header.Location); });
                        }
                    });

                    // RIGHT COLUMN
                    row.RelativeItem().Column(c =>
                    {
                        if (isVideo)
                        {
                            c.Item().Text(t => { t.Span("Director: ").SemiBold(); t.Span(header.Director); });
                            c.Item().Text(t => { t.Span("DOP: ").SemiBold(); t.Span(header.Dop); });
                            c.Item().Text(t => { t.Span("Cam ID: ").SemiBold(); t.Span(header.CameraId); });
                            c.Item().Text(t => { t.Span("Roll: ").SemiBold(); t.Span(header.ReelName); });
                            c.Item().Text(t => { t.Span("Data Manager: ").SemiBold(); t.Span(header.DataManager); });
                        }
                        else
                        {
                            c.Item().Text(t => { t.Span("Sound Mixer: ").SemiBold(); t.Span(header.SoundMixer); });
                            c.Item().Text(t => { t.Span("Boom Op: ").SemiBold(); t.Span(header.BoomOperator); });
                            c.Item().Text(t => { t.Span("Timecode Rate: ").SemiBold(); t.Span(header.TimecodeRate); });
                            c.Item().Text(t => { t.Span("Bit Depth: ").SemiBold(); t.Span(header.BitDepth); });
                            c.Item().Text(t => { t.Span("Sample Rate: ").SemiBold(); t.Span(header.SampleRate); });
                            c.Item().Text(t => { t.Span("Files Info: ").SemiBold(); t.Span(header.FilesType); });
                        }
                        
                         c.Item().Text(t => { t.Span("Total Size: ").SemiBold(); t.Span(totalSize); });
                    });
                });
                
                column.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });
        }

        private void ComposeContent(IContainer container, IEnumerable<ReportItem> items)
        {
            container.PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3); // Filename
                    columns.RelativeColumn(1); // Scene
                    columns.RelativeColumn(1); // Take
                    columns.ConstantColumn(85); // Start TC
                    columns.ConstantColumn(85); // Duration
                    columns.RelativeColumn(3); // Notes
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Filename");
                    header.Cell().Element(CellStyle).Text("Scene");
                    header.Cell().Element(CellStyle).Text("Take");
                    header.Cell().Element(CellStyle).Text("Start TC");
                    header.Cell().Element(CellStyle).Text("Duration");
                    header.Cell().Element(CellStyle).Text("Notes");

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.Background("#333333").Padding(6).DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(10));
                    }
                });

                // Content
                var itemList = items.ToList();
                for (int i = 0; i < itemList.Count; i++)
                {
                    var item = itemList[i];
                    var bgColor = i % 2 == 0 ? "#F5F5F5" : "#FFFFFF";
                    var isBold = item.IsCircled; 
                    
                    table.Cell().Element(c => BodyCellStyle(c, bgColor)).Text(t => 
                    {
                        var span = t.Span(item.Filename);
                        if (isBold) span.Bold();
                    });

                    table.Cell().Element(c => BodyCellStyle(c, bgColor)).Text(t => 
                    {
                        var span = t.Span(item.Scene ?? "");
                        if (isBold) span.Bold();
                    });

                    table.Cell().Element(c => BodyCellStyle(c, bgColor)).Text(t => 
                    {
                        var span = t.Span(item.Take ?? "");
                        if (isBold) span.Bold();
                    });

                    table.Cell().Element(c => BodyCellStyle(c, bgColor)).Text(t => 
                    {
                        var span = t.Span(item.StartTimeCode ?? "").FontFamily(Fonts.CourierNew);
                        if (isBold) span.Bold();
                    });

                    table.Cell().Element(c => BodyCellStyle(c, bgColor)).Text(t => 
                    {
                        var span = t.Span(item.Duration ?? "").FontFamily(Fonts.CourierNew);
                        if (isBold) span.Bold();
                    });

                    table.Cell().Element(c => BodyCellStyle(c, bgColor)).Text(t => 
                    {
                        var span = t.Span(item.ItemNotes ?? "");
                        if (isBold) span.Bold();
                    });
                }

                static IContainer BodyCellStyle(IContainer container, string backgroundColor)
                {
                    return container.Background(backgroundColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).AlignMiddle();
                }
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });

                row.RelativeItem().AlignRight().Text("Generated by Veriflow 1.0 (Beta)");
            });
        }
    }
}
