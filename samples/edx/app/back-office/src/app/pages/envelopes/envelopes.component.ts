import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiClient, toApiError } from '../../core/api';
import { InboundEnvelope, OutboundEnvelope, Paged } from '../../core/models';

const outboundText: Record<string, string> = { ToSend: 'Gönderilecek', Sent: 'Merkezde', Delivered: 'İletildi', Refused: 'Merkez reddetti', Failed: 'Başarısız' };
const outboundClass: Record<string, string> = { ToSend: 'warn', Sent: 'warn', Delivered: 'ok', Refused: 'danger', Failed: 'danger' };
const inboundText: Record<string, string> = { Queued: 'Sırada', Accepted: 'Kabul edildi', Refused: 'Reddedildi', Applied: 'İşlendi', Ignored: 'Yok sayıldı' };
const inboundClass: Record<string, string> = { Queued: 'warn', Accepted: 'ok', Refused: 'danger', Applied: 'ok', Ignored: '' };

/**
 * The envelopes of the tenant, in both directions: what the integration service handed to the center for it (its invoices, and the answers of its post box about invoices that came in), and what the center delivered to its post box (invoices for it, and system responses about
 * its envelopes). Each says where it is, the code of the center (or of the post box) and, when it failed, where and why.
 */
@Component({
  selector: 'app-envelopes',
  imports: [FormsModule, DatePipe],
  template: `
    <div class="page-header">
      <div>
        <h1>Zarflar</h1>
        <div class="muted">Merkeze giden ve merkezden gelen zarflar.</div>
      </div>
      <button type="button" (click)="load()">Yenile</button>
    </div>

    @if (error()) {
      <div class="alert error">{{ error() }}</div>
    }

    <div class="tabs">
      <button type="button" [class.active]="direction() === 'outbound'" (click)="show('outbound')">Giden</button>
      <button type="button" [class.active]="direction() === 'inbound'" (click)="show('inbound')">Gelen</button>
    </div>

    <div class="card">
      <div class="toolbar">
        <select [(ngModel)]="state" (change)="search()">
          <option value="">Tüm durumlar</option>
          @for (s of states(); track s) {
            <option [value]="s">{{ text(s) }}</option>
          }
        </select>
        <button type="button" (click)="search()">Ara</button>
      </div>
      @if (direction() === 'outbound') {
        <table>
          <thead>
            <tr><th>Tür</th><th>Fatura no</th><th>Alıcı</th><th>Durum</th><th>Merkez kodu</th><th>Zarf</th><th>Tarih</th></tr>
          </thead>
          <tbody>
            @for (envelope of outbound(); track envelope.id) {
              <tr>
                <td>{{ envelope.kind === 'Invoice' ? 'Fatura' : 'Sistem yanıtı' }}</td>
                <td>{{ envelope.invoiceNumber ?? '' }}</td>
                <td>{{ envelope.receiverTitle }} <span class="muted">{{ envelope.receiverRegisterNumber }}</span></td>
                <td>
                  <span class="badge" [class.ok]="cls(envelope.state) === 'ok'" [class.warn]="cls(envelope.state) === 'warn'" [class.danger]="cls(envelope.state) === 'danger'">{{ text(envelope.state) }}</span>
                  @if (envelope.failureReason) {
                    <div class="muted reason">{{ envelope.failureStage }}: {{ envelope.failureReason }}</div>
                  }
                </td>
                <td>{{ envelope.statusCode }} <span class="muted">{{ envelope.statusDescription }}</span></td>
                <td class="muted">{{ envelope.envelopeId.slice(0, 8) }}…</td>
                <td>{{ envelope.statusChangedAt | date: 'dd.MM.yyyy HH:mm:ss' }}</td>
              </tr>
            } @empty {
              <tr><td colspan="7" class="muted">Gösterilecek zarf yok.</td></tr>
            }
          </tbody>
        </table>
      } @else {
        <table>
          <thead>
            <tr><th>Tür</th><th>Gönderen</th><th>Fatura no</th><th>Durum</th><th>Yanıt kodu</th><th>Zarf</th><th>Geliş</th></tr>
          </thead>
          <tbody>
            @for (envelope of inbound(); track envelope.id) {
              <tr>
                <td>{{ envelope.documentType === 'SENDERENVELOPE' ? 'Fatura' : envelope.documentType === 'SYSTEMENVELOPE' ? 'Sistem yanıtı' : (envelope.documentType ?? '') }}</td>
                <td>{{ envelope.senderTitle ?? '' }} <span class="muted">{{ envelope.senderRegisterNumber ?? '' }}</span></td>
                <td>{{ envelope.invoiceNumber ?? '' }}</td>
                <td><span class="badge" [class.ok]="cls(envelope.state) === 'ok'" [class.warn]="cls(envelope.state) === 'warn'" [class.danger]="cls(envelope.state) === 'danger'">{{ text(envelope.state) }}</span></td>
                <td>{{ envelope.statusCode }} <span class="muted reason">{{ envelope.statusDescription }}</span></td>
                <td class="muted">{{ envelope.envelopeId.slice(0, 8) }}…</td>
                <td>{{ envelope.receivedAt | date: 'dd.MM.yyyy HH:mm:ss' }}</td>
              </tr>
            } @empty {
              <tr><td colspan="7" class="muted">Gösterilecek zarf yok.</td></tr>
            }
          </tbody>
        </table>
      }
      <div class="pager">
        <span class="muted">{{ total() }} kayıt</span>
        <button type="button" class="small" [disabled]="page() <= 1" (click)="go(page() - 1)">‹</button>
        <span>{{ page() }} / {{ pages() }}</span>
        <button type="button" class="small" [disabled]="page() >= pages()" (click)="go(page() + 1)">›</button>
      </div>
    </div>
  `,
  styles: [
    `
      .tabs {
        display: flex;
        gap: 8px;
        margin-bottom: 12px;
      }
      .tabs button.active {
        background: var(--primary, #2563eb);
        color: #fff;
      }
    `,
  ],
})
export class EnvelopesComponent implements OnInit {
  private readonly api = inject(ApiClient);
  private readonly size = 10;

  protected readonly direction = signal<'outbound' | 'inbound'>('outbound');
  protected readonly outbound = signal<OutboundEnvelope[]>([]);
  protected readonly inbound = signal<InboundEnvelope[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly error = signal<string | null>(null);

  protected state = '';

  protected states(): string[] {
    return Object.keys(this.direction() === 'outbound' ? outboundText : inboundText);
  }

  protected text(state: string): string {
    return (this.direction() === 'outbound' ? outboundText : inboundText)[state] ?? state;
  }

  protected cls(state: string): string {
    return (this.direction() === 'outbound' ? outboundClass : inboundClass)[state] ?? '';
  }

  protected pages(): number {
    return Math.max(1, Math.ceil(this.total() / this.size));
  }

  ngOnInit(): void {
    void this.load();
  }

  protected show(direction: 'outbound' | 'inbound'): void {
    this.direction.set(direction);
    this.state = '';
    this.page.set(1);
    void this.load();
  }

  protected search(): void {
    this.page.set(1);
    void this.load();
  }

  protected go(page: number): void {
    this.page.set(page);
    void this.load();
  }

  protected async load(): Promise<void> {
    this.error.set(null);
    const body = { pageNumber: this.page(), maxResultCount: this.size, state: this.state || null };
    try {
      if (this.direction() === 'outbound') {
        const result = await firstValueFrom(this.api.post<Paged<OutboundEnvelope>>('integration-service/v1/envelopes/outbound/paged-list', body));
        this.outbound.set(result.items);
        this.total.set(result.dataTotalCount);
      } else {
        const result = await firstValueFrom(this.api.post<Paged<InboundEnvelope>>('integration-service/v1/envelopes/inbound/paged-list', body));
        this.inbound.set(result.items);
        this.total.set(result.dataTotalCount);
      }
    } catch (error) {
      this.outbound.set([]);
      this.inbound.set([]);
      this.total.set(0);
      this.error.set(toApiError(error).text);
    }
  }
}
