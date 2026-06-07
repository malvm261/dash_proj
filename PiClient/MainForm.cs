using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace PiClient
{
    // ════════════════════════════════════════════════════════════════
    //  Главная форма клиента — он же координатор распределённого расчёта.
    //
    //  Общий объём вычислений (число членов ряда) делится на непересекающиеся
    //  диапазоны — по одному на каждый узел из списка адресов. Каждый узел
    //  получает СВОЙ диапазон через POST /api/start и считает только его,
    //  используя все свои ядра. Клиент периодически опрашивает узлы через
    //  GET /api/status и СКЛАДЫВАЕТ их частичные суммы в одно общее значение π.
    //
    //  Поэтому чем больше узлов участвует в расчёте — тем меньше работы
    //  достаётся каждому, и тем быстрее считается общий итоговый результат.
    // ════════════════════════════════════════════════════════════════
    public partial class MainForm : Form
    {
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

        // Сервер отдаёт JSON в PascalCase — указываем то же на клиенте на всякий случай
        private static readonly JsonSerializerOptions JsonOpts =
            new() { PropertyNameCaseInsensitive = true };

        private CancellationTokenSource? _cts;

        // Строка таблицы для каждого узла (по адресу)
        private readonly Dictionary<string, DataGridViewRow> _rows = new();

        // Последний полученный статус каждого узла — нужен для пересчёта общего π
        private readonly Dictionary<string, NodeStatus> _latestStatus = new();

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

            // Делим общий диапазон [0 .. totalIterations) поровну между узлами —
            // каждому достаётся свой непересекающийся кусок ряда
            var ranges = SplitRange(totalIterations, addresses.Count);
            var assignments = addresses
                .Zip(ranges, (address, range) => (Address: address, Start: range.Start, Count: range.Count))
                .ToList();

            BuildResultRows(assignments);
            _latestStatus.Clear();
            progressOverall.Value = 0;
            lblPi1.Text = "π (Лейбниц, общий результат):    —";
            lblPi2.Text = "π (Нилаканта, общий результат):  —";

            _cts = new CancellationTokenSource();
            SetRunningState(isRunning: true);
            SetStatus("Раздача диапазонов узлам...", Color.DarkOrange);

            // Каждому узлу отправляем ЕГО СОБСТВЕННЫЙ диапазон — это и есть декомпозиция задачи
            foreach (var a in assignments)
            {
                try
                {
                    var response = await _http.PostAsJsonAsync(
                        $"{a.Address}/api/start", new StartRequest(a.Start, a.Count), _cts.Token);
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось отправить задание на узел:\n{a.Address}\n\n{ex.Message}",
                        "Ошибка подключения", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            SetStatus("Выполняется распределённый расчёт...", Color.SeaGreen);

            try
            {
                await PollLoop(assignments.Select(a => a.Address).ToList(), totalIterations, intervalSec, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // расчёт остановлен пользователем — штатная ситуация
            }

            SetStatus("Остановлено", Color.Gray);
            SetRunningState(isRunning: false);
        }

        // ───────────────────────── Кнопка "Стоп" ─────────────────────────

        private async void BtnStop_Click(object sender, EventArgs e)
        {
            SetStatus("Остановка узлов...", Color.Gray);
            _cts?.Cancel();

            foreach (var address in _rows.Keys)
            {
                try { await _http.PostAsync($"{address}/api/stop", content: null); }
                catch { /* узел недоступен — пропускаем, это не критично */ }
            }
        }

        // ───────────────────────── Деление диапазона на части ─────────────────────────

        // Делит [0 .. total) на `parts` непересекающихся последовательных кусков максимально
        // равного размера. Остаток от деления распределяется по одному члену на первые куски,
        // чтобы суммарно куски точно покрывали весь диапазон без пропусков и наложений.
        private static List<(long Start, long Count)> SplitRange(long total, int parts)
        {
            var result = new List<(long, long)>(parts);
            long baseSize = total / parts;
            long remainder = total % parts;
            long cursor = 0;

            for (int i = 0; i < parts; i++)
            {
                long size = baseSize + (i < remainder ? 1 : 0);
                result.Add((cursor, size));
                cursor += size;
            }

            return result;
        }

        // ───────────────────────── Цикл периодического опроса ─────────────────────────

        private async Task PollLoop(List<string> addresses, long totalIterations, int intervalSeconds, CancellationToken ct)
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
                        ShowNodeUnavailable(address);
                        everyNodeFinished = false;
                        continue;
                    }

                    if (status is null) continue;

                    _latestStatus[address] = status;
                    UpdateResultRow(address, status);

                    if (status.IsRunning) everyNodeFinished = false;
                }

                // Складываем частичные суммы со всех узлов в одно общее значение π
                RecalculateCombinedResult(totalIterations);

                if (everyNodeFinished)
                {
                    SetStatus("Распределённый расчёт завершён на всех узлах", Color.SeaGreen);
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

        private void BuildResultRows(List<(string Address, long Start, long Count)> assignments)
        {
            dgvResults.Rows.Clear();
            _rows.Clear();

            foreach (var a in assignments)
            {
                string range = $"{a.Start:N0} — {a.Start + a.Count - 1:N0}  ({a.Count:N0} чл.)";
                int index = dgvResults.Rows.Add(a.Address, range, "0", "0,0");
                _rows[a.Address] = dgvResults.Rows[index];
            }
        }

        private void UpdateResultRow(string address, NodeStatus status)
        {
            if (!_rows.TryGetValue(address, out var row)) return;

            row.Cells[2].Value = status.IterationsDone.ToString("N0");
            row.Cells[3].Value = status.Progress.ToString("F1");
            row.DefaultCellStyle.ForeColor = dgvResults.ForeColor;
        }

        private void ShowNodeUnavailable(string address)
        {
            if (!_rows.TryGetValue(address, out var row)) return;

            row.Cells[2].Value = "—";
            row.Cells[3].Value = "узел недоступен";
            row.DefaultCellStyle.ForeColor = Color.Firebrick;
        }

        // Складывает частичные суммы рядов со всех узлов в одно общее значение π
        // и обновляет общий прогресс по сумме реально посчитанных членов
        private void RecalculateCombinedResult(long totalIterations)
        {
            double leibnizSum = 0;
            double nilakanthaSum = 0;
            long doneTotal = 0;

            foreach (var status in _latestStatus.Values)
            {
                leibnizSum    += status.LeibnizPartialSum;
                nilakanthaSum += status.NilakanthaPartialSum;
                doneTotal     += status.IterationsDone;
            }

            lblPi1.Text = $"π (Лейбниц, общий результат):    {leibnizSum:F12}";
            lblPi2.Text = $"π (Нилаканта, общий результат):  {3.0 + nilakanthaSum:F12}";

            if (totalIterations > 0)
                progressOverall.Value = (int)Math.Clamp((double)doneTotal / totalIterations * 100, 0, 100);
        }

        // ───────────────────────── Вспомогательные методы интерфейса ─────────────────────────

        private void SetRunningState(bool isRunning)
        {
            btnStart.Enabled      = !isRunning;
            btnStop.Enabled       = isRunning;
            txtAddresses.Enabled  = !isRunning;
            numIterations.Enabled = !isRunning;
            numInterval.Enabled   = !isRunning;
        }

        private void SetStatus(string text, Color color)
        {
            lblStatus.Text = text;
            lblStatus.ForeColor = color;
        }
    }

    // ════════════════════ Модели обмена данными (должны совпадать с сервером) ════════════════════

    // Задание узлу: посчитать члены ряда с индексами [RangeStart .. RangeStart + RangeCount)
    public record StartRequest(long RangeStart, long RangeCount);

    // Статус узла: его диапазон, сколько уже посчитано, и ЧАСТИЧНЫЕ суммы по обеим формулам
    // (это не значения π, а вклад только этого узла — итоговое π координатор получает,
    // складывая частичные суммы со всех узлов)
    public record NodeStatus(
        bool IsRunning,
        long RangeStart,
        long RangeCount,
        long IterationsDone,
        double Progress,
        double LeibnizPartialSum,
        double NilakanthaPartialSum);
}
