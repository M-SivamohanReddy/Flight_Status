import { Component, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FlightStatusService } from '../../services/flight-status.service';
import { FlightStatusRequest, FlightStatusResult } from '../../models/flight-status.models';
import { SearchFormComponent } from '../search-form/search-form.component';
import { ResultCardComponent } from '../result-card/result-card.component';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, SearchFormComponent, ResultCardComponent],
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.css']
})
export class LandingComponent {
  @ViewChild(SearchFormComponent) searchForm!: SearchFormComponent;

  result: FlightStatusResult | null = null;
  errorMessage: string | null = null;

  constructor(private router: Router, private flightSvc: FlightStatusService) {}

  goLogin()    { this.router.navigate(['/login']); }
  goRegister() { this.router.navigate(['/register']); }

  onSearch(req: FlightStatusRequest): void {
    this.result = null;
    this.errorMessage = null;
    this.searchForm.setLoading(true);
    this.flightSvc.getStatus(req.flightNumber, req.date).subscribe({
      next: data => { this.result = data; this.searchForm.setLoading(false); },
      error: err  => {
        this.errorMessage = err.status === 400
          ? 'Invalid flight number or date format.'
          : 'Could not retrieve flight status. Please try again.';
        this.searchForm.setLoading(false);
      }
    });
  }
}
