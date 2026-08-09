// Shared response shape returned by GET /flights/status
export interface FlightStatus {
  flightNumber: string;
  date: string;
  status: string;
  scheduledDeparture?: string;
  actualDeparture?: string;
  scheduledArrival?: string;
  actualArrival?: string;
  terminal?: string;
  gate?: string;
  delayReason?: string;
  lastUpdatedUtc: string;
  sourceProvider: string;
  message?: string;
}

// Catalog entry returned by GET /flights
export interface FlightInfo {
  flightNumber: string;
  route: string;
  origin: string;
  destination: string;
}

export const fmtTime = (iso?: string): string =>
  iso
    ? new Date(iso).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', timeZone: 'UTC' }) + ' UTC'
    : '—';
