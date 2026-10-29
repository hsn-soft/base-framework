import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiClient, toApiError } from '../../core/api';
import { InvoiceReport, Paged } from '../../core/models';
import { SessionService } from '../../core/session.service';

const day = (offset: number): string => new Date(Date.now() + offset * 86_400_000).toISOString().slice(0, 10);

/**
 * The invoice reports. Reading is `endpoint.invoice-report.read` (required); making and deleting are the actions; the total is an operation (masked without `operation.invoice-report.total.view`); and how far back the user may look is its constraint
 * (`constraint.invoice-report.max-days`), shown above the list: the service lists the reports of those days only and refuses a longer period.
 */
@Component({
  selector: 'app-invoice-reports',
  imports: [FormsModule, DatePipe, DecimalPipe],
  template: `
    <div class="page-header">
      <div>
        <h1>Fatura raporları</h1>
        <div class="muted">
          @if (session.valueOf('constraint.invoice-report.max-days'); as days) { En fazla {{ days }} gün geriye bakabilirsiniz. }
        </div>
      </div>
    </div>

    @if (error()) {
      <div class="alert error">{{ error() }}</div>
    }

    <div class="card" style="margin-bottom: 16px">
      <h2>Rapor oluştur</h2>
      <div class="toolbar">
        <label class="muted">Başlangıç <input type="date" [(ngModel)]="start" /></label>
        <label class="muted">Bitiş <input type="date" [(ngModel)]="end" /></label>
        <label class="muted">Para birimi <input maxlength="3" style="width: 70px" [(ngModel)]="currency" /></label>
        <button class="primary" type="button" [disabled]="!session.has('endpoint.invoice-report.generate') || busy()" (click)="generate()">Oluştur</button>
      </div>
    </div>

    <div class="card">
      <table>
        <thead>
          <tr><th>Rapor</th><th>Oluşturulma</th><th class="num">Fatura</th><th class="num">Onaylı</th><th class="num">Toplam</th><th></th></tr>
        </thead>
        <tbody>
          @for (report of items(); track report.id) {
            <tr>
              <td>{{ report.name }}</td>
              <td>{{ report.generatedAt | date: 'dd.MM.yyyy HH:mm' }}</td>
              <td class="num">{{ report.invoiceCount }}</td>
              <td class="num">{{ report.approvedCount }}</td>
              <td class="num">
                @if (report.totalAmount === null || report.totalAmount === undefined) { <span class="masked">••••</span> } @else { {{ report.totalAmount | number: '1.2-2' }} {{ report.currency }} }
              </td>
              <td class="actions"><button type="button" class="small danger" [disabled]="!session.has('endpoint.invoice-report.delete')" (click)="remove(report)">Sil</button></td>
            </tr>
          } @empty {
            <tr><td colspan="6" class="muted">Gösterilecek rapor yok.</td></tr>
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
  `,
})
export class InvoiceReportsComponent implements OnInit {
  protected readonly session = inject(SessionService);
  private readonly api = inject(ApiClient);
  private readonly size = 10;

  protected readonly items = signal<InvoiceReport[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly error = signal<string | null>(null);
  protected readonly busy = signal(false);

  protected start = day(-7);
  protected end = day(0);
  protected currency = 'TRY';

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

  protected async generate(): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(this.api.post('invoice-service/v1/invoice-reports/generate', { periodStart: this.start, periodEnd: this.end, currency: this.currency.toUpperCase() }));
      this.page.set(1);
      await this.load();
    } catch (error) {
      this.error.set(toApiError(error).text);
    } finally {
      this.busy.set(false);
    }
  }

  protected async remove(report: InvoiceReport): Promise<void> {
    if (!confirm(`${report.name} raporu silinsin mi?`)) {
      return;
    }
    this.error.set(null);
    try {
      await firstValueFrom(this.api.delete(`invoice-service/v1/invoice-reports/${report.id}`));
      await this.load();
    } catch (error) {
      this.error.set(toApiError(error).text);
    }
  }

  private async load(): Promise<void> {
    this.error.set(null);
    try {
      const result = await firstValueFrom(this.api.post<Paged<InvoiceReport>>('invoice-service/v1/invoice-reports/paged-list', { pageNumber: this.page(), maxResultCount: this.size }));
      this.items.set(result.items);
      this.total.set(result.dataTotalCount);
    } catch (error) {
      this.items.set([]);
      this.total.set(0);
      this.error.set(toApiError(error).text);
    }
  }
}
