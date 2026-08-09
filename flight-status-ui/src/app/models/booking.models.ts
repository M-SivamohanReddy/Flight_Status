export interface BookingRequest  { flightNumber: string; travelDate: string; }
export interface BookingResponse {
  id: number; flightNumber: string; route: string; origin: string;
  destination: string; travelDate: string; bookedAtUtc: string;
  userEmail?: string; userFullName?: string;
}
