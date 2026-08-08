export type FlightStatus = 'OnTime' | 'Delayed' | 'Cancelled' | 'Diverted' | 'Unknown';

export interface FlightStatusResult {
  flightNumber: string;
  date: string;
  status: FlightStatus;
  scheduledDeparture: string | null;
  actualDeparture: string | null;
  scheduledArrival: string | null;
  actualArrival: string | null;
  terminal: string | null;
  gate: string | null;
  delayReason: string | null;
  lastUpdatedUtc: string;
  sourceProvider: string;
  message: string | null;
}

export interface FlightStatusRequest {
  flightNumber: string;
  date: string;
}
