import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FlightStatusResult } from '../models/flight-status.models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class FlightStatusService {
  private readonly baseUrl = `${environment.apiUrl}/flights/status`;

  constructor(private http: HttpClient) {}

  getStatus(flightNumber: string, date: string): Observable<FlightStatusResult> {
    const params = new HttpParams()
      .set('flightNumber', flightNumber)
      .set('date', date);
    return this.http.get<FlightStatusResult>(this.baseUrl, { params });
  }
}
