using System;
using System.Collections.Generic;
using Veriflow.Avalonia.Models;
using Veriflow.Core.Models;

namespace Veriflow.Avalonia.Services
{
    // STUB: ReportPrintingService disabled during Avalonia migration
    // Requires rewrite using SkiaSharp or Avalonia.Printing
    public class ReportPrintingService : IReportPrintingService
    {
        public void PrintReport(ReportHeader header, IEnumerable<ReportItem> items, ReportType type)
        {
            System.Diagnostics.Debug.WriteLine("PrintReport Stub called.");
        }
    }
}
