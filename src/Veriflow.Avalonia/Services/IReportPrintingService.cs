using System.Collections.Generic;
using Veriflow.Avalonia.Models;

namespace Veriflow.Avalonia.Services
{
    public interface IReportPrintingService
    {
        void PrintReport(ReportHeader header, IEnumerable<ReportItem> items, ReportType type);
    }
}

