import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FlightStatusService } from '../../services/flight-status.service';
import { FlightInfo, FlightStatusRequest, FlightStatusResult, FlightStatus } from '../../models/flight-status.models';
import { of } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface FlightRow {
  meta: FlightInfo;
  result: FlightStatusResult | null;
  loading: boolean;
  error: boolean;
}

@Component({
  selector: 'app-flight-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './flight-list.component.html',
  styleUrls: ['./flight-list.component.css']
})
export class FlightListComponent implements OnInit {
  @Output() selectFlight = new EventEmitter<FlightStatusRequest>();

  today = new Date().toISOString().split('T')[0];
  rows: FlightRow[] = [];
  catalogLoading = true;
  catalogError = false;

  filters = {
    flightNumber: '',
    route: '',
    status: '' as FlightStatus | '',
    terminal: '',
    gate: '',
    delayReason: ''
  };

  readonly statusOptions: Array<{ value: FlightStatus | ''; label: string }> = [
    { value: '',          label: 'All'       },
    { value: 'OnTime',    label: 'On Time'   },
    { value: 'Delayed',   label: 'Delayed'   },
    { value: 'Cancelled', label: 'Cancelled' },
    { value: 'Diverted',  label: 'Diverted'  },
    { value: 'Unknown',   label: 'Unknown'   },
  ];

  get terminalOptions(): string[] {
    const vals = this.rows.map(r => r.result?.terminal ?? '').filter(Boolean);
    return ['', ...Array.from(new Set(vals)).sort()];
  }

  get filteredRows(): FlightRow[] {
    const f = this.filters;
    return this.rows.filter(row => {
      if (f.flightNumber && !row.meta.flightNumber.toLowerCase().includes(f.flightNumber.toLowerCase())) return false;
      if (f.route      && !row.meta.route.toLowerCase().includes(f.route.toLowerCase())) return false;
      if (f.status     && row.result?.status !== f.status) return false;
      if (f.terminal   && (row.result?.terminal ?? '').toLowerCase() !== f.terminal.toLowerCase()) return false;
      if (f.gate       && !(row.result?.gate ?? '').toLowerCase().includes(f.gate.toLowerCase())) return false;
      if (f.delayReason && !(row.result?.delayReason ?? '').toLowerCase().includes(f.delayReason.toLowerCase())) return false;
      return true;
    });
  }

  get activeFilterCount(): number {
    return Object.values(this.filters).filter(v => v !== '').length;
  }

  clearFilters(): void {
    this.filters = { flightNumber: '', route: '', status: '', terminal: '', gate: '', delayReason: '' };
  }

  constructor(private flightStatusService: FlightStatusService) {}

  ngOnInit(): void {
    this.flightStatusService.getFlights().pipe(
      catchError(() => {
        this.catalogError = true;
        this.catalogLoading = false;
        return of([] as FlightInfo[]);
      })
    ).subscribe(flights => {
      this.catalogLoading = false;
      this.rows = flights.map(meta => ({ meta, result: null, loading: true, error: false }));
      flights.forEach((meta, i) => {
        this.flightStatusService.getStatus(meta.flightNumber, this.today)
          .pipe(catchError(() => of(null)))
          .subscribe(result => {
            this.rows[i] = { meta, result, loading: false, error: result === null };
          });
      });
    });
  }

  onRowClick(row: FlightRow): void {
    this.selectFlight.emit({ flightNumber: row.meta.flightNumber, date: this.today });
  }

  statusClass(status: FlightStatus | undefined): string {
    switch (status) {
      case 'OnTime':    return 'status-on-time';
      case 'Delayed':   return 'status-delayed';
      case 'Cancelled': return 'status-cancelled';
      case 'Diverted':  return 'status-diverted';
      default:          return 'status-unknown';
    }
  }

  statusLabel(status: FlightStatus | undefined): string {
    switch (status) {
      case 'OnTime':    return 'On Time';
      case 'Delayed':   return 'Delayed';
      case 'Cancelled': return 'Cancelled';
      case 'Diverted':  return 'Diverted';
      default:          return 'Unknown';
    }
  }

  formatTime(iso: string | null | undefined): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', timeZone: 'UTC' });
  }
}
