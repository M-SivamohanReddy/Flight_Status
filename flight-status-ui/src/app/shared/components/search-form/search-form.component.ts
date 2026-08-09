import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { FlightStatusRequest } from '../../../models/flight-status.models';

const FLIGHT_NUMBER_PATTERN = /^[A-Za-z]{2,3}\d{1,4}$/;

@Component({
  selector: 'app-search-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './search-form.component.html',
  styleUrls: ['./search-form.component.css']
})
export class SearchFormComponent {
  @Output() search = new EventEmitter<FlightStatusRequest>();

  form: FormGroup;
  loading = false;

  constructor(private fb: FormBuilder) {
    this.form = this.fb.group({
      flightNumber: ['', [Validators.required, Validators.pattern(FLIGHT_NUMBER_PATTERN)]],
      date: ['', [Validators.required]]
    });
  }

  get flightNumber() { return this.form.get('flightNumber')!; }
  get date() { return this.form.get('date')!; }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.search.emit({
      flightNumber: this.form.value.flightNumber.trim().toUpperCase(),
      date: this.form.value.date
    });
  }

  setLoading(value: boolean): void {
    this.loading = value;
    value ? this.form.disable() : this.form.enable();
  }
}
