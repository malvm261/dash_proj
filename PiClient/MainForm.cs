using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace PiClient
{
    // ════════════════════════════════════════════════════════════════
    //  Главная форма клиента.
    //
    //  Клиент рассылает команду "старт" на список узлов-серверов,
    //  затем периодически (с заданным интервалом) опрашивает каждый
    //  узел через HTTP GET /api/status, обновляет таблицу результатов
    //  и считает усреднённое по всем узлам значение π.
    // ════════════════════════════════════════════════════════════════
    public partial class MainForm : Form
    {
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

        // Сервер отдаёт JSON в PascalCase — указываем то же на клиенте на всякий случай
        private static readonly JsonSerializerOptions JsonOpts =
            new() { PropertyNameCaseInsensitive = true };

        private CancellationTokenSource? _cts;

        // Для каждого адреса узла храним две строки таблицы — отдельно для каждой формулы
        private readonly Dictionary<string, (DataGridViewRow Leibniz, DataGridViewRow Nilakantha)> _rows = new();

        public MainForm()
        {
            InitializeComponent();
            lblCores.Text = $"Ядер CPU: {Environment.ProcessorCount}";
        }

        // ───────────────────────── Кнопка "Старт" ─────────────────────────

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            var addresses = ParseAddresses();
            if (addresses.Count == 0)
            {
                MessageBox.Show("Укажите хотя бы один адрес сервера (например, http://192.168.1.10:5050).",
                    "Не указаны адреса", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            long totalIterations = (long)numIterations.Value;
            int intervalSec = (int)numInterval.Value;

            BuildResultRows(addresses);
            progressOverall.Value = 0;
            lblAveragePi.Text = "Среднее значение π:  —";

            _cts = new CancellationTokenSource();
            SetRunningState(isRunning: true);
            SetStatus("Отправка команды на узлы...", Color.DarkOrange);

            // Рассылаем каждому узлу команду начать расчёт
            foreach (var address in addresses)
            {
                try
                {
                    var response = await _http.PostAsJsonAsync(
                        $"{address}/api/start", new StartRequest(totalIterations), _cts.Token);
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось запустить расчёт на узле:\n{address}\n\n{ex.Message}",
                        "Ошибка подключения", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            SetStatus("Выполняется расчёт...", Color.SeaGreen);

            try
            {
                await PollLoop(addresses, intervalSec, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // расчёт остановлен пользователем — это штатная ситуация
            }

            SetStatus("Остановлено", Color.Gray);
            SetRunningState(isRunning: false);
        }

        // ───────────────────────── Кнопка "Стоп" ─────────────────────────

        private async void BtnStop_Click(object sender, EventArgs e)
        {
            SetStatus("Остановка узлов...", Color.Gray);
            _cts?.Cancel();

            // Рассылаем команду остановки всем узлам, которые участвовали в расчёте
            foreach (var address in _rows.Keys)
            {
                try { await _http.PostAsync($"{address}/api/stop", content: null); }
                catch { /* узел недоступен — пропускаем, это не критично */ }
            }
        }

        // ───────────────────────── Цикл периодического опроса ─────────────────────────

        private async Task PollLoop(List<string> addresses, int intervalSeconds, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                bool everyNodeFinished = true;

                foreach (var address in addresses)
                {
                    NodeStatus? status;
                    try
                    {
                        status = await _http.GetFromJsonAsync<NodeStatus>($"{address}/api/status", JsonOpts, ct);
                    }
                    catch
                    {
                        // узел недоступен — показываем это в таблице и продолжаем опрос остальных
                        ShowNodeUnavailable(address);
                        everyNodeFinished = false;
                        continue;
                    }

                    if (status is null) continue;

                    UpdateResultRow(address, isLeibniz: true, status.Leibniz);
                    UpdateResultRow(address, isLeibniz: false, status.Nilakantha);

                    if (status.IsRunning) everyNodeFinished = false;
                }

                RecalculateAverages();

                if (everyNodeFinished)
                {
                    SetStatus("Расчёт завершён на всех узлах", Color.SeaGreen);
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), ct);
            }
        }

        // ───────────────────────── Работа с таблицей результатов ─────────────────────────

        private List<string> ParseAddresses() =>
            txtAddresses.Text
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(a => a.TrimEnd('/'))
                .Where(a => a.Length > 0)
                .Distinct()
                .ToList();

        private void BuildResultRows(List<string> addresses)
        {
            dgvResults.Rows.Clear();
            _rows.Clear();

            foreach (var address in addresses)
            {
                int leibnizIndex    = dgvResults.Rows.Add(address, "Лейбниц",            "—", "0,0");
                int nilakanthaIndex = dgvResults.Rows.Add(address, "Нилаканта–Мадхава",  "—", "0,0");
                _rows[address] = (dgvResults.Rows[leibnizIndex], dgvResults.Rows[nilakanthaIndex]);
            }
        }

        private void UpdateResultRow(string address, bool isLeibniz, FormulaStatus formula)
        {
            if (!_rows.TryGetValue(address, out var pair)) return;
            var row = isLeibniz ? pair.Leibniz : pair.Nilakantha;

            row.Cells[2].Value = formula.Pi.ToString("F12");
            row.Cells[3].Value = formula.Progress.ToString("F1");
            row.DefaultCellStyle.ForeColor = dgvResults.ForeColor;
        }

        private void ShowNodeUnavailable(string address)
        {
            if (!_rows.TryGetValue(address, out var pair)) return;
            foreach (var row in new[] { pair.Leibniz, pair.Nilakantha })
            {
                row.Cells[2].Value = "узел недоступен";
                row.Cells[3].Value = "—";
                row.DefaultCellStyle.ForeColor = Color.Firebrick;
            }
        }

        // Пересчитывает усреднённое по всем узлам/формулам значение π и общий прогресс
        private void RecalculateAverages()
        {
            var piValues = new List<double>();
            double progressSum = 0;
            int progressCount = 0;

            foreach (var (leibniz, nilakantha) in _rows.Values)
            {
                foreach (var row in new[] { leibniz, nilakantha })
                {
                    if (row.Cells[2].Value is string piText && double.TryParse(piText, out double pi))
                        piValues.Add(pi);

                    if (row.Cells[3].Value is string progressText && double.TryParse(progressText, out double progress))
                    {
                        progressSum += progress;
                        progressCount++;
                    }
                }
            }

            if (piValues.Count > 0)
                lblAveragePi.Text = $"Среднее значение π:  {piValues.Average():F12}   (по {piValues.Count} результатам)";

            if (progressCount > 0)
                progressOverall.Value = (int)Math.Clamp(progressSum / progressCount, 0, 100);
        }

        // ───────────────────────── Вспомогательные методы интерфейса ─────────────────────────

        private void SetRunningState(bool isRunning)
        {
            btnStart.Enabled       = !isRunning;
            btnStop.Enabled        = isRunning;
            txtAddresses.Enabled   = !isRunning;
            numIterations.Enabled  = !isRunning;
            numInterval.Enabled    = !isRunning;
        }

        private void SetStatus(string text, Color color)
        {
            lblStatus.Text = text;
            lblStatus.ForeColor = color;
        }
    }

    // ════════════════════ Модели обмена данными (должны совпадать с сервером) ════════════════════

    public record StartRequest(long TotalIterations);
    public record FormulaStatus(double Pi, double Progress, long IterationsDone);
    public record NodeStatus(bool IsRunning, long TotalIterations, FormulaStatus Leibniz, FormulaStatus Nilakantha);
}
