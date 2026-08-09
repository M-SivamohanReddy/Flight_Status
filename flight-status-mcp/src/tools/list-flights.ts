import type { McpServer } from '@modelcontextprotocol/server';
import { ApiError, type SkyRouteApiClient } from '../api-client.js';

export function registerListFlights(server: McpServer, client: SkyRouteApiClient): void {
  server.registerTool(
    'list_flights',
    {
      description: 'List all available SkyRoute flights with their route names and IATA airport codes.',
    },
    async () => {
      try {
        const flights = await client.getFlights();
        const lines = flights.map(
          f => `  * ${f.flightNumber.padEnd(7)} ${f.route}  (${f.origin} -> ${f.destination})`
        );
        return {
          content: [{
            type: 'text' as const,
            text: `SkyRoute fleet -- ${flights.length} flights available:\n\n${lines.join('\n')}`,
          }],
        };
      } catch (err) {
        const msg = err instanceof ApiError ? err.message : 'Unexpected error fetching flights.';
        return { content: [{ type: 'text' as const, text: msg }], isError: true };
      }
    }
  );
}
