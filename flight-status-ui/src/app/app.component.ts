import { Component, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { SearchFormComponent } from './components/search-form/search-form.component';
import { ResultCardComponent } from './components/result-card/result-card.component';
import { FlightStatusService } from './services/flight-status.service';
import { FlightStatusRequest, FlightStatusResult } from './models/flight-status.models';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, SearchFormComponent, ResultCardComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  @ViewChild(SearchFormComponent) searchForm!: SearchFormComponent;

  result: FlightStatusResult | null = null;
  errorMessage: string | null = null;

  constructor(private flightStatusService: FlightStatusService) {}

  onSearch(request: FlightStatusRequest): void {
    this.result = null;
    this.errorMessage = null;
    this.searchForm.setLoading(true);

    this.flightStatusService.getStatus(request.flightNumber, request.date).subscribe({
      next: (data) => {
        this.result = data;
        this.searchForm.setLoading(false);
      },
      error: (err) => {
        this.errorMessage = err.status === 0
          ? 'Could not connect to the API. Make sure the backend is running on http://localhost:5000.'
          : `Error ${err.status}: ${err.error?.errors ? JSON.stringify(err.error.errors) : 'An unexpected error occurred.'}`;
        this.searchForm.setLoading(false);
      }
    });
  }
}

