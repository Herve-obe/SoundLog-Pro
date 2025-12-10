using LibVLCSharp.Shared;
using System;

namespace Veriflow.Desktop.Services
{
    public class VideoEngineService
    {
        private static VideoEngineService? _instance;
        public static VideoEngineService Instance => _instance ??= new VideoEngineService();

        public LibVLC? LibVLC { get; private set; }

        public void Initialize()
        {
            if (LibVLC != null) return;

            LibVLCSharp.Shared.Core.Initialize();

            // --avcodec-hw=any : Valid for any hardware decoder (DXVA2, D3D11VA, etc.)
            // Essential for 4K/ProRes performance on Windows.
            var options = new[] { "--avcodec-hw=any" };
            LibVLC = new LibVLC(options);
        }
    }
}
