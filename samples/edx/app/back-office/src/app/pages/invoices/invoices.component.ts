import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiClient, toApiError } from '../../core/api';
import { Invoice, Paged } from '../../core/models';
import { SessionService } from '../../core/session.service';

const statusText: Record<string, string> = { Processing: 'İşleniyor', Failed: 'Hatalı', DraftCreated: 'Taslak', Rejected: 'Reddedildi', Approved: 'Onaylandı', Signed: 'İmzalandı' };
const statusClass: Record<string, string> = { Processing: 'warn', Failed: 'danger', DraftCreated: 'warn', Rejected: 'danger', Approved: 'warn', Signed: 'ok' };

/**
 * The invoices, every record of them: the ones being processed, the failed and rejected ones (with the reason), drafts, approved and signed. The page reads with `endpoint.invoice.read` (required); what the user may not see comes masked from the service (price and
 * tax id are operations), and what it may see at most is the limit of its role (a constraint), shown above the list. Drafts are approved on their own page; the XML of an invoice is read here by who may.
 */
@Component({
  selector: 'app-invoices',
  imports: [FormsModule, DatePipe, DecimalPipe],
  template: `
    <div class="page-header">
      <div>
        <h1>Faturalar</h1>
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
        <button type="button" (click)="search()">Ara</button>
      </div>
      <table>
        <thead>
          <tr><th>No</th><th>Tarih</th><th>Satıcı</th><th>Müşteri</th><th>Kaynak</th><th>Durum</th><th class="num">Toplam</th><th></th></tr>
        </thead>
        <tbody>
          @for (invoice of items(); track invoice.id) {
            <tr>
              <td>{{ invoice.number }}</td>
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
              <td class="num">
                @if (invoice.total === null || invoice.total === undefined) {
                  @if (invoice.lines.length > 0) { <span class="masked">••••</span> }
                } @else { {{ invoice.total | number: '1.2-2' }} {{ invoice.currency }} }
              </td>
              <td class="actions">
                <button type="button" class="small" [disabled]="!session.has('endpoint.invoice.xml') || !hasXml(invoice)" (click)="showXml(invoice)">XML</button>
              </td>
            </tr>
          } @empty {
            <tr><td colspan="8" class="muted">Gösterilecek fatura yok.</td></tr>
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
  protected readonly items = signal<Invoice[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly error = signal<string | null>(null);
  protected readonly xml = signal<{ title: string; text: string } | null>(null);

  protected number = '';
  protected partyName = '';
  protected status = '';

  protected text(status: string): string {
    return statusText[status] ?? status;
  }

  protected cls(status: string): string {
    return statusClass[status] ?? '';
  }

  // a record has an XML from its draft on, and not after it was rejected or failed
  protected hasXml(invoice: Invoice): boolean {
    return invoice.status === 'DraftCreated' || invoice.status === 'Approved' || invoice.status === 'Signed';
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
      const blob = await firstValueFrom(this.api.getFile(`invoice-service/v1/invoices/${invoice.id}/xml`));
      this.xml.set({ title: `${invoice.number} XML`, text: await blob.text() });
    } catch (error) {
      this.error.set(await this.blobError(error));
    }
  }

  protected async load(): Promise<void> {
    this.error.set(null);
    try {
      const result = await firstValueFrom(
        this.api.post<Paged<Invoice>>('invoice-service/v1/invoices/paged-list', {
          pageNumber: this.page(),
          maxResultCount: this.size,
          number: this.number || null,
          partyName: this.partyName || null,
          status: this.status || null,
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
