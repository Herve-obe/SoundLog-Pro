using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Veriflow.Desktop.Services
{
    public static class DocumentationService
    {
        public static void GenerateUserGuidePDF(string outputPath)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header()
                        .Text("Veriflow Pro - User Guide")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(20);

                            // COVER PAGE
                            x.Item().AlignCenter().Text("VERIFLOW PRO").FontSize(32).Bold();
                            x.Item().AlignCenter().Text("User Guide").FontSize(24).SemiBold();
                            x.Item().AlignCenter().Text("Version 1.0.0").FontSize(14);
                            x.Item().AlignCenter().PaddingTop(10).Text("© 2025 Veriflow. All rights reserved.").FontSize(10);

                            x.Item().PageBreak();

                            // TABLE OF CONTENTS
                            x.Item().Text("Table of Contents").FontSize(18).Bold();
                            x.Item().PaddingLeft(20).Column(toc =>
                            {
                                toc.Item().Text("PART I: GETTING STARTED");
                                toc.Item().PaddingLeft(10).Text("1. Installation and Setup");
                                toc.Item().PaddingLeft(10).Text("2. User Interface Overview");
                                toc.Item().PaddingLeft(10).Text("3. Basic Workflow");
                                toc.Item().PaddingTop(5).Text("PART II: CORE FEATURES");
                                toc.Item().PaddingLeft(10).Text("4. Session Management");
                                toc.Item().PaddingLeft(10).Text("5. Profile System");
                                toc.Item().PaddingTop(5).Text("PART III: PAGES IN-DEPTH");
                                toc.Item().PaddingLeft(10).Text("6. SECURE COPY Page");
                                toc.Item().PaddingLeft(10).Text("7. MEDIA Page");
                                toc.Item().PaddingLeft(10).Text("8. PLAYER Page");
                                toc.Item().PaddingLeft(10).Text("9. SYNC Page");
                                toc.Item().PaddingLeft(10).Text("10. TRANSCODE Page");
                                toc.Item().PaddingLeft(10).Text("11. REPORTS Page");
                                toc.Item().PaddingTop(5).Text("PART IV: ADVANCED TOPICS");
                                toc.Item().PaddingLeft(10).Text("12. Keyboard Shortcuts");
                                toc.Item().PaddingLeft(10).Text("13. File Formats");
                                toc.Item().PaddingTop(5).Text("APPENDICES");
                                toc.Item().PaddingLeft(10).Text("A. Troubleshooting");
                                toc.Item().PaddingLeft(10).Text("B. Legal Information");
                            });

                            x.Item().PageBreak();

                            // PART I: GETTING STARTED
                            x.Item().Text("PART I: GETTING STARTED").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                            x.Item().LineHorizontal(1);
                            
                            x.Item().PaddingTop(10).Text("Chapter 1: Installation and Setup").FontSize(14).Bold();
                            x.Item().Text("1.1 System Requirements").FontSize(12).SemiBold();
                            x.Item().PaddingLeft(10).Text(@"
MINIMUM REQUIREMENTS:
• Operating System: Windows 10 (64-bit) or Windows 11
• Processor: Intel Core i5 or AMD equivalent
• RAM: 8GB
• Storage: 500MB free disk space
• .NET Runtime: .NET 8.0 or higher

RECOMMENDED REQUIREMENTS:
• Operating System: Windows 11 (64-bit)
• Processor: Intel Core i7 or AMD Ryzen 7
• RAM: 16GB or more
• Storage: 1GB free disk space (SSD recommended)
• Display: 1920x1080 or higher resolution");

                            x.Item().PaddingTop(10).Text("1.2 Installation Steps").FontSize(12).SemiBold();
                            x.Item().PaddingLeft(10).Text(@"
1. Download the Veriflow installer
2. Run the installer executable
3. Accept the license agreement
4. Choose installation directory
5. Click 'Install' and wait for completion
6. Launch Veriflow from the Start menu");

                            x.Item().PageBreak();

                            x.Item().Text("Chapter 2: User Interface Overview").FontSize(14).Bold();
                            x.Item().Text("2.1 Main Window Layout").FontSize(12).SemiBold();
                            x.Item().PaddingLeft(10).Text(@"
The Veriflow main window consists of:

TOP MENU BAR:
• File menu (sessions, settings, exit)
• Edit menu (clipboard, clear page)
• View menu (navigation, display options)
• Help menu (documentation, logs, about)

MAIN CONTENT AREA:
Displays the active page (SECURE COPY, MEDIA, PLAYER, SYNC, TRANSCODE, REPORTS)

BOTTOM NAVIGATION BAR:
Quick access buttons for all pages with visual indicators

PROFILE TOGGLE:
Switch between VIDEO profile (blue) and AUDIO profile (red)");

                            x.Item().PaddingTop(10).Text("2.2 Menu System").FontSize(12).SemiBold();
                            x.Item().PaddingLeft(10).Text(@"
FILE MENU:
• New Session (Ctrl+N) - Create new workspace
• Open Session (Ctrl+O) - Load saved session
• Save Session (Ctrl+S) - Save current workspace
• Settings - Application preferences
• Exit (Alt+F4) - Close application

EDIT MENU:
• Cut (Ctrl+X), Copy (Ctrl+C), Paste (Ctrl+V)
• Clear Current Page - Reset active page

VIEW MENU:
• Navigation shortcuts to all pages (F1-F6)

HELP MENU:
• View Help (F12) - Open this user guide
• Open Log Folder - Access application logs
• About Veriflow - Version and license information");

                            x.Item().PageBreak();

                            x.Item().Text("Chapter 3: Basic Workflow").FontSize(14).Bold();
                            x.Item().Text("3.1 Navigating the Application").FontSize(12).SemiBold();
                            x.Item().PaddingLeft(10).Text(@"
USING FUNCTION KEYS:
• F1: SECURE COPY
• F2: MEDIA
• F3: PLAYER
• F4: SYNC
• F5: TRANSCODE
• F6: REPORTS

USING THE NAVIGATION BAR:
Click any page button at the bottom (current page is highlighted)

USING THE MENU:
View menu > Select page name");

                            x.Item().PaddingTop(10).Text("3.2 Switching Profiles").FontSize(12).SemiBold();
                            x.Item().PaddingLeft(10).Text(@"
VIDEO PROFILE (Blue):
• Optimized for video workflows
• Video-specific features enabled
• Default profile on startup

AUDIO PROFILE (Red):
• Optimized for audio workflows
• Multi-track audio support

TO SWITCH: Click profile toggle or press Ctrl+Tab");

                            x.Item().PageBreak();

                            // PART II: CORE FEATURES
                            x.Item().Text("PART II: CORE FEATURES").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                            x.Item().LineHorizontal(1);

                            x.Item().PaddingTop(10).Text("Chapter 4: Session Management").FontSize(14).Bold();
                            x.Item().Text("4.1 What is a Session?").FontSize(12).SemiBold();
                            x.Item().PaddingLeft(10).Text(@"
A session saves your complete workspace state:
• Current page and profile mode
• Loaded media files
• Generated reports
• Transcode queue
• Secure copy settings

Sessions are saved as .vfsession files (JSON format)");

                            x.Item().PaddingTop(10).Text("4.2 Session Operations").FontSize(12).SemiBold();
                            x.Item().PaddingLeft(10).Text(@"
NEW SESSION (Ctrl+N):
Creates empty workspace, prompts to save if modified

OPEN SESSION (Ctrl+O):
Loads .vfsession file, restores complete state

SAVE SESSION (Ctrl+S):
Saves current workspace to .vfsession file

BEST PRACTICES:
• Save sessions regularly
• Use descriptive filenames
• Organize sessions by project");

                            x.Item().PageBreak();

                            x.Item().Text("Chapter 5: Profile System").FontSize(14).Bold();
                            x.Item().Text("5.1 Understanding Profiles").FontSize(12).SemiBold();
                            x.Item().PaddingLeft(10).Text(@"
Veriflow uses dual-profile system:

VIDEO PROFILE:
• Video-centric workflows
• EDL generation
• Logged clips
• Frame-accurate playback

AUDIO PROFILE:
• Audio-centric workflows
• Multi-track display (up to 32 tracks)
• VU meters
• Waveform visualization");

                            x.Item().PageBreak();

                            // PART III: PAGES IN-DEPTH
                            x.Item().Text("PART III: PAGES IN-DEPTH").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                            x.Item().LineHorizontal(1);

                            x.Item().PaddingTop(10).Text("Chapter 6: SECURE COPY Page").FontSize(14).Bold();
                            x.Item().PaddingLeft(10).Text(@"
PURPOSE: Dual-destination file copying with hash verification

WORKFLOW:
1. Select source file/folder
2. Set Main Destination (A)
3. Set Secondary Destination (B)
4. Click START COPY
5. Monitor progress
6. Verify completion

FEATURES:
• xxHash64 verification
• Progress monitoring
• Copy history
• Error detection");

                            x.Item().PageBreak();

                            x.Item().Text("Chapter 8: PLAYER Page").FontSize(14).Bold();
                            x.Item().Text("VIDEO PROFILE").FontSize(12).SemiBold();
                            x.Item().PaddingLeft(10).Text(@"
FEATURES:
• Video viewport
• Transport controls (Space: Play/Pause, Enter: Stop)
• Frame-accurate playback
• Timecode display
• Metadata panel
• Logged clips list
• EDL markers

KEYBOARD SHORTCUTS:
• Space: Play/Pause
• Enter: Stop
• Left/Right: Frame step
• J/K/L: Shuttle control");

                            x.Item().PaddingTop(10).Text("AUDIO PROFILE").FontSize(12).SemiBold();
                            x.Item().PaddingLeft(10).Text(@"
FEATURES:
• Waveform display
• Multi-track audio (up to 32 tracks)
• Per-track controls:
  - Mute button
  - Solo button
  - Volume fader (double-click to reset)
  - Pan fader (double-click to reset)
  - VU meter (real-time levels)
• Transport controls
• Metadata panel");

                            x.Item().PageBreak();

                            // PART IV: ADVANCED TOPICS
                            x.Item().Text("PART IV: ADVANCED TOPICS").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                            x.Item().LineHorizontal(1);

                            x.Item().PaddingTop(10).Text("Chapter 12: Keyboard Shortcuts Reference").FontSize(14).Bold();
                            x.Item().Text("GLOBAL SHORTCUTS").FontSize(12).SemiBold();
                            x.Item().PaddingLeft(10).Text(@"
SESSION:
• Ctrl+N: New Session
• Ctrl+O: Open Session
• Ctrl+S: Save Session

EDIT:
• Ctrl+X: Cut
• Ctrl+C: Copy
• Ctrl+V: Paste

NAVIGATION:
• F1: SECURE COPY
• F2: MEDIA
• F3: PLAYER
• F4: SYNC
• F5: TRANSCODE
• F6: REPORTS

PROFILE:
• Ctrl+Tab: Toggle Audio/Video

HELP:
• F12: View Help");

                            x.Item().PaddingTop(10).Text("PLAYER SHORTCUTS").FontSize(12).SemiBold();
                            x.Item().PaddingLeft(10).Text(@"
PLAYBACK:
• Space: Play/Pause
• Enter: Stop
• Left/Right: Frame step
• Home/End: Go to start/end

SHUTTLE:
• J: Reverse
• K: Pause
• L: Forward");

                            x.Item().PageBreak();

                            // APPENDICES
                            x.Item().Text("APPENDICES").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                            x.Item().LineHorizontal(1);

                            x.Item().PaddingTop(10).Text("Appendix B: Legal Information").FontSize(14).Bold();
                            x.Item().PaddingLeft(10).Text(@"
THIRD-PARTY SOFTWARE:

FFmpeg (LGPL v2.1 / GPL v2+)
Source: https://ffmpeg.org
Used for: Media transcoding

LibVLC (LGPL v2.1+)
Source: https://www.videolan.org
Used for: Video playback

.NET 8.0 Runtime (MIT)
Source: https://dotnet.microsoft.com

CSCore (MS-PL)
Used for: Audio processing

QuestPDF (MIT)
Used for: PDF generation

MathNet.Numerics (MIT)
Used for: Mathematical computations

For complete license information, see Help > About Veriflow

COPYRIGHT:
© 2025 Veriflow. All rights reserved.");
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" | © 2025 Veriflow");
                        });
                });
            })
            .GeneratePdf(outputPath);
        }
    }
}
