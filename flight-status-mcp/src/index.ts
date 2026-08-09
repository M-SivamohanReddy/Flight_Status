import { McpServer } from '@modelcontextprotocol/server';
import { StdioServerTransport } from '@modelcontextprotocol/server/stdio';
import * as z from 'zod/v4';

const API_BASE = process.env['FLIGHT_API_URL'] ?? 'http://localhost:5000';

const server = new McpServer({
  name: 'skyroute-flight-status',
  version: '1.0.0',
});

// ── Tool: list_flights ────────────────────────────────────────────────────────
server.registerTool(
  'list_flights',
  {
    description: 'List all available SkyRoute flights with their route names and IATA airport codes.',
    inputSchema: z.object({}),
  },
  async () => {
    const res = await fetch(`${API_BASE}/flights`);
    if (!res.ok) {
      return {
        content: [{ type: 'text' as const, text: `Error: ${res.status} ${res.statusText}` }],
        isError: true,
      };
    }
    const flights: { flightNumber: string; route: string; origin: string; destination: string }[] = await res.json();
    const lines = flights.map(f => `  • ${f.flightNumber.padEnd(7)} ${f.route}  (${f.origin} → ${f.destination})`);
    return {
      content: [{
        type: 'text' as const,
        text: `SkyRoute fleet — ${flights.length} flights available:\n\n${lines.join('\n')}`,
      }],
    };
  }
);

// ── Tool: check_flight_status ─────────────────────────────────────────────────
server.registerTool(
  'check_flight_status',
  {
    description: [
      'Check the real-time status of a SkyRoute flight.',
      'Returns: status (OnTime / Delayed / Cancelled / Diverted / Unknown), scheduled and actual departure/arrival times,',
      'terminal, gate, delay reason, and the data provider.',
      'The date defaults to today if omitted.',
    ].join(' '),
    inputSchema: z.object({
      flightNumber: z.string()
        .describe('IATA-style flight number, e.g. AA100 or BA200. 2–3 uppercase letters + 1–4 digits.'),
      date: z.string()
        .default(new Date().toISOString().split('T')[0])
        .describe('Travel date in yyyy-MM-dd format, e.g. 2026-08-10. Defaults to today.'),
    }),
  },
  async ({ flightNumber, date }) => {
    const url = `${API_BASE}/flights/status?flightNumber=${encodeURIComponent(flightNumber)}&date=${encodeURIComponent(date)}`;
    const res = await fetch(url);

    if (!res.ok) {
      const body = await res.text().catch(() => '');
      return {
        content: [{ type: 'text' as const, text: `Error checking ${flightNumber}: ${res.status}\n${body}` }],
        isError: true,
      };
    }

    const s: {
      flightNumber: string; date: string; status: string;
      scheduledDeparture?: string; actualDeparture?: string;
      scheduledArrival?: string;  actualArrival?: string;
      terminal?: string; gate?: string; delayReason?: string;
      lastUpdatedUtc: string; sourceProvider: string; message?: string;
    } = await res.json();

    const fmtTime = (iso?: string) => iso ? new Date(iso).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', timeZone: 'UTC' }) + ' UTC' : '—';

    let text = `Flight ${s.flightNumber}  ·  ${s.date}\n`;
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

async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
}

main().catch(console.error);
