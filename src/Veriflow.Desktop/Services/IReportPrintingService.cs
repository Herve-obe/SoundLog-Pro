using System.Collections.Generic;
using Veriflow.Desktop.Models;

namespace Veriflow.Desktop.Services
{
    public interface IReportPrintingService
    {
        void PrintReport(ReportHeader header, IEnumerable<ReportItem> items, ReportType type);
    }
}
