using Veriflow.Avalonia.ViewModels;
using Veriflow.Core.Models;
using Veriflow.Avalonia.Models;
using System.Linq;

namespace Veriflow.Avalonia.Commands.Reports
{
    public class AddClipCommand : IUndoableCommand
    {
        private readonly ReportItem _target;
        private readonly ClipLogItem _item;

        public string Description => "Add Clip";

        public AddClipCommand(ReportItem target, ClipLogItem item)
        {
            _target = target;
            _item = item;
        }

        public void Execute()
        {
            _target.Clips.Add(_item);
        }

        public void UnExecute()
        {
            _target.Clips.Remove(_item);
        }

        public void Undo() => UnExecute();
    }

    public class RemoveClipCommand : IUndoableCommand
    {
        private readonly ReportItem _target;
        private readonly ClipLogItem _item;

        public string Description => "Remove Clip";

        public RemoveClipCommand(ReportItem target, ClipLogItem item)
        {
            _target = target;
            _item = item;
        }

        public void Execute()
        {
            _target.Clips.Remove(_item);
        }

        public void UnExecute()
        {
            _target.Clips.Add(_item);
        }
        
        public void Undo() => UnExecute();
    }

    public class ClearListCommand : IUndoableCommand
    {
         private readonly ReportsViewModel _vm;
         private readonly bool _isVideo;
         // Storing list copy for undo
         private System.Collections.Generic.List<ReportItem> _backup;

         public string Description => "Clear List";

         public ClearListCommand(ReportsViewModel vm, bool isVideo)
         {
             _vm = vm;
             _isVideo = isVideo;
         }

         public void Execute()
         {
             var list = _isVideo ? _vm.VideoReportItems : _vm.AudioReportItems;
             _backup = list.ToList();
             list.Clear();
         }

         public void UnExecute()
         {
             var list = _isVideo ? _vm.VideoReportItems : _vm.AudioReportItems;
             foreach(var item in _backup) list.Add(item);
         }
         
         public void Undo() => UnExecute();
    }

    public class RemoveReportItemCommand : IUndoableCommand
    {
        private readonly ReportsViewModel _vm;
        private readonly ReportItem _item;
        private readonly bool _isVideo;

        public string Description => "Remove Item";

        public RemoveReportItemCommand(ReportsViewModel vm, ReportItem item, bool isVideo)
        {
            _vm = vm;
            _item = item;
            _isVideo = isVideo;
        }

        public void Execute()
        {
            if (_isVideo) _vm.VideoReportItems.Remove(_item);
            else _vm.AudioReportItems.Remove(_item);
        }

        public void UnExecute()
        {
             if (_isVideo) _vm.VideoReportItems.Add(_item);
             else _vm.AudioReportItems.Add(_item);
        }
        
        public void Undo() => UnExecute();
    }

    public class ClearInfosCommand : IUndoableCommand
    {
        private readonly ReportsViewModel _vm;
        public string Description => "Clear Infos";
        
        public ClearInfosCommand(ReportsViewModel vm)
        {
            _vm = vm;
        }

        public void Execute()
        {
            _vm.Header.ProjectName = "";
            _vm.Header.ReportDate = "";
            // ... clear others
        }
        
        public void UnExecute()
        {
            // Restore... stub
        }
        
        public void Undo() => UnExecute();
    }
}
