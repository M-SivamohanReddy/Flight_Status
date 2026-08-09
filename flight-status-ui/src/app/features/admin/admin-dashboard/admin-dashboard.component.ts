import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../services/auth.service';
import { BookingService } from '../../../services/booking.service';
import { FlightStatusService } from '../../../services/flight-status.service';
import { SearchFormComponent } from '../../../shared/components/search-form/search-form.component';
import { ResultCardComponent } from '../../../shared/components/result-card/result-card.component';
import { FlightListComponent } from '../flight-list/flight-list.component';
import { FlightStatusRequest, FlightStatusResult } from '../../../models/flight-status.models';
import { BookingResponse } from '../../../models/booking.models';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, SearchFormComponent, ResultCardComponent, FlightListComponent],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.css']
})
export class AdminDashboardComponent implements OnInit {
  @ViewChild(SearchFormComponent) searchForm!: SearchFormComponent;

  result: FlightStatusResult | null = null;
  errorMessage: string | null = null;
  allBookings: BookingResponse[] = [];
  loadingBookings = true;
  bookingsPage = 1;
  readonly bookingsPageSize = 5;

  get paginatedBookings(): BookingResponse[] {
    const start = (this.bookingsPage - 1) * this.bookingsPageSize;
    return this.allBookings.slice(start, start + this.bookingsPageSize);
  }
  get totalBookingPages(): number { return Math.ceil(this.allBookings.length / this.bookingsPageSize) || 1; }

  get userName() {
    const u = this.auth.currentUser();
    return u ? `${u.firstName} ${u.lastName}` : 'Admin';
  }

  constructor(
    private auth: AuthService,
    private bookingSvc: BookingService,
    private flightSvc: FlightStatusService
  ) {}

  ngOnInit(): void {
    this.bookingSvc.getAllBookings().subscribe({
      next: b => { this.allBookings = b; this.loadingBookings = false; },
      error: () => { this.loadingBookings = false; }
    });
  }

  onSearch(req: FlightStatusRequest): void {
    this.result = null; this.errorMessage = null;
    this.searchForm.setLoading(true);
    this.flightSvc.getStatus(req.flightNumber, req.date).subscribe({
      next: data => { this.result = data; this.searchForm.setLoading(false); },
      error: err  => {
        this.errorMessage = err.status === 0 ? 'API unreachable.' : `Error ${err.status}`;
        this.searchForm.setLoading(false);
      }
    });
  }

  onSelectFlight(req: FlightStatusRequest): void {
    this.searchForm.form.setValue({ flightNumber: req.flightNumber, date: req.date });
    this.onSearch(req);
    setTimeout(() => document.getElementById('detail-anchor')?.scrollIntoView({ behavior: 'smooth' }), 100);
  }

  logout(): void { this.auth.logout(); }
}
