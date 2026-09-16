document.addEventListener("DOMContentLoaded", function () {
    const darkThemeOptions = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: { labels: { color: '#8a99ad', font: { size: 11 } } },
            tooltip: { backgroundColor: '#1e293b', titleColor: '#fff', bodyColor: '#cbd5e1', padding: 10, cornerRadius: 8 }
        },
        scales: {
            x: { grid: { display: false }, ticks: { color: '#64748b' } },
            y: { grid: { color: 'rgba(255, 255, 255, 0.05)' }, ticks: { color: '#64748b' } }
        }
    };

    // 1. Sparkline Inventario (KPI)
    if (document.getElementById('sparklineInventario')) {
        new Chart(document.getElementById('sparklineInventario'), {
            type: 'line',
            data: {
                labels: ['', '', '', '', '', ''],
                datasets: [{
                    data: [65, 59, 80, 81, 56, 75],
                    borderColor: '#3b82f6',
                    borderWidth: 2,
                    pointRadius: 0,
                    tension: 0.4
                }]
            },
            options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } }, scales: { x: { display: false }, y: { display: false } } }
        });
    }

    // 2. Tendencia Calidad
    if (document.getElementById('chartTendenciaCalidad')) {
        new Chart(document.getElementById('chartTendenciaCalidad'), {
            type: 'line',
            data: {
                labels: ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun'],
                datasets: [
                    { label: 'Temperatura (°C)', data: [18, 19, 21, 20, 22, 21], borderColor: '#f43f5e', backgroundColor: 'rgba(244, 63, 94, 0.08)', fill: true, tension: 0.4 },
                    { label: 'Oxígeno (mg/L)', data: [8.5, 8.2, 7.9, 8.1, 7.5, 8.0], borderColor: '#0ea5e9', backgroundColor: 'rgba(14, 165, 233, 0.08)', fill: true, tension: 0.4 }
                ]
            },
            options: darkThemeOptions
        });
    }

    // 3. Resumen Anomalías
    if (document.getElementById('chartAnomalias')) {
        new Chart(document.getElementById('chartAnomalias'), {
            type: 'doughnut',
            data: {
                labels: ['Temp Fuera de Rango', 'Bajo Oxígeno', 'PH Anormal'],
                datasets: [{
                    data: [10, 5, 3],
                    backgroundColor: ['#ef4444', '#f59e0b', '#3b82f6'],
                    borderWidth: 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom', labels: { color: '#8a99ad', font: { size: 11 } } } },
                cutout: '75%'
            }
        });
    }

    // 4. Crecimiento por Especie
    if (document.getElementById('chartCrecimientoEspecie')) {
        new Chart(document.getElementById('chartCrecimientoEspecie'), {
            type: 'bar',
            data: {
                labels: ['Trucha', 'Salmón', 'Tilapia', 'Carpa'],
                datasets: [{
                    label: 'Crecimiento Promedio (cm)',
                    data: [12, 19, 8, 15],
                    backgroundColor: '#10b981',
                    borderRadius: 6
                }]
            },
            options: darkThemeOptions
        });
    }

    // 5. Alimentación vs Inventario
    if (document.getElementById('chartAlimentacionPeces')) {
        new Chart(document.getElementById('chartAlimentacionPeces'), {
            type: 'bar',
            data: {
                labels: ['Sem 1', 'Sem 2', 'Sem 3', 'Sem 4'],
                datasets: [
                    { label: 'Alimento (kg)', data: [400, 450, 420, 480], backgroundColor: '#8b5cf6', borderRadius: 6 },
                    { label: 'Población (x100)', data: [350, 360, 360, 370], backgroundColor: '#3b82f6', borderRadius: 6 }
                ]
            },
            options: darkThemeOptions
        });
    }
});