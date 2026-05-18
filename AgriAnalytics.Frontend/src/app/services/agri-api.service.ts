// agri-api.service.ts
// Centralized HTTP service for all AgriAnalytics API calls.
// All components use this service instead of making direct HTTP calls.

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// Interfaces matching the backend DTOs exactly

export interface MonthlyTrend {
  monthLabel: string;
  year: number;
  month: number;
  avgTemperature: number;
  avgHumidity: number;
  avgSoilPH: number;
  avgRainfall: number;
  recordCount: number;
}

export interface DashboardSummary {
  totalRecords: number;
  earliestRecord: string;
  latestRecord: string;
  overallAvgTemperature: number;
  overallAvgHumidity: number;
  overallAvgRainfall: number;
  monthsCovered: number;
}

export interface CropPredictionRequest {
  temperature: number;
  humidity: number;
  soil_pH: number;
  rainfall: number;
}

export interface CropProbability {
  cropName: string;
  probability: number;
}

export interface CropPredictionResult {
  recommendedCrop: string;
  confidencePercent: number;
  allProbabilities: CropProbability[];
  message: string;
}

@Injectable({ providedIn: 'root' })
export class AgriApiService {
  // Base URL — must match the port from dotnet run output
  private readonly baseUrl = 'https://smart-crop-recommender.runasp.net/api';

  private http = inject(HttpClient);

  // Fetches KPI summary stats for the dashboard header cards
  getSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.baseUrl}/analytics/summary`);
  }

  // Fetches monthly aggregated trend data for the charts
  getMonthlyTrends(): Observable<MonthlyTrend[]> {
    return this.http.get<MonthlyTrend[]>(`${this.baseUrl}/analytics/trends`);
  }

  // Sends environmental inputs to the ML model and gets crop recommendation
  predictCrop(request: CropPredictionRequest): Observable<CropPredictionResult> {
    return this.http.post<CropPredictionResult>(`${this.baseUrl}/predict-crop`, request);
  }
}
