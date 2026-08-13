using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace AirMirror;

internal sealed class MainForm : Form
{
    private const string PreferredVideoRenderer = "d3d12";
    private const string VideoWindowTitle = "AirPlay Video Stream";
    private static readonly Color Page = Color.FromArgb(5, 7, 11);
    private static readonly Color Card = Color.FromArgb(15, 21, 31);
    private static readonly Color Input = Color.FromArgb(8, 12, 19);
    private static readonly Color Foreground = Color.FromArgb(241, 247, 255);
    private static readonly Color Muted = Color.FromArgb(148, 165, 184);
    private static readonly Color Cyan = Color.FromArgb(0, 229, 255);
    private static readonly Color Purple = Color.FromArgb(177, 64, 255);
    private static readonly Color Green = Color.FromArgb(0, 235, 166);
    private static readonly Color Red = Color.FromArgb(255, 83, 112);

    private readonly string? _engineDirectory;
    private readonly string _dataRoot;
    private readonly string _settingsPath;
    private readonly string _uxPlaySettingsRoot;
    private readonly string _receiverConfigPath;
    private readonly ReceiverSettings _settings;
    private readonly bool _startReceiverImmediately;

    private readonly TextBox _deviceNameInput = new();
    private readonly NeonButton _startButton = new();
    private readonly NeonButton _stopButton = new();
    private readonly Label _statusText = new();
    private readonly Panel _statusDot = new();

    private Process? _receiver;
    private CancellationTokenSource? _videoWindowMonitorCancellation;
    private CancellationTokenSource? _disconnectWindowCloseCancellation;
    private double _detectedVideoAspectRatio;
    private int _detectedVideoWidth;
    private int _detectedVideoHeight;
    private bool _stopping;

    public MainForm(bool startReceiverImmediately = false)
    {
        _startReceiverImmediately = startReceiverImmediately;
        _engineDirectory = FindEngineDirectory();
        // The application may be installed under Program Files, where a normal
        // user cannot write settings. Keep per-user runtime data outside the
        // installation directory so both the installed and portable builds
        // can save their configuration without elevation.
        _dataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AirMirror");
        _settingsPath = Path.Combine(_dataRoot, "airmirror.json");
        _uxPlaySettingsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "leapbtw",
            "uxplay-windows");
        _receiverConfigPath = Path.Combine(_uxPlaySettingsRoot, "arguments.txt");
        _settings = ReceiverSettings.Load(_settingsPath);

        BuildUserInterface();
        Icon = CreateWhiteTitleIcon();
        _deviceNameInput.Text = _settings.DeviceName;

        if (_engineDirectory is null)
        {
            SetState("缺少 AirPlay 接收内核", Red, false);
            _startButton.Enabled = false;
        }
        else
        {
            SetState("未启动", Muted, false);
        }
    }

    private void BuildUserInterface()
    {
        SuspendLayout();
        Text = "AirMirror";
        BackColor = Page;
        ForeColor = Foreground;
        Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        ClientSize = new Size(700, 420);
        MinimumSize = new Size(700, 420);
        MaximumSize = new Size(700, 420);
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;
        Shown += (_, _) =>
        {
            NativeWindow.ApplyBlackTitleBar(Handle);
            if (_startReceiverImmediately)
            {
                BeginInvoke((Action)(() => _ = StartReceiverAsync()));
            }
        };
        FormClosing += (_, _) => StopReceiverImmediately();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Page,
            Padding = new Padding(32, 30, 32, 24),
            ColumnCount = 1,
            RowCount = 2
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 286));

        root.Controls.Add(CreateHeader(), 0, 0);
        root.Controls.Add(CreateControlCard(), 0, 1);
        Controls.Add(root);
        ResumeLayout();
    }

    private Control CreateHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var statusPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(11, 8, 11, 8),
            BackColor = Card,
            Margin = new Padding(0, 10, 0, 0)
        };
        _statusDot.Size = new Size(8, 8);
        _statusDot.Margin = new Padding(0, 5, 7, 0);
        _statusDot.BackColor = Muted;
        _statusText.AutoSize = true;
        _statusText.ForeColor = Muted;
        _statusText.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        statusPanel.Controls.Add(_statusDot);
        statusPanel.Controls.Add(_statusText);

        header.Controls.Add(statusPanel, 0, 0);
        return header;
    }

    private Control CreateControlCard()
    {
        var card = new BorderPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Card,
            BorderColor = Color.FromArgb(50, Cyan),
            CornerRadius = 16,
            Padding = new Padding(22, 19, 22, 18)
        };

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Card,
            ColumnCount = 1,
            RowCount = 6
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        content.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "接收设备名",
            ForeColor = Foreground,
            Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        content.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "它会显示在 iPhone 的「屏幕镜像」列表中",
            ForeColor = Muted,
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);

        _deviceNameInput.Dock = DockStyle.Fill;
        _deviceNameInput.BackColor = Input;
        _deviceNameInput.ForeColor = Foreground;
        _deviceNameInput.BorderStyle = BorderStyle.FixedSingle;
        _deviceNameInput.Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold, GraphicsUnit.Point);
        _deviceNameInput.PlaceholderText = "例如：波西投屏";
        _deviceNameInput.Margin = new Padding(0, 1, 0, 0);
        _deviceNameInput.MaxLength = 32;
        content.Controls.Add(_deviceNameInput, 0, 2);

        var buttons = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Card,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

        ConfigureButton(_startButton, "开始接收 AirPlay", Cyan, Page);
        _startButton.Click += async (_, _) => await StartReceiverAsync();
        _startButton.Margin = new Padding(0, 0, 9, 0);

        ConfigureButton(_stopButton, "停止", Color.FromArgb(47, 28, 40), Color.FromArgb(255, 128, 155));
        _stopButton.Enabled = false;
        _stopButton.Click += async (_, _) => await StopReceiverAsync();
        _stopButton.Margin = new Padding(0);

        buttons.Controls.Add(_startButton, 0, 0);
        buttons.Controls.Add(_stopButton, 1, 0);
        content.Controls.Add(buttons, 0, 4);

        content.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "状态只代表接收器是否运行；连接由 iPhone 发起。",
            ForeColor = Color.FromArgb(104, 125, 149),
            Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 5);

        card.Controls.Add(content);
        return card;
    }

    private static void ConfigureButton(NeonButton button, string text, Color fill, Color foreground)
    {
        button.Dock = DockStyle.Fill;
        button.Text = text;
        button.FillColor = fill;
        button.HoverColor = ControlPaint.Light(fill, 0.1F);
        button.ForeColor = foreground;
        button.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
        button.Cursor = Cursors.Hand;
    }

    private Task StartReceiverAsync()
    {
        if (_engineDirectory is null || _receiver is { HasExited: false })
        {
            return Task.CompletedTask;
        }

        var deviceName = NormalizeDeviceName(_deviceNameInput.Text);
        if (string.IsNullOrEmpty(deviceName))
        {
            MessageBox.Show("设备名不能为空。", "AirMirror", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return Task.CompletedTask;
        }

        _deviceNameInput.Text = deviceName;
        _settings.DeviceName = deviceName;
        _settings.Save(_settingsPath);
        WriteReceiverConfiguration(deviceName);

        var exePath = Path.Combine(_engineDirectory, "uxplay-windows.exe");
        Interlocked.Exchange(ref _detectedVideoAspectRatio, 0D);
        Interlocked.Exchange(ref _detectedVideoWidth, 0);
        Interlocked.Exchange(ref _detectedVideoHeight, 0);

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = _engineDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Normal,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        // 读取客户端实际送来的尺寸；4K 请求是否接受由 iPhone 和网络决定。
        startInfo.Environment["GST_DEBUG"] = "GST_CAPS:6";
        startInfo.Environment["GST_DEBUG_NO_COLOR"] = "1";

        try
        {
            var receiver = Process.Start(startInfo);
            if (receiver is null)
            {
                throw new InvalidOperationException("接收内核没有启动。");
            }

            AttachReceiver(receiver);
            SetState("正在启动…", Purple, true);
            SetButtons(receiverRunning: true);

            if (!receiver.HasExited)
            {
                SetState($"已就绪：选择「{deviceName}」", Green, true);
            }
        }
        catch (Exception exception)
        {
            _receiver?.Dispose();
            _receiver = null;
            SetState("启动失败", Red, false);
            SetButtons(receiverRunning: false);
            MessageBox.Show(exception.Message, "AirMirror 无法启动", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        return Task.CompletedTask;
    }

    private async Task StopReceiverAsync()
    {
        StopVideoWindowMonitor();

        var receiver = _receiver;
        if (receiver is null)
        {
            return;
        }

        _stopping = true;
        SetState("正在停止…", Muted, true);
        SetButtons(receiverRunning: false);

        try
        {
            if (!receiver.HasExited)
            {
                receiver.Kill(entireProcessTree: true);
                await receiver.WaitForExitAsync();
            }
        }
        catch (InvalidOperationException)
        {
            // 已结束时无需处理。
        }
        finally
        {
            if (ReferenceEquals(_receiver, receiver))
            {
                receiver.Dispose();
                _receiver = null;
            }

            _stopping = false;
            SetState("未启动", Muted, false);
            SetButtons(receiverRunning: false);
        }
    }

    private void StopReceiverImmediately()
    {
        StopVideoWindowMonitor();
        _stopping = true;
        try
        {
            if (_receiver is { HasExited: false } receiver)
            {
                receiver.Kill(entireProcessTree: true);
                receiver.WaitForExit(1500);
            }
        }
        catch (InvalidOperationException)
        {
            // 进程已结束。
        }
        finally
        {
            _receiver?.Dispose();
            _receiver = null;
        }
    }

    private void AttachReceiver(Process receiver)
    {
        StopVideoWindowMonitor();
        _receiver = receiver;
        receiver.EnableRaisingEvents = true;
        receiver.Exited += ReceiverExited;
        receiver.OutputDataReceived += ReceiverLogReceived;
        receiver.ErrorDataReceived += ReceiverLogReceived;
        receiver.BeginOutputReadLine();
        receiver.BeginErrorReadLine();
        StartVideoWindowMonitor(receiver.Id);
    }

    private void ReceiverExited(object? sender, EventArgs eventArgs)
    {
        if (sender is not Process exitedProcess || IsDisposed || !IsHandleCreated)
        {
            return;
        }

        BeginInvoke((Action)(() => HandleReceiverExitAsync(exitedProcess)));
    }

    private async void HandleReceiverExitAsync(Process exitedProcess)
    {
        if (!ReferenceEquals(_receiver, exitedProcess))
        {
            return;
        }

        StopVideoWindowMonitor();
        exitedProcess.Dispose();
        _receiver = null;

        if (_stopping)
        {
            SetState("未启动", Muted, false);
            SetButtons(receiverRunning: false);
            return;
        }

        SetState("接收器正在重启…", Purple, true);
        SetButtons(receiverRunning: true);

        for (var attempt = 0; attempt < 12; attempt++)
        {
            await Task.Delay(500);
            var restartedReceiver = FindExistingEngineProcess();
            if (restartedReceiver is null)
            {
                continue;
            }

            AttachReceiver(restartedReceiver);
            SetState($"已就绪：选择「{_settings.DeviceName}」", Green, true);
            SetButtons(receiverRunning: true);
            return;
        }

        SetState("接收器已停止", Red, false);
        SetButtons(receiverRunning: false);
    }

    private void WriteReceiverConfiguration(string deviceName)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_receiverConfigPath)!);
        File.WriteAllText(
            _receiverConfigPath,
            $"-n {deviceName} -nh -h265 -s 3840x2160@60 -nofreeze -nc no -reset 2 -avdec -vsync no",
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        // uxplay-windows 读取这里的值，并据此选择视频输出器。
        using var settings = Registry.CurrentUser.CreateSubKey(
            @"Software\leapbtw\uxplay-windows");
        settings?.SetValue(
            "renderer_mode",
            PreferredVideoRenderer,
            RegistryValueKind.String);
    }

    private void StartVideoWindowMonitor(int processId)
    {
        var cancellation = new CancellationTokenSource();
        _videoWindowMonitorCancellation = cancellation;
        _ = KeepVideoWindowProportionalAsync(processId, cancellation.Token);
    }

    private void StopVideoWindowMonitor()
    {
        CancelScheduledVideoWindowClose();
        var cancellation = Interlocked.Exchange(ref _videoWindowMonitorCancellation, null);
        if (cancellation is null)
        {
            return;
        }

        cancellation.Cancel();
        cancellation.Dispose();
    }

    private void CancelScheduledVideoWindowClose()
    {
        var cancellation = Interlocked.Exchange(ref _disconnectWindowCloseCancellation, null);
        cancellation?.Cancel();
    }

    private void ScheduleVideoWindowCloseAfterDisconnect(int processId)
    {
        var currentCancellation = new CancellationTokenSource();
        var previousCancellation = Interlocked.Exchange(
            ref _disconnectWindowCloseCancellation,
            currentCancellation);
        previousCancellation?.Cancel();

        _ = CloseVideoWindowAfterDisconnectAsync(processId, currentCancellation);
    }

    private async Task CloseVideoWindowAfterDisconnectAsync(
        int processId,
        CancellationTokenSource cancellation)
    {
        try
        {
            // 先留给接收内核自行关窗；仍存在才用 Windows 消息兜底关闭。
            await Task.Delay(500, cancellation.Token);
            var videoWindow = NativeWindow.FindVisibleWindow(processId, VideoWindowTitle);
            if (videoWindow != IntPtr.Zero)
            {
                NativeWindow.RequestClose(videoWindow);
            }
        }
        catch (OperationCanceledException)
        {
            // 接收器重启或程序退出时无需再处理旧窗口。
        }
        finally
        {
            if (ReferenceEquals(
                    Interlocked.CompareExchange(ref _disconnectWindowCloseCancellation, null, cancellation),
                    cancellation))
            {
                cancellation.Dispose();
            }
        }
    }

    private async Task KeepVideoWindowProportionalAsync(int processId, CancellationToken cancellationToken)
    {
        const double aspectTolerance = 0.012;
        var nextConnectionCheckAt = DateTime.MinValue;
        var lastRemoteClientSeenAt = DateTime.MinValue;
        var visibleWindowHandle = IntPtr.Zero;
        var closeRequestedForCurrentWindow = false;
        double? videoAspectRatio = null;
        Size videoSize = Size.Empty;
        var lastStableSize = Size.Empty;
        var lastObservedSize = Size.Empty;
        var lastUserResizeAt = DateTime.MinValue;
        var ignoreResizeUntil = DateTime.MinValue;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var windowHandle = NativeWindow.FindVisibleWindow(processId, VideoWindowTitle);
                if (windowHandle != IntPtr.Zero &&
                    NativeWindow.TryGetClientSize(windowHandle, out var clientSize) &&
                    clientSize.Width >= 240 &&
                    clientSize.Height >= 240)
                {
                    if (windowHandle != visibleWindowHandle)
                    {
                        visibleWindowHandle = windowHandle;
                        lastRemoteClientSeenAt = DateTime.UtcNow;
                        closeRequestedForCurrentWindow = false;
                    }

                    if (!closeRequestedForCurrentWindow && DateTime.UtcNow >= nextConnectionCheckAt)
                    {
                        nextConnectionCheckAt = DateTime.UtcNow.AddMilliseconds(400);
                        var remoteClientConnected = NativeNetwork.HasRemoteTcpClient(processId);
                        if (remoteClientConnected == true)
                        {
                            lastRemoteClientSeenAt = DateTime.UtcNow;
                        }
                        else if (remoteClientConnected == false &&
                                 DateTime.UtcNow - lastRemoteClientSeenAt >= TimeSpan.FromSeconds(2))
                        {
                            NativeWindow.RequestClose(windowHandle);
                            closeRequestedForCurrentWindow = true;
                        }
                    }

                    if (!videoAspectRatio.HasValue)
                    {
                        var detectedVideoSize = GetDetectedVideoSize();
                        if (detectedVideoSize.IsEmpty)
                        {
                            await Task.Delay(90, cancellationToken);
                            continue;
                        }

                        videoSize = detectedVideoSize;
                        videoAspectRatio = (double)videoSize.Width / videoSize.Height;
                        _settings.VideoAspectRatio = videoAspectRatio.Value;
                        _settings.VideoWidth = videoSize.Width;
                        _settings.VideoHeight = videoSize.Height;
                        _settings.Save(_settingsPath);
                    }

                    if (lastObservedSize.IsEmpty)
                    {
                        // 初始窗口按真实像素大小显示：没有黑边，也不会用模糊放大伪造高清。
                        var correctedSize = FitVideoSizeToScreen(windowHandle, videoSize);

                        NativeWindow.ResizeClientArea(windowHandle, correctedSize);
                        lastStableSize = correctedSize;
                        lastObservedSize = correctedSize;
                        ignoreResizeUntil = DateTime.UtcNow.AddMilliseconds(500);
                    }
                    else if (clientSize != lastObservedSize)
                    {
                        if (DateTime.UtcNow >= ignoreResizeUntil)
                        {
                            lastUserResizeAt = DateTime.UtcNow;
                        }

                        lastObservedSize = clientSize;
                    }

                    if (lastUserResizeAt != DateTime.MinValue &&
                        DateTime.UtcNow - lastUserResizeAt >= TimeSpan.FromMilliseconds(180))
                    {
                        var currentAspectRatio = (double)clientSize.Width / clientSize.Height;
                        if (Math.Abs(currentAspectRatio - videoAspectRatio.Value) > aspectTolerance)
                        {
                            var widthChangedMore = Math.Abs(clientSize.Width - lastStableSize.Width) >=
                                                   Math.Abs(clientSize.Height - lastStableSize.Height);
                            var correctedSize = widthChangedMore
                                ? new Size(clientSize.Width, (int)Math.Round(clientSize.Width / videoAspectRatio.Value))
                                : new Size((int)Math.Round(clientSize.Height * videoAspectRatio.Value), clientSize.Height);

                            correctedSize = LimitToVideoPixels(correctedSize, videoSize);

                            if (NativeWindow.ResizeClientArea(windowHandle, correctedSize))
                            {
                                lastStableSize = correctedSize;
                                lastObservedSize = correctedSize;
                                ignoreResizeUntil = DateTime.UtcNow.AddMilliseconds(500);
                            }
                        }
                        else
                        {
                            lastStableSize = clientSize;
                        }

                        lastUserResizeAt = DateTime.MinValue;
                    }
                }
                else
                {
                    visibleWindowHandle = IntPtr.Zero;
                    lastRemoteClientSeenAt = DateTime.MinValue;
                    closeRequestedForCurrentWindow = false;
                }

                await Task.Delay(90, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 接收器停止时正常退出监控。
        }
        catch
        {
            // 不让窗口辅助功能影响投屏本身。
        }
    }

    private Size GetDetectedVideoSize()
    {
        var width = Volatile.Read(ref _detectedVideoWidth);
        var height = Volatile.Read(ref _detectedVideoHeight);
        return width >= 240 && height >= 240 ? new Size(width, height) : Size.Empty;
    }

    private void ReceiverLogReceived(object sender, DataReceivedEventArgs eventArgs)
    {
        if (sender is not Process receiver ||
            eventArgs.Data is not { } logLine ||
            receiver.Id != _receiver?.Id)
        {
            return;
        }

        if (IsMirrorStopSignal(logLine))
        {
            ScheduleVideoWindowCloseAfterDisconnect(receiver.Id);
        }

        if (Interlocked.CompareExchange(ref _detectedVideoAspectRatio, 0D, 0D) > 0D)
        {
            return;
        }

        if (!logLine.Contains("video/x-", StringComparison.OrdinalIgnoreCase) ||
            !logLine.Contains("width=(int)", StringComparison.OrdinalIgnoreCase) ||
            !logLine.Contains("height=(int)", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var match = Regex.Match(
            logLine,
            @"video/x-(?:h264|h265|raw).*?width=\(int\)(?<width>\d+).*?height=\(int\)(?<height>\d+)",
            RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return;
        }

        var width = int.Parse(match.Groups["width"].Value);
        var height = int.Parse(match.Groups["height"].Value);
        if (width >= 240 && height >= 240)
        {
            if (Interlocked.CompareExchange(ref _detectedVideoAspectRatio, (double)width / height, 0D) == 0D)
            {
                Interlocked.Exchange(ref _detectedVideoWidth, width);
                Interlocked.Exchange(ref _detectedVideoHeight, height);
            }
        }
    }

    private static bool IsMirrorStopSignal(string logLine)
    {
        return logLine.Contains("client HTTP request POST stop", StringComparison.OrdinalIgnoreCase) ||
               logLine.Contains("video_reset: type = RTP_Shutdown", StringComparison.OrdinalIgnoreCase) ||
               logLine.Contains("TEARDOWN request,", StringComparison.OrdinalIgnoreCase) ||
               logLine.Contains("onAirplayStopped()", StringComparison.OrdinalIgnoreCase);
    }

    private static Size FitVideoSizeToScreen(IntPtr windowHandle, Size videoSize)
    {
        var workingArea = Screen.FromHandle(windowHandle).WorkingArea;
        var maximumHeight = Math.Max(400, workingArea.Height - 120);
        var scale = Math.Min(1D, (double)maximumHeight / videoSize.Height);
        return new Size(
            Math.Max(240, (int)Math.Floor(videoSize.Width * scale)),
            Math.Max(240, (int)Math.Floor(videoSize.Height * scale)));
    }

    private static Size LimitToVideoPixels(Size requestedSize, Size videoSize)
    {
        if (requestedSize.Width <= videoSize.Width && requestedSize.Height <= videoSize.Height)
        {
            return requestedSize;
        }

        var scale = Math.Min((double)videoSize.Width / requestedSize.Width, (double)videoSize.Height / requestedSize.Height);
        return new Size(
            Math.Max(240, (int)Math.Floor(requestedSize.Width * scale)),
            Math.Max(240, (int)Math.Floor(requestedSize.Height * scale)));
    }

    private static string NormalizeDeviceName(string input)
    {
        var allowed = input
            .Where(character => char.IsLetterOrDigit(character) || character is '_' or '-')
            .Take(32);

        return new string(allowed.ToArray());
    }

    private static Icon CreateWhiteTitleIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        using var brush = new SolidBrush(Color.White);
        graphics.FillRectangle(brush, 5, 5, 9, 9);
        graphics.FillRectangle(brush, 18, 5, 9, 9);
        graphics.FillRectangle(brush, 5, 18, 9, 9);

        var iconHandle = bitmap.GetHicon();
        try
        {
            using var temporaryIcon = Icon.FromHandle(iconHandle);
            return (Icon)temporaryIcon.Clone();
        }
        finally
        {
            NativeWindow.DestroyIcon(iconHandle);
        }
    }

    private void SetState(string state, Color color, bool running)
    {
        _statusText.Text = state;
        _statusText.ForeColor = color;
        _statusDot.BackColor = color;
        _deviceNameInput.Enabled = !running;
    }

    private void SetButtons(bool receiverRunning)
    {
        _startButton.Enabled = !receiverRunning && _engineDirectory is not null;
        _stopButton.Enabled = receiverRunning;
    }

    private static string? FindEngineDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "engine", "uxplay-windows.exe");
            if (File.Exists(candidate))
            {
                return Path.GetDirectoryName(candidate);
            }

            directory = directory.Parent;
        }

        return null;
    }

    private Process? FindExistingEngineProcess()
    {
        if (_engineDirectory is null)
        {
            return null;
        }

        var expectedPath = Path.GetFullPath(Path.Combine(_engineDirectory, "uxplay-windows.exe"));
        foreach (var candidate in Process.GetProcessesByName("uxplay-windows"))
        {
            try
            {
                if (candidate.HasExited)
                {
                    candidate.Dispose();
                    continue;
                }

                var actualPath = candidate.MainModule?.FileName;
                if (actualPath is not null && string.Equals(
                        Path.GetFullPath(actualPath),
                        expectedPath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }
            catch (InvalidOperationException)
            {
                // 进程刚结束时跳过。
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // 无法读取其他进程模块时跳过。
            }

            candidate.Dispose();
        }

        return null;
    }
}

internal sealed class BorderPanel : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = Color.Transparent;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius { get; set; } = 12;

    public BorderPanel()
    {
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        var bounds = ClientRectangle;
        bounds.Width--;
        bounds.Height--;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedPath(bounds, CornerRadius);
        using var pen = new Pen(BorderColor, 1F);
        eventArgs.Graphics.DrawPath(pen, path);
    }

    private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class NeonButton : Button
{
    private bool _hovered;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FillColor { get; set; } = Color.Black;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverColor { get; set; } = Color.Black;

    public NeonButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        TabStop = true;
    }

    protected override void OnMouseEnter(EventArgs eventArgs)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(eventArgs);
    }

    protected override void OnMouseLeave(EventArgs eventArgs)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(eventArgs);
    }

    protected override void OnEnabledChanged(EventArgs eventArgs)
    {
        Invalidate();
        base.OnEnabledChanged(eventArgs);
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = ClientRectangle;
        bounds.Width--;
        bounds.Height--;
        using var path = RoundedPath(bounds, 10);
        var color = Enabled ? (_hovered ? HoverColor : FillColor) : Color.FromArgb(33, 42, 55);
        using var brush = new SolidBrush(color);
        eventArgs.Graphics.FillPath(brush, path);

        var foreground = Enabled ? ForeColor : Color.FromArgb(100, 116, 139);
        TextRenderer.DrawText(
            eventArgs.Graphics,
            Text,
            Font,
            bounds,
            foreground,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }

    private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
