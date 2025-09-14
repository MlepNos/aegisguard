const charts = {};
function ensure(canvasId) {
  const el = document.getElementById(canvasId);
  if (!el) return null;
  if (charts[canvasId]) { charts[canvasId].destroy(); delete charts[canvasId]; }
  return el;
}

export function renderSeverityOverTime(canvasId, rows) {
  const el = document.getElementById(canvasId);
  if (!el) return;

  // rows: [{ day: "2025-09-14", severity: "Warning", count: 3 }, ...]
  // -> in Datasets pro Severity umwandeln und x = Date, y = count
  const bySeverity = {};
  for (const r of rows) {
    const sev = r.severity ?? "Unknown";
    (bySeverity[sev] ??= []).push({ x: new Date(r.day), y: r.count });
  }

  const datasets = Object.entries(bySeverity).map(([label, data]) => ({
    label, data, tension: 0.2, borderWidth: 2, pointRadius: 2
  }));

  // Wichtig: parsing:false, type:'time' + time.unit
  new Chart(el, {
    type: 'line',
    data: { datasets },
    options: {
      parsing: false,
      responsive: true,
      maintainAspectRatio: false,
      scales: {
        x: { type: 'time', time: { unit: 'day' } },
        y: { beginAtZero: true }
      },
      plugins: { legend: { position: 'bottom' } }
    }
  });
}


export function renderTopSources(canvasId, rows) {
  const el = ensure(canvasId); if (!el) return;
  const labels = rows.map(r => r.name);
  const data   = rows.map(r => r.count);
  charts[canvasId] = new Chart(el, {
    type: 'bar',
    data: { labels, datasets: [{ label: 'Findings', data }] },
    options: {
      indexAxis: 'y', responsive: true, maintainAspectRatio: false,
      scales: { x: { beginAtZero: true, ticks: { precision: 0 } } },
      plugins: { legend: { display: false } }
    }
  });
}

export function renderSeverityShare(canvasId, items) {
  const el = ensure(canvasId); if (!el) return;
  const labels = items.map(i => i.severity);
  const data   = items.map(i => i.count);
  charts[canvasId] = new Chart(el, {
    type: 'doughnut',
    data: { labels, datasets: [{ data }] },
    options: { responsive: true, maintainAspectRatio: false, plugins:{legend:{position:'bottom'}} }
  });
}

// Platzhalter: Heatmap – du kannst später ein Plugin oder ECharts nehmen
export function renderHeatmap(canvasId, rows) {
  const el = document.getElementById(canvasId);
  if (!el) return;

  // Achsen-Labels (0=So..6=Sa) → auf Deutsch
  const dayLabels = ['So','Mo','Di','Mi','Do','Fr','Sa'];
  const hourLabels = Array.from({length:24}, (_,h) => `${h}:00`);

  // Wertebereich für Farbschema bestimmen
  const max = Math.max(1, ...rows.map(r => r.value || 0));

  // Matrix-Daten in das vom Plugin erwartete Format bringen
  const data = rows.map(r => ({
    x: r.day,       // 0..6
    y: r.hour,      // 0..23
    v: r.value      // Intensität
  }));

  // einfache Farbfunktion (hell → dunkel)
  const colorFor = (v) => {
    const t = v / max;                           // 0..1
    const alpha = 0.15 + 0.85 * t;               // minimal sichtbar
    return `rgba(33, 150, 243, ${alpha})`;       // blau
  };

  new Chart(el, {
    type: 'matrix',
    data: {
      datasets: [{
        label: 'Events',
        data,
        width:  ({chart}) => (chart.chartArea?.width  ?? 0) / 7 - 4,
        height: ({chart}) => (chart.chartArea?.height ?? 0) / 24 - 2,
        backgroundColor: (ctx) => colorFor(ctx.raw.v),
        borderWidth: 1,
        borderColor: 'rgba(0,0,0,0.05)',
        hoverBackgroundColor: 'rgba(255,193,7,0.9)'
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      scales: {
        x: {
          type: 'category',
          labels: dayLabels,
          offset: true,
          grid: { display: false }
        },
        y: {
          type: 'category',
          labels: hourLabels,
          offset: true,
          reverse: true,          // 0 Uhr oben
          grid: { display: false }
        }
      },
      plugins: {
        legend: { display: false },
        tooltip: {
          callbacks: {
            title: (items) => {
              const r = items[0].raw;
              return `${dayLabels[r.x]} ${hourLabels[r.y]}`;
            },
            label: (item) => `Anzahl: ${item.raw.v}`
          }
        }
      }
    }
  });
}

