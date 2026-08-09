import type { McpServer } from '@modelcontextprotocol/server';
import * as z from 'zod/v4';
import { ApiError, type SkyRouteApiClient } from '../api-client.js';

const inputSchema = {
  query: z.string()
    .describe(
      'Search term matched against route name, origin IATA code, or destination IATA code. ' +
      'Examples: "London", "JFK", "Tokyo", "AA1".'
    ),
};

export function registerSearchByRoute(server: McpServer, client: SkyRouteApiClient): void {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (server as any).registerTool(
    'search_by_route',
    {
      description:
        'Search SkyRoute flights by route name, origin airport, or destination airport. ' +
        'Returns all matching flights. Use check_flight_status to get the live status of a result.',
      inputSchema,
    },
    async (args: Record<string, unknown>) => {
      const query = ((args['query'] ?? '') as string).trim();

      if (!query) {
        return {
          content: [{ type: 'text' as const, text: 'Please provide a search term (e.g. "London", "JFK", "AA1").' }],
          isError: true,
        };
      }

      try {
        const matches = await client.searchFlightsByRoute(query);

        if (matches.length === 0) {
          return {
            content: [{
              type: 'text' as const,
              text: `No flights found matching "${query}". Try an airport code (JFK, LHR) or city name (London, Tokyo).`,
            }],
          };
        }

        const lines = matches.map(
          f => `  * ${f.flightNumber.padEnd(7)} ${f.route}  (${f.origin} -> ${f.destination})`
        );

        return {
          content: [{
            type: 'text' as const,
            text: `Found ${matches.length} flight${matches.length === 1 ? '' : 's'} matching "${query}":\n\n${lines.join('\n')}\n\nUse check_flight_status with any flight number above to see its live status.`,
          }],
        };
      } catch (err) {
        const msg = err instanceof ApiError ? err.message : `Unexpected error searching for "${query}".`;
        return { content: [{ type: 'text' as const, text: msg }], isError: true };
      }
    }
  );
}
