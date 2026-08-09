import { StdioServerTransport } from '@modelcontextprotocol/server/stdio';
import { createServer } from './server.js';

async function main(): Promise<void> {
  const server = createServer();
  const transport = new StdioServerTransport();
  await server.connect(transport);
}

main().catch(console.error);
