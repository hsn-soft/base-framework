import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiClient, toApiError } from '../../core/api';
import { Paged, PurchaseInvoice } from '../../core/models';
import { SessionService } from '../../core/session.service';
import { EnvelopeDetailComponent } from '../../shared/envelope-detail.component';

/**
 * The invoices issued to the tenant: they came in at its post box (an envelope of the center, checked against the UBL-TR schema) and are kept as they came. The page reads with `endpoint.purchase-invoice.read`; the amounts are an operation (hidden from whoever was not
 * granted it) and what may be seen at most is the limit of the role (a constraint). The XML of an invoice can be read by who may (both operations).
 */
@Component({
  selector: 'app-purchase-invoices',
  imports: [FormsModule, DatePipe, DecimalPipe, EnvelopeDetailComponent],
  template: `
    <div class="page-header">
      <div>
        <h1>Alış faturaları</h1>
        <div class="muted">Firmanıza kesilmiş ve posta kutunuza ulaşmış faturalar.</div>
      </div>
      <button type="button" (click)="load()">Yenile</button>
    </div>

    @if (error()) {
      <div class="alert error">{{ error() }}</div>
    }

    <div class="card">
      <div class="toolbar">
        <input placeholder="Fatura no" [(ngModel)]="number" (keyup.enter)="search()" />
        <input placeholder="Düzenleyen" [(ngModel)]="supplier" (keyup.enter)="search()" />
        <button type="button" (click)="search()">Ara</button>
      </div>
      <table>
        <thead>
          <tr><th>No</th><th>Tarih</th><th>Düzenleyen</th><th>Kalem</th><th class="num">Ödenecek</th><th>Geliş</th><th>Zarf (merkezde)</th><th>Cevabınız</th><th></th></tr>
        </thead>
        <tbody>
          @for (invoice of items(); track invoice.id) {
            <tr>
              <td>{{ invoice.number }}@if (invoice.profile === 'TICARIFATURA') { <span class="badge warn" title="Ticari fatura: kabul veya red cevabı verilir">T</span> }</td>
              <td>{{ invoice.issueDate | date: 'dd.MM.yyyy' }}</td>
              <td>{{ invoice.supplierName }} <span class="muted">{{ invoice.supplierRegisterNumber }}</span></td>
              <td>{{ invoice.lineCount }}</td>
              <td class="num">
                @if (invoice.payableAmount === null || invoice.payableAmount === undefined) { <span class="masked">••••</span> } @else { {{ invoice.payableAmount | number: '1.2-2' }} {{ invoice.currency }} }
              </td>
              <td>{{ invoice.receivedAt | date: 'dd.MM.yyyy HH:mm' }}</td>
              <td>
                <span class="badge" [class.ok]="invoice.envelopeStatusCode === 1300" [class.warn]="invoice.envelopeStatusCode !== 1300 && invoice.envelopeStatusCode < 1210" [class.danger]="invoice.envelopeStatusCode >= 1210 && invoice.envelopeStatusCode !== 1220 && invoice.envelopeStatusCode !== 1300">{{ invoice.envelopeStatusCode }}</span>
                <span class="muted">{{ invoice.envelopeStatusText }}</span>
              </td>
              <td>
                @if (invoice.responses.length > 0) {
                  <span class="badge" [class.ok]="invoice.responses[0].kind === 'Accepted'" [class.danger]="invoice.responses[0].kind === 'Rejected'">{{ invoice.responses[0].kind === 'Accepted' ? 'Kabul' : 'Red' }}</span>
                  @if (invoice.responses[0].envelopeStatusCode) { <span class="muted"> zarf {{ invoice.responses[0].envelopeStatusCode }}</span> }
                  @if (invoice.responses[0].note) { <div class="muted reason">{{ invoice.responses[0].note }}</div> }
                } @else if (invoice.profile === 'TICARIFATURA') {
                  <span class="muted">bekleniyor</span>
                } @else {
                  <span class="muted">—</span>
                }
              </td>
              <td class="actions">
                @if (canAnswer(invoice)) {
                  <button type="button" class="small" (click)="respond(invoice, true, '')">Kabul</button>
                  <button type="button" class="small danger" (click)="refusing.set(invoice)">Red</button>
                }
                <button type="button" class="small" [disabled]="!session.has('endpoint.integration-envelope.read')" [title]="session.has('endpoint.integration-envelope.read') ? '' : 'Zarf görme yetkiniz yok'" (click)="envelope.set(invoice.envelopeId)">Zarf</button>
                <button type="button" class="small" [disabled]="!session.has('endpoint.purchase-invoice.xml')" [title]="session.has('endpoint.purchase-invoice.xml') ? '' : 'XML görme yetkiniz yok'" (click)="showXml(invoice)">XML</button>
              </td>
            </tr>
          } @empty {
            <tr><td colspan="9" class="muted">Gösterilecek fatura yok.</td></tr>
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

    @if (refusing(); as target) {
      <div class="backdrop">
        <div class="modal">
          <h2>{{ target.number }} faturasını reddet</h2>
          <textarea rows="3" placeholder="Red gerekçesi (zorunlu)" [(ngModel)]="reason" style="width: 100%"></textarea>
          <div class="modal-actions">
            <button type="button" (click)="refusing.set(null)">Vazgeç</button>
            <button type="button" class="danger" [disabled]="reason.trim().length === 0" (click)="respond(target, false, reason)">Reddet</button>
          </div>
        </div>
      </div>
    }

    @if (envelope(); as id) {
      <app-envelope-detail [envelopeId]="id" direction="inbound" (closed)="envelope.set(null)" />
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
export class PurchaseInvoicesComponent implements OnInit {
  protected readonly session = inject(SessionService);
  private readonly api = inject(ApiClient);
  private readonly size = 10;

  protected readonly items = signal<PurchaseInvoice[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly error = signal<string | null>(null);
  protected readonly envelope = signal<string | null>(null);
  protected readonly refusing = signal<PurchaseInvoice | null>(null);
  protected readonly xml = signal<{ title: string; text: string } | null>(null);

  protected reason = '';
  protected number = '';
  protected supplier = '';

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

  // a commercial invoice that has not been answered: the one who may (endpoint respond) accepts it or refuses it
  protected canAnswer(invoice: PurchaseInvoice): boolean {
    return invoice.profile === 'TICARIFATURA' && invoice.responses.length === 0 && this.session.has('endpoint.purchase-invoice.respond');
  }

  protected async respond(invoice: PurchaseInvoice, accept: boolean, note: string): Promise<void> {
    this.error.set(null);
    try {
      await firstValueFrom(this.api.post<PurchaseInvoice>(`invoice-service/v1/purchase-invoices/${invoice.id}/respond`, { accept, note: note.trim() || null }));
      this.refusing.set(null);
      this.reason = '';
      await this.load();
    } catch (error) {
      this.error.set(toApiError(error).text);
    }
  }

  protected async showXml(invoice: PurchaseInvoice): Promise<void> {
    this.error.set(null);
    try {
      const blob = await firstValueFrom(this.api.getFile(`invoice-service/v1/purchase-invoices/${invoice.id}/xml`));
      this.xml.set({ title: `${invoice.number} XML`, text: await blob.text() });
    } catch (error) {
      this.error.set(await this.blobError(error));
    }
  }

  protected async load(): Promise<void> {
    this.error.set(null);
    try {
      const result = await firstValueFrom(
        this.api.post<Paged<PurchaseInvoice>>('invoice-service/v1/purchase-invoices/paged-list', {
          pageNumber: this.page(),
          maxResultCount: this.size,
          number: this.number || null,
          supplierName: this.supplier || null,
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
