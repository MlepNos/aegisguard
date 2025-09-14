export function renderBarChart(canvasId, labels, counts) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    if (ctx._chartInstance) {
        ctx._chartInstance.destroy();
    }

    ctx._chartInstance = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Findings',
                data: counts,
                backgroundColor: ['#007bff', '#ffc107', '#dc3545', '#6c757d']
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false
        }
    });
}
