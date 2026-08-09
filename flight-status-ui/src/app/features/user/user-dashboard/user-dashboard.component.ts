import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../../../services/auth.service';
import { BookingService } from '../../../services/booking.service';
import { FlightStatusService } from '../../../services/flight-status.service';
import { FlightInfo, FlightStatusRequest, FlightStatusResult } from '../../../models/flight-status.models';
import { BookingResponse } from '../../../models/booking.models';
import { SearchFormComponent } from '../../../shared/components/search-form/search-form.component';
import { ResultCardComponent } from '../../../shared/components/result-card/result-card.component';

@Component({
  selector: 'app-user-dashboard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, SearchFormComponent, ResultCardComponent],
  templateUrl: './user-dashboard.component.html',
  styleUrls: ['./user-dashboard.component.css']
})
export class UserDashboardComponent implements OnInit {
  @ViewChild(SearchFormComponent) searchForm!: SearchFormComponent;

  // Flight status search (any flight, no booking required)
  searchResult: FlightStatusResult | null = null;
  searchError: string | null = null;

  flights: FlightInfo[] = [];

  myBookings: BookingResponse[] = [];
  myBookingsPage = 1;
  readonly myBookingsPageSize = 5;

  get paginatedMyBookings(): BookingResponse[] {
    const start = (this.myBookingsPage - 1) * this.myBookingsPageSize;
    return this.myBookings.slice(start, start + this.myBookingsPageSize);
  }
  get totalMyBookingPages(): number { return Math.ceil(this.myBookings.length / this.myBookingsPageSize) || 1; }

  selectedFlight: FlightInfo | null = null;
  bookingForm: FormGroup;
  bookingLoading = false;
  bookingSuccess = '';
  bookingError   = '';
  loadingFlights  = true;
  loadingBookings = true;

  // keyed by booking ID: { result, loading, error }
  private bookingStatusMap = new Map<number, { result: FlightStatusResult | null; loading: boolean; error: string }>();
  expandedBookings = new Set<number>();

  get userName() {
    const u = this.auth.currentUser();
    return u ? `${u.firstName} ${u.lastName}` : 'Passenger';
  }

  constructor(
    private auth: AuthService,
    private bookingSvc: BookingService,
    private flightSvc: FlightStatusService,
    private fb: FormBuilder
  ) {
    this.bookingForm = this.fb.group({ travelDate: ['', Validators.required] });
  }

  ngOnInit(): void {
    this.flightSvc.getFlights().subscribe({
      next: f => { this.flights = f; this.loadingFlights = false; },
      error: () => { this.loadingFlights = false; }
    });
    this.loadMyBookings();
  }

  loadMyBookings(): void {
    this.loadingBookings = true;
    this.myBookingsPage = 1;
    this.bookingSvc.getMyBookings().subscribe({
      next: b => { this.myBookings = b; this.loadingBookings = false; },
      error: () => { this.loadingBookings = false; }
    });
  }

  // -- Flight status search ---------------------------------------------------

  onSearch(req: FlightStatusRequest): void {
    this.searchResult = null;
    this.searchError  = null;
    this.searchForm.setLoading(true);
    this.flightSvc.getStatus(req.flightNumber, req.date).subscribe({
      next: data => { this.searchResult = data; this.searchForm.setLoading(false); },
      error: err  => {
        this.searchError = err.status === 400
          ? 'Invalid flight number or date format.'
          : 'Could not retrieve flight status. Please try again.';
        this.searchForm.setLoading(false);
      }
    });
  }

  // -- Booking status (per booked flight, expandable inline) -----------------

  toggleBookingStatus(booking: BookingResponse): void {
    if (this.expandedBookings.has(booking.id)) {
      this.expandedBookings.delete(booking.id);
      return;
    }
    this.expandedBookings.add(booking.id);
    if (this.bookingStatusMap.has(booking.id)) return;

    this.bookingStatusMap.set(booking.id, { result: null, loading: true, error: '' });
    this.flightSvc.getStatus(booking.flightNumber, booking.travelDate).subscribe({
      next: result => this.bookingStatusMap.set(booking.id, { result, loading: false, error: '' }),
      error: ()     => this.bookingStatusMap.set(booking.id, { result: null, loading: false, error: 'Could not load status.' })
    });
  }

  bookingStatus(id: number) { return this.bookingStatusMap.get(id); }
  isExpanded(id: number)    { return this.expandedBookings.has(id); }

  statusClass(s: string | undefined): string {
    return s === 'OnTime' ? 'bs-on-time' : s === 'Delayed' ? 'bs-delayed'
         : s === 'Cancelled' || s === 'Diverted' ? 'bs-red' : 'bs-unknown';
  }
  statusLabel(s: string | undefined): string {
    return s === 'OnTime' ? 'On Time' : s === 'Delayed' ? 'Delayed'
         : s === 'Cancelled' ? 'Cancelled' : s === 'Diverted' ? 'Diverted' : 'Unknown';
  }

  // -- Flight booking --------------------------------------------------------

  selectFlight(f: FlightInfo): void {
    this.selectedFlight = f;
    this.bookingSuccess = ''; this.bookingError = '';
    this.bookingForm.reset();
  }

  cancelSelection(): void { this.selectedFlight = null; }

  bookFlight(): void {
    if (this.bookingForm.invalid || !this.selectedFlight) { this.bookingForm.markAllAsTouched(); return; }
    this.bookingLoading = true; this.bookingError = '';
    this.bookingSvc.book({
      flightNumber: this.selectedFlight.flightNumber,
      travelDate:   this.bookingForm.value.travelDate
    }).subscribe({
      next: () => {
        this.bookingLoading = false;
        this.bookingSuccess = `Flight ${this.selectedFlight!.flightNumber} booked successfully!`;
        this.selectedFlight = null;
        this.loadMyBookings();
      },
      error: err => {
        this.bookingLoading = false;
        this.bookingError = err.error?.message ?? 'Booking failed. Please try again.';
      }
    });
  }

  logout(): void { this.auth.logout(); }
}
