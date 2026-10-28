import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiClient, toApiError } from '../../core/api';
import { Invoice, Paged } from '../../core/models';
import { SessionService } from '../../core/session.service';

/**
 * The draft approval page: the invoices whose draft XML was made wait here for a person. The XML can be read (it holds what the operations hide, so the button needs them), and the draft is approved (it is signed afterwards) or rejected with a reason the sender
 * reads in the queue. How much the user may approve is the limit of its role (a constraint), shown above the list.
 */
@Component({
  selector: 'app-invoice-drafts',
  imports: [FormsModule, DatePipe, DecimalPipe],
  template: `
    <div class="page-header">
      <div>
        <h1>Taslak onayı</h1>
        <div class="muted">
          Onay bekleyen faturalar.
          @if (session.valueOf('constraint.invoice.max-approve-amount'); as limit) { Onay limiti: {{ +limit | number }} }
        </div>
      </div>
      <button type="button" (click)="load()">Yenile</button>
    </div>

    @if (error()) {
      <div class="alert error">{{ error() }}</div>
    }

    <div class="card">
      <table>
        <thead>
          <tr><th>No</th><th>Tarih</th><th>Satıcı</th><th>Müşteri</th><th>Kaynak</th><th class="num">Toplam</th><th></th></tr>
        </thead>
        <tbody>
          @for (invoice of items(); track invoice.id) {
            <tr>
              <td>{{ invoice.number }}</td>
              <td>{{ invoice.issueDate | date: 'dd.MM.yyyy' }}</td>
              <td>{{ invoice.supplierName }} <span class="muted">{{ invoice.supplierTaxId ?? '' }}</span></td>
              <td>{{ invoice.customerName }} <span class="muted">{{ invoice.customerTaxId ?? '' }}</span></td>
              <td>{{ invoice.sourceType }} <span class="muted">{{ invoice.channelType }}</span></td>
              <td class="num">
                @if (invoice.total === null || invoice.total === undefined) { <span class="masked">••••</span> } @else { {{ invoice.total | number: '1.2-2' }} {{ invoice.currency }} }
              </td>
              <td class="actions">
                <button type="button" class="small" [disabled]="!session.has('endpoint.invoice.xml')" [title]="session.has('endpoint.invoice.xml') ? '' : 'XML görme yetkiniz yok'" (click)="showXml(invoice)">XML</button>
                <button type="button" class="small" [disabled]="!session.has('endpoint.invoice.approve')" (click)="approve(invoice)">Onayla</button>
                <button type="button" class="small danger" [disabled]="!session.has('endpoint.invoice.reject')" (click)="openReject(invoice)">Reddet</button>
              </td>
            </tr>
          } @empty {
            <tr><td colspan="7" class="muted">Onay bekleyen taslak yok.</td></tr>
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

    @if (rejecting(); as target) {
      <div class="backdrop">
        <form class="modal" (ngSubmit)="reject()">
          <h2>{{ target.number }} numaralı faturayı reddet</h2>
          @if (rejectError()) {
            <div class="alert error">{{ rejectError() }}</div>
          }
          <label>Red nedeni <input name="reason" placeholder="Red nedeni" [(ngModel)]="reason" required /></label>
          <div class="modal-actions">
            <button type="button" (click)="rejecting.set(null)">Vazgeç</button>
            <button type="submit" class="primary danger" [disabled]="!reason.trim()">Reddet</button>
          </div>
        </form>
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
export class InvoiceDraftsComponent implements OnInit {
  protected readonly session = inject(SessionService);
  private readonly api = inject(ApiClient);
  private readonly size = 10;

  protected readonly items = signal<Invoice[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly error = signal<string | null>(null);
  protected readonly xml = signal<{ title: string; text: string } | null>(null);
  protected readonly rejecting = signal<Invoice | null>(null);
  protected readonly rejectError = signal<string | null>(null);

  protected reason = '';

  protected pages(): number {
    return Math.max(1, Math.ceil(this.total() / this.size));
  }

  ngOnInit(): void {
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
      this.xml.set({ title: `${invoice.number} taslak XML`, text: await blob.text() });
    } catch (error) {
      this.error.set(await this.blobError(error));
    }
  }

  protected async approve(invoice: Invoice): Promise<void> {
    this.error.set(null);
    try {
      await firstValueFrom(this.api.post(`invoice-service/v1/invoices/${invoice.id}/approve`));
      await this.load();
    } catch (error) {
      this.error.set(toApiError(error).text);
    }
  }

  protected openReject(invoice: Invoice): void {
    this.reason = '';
    this.rejectError.set(null);
    this.rejecting.set(invoice);
  }

  protected async reject(): Promise<void> {
    const invoice = this.rejecting();
    if (!invoice) {
      return;
    }
    try {
      await firstValueFrom(this.api.post(`invoice-service/v1/invoices/${invoice.id}/reject`, { reason: this.reason.trim() }));
      this.rejecting.set(null);
      await this.load();
    } catch (error) {
      this.rejectError.set(toApiError(error).text);
    }
  }

  protected async load(): Promise<void> {
    this.error.set(null);
    try {
      const result = await firstValueFrom(
        this.api.post<Paged<Invoice>>('invoice-service/v1/invoices/paged-list', { pageNumber: this.page(), maxResultCount: this.size, status: 'DraftCreated' }),
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
