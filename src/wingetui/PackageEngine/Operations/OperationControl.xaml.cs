using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using ModernWindow.Core.Data;
using ModernWindow.PackageEngine.Classes;
using ModernWindow.PackageEngine.Operations;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.PackageEngine
{
    public abstract partial class AbstractOperation : UserControl
    {
        protected enum AfterFinshAction
        {
            TimeoutClose,
            ManualClose,
            Retry,
        }

        private enum WidgetLayout
        {
            Default,
            Compact,
        }

        public static AppTools Tools = AppTools.Instance;

        private string __button_text;
        private string __line_info_text = "Please wait...";
        private Uri __icon_source = new("ms-appx://wingetui/resources/package_color.png");
        private string __operation_description = "$Package Install";
        private SolidColorBrush? __progressbar_color = null;
        private OperationStatus __status = OperationStatus.Pending;
        private bool IsDialogOpen = false;

        private WidgetLayout __layout_mode;
        private WidgetLayout LayoutMode
        {
            set
            {
                if (value == WidgetLayout.Compact)
                {
                    Grid.SetColumn(OutputViewewBlock, 0);
                    Grid.SetColumnSpan(OutputViewewBlock, 4);
                    Grid.SetRow(OutputViewewBlock, 1);
                    Grid.SetColumn(ProgressIndicator, 0);
                    Grid.SetColumnSpan(ProgressIndicator, 4);
                    Grid.SetRow(ProgressIndicator, 1);
                    if (MainGrid.RowDefinitions.Count < 2)
                        MainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                }
                else
                {
                    Grid.SetColumn(OutputViewewBlock, 2);
                    Grid.SetColumnSpan(OutputViewewBlock, 1);
                    Grid.SetRow(OutputViewewBlock, 0);
                    Grid.SetColumn(ProgressIndicator, 2);
                    Grid.SetColumnSpan(ProgressIndicator, 1);
                    Grid.SetRow(ProgressIndicator, 0);
                    if (MainGrid.RowDefinitions.Count >= 2)
                        MainGrid.RowDefinitions.RemoveAt(1);
                }
                __layout_mode = value;
            }
            get { return __layout_mode; }
        }

        protected string ButtonText
        {
            get { return __button_text; }
            set { __button_text = value; if (ActionButton != null) ActionButton.Content = __button_text; }
        }
        protected string LineInfoText
        {
            get { return __line_info_text; }
            set { __line_info_text = value; if (OutputViewewBlock != null) OutputViewewBlock.Content = __line_info_text; }
        }
        protected Uri IconSource
        {
            get { return __icon_source; }
            set { __icon_source = value; if (PackageIcon != null) PackageIcon.Source = new BitmapImage(__icon_source); }
        }
        protected string OperationTitle
        {
            get { return __operation_description; }
            set { __operation_description = value; if (InfoTextBlock != null) InfoTextBlock.Text = __operation_description; }
        }
        protected SolidColorBrush? ProgressBarColor
        {
            get { return __progressbar_color; }
            set { __progressbar_color = value; if (ProgressIndicator != null) ProgressIndicator.Foreground = (__progressbar_color != null) ? __progressbar_color : null; }
        }

#pragma warning disable CS0067
        protected event EventHandler<OperationCancelledEventArgs> CancelRequested;
        protected event EventHandler<OperationCancelledEventArgs> CloseRequested;
#pragma warning restore CS0067
        protected Process Process;
        protected ObservableCollection<string> ProcessOutput = new();

        private ContentDialog OutputDialog = new();
        private ScrollViewer LiveOutputScrollBar = new();
        private RichTextBlock LiveOutputTextBlock = new();

        public OperationStatus Status
        {
            get { return __status; }
            set
            {
                MainGrid.RequestedTheme = Tools.App.MainWindow.ContentRoot.RequestedTheme;
                __status = value;
                switch (__status)
                {

                    /*
                     * 
                     * 
        <SolidColorBrush x:Key="ProgressWaiting" Color="{ThemeResource SystemFillColorNeutralBrush}"/>
        <SolidColorBrush x:Key="ProgressRunning" Color="{ThemeResource SystemFillColorAttentionBrush}"/>
        <SolidColorBrush x:Key="ProgressSucceeded" Color="{ThemeResource SystemFillColorSuccessBrush}"/>
        <SolidColorBrush x:Key="ProgressFailed" Color="{ThemeResource SystemFillColorCriticalBrush}"/>
        <SolidColorBrush x:Key="ProgressCanceled" Color="{ThemeResource SystemFillColorCautionBrush}"/>
                     * */
                    case OperationStatus.Pending:
                        ProgressIndicator.IsIndeterminate = false;
                        ProgressIndicator.Foreground = (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBrush"];
                        MainGrid.Background = (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBackgroundBrush"];
                        ButtonText = Tools.Translate("Cancel");
                        break;

                    case OperationStatus.Running:
                        ProgressIndicator.IsIndeterminate = true;
                        ProgressIndicator.Foreground = (SolidColorBrush)Application.Current.Resources["SystemFillColorAttentionBrush"];
                        MainGrid.Background = (SolidColorBrush)Application.Current.Resources["SystemFillColorAttentionBackgroundBrush"];

                        ButtonText = Tools.Translate("Cancel");
                        break;

                    case OperationStatus.Succeeded:
                        ProgressIndicator.Foreground = (SolidColorBrush)Application.Current.Resources["SystemFillColorSuccessBrush"];
                        MainGrid.Background = (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBackgroundBrush"];
                        ProgressIndicator.IsIndeterminate = false;
                        ButtonText = Tools.Translate("Close");
                        break;

                    case OperationStatus.Failed:
                        ProgressIndicator.IsIndeterminate = false;
                        ProgressIndicator.Foreground = (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                        MainGrid.Background = (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBackgroundBrush"];
                        ButtonText = Tools.Translate("Close");
                        break;

                    case OperationStatus.Cancelled:
                        ProgressIndicator.IsIndeterminate = false;
                        ProgressIndicator.Foreground = (SolidColorBrush)Application.Current.Resources["SystemFillColorCautionBrush"];
                        MainGrid.Background = (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBackgroundBrush"];
                        ButtonText = Tools.Translate("Close");
                        break;
                }
            }
        }
        public AbstractOperation()
        {
            InitializeComponent();

            OutputDialog = new ContentDialog();
            OutputDialog.Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"];
            OutputDialog.XamlRoot = XamlRoot;
            OutputDialog.Resources["ContentDialogMaxWidth"] = 1200;
            OutputDialog.Resources["ContentDialogMaxHeight"] = 1000;

            LiveOutputTextBlock = new RichTextBlock();
            LiveOutputTextBlock.Margin = new Thickness(8);
            LiveOutputTextBlock.FontFamily = new FontFamily("Consolas");

            LiveOutputScrollBar = new ScrollViewer();
            LiveOutputScrollBar.CornerRadius = new CornerRadius(6);
            LiveOutputScrollBar.Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"];
            LiveOutputScrollBar.Height = 400;
            LiveOutputScrollBar.Width = 600;
            LiveOutputScrollBar.Content = LiveOutputTextBlock;

            OutputDialog.Title = Tools.Translate("Live output");
            OutputDialog.CloseButtonText = Tools.Translate("Close");


            OutputDialog.SizeChanged += (s, e) =>
            {
                if (!IsDialogOpen)
                    return;

                LiveOutputScrollBar.MinWidth = Tools.App.MainWindow.NavigationPage.ActualWidth - 400;
                LiveOutputScrollBar.MinHeight = Tools.App.MainWindow.NavigationPage.ActualHeight - 200;
            };

            OutputDialog.Content = LiveOutputScrollBar;

            ProcessOutput.CollectionChanged += async (s, e) =>
            {
                if (!IsDialogOpen)
                    return;

                LiveOutputTextBlock.Blocks.Clear();
                Paragraph p = new();
                foreach (string line in ProcessOutput)
                {
                    if (line.Contains("  | "))
                        p.Inlines.Add(new Run() { Text = line.Replace(" | ", "").Trim() + "\x0a" });
                }
                LiveOutputTextBlock.Blocks.Add(p);
                await Task.Delay(100);
                LiveOutputScrollBar.ScrollToVerticalOffset(LiveOutputScrollBar.ScrollableHeight);
            };

            Status = OperationStatus.Pending;

            ActionButton.Click += ActionButtonClicked;
            OutputViewewBlock.Click += (s, e) =>
            {
                OpenLiveViewDialog();
            };
        }

        public async void OpenLiveViewDialog()
        {
            OutputDialog.XamlRoot = XamlRoot;
            LiveOutputTextBlock.Blocks.Clear();
            Paragraph p = new();
            p.LineHeight = 4.8;
            foreach (string line in ProcessOutput)
            {
                if (Status != OperationStatus.Failed)
                {
                    if (line.Contains("  | "))
                        p.Inlines.Add(new Run() { Text = line.Replace(" | ", "").Trim() + "\x0a" });
                }
                else
                {
                    p.Inlines.Add(new Run() { Text = line + "\x0a" });
                }
            }
            LiveOutputTextBlock.Blocks.Add(p);
            IsDialogOpen = true;

            if (await Tools.App.MainWindow.ShowDialogAsync(OutputDialog) == ContentDialogResult.Secondary)
            {
                LiveOutputScrollBar.ScrollToVerticalOffset(LiveOutputScrollBar.ScrollableHeight);
                Clipboard.WindowsClipboard.SetText(string.Join('\n', ProcessOutput.ToArray()));
            }
            IsDialogOpen = false;
        }

        public void ActionButtonClicked(object sender, RoutedEventArgs args)
        {
            if (Status == OperationStatus.Pending || Status == OperationStatus.Running)
            {
                CancelButtonClicked(Status);
            }
            else
                CloseButtonClicked(Status);
        }

        protected void RemoveFromQueue()
        {
            while(Tools.OperationQueue.IndexOf(this) != -1)
                Tools.OperationQueue.Remove(this);
        }
        protected void AddToQueue()
        {
            if (!Tools.OperationQueue.Contains(this))
                Tools.OperationQueue.Add(this);
        }

        public void CancelButtonClicked(OperationStatus OldStatus)
        {
            RemoveFromQueue();
            Status = OperationStatus.Cancelled;
            LineInfoText = Tools.Translate("Operation cancelled");

            if (this is PackageOperation)
                (this as PackageOperation).Package.Tag = PackageTag.Default;

            if (OldStatus == OperationStatus.Running)
            {
                Process.Kill();
                ProcessOutput.Add("Operation was cancelled by the user!");
            }
        }

        public void CloseButtonClicked(OperationStatus OldStatus)
        {
            _ = Close();
        }

        protected void AddToQueue_Priority()
        {
            Tools.OperationQueue.Insert(0, this);
        }

        protected virtual async Task WaitForAvailability()
        {
            AddToQueue();
            int currentIndex = -2;
            int oldIndex = -1;
            while (currentIndex != 0)
            {
                if (Status == OperationStatus.Cancelled)
                    return; // If the operation has been cancelled

                currentIndex = Tools.OperationQueue.IndexOf(this);
                if (currentIndex != oldIndex)
                {
                    LineInfoText = Tools.Translate("Operation on queue (position {0})...", currentIndex);
                    oldIndex = currentIndex;
                }
                await Task.Delay(100);
            }
        }
        protected async Task PreMainThread()
        {
            Status = OperationStatus.Pending;
            await WaitForAvailability();
            await MainThread();
        }
        protected async Task MainThread()
        {
            try
            {

                if (Status == OperationStatus.Cancelled)
                    return; // If the operation was cancelled, do nothing.

                Tools.TooltipStatus.OperationsInProgress = Tools.TooltipStatus.OperationsInProgress + 1;

                Status = OperationStatus.Running;
                LineInfoText = Tools.Translate("Launching subprocess...");
                ProcessStartInfo startInfo = new();
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
                startInfo.StandardInputEncoding = System.Text.Encoding.UTF8;
                startInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
                startInfo.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                Process = BuildProcessInstance(startInfo);

                foreach (string infoLine in GenerateProcessLogHeader())
                    ProcessOutput.Add(infoLine);

                ProcessOutput.Add("Process Executable     : " + Process.StartInfo.FileName);
                ProcessOutput.Add("Process Call Arguments : " + Process.StartInfo.Arguments);
                ProcessOutput.Add("Working Directory      : " + Process.StartInfo.WorkingDirectory);
                ProcessOutput.Add("Process Start Time     : " + DateTime.Now.ToString());

                Process.Start();

                string line;
                while ((line = await Process.StandardOutput.ReadLineAsync()) != null)
                {
                    if (line.Trim() != "")
                    {
                        if (line.Contains("For the question below") || line.Contains("Would remove:")) // Mitigate chocolatey timeouts
                            Process.StandardInput.WriteLine("");

                        LineInfoText = line.Trim();
                        if (line.Length > 5 || ProcessOutput.Count == 0)
                            ProcessOutput.Add("    | " + line);
                        else
                            ProcessOutput[^1] = "    | " + line;
                    }
                }

                foreach (string errorLine in (await Process.StandardError.ReadToEndAsync()).Split('\n'))
                    if (errorLine.Trim() != "")
                        ProcessOutput.Add("ERR | " + errorLine);

                await Process.WaitForExitAsync();

                ProcessOutput.Add("Process Exit Code      : " + Process.ExitCode.ToString());
                ProcessOutput.Add("Process End Time       : " + DateTime.Now.ToString());



                AfterFinshAction postAction = AfterFinshAction.ManualClose;

                OperationVeredict OperationVeredict = GetProcessVeredict(Process.ExitCode, ProcessOutput.ToArray());

                if (Status != OperationStatus.Cancelled)
                {
                    switch (OperationVeredict)
                    {
                        case OperationVeredict.Failed:
                            Status = OperationStatus.Failed;
                            RemoveFromQueue();
                            Tools.TooltipStatus.ErrorsOccurred = Tools.TooltipStatus.ErrorsOccurred + 1;
                            postAction = await HandleFailure();
                            Tools.TooltipStatus.ErrorsOccurred = Tools.TooltipStatus.ErrorsOccurred - 1;
                            break;

                        case OperationVeredict.Succeeded:
                            Status = OperationStatus.Succeeded;
                            postAction = await HandleSuccess();
                            RemoveFromQueue();
                            break;

                        case OperationVeredict.AutoRetry:
                            Status = OperationStatus.Pending;
                            postAction = AfterFinshAction.Retry;
                            break;
                    }
                }

                switch (postAction)
                {
                    case AfterFinshAction.TimeoutClose:
                        if (Tools.OperationQueue.Count == 0)
                            if (Tools.GetSettings("DoCacheAdminRightsForBatches"))
                            {
                                AppTools.Log("Erasing admin rights");
                                Process p = new();
                                p.StartInfo.FileName = CoreData.GSudoPath;
                                p.StartInfo.Arguments = "cache off";
                                p.Start();
                                p.WaitForExit();
                            }
                        await Task.Delay(5000);
                        if (!Tools.GetSettings("MaintainSuccessfulInstalls"))
                            _ = Close();
                        break;

                    case AfterFinshAction.ManualClose:
                        if (Tools.OperationQueue.Count == 0)
                            if (Tools.GetSettings("DoCacheAdminRightsForBatches"))
                            {
                                AppTools.Log("Erasing admin rights");
                                Process p = new();
                                p.StartInfo.FileName = CoreData.GSudoPath;
                                p.StartInfo.Arguments = "cache off";
                                p.Start();
                                p.WaitForExit();
                            }
                        break;

                    case AfterFinshAction.Retry:
                        Retry();
                        break;
                }

                ProcessOutput.Insert(0, "                           ");
                ProcessOutput.Insert(0, "▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄");
                ProcessOutput.Add("");
                ProcessOutput.Add("");
                ProcessOutput.Add("");

                var oldHistory = Tools.GetSettingsValue("OperationHistory").Split("\n");

                if(oldHistory.Length > 1000)
                {
                    oldHistory = oldHistory.Take(1000).ToArray();
                }

                var newHistory = new List<string>();
                newHistory.AddRange(ProcessOutput);
                newHistory.AddRange(oldHistory);

                Tools.SetSettingsValue("OperationHistory", String.Join('\n', newHistory).Replace(" | ", " ║ "));
            }
            catch (Exception e)
            {
                AppTools.Log("Operation failed: " + e.ToString());
                LineInfoText = Tools.Translate("An unexpected error occurred:") + " " + e.Message;
                RemoveFromQueue();
                try { Status = OperationStatus.Failed; } catch { }
            }
            Tools.TooltipStatus.OperationsInProgress = Tools.TooltipStatus.OperationsInProgress - 1;


        }
        protected async Task Close()
        {
            while (IsDialogOpen)
                await Task.Delay(1000);

            RemoveFromQueue();
            if (Tools.App.MainWindow.NavigationPage.OperationStackPanel.Children.Contains(this))
            {
                Tools.App.MainWindow.NavigationPage.OperationStackPanel.Children.Remove(this);
            }
        }

        protected abstract void Initialize();
        protected abstract Process BuildProcessInstance(ProcessStartInfo startInfo);
        protected abstract OperationVeredict GetProcessVeredict(int ReturnCode, string[] Output);
        protected abstract Task<AfterFinshAction> HandleFailure();
        protected abstract Task<AfterFinshAction> HandleSuccess();
        protected abstract string[] GenerateProcessLogHeader();

        protected void Retry()
        {
            AddToQueue_Priority();
            LineInfoText = Tools.Translate("Retrying, please wait...");
            ProcessOutput.Clear();
            Status = OperationStatus.Pending;
            _ = MainThread();
        }

        protected void MainProcedure()
        {
            Initialize();
            _ = PreMainThread();
        }
        private void ResizeEvent(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width < 500)
            {
                if (LayoutMode != WidgetLayout.Compact)
                    LayoutMode = WidgetLayout.Compact;
            }
            else
            {
                if (LayoutMode != WidgetLayout.Default)
                    LayoutMode = WidgetLayout.Default;
            }

        }
    }
}
