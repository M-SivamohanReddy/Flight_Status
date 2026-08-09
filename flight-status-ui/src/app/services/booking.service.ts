import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { BookingRequest, BookingResponse } from '../models/booking.models';

@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly api = environment.apiUrl;
  constructor(private http: HttpClient) {}

  book(req: BookingRequest): Observable<BookingResponse> {
    return this.http.post<BookingResponse>(`${this.api}/bookings`, req);
  }

  getMyBookings(): Observable<BookingResponse[]> {
    return this.http.get<BookingResponse[]>(`${this.api}/bookings/my`);
  }

  getAllBookings(): Observable<BookingResponse[]> {
    return this.http.get<BookingResponse[]>(`${this.api}/admin/bookings`);
  }
}
