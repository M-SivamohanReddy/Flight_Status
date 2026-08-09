import { Component, OnInit, AfterViewChecked, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { FlightInfo, FlightStatusResult } from '../../models/flight-status.models';
import { environment } from '../../../environments/environment';

interface ChatMsg {
  id: number;
  role: 'user' | 'bot';
  type: 'text' | 'flights' | 'status' | 'error';
  html: SafeHtml;
  loading: boolean;
  flights?: FlightInfo[];
  status?: FlightStatusResult;
}

@Component({
  selector: 'app-chatbot',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chatbot.component.html',
  styleUrls: ['./chatbot.component.css']
})
export class ChatbotComponent implements OnInit, AfterViewChecked {
  @ViewChild('msgArea') msgArea!: ElementRef<HTMLElement>;

  isOpen = false;
  userInput = '';
  messages: ChatMsg[] = [];
  private nextId = 0;

  readonly today = new Date().toISOString().split('T')[0];
  readonly api   = environment.apiUrl;

  readonly suggestions = ['List flights', 'Check AA100', 'Check AA200', 'Help'];

  constructor(private http: HttpClient, private san: DomSanitizer) {}

  ngOnInit(): void {
    this.push({
      role: 'bot', type: 'text', loading: false,
      html: this.h(`Hi! I'm <strong>SkyRoute Assistant</strong> ✈<br>
I can help you:<br>
&nbsp;• <em>list flights</em> — see all routes<br>
&nbsp;• <em>check AA100</em> — check a flight's live status<br>
&nbsp;• <em>AA200 on 2026-08-10</em> — check for a specific date<br><br>
<strong>What would you like to know?</strong>`)
    });
  }

  ngAfterViewChecked(): void {
    if (this.msgArea) {
      const el = this.msgArea.nativeElement;
      el.scrollTop = el.scrollHeight;
    }
  }

  toggle(): void { this.isOpen = !this.isOpen; }

  send(): void {
    const text = this.userInput.trim();
    if (!text) return;
    this.userInput = '';
    this.push({ role: 'user', type: 'text', loading: false, html: this.h(this.esc(text)) });
    this.process(text);
  }

  useSuggestion(s: string): void {
    this.push({ role: 'user', type: 'text', loading: false, html: this.h(this.esc(s)) });
    this.process(s);
  }

  checkFlight(flightNumber: string): void {
    this.useSuggestion(`check ${flightNumber}`);
  }

  getStatusClass(s: string | undefined): string {
    switch (s) {
      case 'OnTime':    return 'sm-on-time';
      case 'Delayed':   return 'sm-delayed';
      case 'Cancelled': return 'sm-cancelled';
      case 'Diverted':  return 'sm-diverted';
      default:          return 'sm-unknown';
    }
  }

  getStatusIcon(s: string | undefined): string {
    switch (s) {
      case 'OnTime':    return '✅';
      case 'Delayed':   return '⏰';
      case 'Cancelled': return '❌';
      case 'Diverted':  return '↗️';
      default:          return '❓';
    }
  }

  getStatusLabel(s: string | undefined): string {
    switch (s) {
      case 'OnTime': return 'On Time';
      default:       return s ?? 'Unknown';
    }
  }

  fmtTime(iso: string | null | undefined): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', timeZone: 'UTC' }) + ' UTC';
  }

  // ── NLP message processing ─────────────────────────────────────────────────

  private process(text: string): void {
    const n = text.toLowerCase().trim();
    const lid = this.addLoading();

    // List flights
    if (/\b(list|show|all|available|which|what)\b.*\bflight/.test(n) ||
        n === 'flights' || n === 'list flights' || n === 'show flights') {
      this.fetchFlights(lid);
      return;
    }

    // Flight number match (e.g. AA100, BA1234)
    const fm = text.match(/\b([A-Za-z]{2,3}\d{1,4})\b/i);
    if (fm) {
      const fn   = fm[1].toUpperCase();
      const dm   = text.match(/\b(\d{4}-\d{2}-\d{2})\b/);
      const date = dm ? dm[1] : this.today;
      this.fetchStatus(fn, date, lid);
      return;
    }

    // Help
    if (/^help$/.test(n) || /what can you/.test(n)) {
      setTimeout(() => this.replace(lid, {
        role: 'bot', type: 'text', loading: false,
        html: this.h(`<strong>What I can do:</strong><br><br>
✈ <strong>List flights</strong> — "list available flights"<br>
🔍 <strong>Check status</strong> — "check AA100" or just type "AA200"<br>
📅 <strong>By date</strong> — "AA100 on 2026-08-10"<br><br>
Click any flight chip to instantly check its status!`)
      }), 350);
      return;
    }

    setTimeout(() => this.replace(lid, {
      role: 'bot', type: 'text', loading: false,
      html: this.h(`I didn't understand that. Try:<br>
&nbsp;• <em>list flights</em><br>
&nbsp;• <em>check AA100</em><br>
&nbsp;• <em>help</em>`)
    }), 300);
  }

  private fetchFlights(lid: number): void {
    this.http.get<FlightInfo[]>(`${this.api}/flights`).subscribe({
      next: flights => {
        this.replace(lid, {
          role: 'bot', type: 'flights', loading: false, flights,
          html: this.h(`Found <strong>${flights.length} flights</strong> on SkyRoute.<br>
<small>Click any flight to check its status.</small>`)
        });
      },
      error: () => this.replace(lid, {
        role: 'bot', type: 'error', loading: false,
        html: this.h('⚠ Could not fetch flights. Make sure the API is running on port 5000.')
      })
    });
  }

  private fetchStatus(flightNumber: string, date: string, lid: number): void {
    this.http.get<FlightStatusResult>(
      `${this.api}/flights/status?flightNumber=${flightNumber}&date=${date}`
    ).subscribe({
      next: status => {
        this.replace(lid, {
          role: 'bot', type: 'status', loading: false, status,
          html: this.h(`Status for <strong>${flightNumber}</strong> on ${date}:`)
        });
      },
      error: err => {
        const msg = err.status === 400
          ? `⚠ <strong>${flightNumber}</strong> isn't a recognised flight number. Try AA100–AA1400.`
          : `⚠ Could not retrieve status for ${flightNumber}. Check the API is running.`;
        this.replace(lid, { role: 'bot', type: 'error', loading: false, html: this.h(msg) });
      }
    });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────

  private addLoading(): number {
    return this.push({ role: 'bot', type: 'text', loading: true, html: this.h('') });
  }

  private push(partial: Omit<ChatMsg, 'id'>): number {
    const id = this.nextId++;
    this.messages.push({ id, ...partial });
    return id;
  }

  private replace(id: number, partial: Omit<ChatMsg, 'id'>): void {
    const i = this.messages.findIndex(m => m.id === id);
    if (i !== -1) this.messages[i] = { id, ...partial };
  }

  private h(raw: string): SafeHtml { return this.san.bypassSecurityTrustHtml(raw); }
  private esc(s: string): string {
    return s.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
  }
}
