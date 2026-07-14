using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NetworkTest;

public partial class MainWindow : Window
{
    private readonly CancellationTokenSource _shutdown = new();
    private CancellationTokenSource? _testCancellation;
    public ObservableCollection<TestResult> Results { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        LogPathText.Text = $"저장 위치: {System.IO.Path.Combine(AppContext.BaseDirectory, "Logs")}";
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        var host = HostTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(host) || !double.TryParse(IntervalTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var seconds) || seconds < .2)
        { MessageBox.Show("대상 주소와 0.2초 이상의 올바른 측정 간격을 입력하세요.", "입력 확인"); return; }
        int? port = null;
        if (!string.IsNullOrWhiteSpace(PortTextBox.Text) && (!int.TryParse(PortTextBox.Text, out var parsedPort) || parsedPort is < 1 or > 65535))
        { MessageBox.Show("포트는 1~65535 범위로 입력하세요.", "입력 확인"); return; }
        else if (int.TryParse(PortTextBox.Text, out var validPort)) port = validPort;
        Results.Clear();
        ResetDashboard();
        var session = await CreateLogSessionAsync(host, port, TimeSpan.FromSeconds(seconds));
        LogPathText.Text = $"저장 위치: {session.FilePath}";
        _testCancellation = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
        StartButton.IsEnabled = false; StopButton.IsEnabled = true;
        await RunTestsAsync(host, port, TimeSpan.FromSeconds(seconds), session, _testCancellation.Token);
    }

    private void StopButton_Click(object sender, RoutedEventArgs e) => _testCancellation?.Cancel();

    private async Task RunTestsAsync(string host, int? port, TimeSpan interval, LogSession session, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var result = await TestAsync(host, port, token);
                Results.Insert(0, result);
                while (Results.Count > 1000) Results.RemoveAt(Results.Count - 1);
                await WriteLogAsync(session, result, token);
                UpdateDashboard();
                await Task.Delay(interval, token);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            await CompleteLogAsync(session);
            StartButton.IsEnabled = true; StopButton.IsEnabled = false; _testCancellation?.Dispose(); _testCancellation = null;
        }
    }

    private static async Task<TestResult> TestAsync(string host, int? port, CancellationToken token)
    {
        var timestamp = DateTime.Now;
        try
        {
            long ms;
            string method;
            if (port is null)
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, 3000);
                ms = reply.RoundtripTime; method = "ICMP Ping";
                return new(timestamp, method, reply.Status == IPStatus.Success, ms, reply.Status.ToString());
            }
            using var client = new TcpClient();
            var watch = Stopwatch.StartNew();
            await client.ConnectAsync(host, port.Value, token);
            watch.Stop(); ms = watch.ElapsedMilliseconds; method = $"TCP {port}";
            return new(timestamp, method, true, ms, "연결됨");
        }
        catch (Exception ex) when (ex is SocketException or PingException or TimeoutException or OperationCanceledException)
        { return new(timestamp, port is null ? "ICMP Ping" : $"TCP {port}", false, null, ex is OperationCanceledException ? "중지됨" : ex.Message); }
    }

    private static async Task<LogSession> CreateLogSessionAsync(string host, int? port, TimeSpan interval)
    {
        var startedAt = DateTime.Now;
        var folder = System.IO.Path.Combine(AppContext.BaseDirectory, "Logs"); Directory.CreateDirectory(folder);
        var path = System.IO.Path.Combine(folder, $"network-test-{startedAt:yyyyMMdd-HHmmss-fff}.csv");
        var header = $"# SessionStart,{startedAt:O}\r\n# TargetHost,{Csv(host)}\r\n# Port,{port?.ToString() ?? "ICMP Ping"}\r\n# IntervalSeconds,{interval.TotalSeconds.ToString(CultureInfo.InvariantCulture)}\r\nTimestamp,Method,Status,LatencyMs,Detail\r\n";
        await File.WriteAllTextAsync(path, header, new UTF8Encoding(true));
        return new LogSession(path, startedAt);
    }

    private static async Task CompleteLogAsync(LogSession session)
        => await File.AppendAllTextAsync(session.FilePath, $"# SessionEnd,{DateTime.Now:O}{Environment.NewLine}", new UTF8Encoding(true));

    private static async Task WriteLogAsync(LogSession session, TestResult result, CancellationToken token)
    {
        var row = $"{result.Timestamp:O},{result.Method},{result.Status},{result.LatencyMs?.ToString() ?? ""},\"{result.Detail.Replace("\"", "\"\"")}\"{Environment.NewLine}";
        await File.AppendAllTextAsync(session.FilePath, row, new UTF8Encoding(true), token);
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private void UpdateDashboard()
    {
        var ordered = Results.OrderBy(x => x.Timestamp).ToList();
        var total = ordered.Count; var success = ordered.Count(x => x.Success); var failure = total - success;
        var average = ordered.Where(x => x.Success && x.LatencyMs.HasValue).Select(x => x.LatencyMs!.Value).DefaultIfEmpty().Average();
        SummaryText.Text = $"총 {total:N0}회  |  정상 {success:N0}회  |  실패 {failure:N0}회  |  성공률 {(total == 0 ? 0 : success * 100.0 / total):F1}%  |  평균 {average:F1} ms";
        DrawCountChart(total, success, failure); DrawLatencyChart(ordered);
    }

    private void ResetDashboard()
    {
        SummaryText.Text = "대기 중 · 테스트를 시작하세요.";
        Clear(CountChart);
        Clear(LatencyChart);
    }

    private void Chart_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateDashboard();
    private static void Clear(Canvas canvas) { canvas.Children.Clear(); }
    private static void AddText(Canvas c, string text, double x, double y, Brush brush) { var t = new System.Windows.Controls.TextBlock { Text = text, FontSize = 11, Foreground = brush }; Canvas.SetLeft(t, x); Canvas.SetTop(t, y); c.Children.Add(t); }
    private static void DrawLine(Canvas c, Point[] points, Brush brush)
    { if (points.Length < 1) return; var line = new Polyline { Stroke = brush, StrokeThickness = 2, Points = new PointCollection(points) }; c.Children.Add(line); }
    private void DrawCountChart(int total, int success, int failure)
    {
        Clear(CountChart);
        DrawCountBarChart(total, success, failure);
    }
    private void DrawCountBarChart(int total, int success, int failure)
    {
        var c = CountChart; var w = c.ActualWidth; var h = c.ActualHeight;
        if (w < 80 || h < 50 || total == 0) { AddText(c, "측정 데이터가 쌓이면 통계가 표시됩니다.", 10, 10, Brushes.Gray); return; }
        const double left = 78, top = 16, bottom = 18, right = 35;
        var max = Math.Max(1, total); var pw = w - left - right; var ph = h - top - bottom;
        var labels = new[] { "총 횟수", "성공 횟수", "실패 횟수" };
        var values = new[] { total, success, failure };
        var colors = new[] { Brushes.SlateGray, Brushes.MediumSeaGreen, Brushes.IndianRed };
        var rowHeight = ph / labels.Length;
        for (var i = 0; i < labels.Length; i++)
        {
            var y = top + rowHeight * i + rowHeight * .2;
            var barWidth = values[i] / (double)max * pw;
            var rectangle = new Rectangle { Width = barWidth, Height = Math.Max(8, rowHeight * .55), Fill = colors[i] };
            Canvas.SetLeft(rectangle, left); Canvas.SetTop(rectangle, y); c.Children.Add(rectangle);
            AddText(c, labels[i], 4, y + 2, Brushes.DimGray);
            AddText(c, values[i].ToString("N0"), left + barWidth + 6, y + 2, Brushes.DimGray);
        }
    }
    private void DrawLatencyChart(List<TestResult> data)
    {
        Clear(LatencyChart);
        var c = LatencyChart; var w = c.ActualWidth; var h = c.ActualHeight;
        if (w < 80 || h < 50 || data.Count == 0) { AddText(c, "측정 데이터가 쌓이면 응답시간이 표시됩니다.", 10, 10, Brushes.Gray); return; }

        // Task Manager-style graph: recent individual samples, a subtle grid, filled area, and a live line.
        const double left = 36, top = 12, bottom = 25, right = 12;
        var pw = w - left - right; var ph = h - top - bottom;
        // Keep one sample per twenty-four pixels. This makes the visible time window grow/shrink with the panel,
        // and new samples continuously push the oldest ones off the left edge.
        var visibleSampleCount = Math.Clamp((int)(pw / 24), 10, 30);
        var samples = data.TakeLast(visibleSampleCount).ToList();
        var successful = samples.Where(x => x.Success && x.LatencyMs.HasValue).Select(x => (double)x.LatencyMs!.Value).ToList();
        var maximum = Math.Max(10, successful.DefaultIfEmpty(0).Max() * 1.15);
        for (var grid = 0; grid <= 4; grid++)
        {
            var y = top + ph * grid / 4;
            c.Children.Add(new Line { X1 = left, Y1 = y, X2 = left + pw, Y2 = y, Stroke = new SolidColorBrush(Color.FromRgb(222, 231, 226)), StrokeThickness = 1 });
        }
        for (var grid = 0; grid <= 6; grid++)
        {
            var x = left + pw * grid / 6;
            c.Children.Add(new Line { X1 = x, Y1 = top, X2 = x, Y2 = top + ph, Stroke = new SolidColorBrush(Color.FromRgb(235, 240, 237)), StrokeThickness = 1 });
        }
        AddText(c, $"{maximum:F0} ms", 1, top - 5, Brushes.Gray);
        AddText(c, "0", 18, top + ph - 8, Brushes.Gray);
        AddText(c, samples.First().Timestamp.ToString("HH:mm:ss"), left, top + ph + 5, Brushes.Gray);
        AddText(c, samples.Last().Timestamp.ToString("HH:mm:ss"), Math.Max(left, left + pw - 42), top + ph + 5, Brushes.Gray);

        var segment = new List<Point>();
        void DrawSegment()
        {
            if (segment.Count == 0) return;
            var area = new Polygon { Fill = new SolidColorBrush(Color.FromArgb(55, 0, 130, 90)), Points = new PointCollection(segment) };
            area.Points.Add(new Point(segment[^1].X, top + ph));
            area.Points.Add(new Point(segment[0].X, top + ph));
            c.Children.Add(area);
            DrawLine(c, segment.ToArray(), new SolidColorBrush(Color.FromRgb(0, 130, 90)));
            segment.Clear();
        }
        for (var i = 0; i < samples.Count; i++)
        {
            if (!samples[i].Success || !samples[i].LatencyMs.HasValue) { DrawSegment(); continue; }
            var x = left + (samples.Count == 1 ? pw : pw * i / (samples.Count - 1));
            var y = top + ph - Math.Min(samples[i].LatencyMs!.Value / maximum, 1) * ph;
            segment.Add(new Point(x, y));
        }
        DrawSegment();
        AddText(c, $"현재 {successful.LastOrDefault():F0} ms", left + 4, top + 3, new SolidColorBrush(Color.FromRgb(0, 130, 90)));
    }
    private void DrawChart(Canvas c, int count, double max, Func<int,double[]> values, Brush[] colors, string[] legends)
    {
        var w=c.ActualWidth; var h=c.ActualHeight; if(w<80||h<50||count==0) { AddText(c,"측정 데이터가 쌓이면 그래프가 표시됩니다.",10,10,Brushes.Gray); return; }
        const double left=38, top=8, bottom=24, right=8; max=Math.Max(max,1); var pw=w-left-right; var ph=h-top-bottom;
        c.Children.Add(new Line { X1=left,Y1=top,X2=left,Y2=top+ph,Stroke=Brushes.LightGray }); c.Children.Add(new Line { X1=left,Y1=top+ph,X2=left+pw,Y2=top+ph,Stroke=Brushes.LightGray });
        AddText(c, max.ToString("F0"), 2, top-4, Brushes.Gray); AddText(c,"0",18,top+ph-8,Brushes.Gray);
        for(int s=0;s<colors.Length;s++) { var points=new Point[count]; for(int i=0;i<count;i++) { var v=values(i)[s]; points[i]=new Point(left+(count==1?pw/2:pw*i/(count-1)),top+ph-(v/max*ph)); } DrawLine(c,points,colors[s]); AddText(c,legends[s],left+s*55,top+2,colors[s]); }
    }
    protected override void OnClosed(EventArgs e) { _shutdown.Cancel(); _shutdown.Dispose(); base.OnClosed(e); }
}

public sealed record TestResult(DateTime Timestamp, string Method, bool Success, long? LatencyMs, string Detail)
{ public string Status => Success ? "정상" : "실패"; }

public sealed record LogSession(string FilePath, DateTime StartedAt);
