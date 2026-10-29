import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiClient, toApiError } from '../../core/api';
import { Invoice, Paged } from '../../core/models';
import { SessionService } from '../../core/session.service';
import { EnvelopeDetailComponent } from '../../shared/envelope-detail.component';

const statusText: Record<string, string> = { Processing: 'İşleniyor', Failed: 'Hatalı', DraftCreated: 'Taslak', Rejected: 'Reddedildi', Approved: 'Onaylandı', Signed: 'İmzalandı' };
const statusClass: Record<string, string> = { Processing: 'warn', Failed: 'danger', DraftCreated: 'warn', Rejected: 'danger', Approved: 'warn', Signed: 'ok' };
// the envelope's side: where it is at the center (the invoice's own life ends when it is signed)
const envelopeText: Record<string, string> = { None: '—', ToSend: 'Gönderilecek', Sent: 'Merkezde', Delivered: 'Alıcıya iletildi', Refused: 'Merkez reddetti', Failed: 'İletilemedi' };
const envelopeClass: Record<string, string> = { None: '', ToSend: 'warn', Sent: 'warn', Delivered: 'ok', Refused: 'danger', Failed: 'danger' };

/**
 * The sales invoices, every record of them: the ones being processed, the failed and rejected ones (with the reason), drafts, approved and signed. The page reads with `endpoint.invoice.read` (required); what the user may not see comes masked from the service (price and
 * tax id are operations), and what it may see at most is the limit of its role (a constraint), shown above the list. Drafts are approved on their own page; the XML of an invoice is read here by who may.
 */
@Component({
  selector: 'app-invoices',
  imports: [FormsModule, DatePipe, DecimalPipe, EnvelopeDetailComponent],
  template: `
    <div class="page-header">
      <div>
        <h1>Satış faturaları</h1>
        <div class="muted">
          @if (session.valueOf('constraint.invoice.max-view-amount'); as limit) { Görüntüleme limiti: {{ +limit | number }} }
          @if (session.valueOf('constraint.invoice.max-create-amount'); as limit) { · Oluşturma limiti: {{ +limit | number }} }
          @if (session.valueOf('constraint.invoice.max-approve-amount'); as limit) { · Onay limiti: {{ +limit | number }} }
        </div>
      </div>
      <button type="button" (click)="load()">Yenile</button>
    </div>

    @if (error()) {
      <div class="alert error">{{ error() }}</div>
    }

    <div class="card">
      <div class="toolbar">
        <input placeholder="Fatura no" [(ngModel)]="number" (keyup.enter)="search()" />
        <input placeholder="Taraf adı" [(ngModel)]="partyName" (keyup.enter)="search()" />
        <select [(ngModel)]="status" (change)="search()">
          <option value="">Tüm durumlar</option>
          @for (s of statuses; track s) {
            <option [value]="s">{{ text(s) }}</option>
          }
        </select>
        <select [(ngModel)]="envelopeState" (change)="search()">
          <option value="">Tüm zarf durumları</option>
          @for (s of envelopeStates; track s) {
            <option [value]="s">{{ envelopeLabel(s) }}</option>
          }
        </select>
        <button type="button" (click)="search()">Ara</button>
      </div>
      <table>
        <thead>
          <tr><th>No</th><th>Tarih</th><th>Satıcı</th><th>Müşteri</th><th>Kaynak</th><th>Fatura durumu</th><th>Zarf durumu</th><th>Alıcı cevabı</th><th class="num">Toplam</th><th></th></tr>
        </thead>
        <tbody>
          @for (invoice of items(); track invoice.id) {
            <tr>
              <td>{{ invoice.number }}@if (invoice.profile === 'TICARIFATURA') { <span class="badge warn" title="Ticari fatura">T</span> }</td>
              <td>{{ invoice.issueDate | date: 'dd.MM.yyyy' }}</td>
              <td>{{ invoice.supplierName ?? '' }} <span class="muted">{{ invoice.supplierTaxId ?? '' }}</span></td>
              <td>{{ invoice.customerName ?? '' }} <span class="muted">{{ invoice.customerTaxId ?? '' }}</span></td>
              <td>{{ invoice.sourceType }} <span class="muted">{{ invoice.channelType }}</span></td>
              <td>
                <span class="badge" [class.ok]="cls(invoice.status) === 'ok'" [class.warn]="cls(invoice.status) === 'warn'" [class.danger]="cls(invoice.status) === 'danger'">{{ text(invoice.status) }}</span>
                @if (invoice.failureReason || invoice.rejectionReason) {
                  <div class="muted reason">{{ invoice.failureStage ? invoice.failureStage + ': ' : '' }}{{ invoice.failureReason ?? invoice.rejectionReason }}</div>
                }
              </td>
              <td>
                @if (answerOf(invoice); as answer) {
                  <span class="badge" [class.ok]="answer.kind === 'Accepted'" [class.danger]="answer.kind === 'Rejected'">{{ answer.kind === 'Accepted' ? 'Kabul' : 'Red' }}</span>
                  @if (answer.note) { <div class="muted reason">{{ answer.note }}</div> }
                } @else if (invoice.profile === 'TICARIFATURA' && invoice.status === 'Signed') {
                  <span class="muted">bekleniyor</span>
                } @else {
                  <span class="muted">—</span>
                }
              </td>
              <td>
                <span class="badge" [class.ok]="envelopeCls(invoice.envelopeState) === 'ok'" [class.warn]="envelopeCls(invoice.envelopeState) === 'warn'" [class.danger]="envelopeCls(invoice.envelopeState) === 'danger'">{{ envelopeLabel(invoice.envelopeState) }}</span>
                @if (invoice.envelopeStatusCode) { <span class="muted"> {{ invoice.envelopeStatusCode }}</span> }
                @if (invoice.envelopeNote) { <div class="muted reason">{{ invoice.envelopeNote }}</div> }
              </td>
              <td class="num">
                @if (invoice.total === null || invoice.total === undefined) {
                  @if (invoice.lines.length > 0) { <span class="masked">••••</span> }
                } @else { {{ invoice.total | number: '1.2-2' }} {{ invoice.currency }} }
              </td>
              <td class="actions">
                <button type="button" class="small" [disabled]="!session.has('endpoint.invoice.xml') || !hasXml(invoice)" (click)="showXml(invoice)">XML</button>
                <button type="button" class="small" [disabled]="!invoice.envelopeId || !session.has('endpoint.integration-envelope.read')" [title]="session.has('endpoint.integration-envelope.read') ? '' : 'Zarf görme yetkiniz yok'" (click)="envelope.set(invoice.envelopeId ?? null)">Zarf</button>
                <button type="button" class="small" (click)="history.set(invoice)">Geçmiş</button>
              </td>
            </tr>
          } @empty {
            <tr><td colspan="10" class="muted">Gösterilecek fatura yok.</td></tr>
          }
        </tbody>
      </table>
      <div class="pager">
        <span class="muted">{{ total() }} kayıt</span>
        <button type="button" class="small" [disabled]="page() <= 1" (click)="go(page() - 1)">‹</button>
        <span>{{ page() }} / {{ pages() }}</span>
        <button type="button" class="small" [disabled]="page() >= pages()" (click)="go(page() + 1)">›</button>
      </div>
    </div>

    @if (envelope(); as id) {
      <app-envelope-detail [envelopeId]="id" direction="outbound" (closed)="envelope.set(null)" />
    }

    @if (history(); as shown) {
      <div class="backdrop">
        <div class="modal">
          <h2>{{ shown.number }} geçmişi</h2>
          <table>
            <thead><tr><th>Zaman</th><th>Tür</th><th>Durum</th><th>Not</th></tr></thead>
            <tbody>
              @for (entry of shown.history; track $index) {
                <tr><td>{{ entry.at | date: 'dd.MM.yyyy HH:mm:ss' }}</td><td>{{ entry.kind === 'Status' ? 'Fatura' : 'Zarf' }}</td><td>{{ entry.value }}</td><td class="muted">{{ entry.note ?? '' }}</td></tr>
              }
            </tbody>
          </table>
          <div class="modal-actions"><button type="button" (click)="history.set(null)">Kapat</button></div>
        </div>
      </div>
    }

    @if (xml(); as shown) {
      <div class="backdrop">
        <div class="modal">
          <h2>{{ shown.title }}</h2>
          <pre class="xml">{{ shown.text }}</pre>
          <div class="modal-actions"><button type="button" (click)="xml.set(null)">Kapat</button></div>
        </div>
      </div>
    }
  `,
  styles: [
    `
      .xml {
        max-height: 420px;
        overflow: auto;
        background: #f3f4f6;
        padding: 10px;
        border-radius: 6px;
        font-size: 12px;
      }
    `,
  ],
})
export class InvoicesComponent implements OnInit {
  protected readonly session = inject(SessionService);
  private readonly api = inject(ApiClient);
  private readonly size = 10;

  protected readonly statuses = Object.keys(statusText);
  protected readonly envelopeStates = Object.keys(envelopeText).filter((state) => state !== 'None');
  protected readonly envelope = signal<string | null>(null);
  protected readonly history = signal<Invoice | null>(null);
  protected readonly items = signal<Invoice[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly error = signal<string | null>(null);
  protected readonly xml = signal<{ title: string; text: string } | null>(null);

  protected number = '';
  protected partyName = '';
  protected status = '';
  protected envelopeState = '';

  protected text(status: string): string {
    return statusText[status] ?? status;
  }

  protected cls(status: string): string {
    return statusClass[status] ?? '';
  }

  protected answerOf(invoice: Invoice): { kind: string; note?: string | null } | null {
    return invoice.responses.length > 0 ? invoice.responses[invoice.responses.length - 1] : null;
  }

  protected envelopeLabel(state: string): string {
    return envelopeText[state] ?? state;
  }

  protected envelopeCls(state: string): string {
    return envelopeClass[state] ?? '';
  }

  // a record has an XML from its draft on, and not after it was rejected or failed
  protected hasXml(invoice: Invoice): boolean {
    return ['DraftCreated', 'Approved', 'Signed'].includes(invoice.status);
  }

  protected pages(): number {
    return Math.max(1, Math.ceil(this.total() / this.size));
  }

  ngOnInit(): void {
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

  protected async showXml(invoice: Invoice): Promise<void> {
    this.error.set(null);
    try {
      const blob = await firstValueFrom(this.api.getFile(`invoice-service/v1/sales-invoices/${invoice.id}/xml`));
      this.xml.set({ title: `${invoice.number} XML`, text: await blob.text() });
    } catch (error) {
      this.error.set(await this.blobError(error));
    }
  }

  protected async load(): Promise<void> {
    this.error.set(null);
    try {
      const result = await firstValueFrom(
        this.api.post<Paged<Invoice>>('invoice-service/v1/sales-invoices/paged-list', {
          pageNumber: this.page(),
          maxResultCount: this.size,
          number: this.number || null,
          partyName: this.partyName || null,
          status: this.status || null,
          envelopeState: this.envelopeState || null,
        }),
      );
      this.items.set(result.items);
      this.total.set(result.dataTotalCount);
    } catch (error) {
      this.items.set([]);
      this.total.set(0);
      this.error.set(toApiError(error).text);
    }
  }

  // a file call that failed answers with a blob that holds the JSON of the error
  private async blobError(error: unknown): Promise<string> {
    const body = (error as { error?: unknown }).error;
    if (body instanceof Blob) {
      try {
        const parsed = JSON.parse(await body.text()) as { statusMessages?: string[] };
        return parsed.statusMessages?.join(' ') ?? 'Hata';
      } catch {
        return 'Hata';
      }
    }
    return toApiError(error).text;
  }
}
