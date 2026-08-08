import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FlightStatusResult } from '../../models/flight-status.models';

@Component({
  selector: 'app-result-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './result-card.component.html',
  styleUrls: ['./result-card.component.css']
})
export class ResultCardComponent {
  @Input() result!: FlightStatusResult;

  get statusClass(): string {
    switch (this.result.status) {
      case 'OnTime':    return 'status-on-time';
      case 'Delayed':   return 'status-delayed';
      case 'Cancelled': return 'status-cancelled';
      case 'Diverted':  return 'status-diverted';
      default:          return 'status-unknown';
    }
  }

  get statusLabel(): string {
    switch (this.result.status) {
      case 'OnTime':    return 'On Time';
      case 'Delayed':   return 'Delayed';
      case 'Cancelled': return 'Cancelled';
      case 'Diverted':  return 'Diverted';
      default:          return 'Unknown';
    }
  }

  formatDateTime(iso: string | null): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleString(undefined, {
      dateStyle: 'short',
      timeStyle: 'short',
      timeZone: 'UTC'
    }) + ' UTC';
  }
}
