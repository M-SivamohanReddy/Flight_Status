import { McpServer } from '@modelcontextprotocol/server';
import { registerListFlights } from './tools/list-flights.js';
import { registerCheckFlightStatus } from './tools/check-flight-status.js';

export const API_BASE = process.env['FLIGHT_API_URL'] ?? 'http://localhost:5000';

export function createServer(): McpServer {
  const server = new McpServer({
    name: 'skyroute-flight-status',
    version: '1.0.0',
  });

  registerListFlights(server, API_BASE);
  registerCheckFlightStatus(server, API_BASE);

  return server;
}
