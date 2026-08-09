import type { McpServer } from '@modelcontextprotocol/server';
import * as z from 'zod/v4';
import { fmtTime, type FlightStatus } from '../types.js';

const inputSchema = {
  flightNumber: z.string()
    .describe('IATA-style flight number, e.g. AA100 or BA200. 2-3 uppercase letters + 1-4 digits.'),
  date: z.string()
    .describe('Travel date in yyyy-MM-dd format. Defaults to today if omitted.'),
};

export function registerCheckFlightStatus(server: McpServer, apiBase: string): void {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (server as any).registerTool(
    'check_flight_status',
    {
      description: [
        'Check the real-time status of a SkyRoute flight.',
        'Returns: status (OnTime / Delayed / Cancelled / Diverted / Unknown),',
        'scheduled and actual departure/arrival times, terminal, gate, delay reason, and source provider.',
        'The date defaults to today if omitted.',
      ].join(' '),
      inputSchema,
    },
    async (args: Record<string, unknown>) => {
      const flightNumber = (args['flightNumber'] ?? '') as string;
      const date = (args['date'] as string | undefined) ?? new Date().toISOString().split('T')[0];

      const url = `${apiBase}/flights/status?flightNumber=${encodeURIComponent(flightNumber)}&date=${encodeURIComponent(date)}`;
      const res = await fetch(url);

      if (!res.ok) {
        const body = await res.text().catch(() => '');
        return {
          content: [{ type: 'text' as const, text: `Error checking ${flightNumber}: ${res.status}\n${body}` }],
          isError: true,
        };
      }

      const s: FlightStatus = await res.json();

      let text = `Flight ${s.flightNumber}  |  ${s.date}\n`;
      text += `Status:   ${s.status}\n`;
      text += `\nDeparture\n`;
      text += `  Scheduled : ${fmtTime(s.scheduledDeparture)}\n`;
      text += `  Actual    : ${fmtTime(s.actualDeparture)}\n`;
      text += `\nArrival\n`;
      text += `  Scheduled : ${fmtTime(s.scheduledArrival)}\n`;
      text += `  Actual    : ${fmtTime(s.actualArrival)}\n`;
      if (s.terminal)    text += `\nTerminal  : ${s.terminal}`;
      if (s.gate)        text += `\nGate      : ${s.gate}`;
      if (s.delayReason) text += `\nDelay     : ${s.delayReason}`;
      if (s.message)     text += `\nNote      : ${s.message}`;
      text += `\n\nData from : ${s.sourceProvider}  (updated ${fmtTime(s.lastUpdatedUtc)})`;

      return { content: [{ type: 'text' as const, text }] };
    }
  );
}
