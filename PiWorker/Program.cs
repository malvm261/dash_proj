// ════════════════════════════════════════════════════════════════════
//  Воркер распределённого расчёта числа π
//
//  Воркер НЕ считает весь ряд целиком — диспетчер присылает ему только
//  СВОЙ ДИАПАЗОН индексов членов ряда (RangeStart, RangeCount).
//  Воркер считает частичную сумму по обеим формулам для этого диапазона,
//  используя все ядра процессора (Parallel.For из TPL), и отдаёт
//  частичный результат + прогресс по запросу диспетчера.
//
//  Диспетчер затем складывает частичные суммы СО ВСЕХ воркеров в одно
//  общее значение π — поэтому чем больше воркеров, тем быстрее считается
//  итоговый результат (настоящие распределённые вычисления).
// ════════════════════════════════════════════════════════════════════

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5050");

builder.Services.AddSingleton<PiCalculator>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Отдаём JSON в PascalCase — имена свойств совпадут с моделями на диспетчере
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.PropertyNamingPolicy = null);

var app = builder.Build();
app.UseCors();

var calculator = app.Services.GetRequiredService<PiCalculator>();

// Получить от диспетчера свой диапазон и начать его считать
app.MapPost("/api/start", (StartRequest req, PiCalculator calc) =>
{
    calc.Start(req.RangeStart, req.RangeCount);
    return Results.Ok();
});

// Текущий статус воркера: прогресс по своему диапазону + частичные суммы рядов
app.MapGet("/api/status", (PiCalculator calc) => Results.Ok(calc.GetStatus()));

// Остановить расчёт (например, по нажатию "Стоп" на диспетчере)
app.MapPost("/api/stop", (PiCalculator calc) =>
{
    calc.Stop();
    return Results.Ok();
});

Console.WriteLine("════════════════════════════════════════════");
Console.WriteLine("   Воркер распределённого расчёта числа π");
Console.WriteLine("════════════════════════════════════════════");
Console.WriteLine($"Адрес:    http://0.0.0.0:5050");
Console.WriteLine($"Ядер CPU: {Environment.ProcessorCount}");
Console.WriteLine("Ожидание задания (диапазона) от диспетчера...\n");

app.Run();

// ════════════════════ Модели обмена данными (JSON) ════════════════════

// Задание воркеру: посчитать члены ряда с индексами [RangeStart .. RangeStart + RangeCount)
record StartRequest(long RangeStart, long RangeCount);

// Статус воркера: его диапазон, сколько уже посчитано и частичные суммы по обеим формулам.
// ВАЖНО: это ЧАСТИЧНЫЕ суммы только по диапазону этого воркера — не значения π!
// Итоговое π диспетчер получает, складывая частичные суммы со всех воркеров.
record NodeStatus(
    bool IsRunning,
    long RangeStart,
    long RangeCount,
    long IterationsDone,
    double Progress,
    double LeibnizPartialSum,
    double NilakanthaPartialSum);

// ════════════════════ Класс, считающий частичную сумму ряда ════════════════════
//
//  Диапазон делится на блоки по ChunkSize членов; каждый блок обрабатывается
//  через Parallel.For — TPL сам распределяет блок между всеми логическими
//  ядрами процессора (схема map/reduce: своя локальная сумма у каждого потока,
//  затем сложение под блокировкой). После каждого блока обновляется прогресс,
//  поэтому GET /api/status можно безопасно вызывать в любой момент во время счёта.

class PiCalculator
{
    private const long ChunkSize = 2_000_000; // размер блока — после каждого обновляется прогресс

    private readonly object _lock = new();

    private long _rangeStart;
    private long _rangeCount;
    private bool _isRunning;
    private CancellationTokenSource? _cts;

    // Накопленные частичные суммы по СВОЕМУ диапазону (не итоговое π!)
    private double _leibnizSum;
    private double _nilakanthaSum;
    private long _iterationsDone;

    public void Start(long rangeStart, long rangeCount)
    {
        Stop(); // на случай, если предыдущее задание ещё не завершилось

        lock (_lock)
        {
            _rangeStart = rangeStart;
            _rangeCount = rangeCount;
            _leibnizSum = 0;
            _nilakanthaSum = 0;
            _iterationsDone = 0;
            _isRunning = true;
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        Task.Run(() => RunComputation(rangeStart, rangeCount, token), token);

        Console.WriteLine($"[{Now}] ▶ Получено задание: диапазон [{rangeStart:N0} .. {rangeStart + rangeCount - 1:N0}] " +
                          $"— {rangeCount:N0} членов, {Environment.ProcessorCount} ядер CPU");
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
            double progress = _rangeCount > 0
                ? Math.Min(100.0, (double)_iterationsDone / _rangeCount * 100.0)
                : 0.0;

            return new NodeStatus(
                _isRunning, _rangeStart, _rangeCount, _iterationsDone, progress,
                _leibnizSum, _nilakanthaSum);
        }
    }

    // Считает частичную сумму ОБЕИХ формул для своего диапазона блоками по ChunkSize членов
    private void RunComputation(long rangeStart, long rangeCount, CancellationToken ct)
    {
        var chunkLock = new object();

        for (long offset = 0; offset < rangeCount; offset += ChunkSize)
        {
            long count = Math.Min(ChunkSize, rangeCount - offset);
            long chunkStart = rangeStart + offset;
            double leibnizPartial = 0;
            double nilakanthaPartial = 0;

            try
            {
                // Один проход Parallel.For сразу для двух формул: для каждого индекса k
                // считаем оба слагаемых и копим их в локальном кортеже потока (local.Leibniz / local.Nilakantha).
                // В конце каждый поток один раз складывает свои локальные суммы в общие — под блокировкой.
                Parallel.For(0, (int)count,
                    new ParallelOptions { CancellationToken = ct },
                    localInit: () => (Leibniz: 0.0, Nilakantha: 0.0),
                    body: (i, _, local) =>
                    {
                        long k = chunkStart + i;
                        return (local.Leibniz + LeibnizTerm(k), local.Nilakantha + NilakanthaTerm(k));
                    },
                    localFinally: local =>
                    {
                        lock (chunkLock)
                        {
                            leibnizPartial += local.Leibniz;
                            nilakanthaPartial += local.Nilakantha;
                        }
                    });
            }
            catch (OperationCanceledException)
            {
                return; // пользователь нажал "Стоп"
            }

            lock (_lock)
            {
                _leibnizSum += leibnizPartial;
                _nilakanthaSum += nilakanthaPartial;
                _iterationsDone += count;
            }
        }

        lock (_lock)
        {
            if (_iterationsDone >= _rangeCount)
            {
                _isRunning = false;
                Console.WriteLine($"[{Now}] ✓ Воркер завершил свой диапазон " +
                                  $"[{rangeStart:N0} .. {rangeStart + rangeCount - 1:N0}]: " +
                                  $"частичные суммы Лейбниц={_leibnizSum:G10}, Нилаканта={_nilakanthaSum:G10}");
            }
        }
    }

    // Член ряда Лейбница номер k (k = 0, 1, 2, ...). Коэффициент 4 включён сразу,
    // поэтому СУММА ВСЕХ членов ряда (по полному диапазону 0..N) даёт приближение π напрямую:
    //   π = 4 · Σ (-1)^k / (2k+1)
    private static double LeibnizTerm(long k)
    {
        double sign = (k % 2 == 0) ? 1.0 : -1.0;
        return 4.0 * sign / (2.0 * k + 1.0);
    }

    // Член ряда Нилаканты номер k — соответствует n = k+1 = 1, 2, 3, ...
    // К сумме ВСЕХ членов нужно прибавить 3, чтобы получить π:
    //   π = 3 + Σ (-1)^(n+1) · 4 / (2n·(2n+1)·(2n+2))
    private static double NilakanthaTerm(long k)
    {
        long n = k + 1;
        double sign = (n % 2 == 1) ? 1.0 : -1.0;
        return sign * 4.0 / ((2.0 * n) * (2.0 * n + 1.0) * (2.0 * n + 2.0));
    }

    private static string Now => DateTime.Now.ToString("HH:mm:ss");
}
