import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FlightInfo, FlightStatusResult } from '../models/flight-status.models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class FlightStatusService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getFlights(): Observable<FlightInfo[]> {
    return this.http.get<FlightInfo[]>(`${this.apiUrl}/flights`);
  }

  getStatus(flightNumber: string, date: string): Observable<FlightStatusResult> {
    const params = new HttpParams()
      .set('flightNumber', flightNumber)
      .set('date', date);
    return this.http.get<FlightStatusResult>(`${this.apiUrl}/flights/status`, { params });
  }
}
