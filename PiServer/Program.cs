// ════════════════════════════════════════════════════════════════════
//  Узел расчёта числа π (worker-сервер)
//
//  Принимает от клиента команду "начать расчёт N членов ряда",
//  считает π параллельно сразу по двум формулам, используя все ядра
//  процессора (Parallel.For из Task Parallel Library), и отдаёт
//  текущий прогресс и приближённое значение π по запросу клиента.
// ════════════════════════════════════════════════════════════════════

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5050");

builder.Services.AddSingleton<PiCalculator>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Отдаём JSON в PascalCase — имена свойств совпадут с моделями на клиенте
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.PropertyNamingPolicy = null);

var app = builder.Build();
app.UseCors();

var calculator = app.Services.GetRequiredService<PiCalculator>();

// Запустить расчёт на этом узле: клиент присылает количество членов ряда
app.MapPost("/api/start", (StartRequest req, PiCalculator calc) =>
{
    calc.Start(req.TotalIterations);
    return Results.Ok();
});

// Текущий статус узла: прогресс и приближённое значение π по каждой формуле
app.MapGet("/api/status", (PiCalculator calc) => Results.Ok(calc.GetStatus()));

// Остановить расчёт (например, по нажатию "Стоп" на клиенте)
app.MapPost("/api/stop", (PiCalculator calc) =>
{
    calc.Stop();
    return Results.Ok();
});

Console.WriteLine("════════════════════════════════════════════");
Console.WriteLine("   Узел расчёта числа π (worker-сервер)");
Console.WriteLine("════════════════════════════════════════════");
Console.WriteLine($"Адрес:    http://0.0.0.0:5050");
Console.WriteLine($"Ядер CPU: {Environment.ProcessorCount}");
Console.WriteLine("Ожидание команды на запуск от клиента...\n");

app.Run();

// ════════════════════ Модели обмена данными (JSON) ════════════════════

// Запрос на запуск расчёта — сколько членов ряда нужно просуммировать
record StartRequest(long TotalIterations);

// Состояние расчёта по одной формуле: текущее приближение π, прогресс (%) и сколько членов посчитано
record FormulaStatus(double Pi, double Progress, long IterationsDone);

// Полный статус узла: обе формулы сразу + признак "ещё считает"
record NodeStatus(bool IsRunning, long TotalIterations, FormulaStatus Leibniz, FormulaStatus Nilakantha);

// ════════════════════ Класс, выполняющий расчёт π ════════════════════
//
//  Обе формулы считаются параллельно в отдельных задачах (Task.Run),
//  а внутри каждой задачи диапазон членов ряда разбивается на блоки и
//  обрабатывается через Parallel.For — TPL сам распределяет блок между
//  всеми логическими ядрами процессора.
//
//  Промежуточные суммы и счётчики защищены блокировкой (lock), поэтому
//  GET /api/status можно безопасно вызывать в любой момент во время счёта.

class PiCalculator
{
    private const long ChunkSize = 2_000_000; // размер блока — после каждого обновляется прогресс

    private readonly object _lock = new();

    private long _totalIterations;
    private bool _isRunning;
    private CancellationTokenSource? _cts;

    // Накопленная сумма ряда и количество обработанных членов — отдельно для каждой формулы
    private double _leibnizSum;
    private long _leibnizDone;
    private double _nilakanthaSum;
    private long _nilakanthaDone;

    public void Start(long totalIterations)
    {
        Stop(); // на случай, если предыдущий расчёт ещё не завершился

        lock (_lock)
        {
            _totalIterations = totalIterations;
            _leibnizSum = 0;
            _leibnizDone = 0;
            _nilakanthaSum = 0;
            _nilakanthaDone = 0;
            _isRunning = true;
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        // Запускаем расчёт обеих формул одновременно — каждая в своей фоновой задаче
        Task.Run(() => RunFormula(totalIterations, token, isLeibniz: true), token);
        Task.Run(() => RunFormula(totalIterations, token, isLeibniz: false), token);

        Console.WriteLine($"[{Now}] ▶ Старт расчёта: {totalIterations:N0} членов ряда " +
                          $"(используется {Environment.ProcessorCount} ядер CPU)");
    }

    public void Stop()
    {
        _cts?.Cancel();
        lock (_lock) _isRunning = false;
    }

    public NodeStatus GetStatus()
    {
        lock (_lock)
        {
            double leibnizPi    = _leibnizSum;             // сумма ряда Лейбница уже даёт приближение π
            double nilakanthaPi = 3.0 + _nilakanthaSum;    // ряд Нилаканты: π = 3 + сумма

            return new NodeStatus(
                _isRunning,
                _totalIterations,
                new FormulaStatus(leibnizPi,    Percent(_leibnizDone,    _totalIterations), _leibnizDone),
                new FormulaStatus(nilakanthaPi, Percent(_nilakanthaDone, _totalIterations), _nilakanthaDone));
        }
    }

    // Считает один из рядов блоками по ChunkSize членов, после каждого блока обновляя общий прогресс
    private void RunFormula(long total, CancellationToken ct, bool isLeibniz)
    {
        var chunkLock = new object();

        for (long start = 0; start < total; start += ChunkSize)
        {
            long count = Math.Min(ChunkSize, total - start);
            double partialSum = 0;

            try
            {
                // Parallel.For делит диапазон [0..count) между потоками — задействуются все ядра CPU.
                // localInit/body/localFinally — стандартная схема параллельного суммирования (map-reduce):
                // у каждого потока своя локальная сумма, в конце все локальные суммы складываются под блокировкой.
                Parallel.For(0, (int)count,
                    new ParallelOptions { CancellationToken = ct },
                    localInit: () => 0.0,
                    body: (i, _, local) => local + Term(start + i, isLeibniz),
                    localFinally: local => { lock (chunkLock) partialSum += local; });
            }
            catch (OperationCanceledException)
            {
                return; // пользователь нажал "Стоп"
            }

            lock (_lock)
            {
                if (isLeibniz) { _leibnizSum += partialSum; _leibnizDone += count; }
                else           { _nilakanthaSum += partialSum; _nilakanthaDone += count; }
            }
        }

        // Когда обе формулы досчитаны до конца — расчёт на этом узле завершён
        lock (_lock)
        {
            if (_leibnizDone >= _totalIterations && _nilakanthaDone >= _totalIterations)
            {
                _isRunning = false;
                Console.WriteLine($"[{Now}] ✓ Расчёт завершён: " +
                                  $"π(Лейбниц)≈{_leibnizSum:F12}   π(Нилаканта)≈{3.0 + _nilakanthaSum:F12}");
            }
        }
    }

    // Член ряда номер k для выбранной формулы.
    // Коэффициенты подобраны так, что СУММА членов ряда Лейбница сразу даёт приближение к π,
    // а для ряда Нилаканты к итоговой сумме нужно прибавить 3.
    private static double Term(long k, bool isLeibniz)
    {
        if (isLeibniz)
        {
            // π = 4 · Σ (-1)^k / (2k+1),     k = 0, 1, 2, 3, ...
            double sign = (k % 2 == 0) ? 1.0 : -1.0;
            return 4.0 * sign / (2.0 * k + 1.0);
        }
        else
        {
            // π = 3 + Σ (-1)^(n+1) · 4 / (2n·(2n+1)·(2n+2)),     n = 1, 2, 3, ...
            long n = k + 1;
            double sign = (n % 2 == 1) ? 1.0 : -1.0;
            return sign * 4.0 / ((2.0 * n) * (2.0 * n + 1.0) * (2.0 * n + 2.0));
        }
    }

    private static double Percent(long done, long total) =>
        total > 0 ? Math.Min(100.0, (double)done / total * 100.0) : 0.0;

    private static string Now => DateTime.Now.ToString("HH:mm:ss");
}
