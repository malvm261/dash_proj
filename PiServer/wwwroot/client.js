// Конфигурация
const API_URL = window.location.origin;
const POLL_INTERVAL = 1000; // мс

let chart = null;
let chartData = {
    labels: [],
    pi1Values: [],
    pi2Values: [],
    progressValues: []
};
let clients = {};
const PI_REAL = 3.14159265358979323846;

// Инициализация
document.addEventListener('DOMContentLoaded', () => {
    initChart();
    setupEventListeners();
    startPolling();
    addLog('ℹ️ Дашборд загружен', 'info');
});

function initChart() {
    const ctx = document.getElementById('piChart').getContext('2d');
    chart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: [],
            datasets: [
                {
                    label: 'π (Лейбниц)',
                    data: [],
                    borderColor: '#667eea',
                    backgroundColor: 'rgba(102, 126, 234, 0.1)',
                    tension: 0.3,
                    borderWidth: 2,
                    fill: true,
                    pointRadius: 3,
                    pointBackgroundColor: '#667eea',
                },
                {
                    label: 'π (Нилаканта)',
                    data: [],
                    borderColor: '#764ba2',
                    backgroundColor: 'rgba(118, 75, 162, 0.1)',
                    tension: 0.3,
                    borderWidth: 2,
                    fill: true,
                    pointRadius: 3,
                    pointBackgroundColor: '#764ba2',
                },
                {
                    label: 'π (эталон)',
                    data: [],
                    borderColor: '#999',
                    borderDash: [5, 5],
                    pointRadius: 0,
                    fill: false,
                    borderWidth: 2,
                    tension: 0,
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: true,
                    position: 'top',
                    labels: {
                        usePointStyle: true,
                        padding: 15,
                        font: { size: 12 }
                    }
                }
            },
            scales: {
                y: {
                    min: 2.5,
                    max: 3.5,
                    ticks: {
                        callback: (v) => v.toFixed(2)
                    }
                }
            }
        }
    });
}

function setupEventListeners() {
    document.getElementById('resetBtn').addEventListener('click', async () => {
        const totalIterations = BigInt(document.getElementById('totalIterations').value);
        const chunkSize = BigInt(document.getElementById('chunkSize').value);

        try {
            const resp = await fetch(`${API_URL}/api/reset`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    totalIterations: totalIterations.toString(),
                    chunkSize: chunkSize.toString()
                })
            });

            if (resp.ok) {
                // Сброс данных графика
                chartData = { labels: [], pi1Values: [], pi2Values: [], progressValues: [] };
                chart.data.labels = [];
                chart.data.datasets[0].data = [];
                chart.data.datasets[1].data = [];
                chart.data.datasets[2].data = [];
                chart.update();

                clients = {};
                updateClientsList();
                document.getElementById('log').innerHTML = '';

                addLog('✅ Вычисления перезагружены', 'info');
            } else {
                addLog('❌ Ошибка сброса', 'error');
            }
        } catch (e) {
            addLog(`❌ Ошибка: ${e.message}`, 'error');
        }
    });
}

async function pollStatus() {
    try {
        const resp = await fetch(`${API_URL}/api/status`);
        if (!resp.ok) return;

        const status = await resp.json();

        // Обновление статистики
        document.getElementById('progress').textContent = `${status.progress.toFixed(1)}%`;
        document.getElementById('progressFill').style.width = `${Math.min(status.progress, 100)}%`;
        document.getElementById('completedChunks').textContent = `${status.completedChunks} / ${status.totalChunks}`;

        // π значения с ошибкой от эталона
        const pi1Str = status.pi1.toFixed(15);
        const pi2Str = status.pi2.toFixed(15);
        const error1 = Math.abs(status.pi1 - PI_REAL);
        const error2 = Math.abs(status.pi2 - PI_REAL);

        document.getElementById('pi1').textContent = pi1Str;
        document.getElementById('error1').textContent = `Ошибка: ${error1.toExponential(2)}`;

        document.getElementById('pi2').textContent = pi2Str;
        document.getElementById('error2').textContent = `Ошибка: ${error2.toExponential(2)}`;

        // Добавить в график (раз в 10 обновлений чтобы не перегружать)
        if (status.completedChunks % 10 === 0 || status.completedChunks === 1) {
            const timestamp = new Date().toLocaleTimeString('ru-RU');
            chartData.labels.push(timestamp);
            chartData.pi1Values.push(status.pi1);
            chartData.pi2Values.push(status.pi2);
            chartData.progressValues.push(status.progress);

            // Ограничить последних 50 точек на графике
            if (chartData.labels.length > 50) {
                chartData.labels.shift();
                chartData.pi1Values.shift();
                chartData.pi2Values.shift();
                chartData.progressValues.shift();
            }

            updateChart();
        }

        // Статус завершения
        if (status.isComplete) {
            document.getElementById('serverStatus').style.background = '#4ade80';
            addLog('✅ Все вычисления завершены!', 'info');
        }

        // Получить список активных клиентов
        await pollClients();

    } catch (e) {
        // Сервер недоступен
    }
}

async function pollClients() {
    try {
        const resp = await fetch(`${API_URL}/api/clients`);
        if (!resp.ok) return;

        const clientsList = await resp.json();
        if (clientsList.length > 0) {
            clients = {};
            clientsList.forEach(client => {
                clients[client.clientId] = {
                    blockCount: client.blockCount,
                    totalTime: client.totalTime,
                    lastUpdate: client.lastUpdate
                };
            });
            updateClientsList();
        }
    } catch (e) {
        // Ошибка получения списка клиентов - игнорировать
    }
}

function updateChart() {
    chart.data.labels = chartData.labels;
    chart.data.datasets[0].data = chartData.pi1Values;
    chart.data.datasets[1].data = chartData.pi2Values;

    // Эталон на всю ширину
    chart.data.datasets[2].data = chartData.labels.map(() => PI_REAL);

    chart.update();
}

function startPolling() {
    pollStatus();
    setInterval(pollStatus, POLL_INTERVAL);
}

function updateClientsList() {
    const list = document.getElementById('clientsList');
    const entries = Object.entries(clients);

    if (entries.length === 0) {
        list.innerHTML = '<p class="placeholder">Ожидание клиентов...</p>';
        return;
    }

    list.innerHTML = entries.map(([id, data]) => `
        <div class="client-card">
            <div class="client-name">💻 ${id}</div>
            <div class="client-info">
                <div>Блоков обработано: ${data.blockCount}</div>
                <div>Время: ${data.totalTime.toFixed(0)}мс</div>
                <div>Обновлено: ${new Date(data.lastUpdate).toLocaleTimeString('ru-RU')}</div>
            </div>
        </div>
    `).join('');
}

function addLog(message, type = 'info') {
    const logContainer = document.getElementById('log');
    const time = new Date().toLocaleTimeString('ru-RU');

    const line = document.createElement('div');
    line.className = `log-line log-${type}`;
    line.innerHTML = `<span class="log-time">[${time}]</span>${message}`;

    logContainer.appendChild(line);
    logContainer.scrollTop = logContainer.scrollHeight;

    // Ограничить максимум 200 строк
    while (logContainer.children.length > 200) {
        logContainer.removeChild(logContainer.firstChild);
    }
}

// Слушатель для обновлений от сервера (через polling результатов)
async function pollResults() {
    try {
        // Можно добавить дополнительный эндпоинт для получения списка активных клиентов
        // Пока используем данные со статуса
    } catch (e) {
        // Ошибка - игнорировать
    }
}

// Периодический опрос клиентов (встроен в pollStatus)
// setInterval(pollResults, 2000);
