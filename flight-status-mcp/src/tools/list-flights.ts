import type { McpServer } from '@modelcontextprotocol/server';
import type { FlightInfo } from '../types.js';

export function registerListFlights(server: McpServer, apiBase: string): void {
  server.registerTool(
    'list_flights',
    {
      description: 'List all available SkyRoute flights with their route names and IATA airport codes.',
    },
    async () => {
      const res = await fetch(`${apiBase}/flights`);
      if (!res.ok) {
        return {
          content: [{ type: 'text' as const, text: `Error: ${res.status} ${res.statusText}` }],
          isError: true,
        };
      }

      const flights: FlightInfo[] = await res.json();
      const lines = flights.map(
        f => `  • ${f.flightNumber.padEnd(7)} ${f.route}  (${f.origin} -> ${f.destination})`
      );

      return {
        content: [{
          type: 'text' as const,
          text: `SkyRoute fleet -- ${flights.length} flights available:\n\n${lines.join('\n')}`,
        }],
      };
    }
  );
}
