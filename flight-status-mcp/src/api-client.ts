import type { FlightInfo, FlightStatus } from './types.js';

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly statusText: string,
    public readonly body = ''
  ) {
    super(`HTTP ${status} ${statusText}${body ? `: ${body}` : ''}`);
  }
}

/** Typed HTTP client for the SkyRoute .NET API. */
export class SkyRouteApiClient {
  constructor(private readonly baseUrl: string) {}

  async getFlights(): Promise<FlightInfo[]> {
    const res = await fetch(`${this.baseUrl}/flights`);
    if (!res.ok) throw new ApiError(res.status, res.statusText);
    return res.json() as Promise<FlightInfo[]>;
  }

  async getFlightStatus(flightNumber: string, date: string): Promise<FlightStatus> {
    const url = new URL(`${this.baseUrl}/flights/status`);
    url.searchParams.set('flightNumber', flightNumber);
    url.searchParams.set('date', date);

    const res = await fetch(url.toString());
    if (!res.ok) {
      const body = await res.text().catch(() => '');
      throw new ApiError(res.status, res.statusText, body);
    }
    return res.json() as Promise<FlightStatus>;
  }
}
