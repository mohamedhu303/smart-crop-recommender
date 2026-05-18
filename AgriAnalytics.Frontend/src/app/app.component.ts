// app.component.ts
// Root dashboard component for AgriAnalytics.
// Handles data fetching, chart configuration, and ML prediction logic.

import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgxEchartsDirective } from 'ngx-echarts';
import { EChartsOption } from 'echarts';
import {
  AgriApiService,
  MonthlyTrend,
  DashboardSummary,
  CropPredictionResult,
} from './services/agri-api.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule, NgxEchartsDirective],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
})
export class AppComponent implements OnInit {
  private api = inject(AgriApiService);

  // State signals for reactive UI updates
  summary = signal<DashboardSummary | null>(null);
  trends = signal<MonthlyTrend[]>([]);
  prediction = signal<CropPredictionResult | null>(null);
  isLoading = signal(true);
  isPredicting = signal(false);
  errorMsg = signal<string | null>(null);
  activeChart = signal<'combined' | 'rainfall'>('combined');

  // Form model bound to the prediction input fields
  formInput = {
    temperature: 28,
    humidity: 80,
    soil_pH: 6.0,
    rainfall: 900,
  };


  // Combined multi-line chart showing all metrics together
  combinedChartOption = computed<EChartsOption>(() => {
    const data = this.trends();
    if (!data.length) return {};

    const labels = data.map(d => d.monthLabel);

    return {
      backgroundColor: 'transparent',
      tooltip: {
        trigger: 'axis',
        backgroundColor: '#1e293b',
        borderColor: '#334155',
        textStyle: { color: '#e2e8f0' },
      },
      legend: {
        data: ['Temperature (°C)', 'Humidity (%)', 'Rainfall (mm/10)'],
        textStyle: { color: '#94a3b8' },
        top: 10,
      },
      grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
      xAxis: {
        type: 'category',
        data: labels,
        axisLine: { lineStyle: { color: '#334155' } },
        axisLabel: { color: '#94a3b8', rotate: 45, fontSize: 11 },
      },
      yAxis: {
        type: 'value',
        axisLine: { lineStyle: { color: '#334155' } },
        axisLabel: { color: '#94a3b8' },
        splitLine: { lineStyle: { color: '#1e293b' } },
      },
      series: [
        {
          name: 'Temperature (°C)',
          type: 'line',
          smooth: true,
          data: data.map(d => d.avgTemperature),
          lineStyle: { color: '#f97316', width: 2.5 },
          itemStyle: { color: '#f97316' },
          areaStyle: { color: 'rgba(249,115,22,0.08)' },
          symbol: 'circle',
          symbolSize: 4,
        },
        {
          name: 'Humidity (%)',
          type: 'line',
          smooth: true,
          data: data.map(d => d.avgHumidity),
          lineStyle: { color: '#38bdf8', width: 2.5 },
          itemStyle: { color: '#38bdf8' },
          areaStyle: { color: 'rgba(56,189,248,0.08)' },
          symbol: 'circle',
          symbolSize: 4,
        },
        {
          name: 'Rainfall (mm/10)',
          type: 'line',
          smooth: true,
          data: data.map(d => +(d.avgRainfall / 10).toFixed(1)),
          lineStyle: { color: '#4ade80', width: 2.5 },
          itemStyle: { color: '#4ade80' },
          areaStyle: { color: 'rgba(74,222,128,0.08)' },
          symbol: 'circle',
          symbolSize: 4,
        },
      ],
    };
  });

  // Bar chart showing average rainfall per month
  rainfallChartOption = computed<EChartsOption>(() => {
    const data = this.trends();
    if (!data.length) return {};

    return {
      backgroundColor: 'transparent',
      tooltip: {
        trigger: 'axis',
        backgroundColor: '#1e293b',
        borderColor: '#334155',
        textStyle: { color: '#e2e8f0' },
        formatter: (params: any) => {
          const p = params[0];
          return `${p.name}<br/>Avg Rainfall: <b>${p.value} mm</b>`;
        },
      },
      grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
      xAxis: {
        type: 'category',
        data: data.map(d => d.monthLabel),
        axisLabel: { color: '#94a3b8', rotate: 45, fontSize: 11 },
        axisLine: { lineStyle: { color: '#334155' } },
      },
      yAxis: {
        type: 'value',
        name: 'mm',
        nameTextStyle: { color: '#94a3b8' },
        axisLabel: { color: '#94a3b8' },
        splitLine: { lineStyle: { color: '#1e293b' } },
      },
      series: [
        {
          name: 'Avg Rainfall',
          type: 'bar',
          data: data.map(d => d.avgRainfall),
          itemStyle: {
            color: {
              type: 'linear', x: 0, y: 0, x2: 0, y2: 1,
              colorStops: [
                { offset: 0, color: '#38bdf8' },
                { offset: 1, color: '#0ea5e9' },
              ],
            },
            borderRadius: [4, 4, 0, 0],
          },
          barMaxWidth: 30,
        },
      ],
    };
  });

  // Horizontal bar chart for ML prediction probability breakdown
  probabilityChartOption = computed<EChartsOption>(() => {
    const pred = this.prediction();
    if (!pred) return {};

    const sorted = [...pred.allProbabilities].sort((a, b) => b.probability - a.probability);

    return {
      backgroundColor: 'transparent',
      tooltip: {
        trigger: 'axis',
        backgroundColor: '#1e293b',
        borderColor: '#334155',
        textStyle: { color: '#e2e8f0' },
        formatter: (params: any) => {
          const p = params[0];
          return `${p.name}<br/>Probability: <b>${p.value}%</b>`;
        },
      },
      grid: { left: '12%', right: '8%', bottom: '3%', top: '3%', containLabel: true },
      xAxis: {
        type: 'value',
        max: 100,
        axisLabel: { color: '#94a3b8', formatter: '{value}%' },
        splitLine: { lineStyle: { color: '#1e293b' } },
      },
      yAxis: {
        type: 'category',
        data: sorted.map(p => p.cropName),
        axisLabel: { color: '#94a3b8', fontSize: 12 },
        axisLine: { lineStyle: { color: '#334155' } },
      },
      series: [
        {
          type: 'bar',
          data: sorted.map(p => ({
            value: p.probability,
            itemStyle: {
              color: p.cropName === pred.recommendedCrop
                ? { type: 'linear', x: 0, y: 0, x2: 1, y2: 0,
                    colorStops: [
                      { offset: 0, color: '#4ade80' },
                      { offset: 1, color: '#22c55e' },
                    ] }
                : { type: 'linear', x: 0, y: 0, x2: 1, y2: 0,
                    colorStops: [
                      { offset: 0, color: '#334155' },
                      { offset: 1, color: '#475569' },
                    ] },
              borderRadius: [0, 4, 4, 0],
            },
          })),
          barMaxWidth: 22,
          label: {
            show: true,
            position: 'right',
            color: '#94a3b8',
            formatter: '{c}%',
            fontSize: 11,
          },
        },
      ],
    };
  });

  ngOnInit(): void {
    this.loadDashboardData();
  }

  // Loads summary and trend data from the API in parallel
  private loadDashboardData(): void {
    this.isLoading.set(true);
    this.errorMsg.set(null);

    this.api.getSummary().subscribe({
      next: data => this.summary.set(data),
      error: err => console.error('Summary load failed:', err),
    });

    this.api.getMonthlyTrends().subscribe({
      next: data => {
        this.trends.set(data);
        this.isLoading.set(false);
      },
      error: err => {
        this.errorMsg.set('Failed to load trend data. Is the API running on port 5284?');
        this.isLoading.set(false);
        console.error('Trends load failed:', err);
      },
    });
  }

  // Sends the form data to the ML prediction endpoint
  predictCrop(): void {
    this.isPredicting.set(true);
    this.prediction.set(null);

    this.api.predictCrop(this.formInput).subscribe({
      next: result => {
        this.prediction.set(result);
        this.isPredicting.set(false);
      },
      error: err => {
        console.error('Prediction failed:', err);
        this.isPredicting.set(false);
      },
    });
  }


  // Returns the CSS class for the confidence badge color
  getConfidenceClass(confidence: number): string {
    if (confidence >= 80) return 'badge-high';
    if (confidence >= 60) return 'badge-medium';
    return 'badge-low';
  }

  // Formats ISO date string for display on the dashboard
  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
    });
  }
}