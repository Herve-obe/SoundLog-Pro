using System.Windows;
using System.Windows.Controls;
using Veriflow.Desktop.ViewModels;

namespace Veriflow.Desktop.Views
{
    public partial class VideoPlayerView : UserControl
    {
        public VideoPlayerView()
        {
            InitializeComponent();
            
            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
            this.DataContextChanged += OnDataContextChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AttachMediaPlayer();
            this.Focus();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Detach to prevent memory leaks and "stolen" instance issues
            if (VideoViewControl != null)
            {
                VideoViewControl.MediaPlayer = null;
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AttachMediaPlayer();
        }

        private void AttachMediaPlayer()
        {
            // Defensive coding: Check if UI is ready and DataContext is correct
            if (VideoViewControl == null) return;

            if (DataContext is VideoPlayerViewModel vm && vm.Player != null)
            {
                // Only attach if not already attached to avoid flickering
                if (VideoViewControl.MediaPlayer != vm.Player)
                {
                    VideoViewControl.MediaPlayer = vm.Player;
                }
            }
        }

        // --- SLIDER LOGIC ---

        private void Slider_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                // Scrubber Logic: Pause Timer
                if (slider.Tag?.ToString() == "Scrubber" && DataContext is VideoPlayerViewModel vm)
                {
                    vm.BeginSeek();
                }

                // Force Capture
                bool captured = slider.CaptureMouse();
                if (captured)
                {
                    UpdateSliderValue(slider, e);
                    e.Handled = true;
                }
            }
        }

        private void Slider_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Slider slider && slider.IsMouseCaptured)
            {
                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                {
                    UpdateSliderValue(slider, e);
                }
                else
                {
                    slider.ReleaseMouseCapture();
                    // Recover EndSeek if lost capture
                     if (slider.Tag?.ToString() == "Scrubber" && DataContext is VideoPlayerViewModel vm)
                    {
                        vm.EndSeek();
                    }
                }
            }
        }

        private void Slider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                slider.ReleaseMouseCapture();

                if (slider.Tag?.ToString() == "Scrubber" && DataContext is VideoPlayerViewModel vm)
                {
                    vm.EndSeek();
                }

                e.Handled = true;
            }
        }

        private void UpdateSliderValue(Slider slider, System.Windows.Input.MouseEventArgs e)
        {
            var point = e.GetPosition(slider);
            var width = slider.ActualWidth;
            if (width > 0)
            {
                double percent = point.X / width;
                if (percent < 0) percent = 0;
                if (percent > 1) percent = 1;

                double range = slider.Maximum - slider.Minimum;
                double value = slider.Minimum + (range * percent);
                slider.Value = value;
            }
        }
    }
}
