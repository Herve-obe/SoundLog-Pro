using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using LibVLCSharp.Shared;
using System.Threading.Tasks;

namespace Veriflow.Avalonia.Views
{
    public partial class VideoPreviewWindow : Window
    {
        public MediaPlayer? Player { get; private set; }
        
        public VideoPreviewWindow()
        {
            InitializeComponent();
            
            // ESC to close
            KeyDown += OnKeyDown;
        }
        
        public void SetPlayer(MediaPlayer player)
        {
            Player = player;
            VideoView.MediaPlayer = player;
        }
        
        public void SetTitle(string title)
        {
            TitleText.Text = title;
        }
        
        /// <summary>
        /// Wait for VideoView to be fully initialized before playing.
        /// This prevents VLC from opening in a separate native window.
        /// </summary>
        public async Task InitializeAsync()
        {
            // Wait for window to be visible
            int attempts = 0;
            while (!IsVisible && attempts < 20)
            {
                await Task.Delay(50);
                attempts++;
            }
            
            // Give VideoView MORE time to create its window handle (increased to 500ms)
            await Task.Delay(500);
            
            // Force layout update
            VideoView.InvalidateVisual();
            VideoView.InvalidateMeasure();
            VideoView.InvalidateArrange();
            
            System.Diagnostics.Debug.WriteLine("[VideoPreviewWindow] Initialized and ready for playback");
        }
        
        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
        
        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
        
        protected override void OnClosing(WindowClosingEventArgs e)
        {
            // Stop playback
            Player?.Stop();
            base.OnClosing(e);
        }
    }
}
