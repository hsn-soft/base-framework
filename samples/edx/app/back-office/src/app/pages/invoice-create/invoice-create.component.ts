import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiClient, toApiError } from '../../core/api';
import { InvoiceModel, QueueItem } from '../../core/models';

interface LineForm {
  description: string;
  quantity: number;
  unitPrice: number;
  vatRate: number;
}

const today = (): string => new Date().toISOString().slice(0, 10);

/**
 * The create page: the model of an invoice is filled in a form and sent to the intake service (`back-office/create`), which queues it as it does for a customer's API. The uuid of the invoice is made here and shown; the page does not make an invoice, it
 * queues one: what became of it is read on the queue page.
 */
@Component({
  selector: 'app-invoice-create',
  imports: [FormsModule],
  template: `
    <div class="page-header">
      <div>
        <h1>Fatura oluştur</h1>
        <div class="muted">Form doldurulur, fatura kuyruğa alınır; sonucu "Kuyruk" sayfasında görürsünüz.</div>
      </div>
    </div>

    @if (error()) {
      <div class="alert error">{{ error() }}</div>
    }
    @if (queued(); as item) {
      <div class="alert ok">Fatura kuyruğa alındı ({{ item.invoiceNumber }}). <a href="javascript:void(0)" (click)="toQueue()">Kuyruğa git</a></div>
    }

    <form class="card" (ngSubmit)="send()">
      <div class="form-grid">
        <label>Fatura uuid <input name="invoiceId" [(ngModel)]="invoiceId" required /></label>
        <label>Fatura no <input name="number" placeholder="Fatura no" [(ngModel)]="number" required /></label>
        <label>Tarih <input name="issueDate" type="date" [(ngModel)]="issueDate" required /></label>
        <label>Para birimi <input name="currency" maxlength="3" [(ngModel)]="currency" required /></label>
        <label>Satıcı <input name="supplierName" placeholder="Satıcı" [(ngModel)]="supplierName" required /></label>
        <label>Satıcı vergi no <input name="supplierTaxId" [(ngModel)]="supplierTaxId" /></label>
        <label>Müşteri <input name="customerName" placeholder="Müşteri" [(ngModel)]="customerName" required /></label>
        <label>Müşteri vergi no <input name="customerTaxId" [(ngModel)]="customerTaxId" /></label>
      </div>
      <h2 style="margin-top: 16px">Kalemler</h2>
      @for (line of lines(); track $index) {
        <div class="toolbar">
          <input [name]="'d' + $index" placeholder="Açıklama" style="flex: 2" [(ngModel)]="line.description" required />
          <input [name]="'q' + $index" type="number" step="any" min="0" placeholder="Miktar" style="width: 90px" [(ngModel)]="line.quantity" />
          <input [name]="'p' + $index" type="number" step="any" min="0" placeholder="Birim fiyat" style="width: 110px" [(ngModel)]="line.unitPrice" />
          <input [name]="'v' + $index" type="number" step="any" min="0" max="100" placeholder="KDV %" style="width: 80px" [(ngModel)]="line.vatRate" />
          <button type="button" class="small danger" [disabled]="lines().length === 1" (click)="removeLine($index)">–</button>
        </div>
      }
      <button type="button" class="small" (click)="addLine()">+ Kalem ekle</button>
      <div class="modal-actions">
        <button type="button" (click)="reset()">Temizle</button>
        <button type="submit" class="primary" [disabled]="sending()">Gönder</button>
      </div>
    </form>
  `,
})
export class InvoiceCreateComponent {
  private readonly api = inject(ApiClient);
  private readonly router = inject(Router);

  protected readonly error = signal<string | null>(null);
  protected readonly queued = signal<QueueItem | null>(null);
  protected readonly sending = signal(false);
  protected readonly lines = signal<LineForm[]>([{ description: '', quantity: 1, unitPrice: 0, vatRate: 20 }]);

  protected invoiceId: string = crypto.randomUUID();
  protected number = '';
  protected issueDate = today();
  protected currency = 'TRY';
  protected supplierName = '';
  protected supplierTaxId = '';
  protected customerName = '';
  protected customerTaxId = '';

  protected addLine(): void {
    this.lines.update((lines) => [...lines, { description: '', quantity: 1, unitPrice: 0, vatRate: 20 }]);
  }

  protected removeLine(index: number): void {
    this.lines.update((lines) => lines.filter((_, i) => i !== index));
  }

  protected reset(): void {
    this.invoiceId = crypto.randomUUID();
    this.number = '';
    this.supplierName = this.supplierTaxId = this.customerName = this.customerTaxId = '';
    this.lines.set([{ description: '', quantity: 1, unitPrice: 0, vatRate: 20 }]);
    this.error.set(null);
    this.queued.set(null);
  }

  protected toQueue(): void {
    void this.router.navigateByUrl('/queue');
  }

  protected async send(): Promise<void> {
    const model: InvoiceModel = {
      invoiceId: this.invoiceId.trim(),
      invoiceNumber: this.number.trim(),
      issueDate: this.issueDate,
      currency: this.currency.toUpperCase(),
      supplierName: this.supplierName,
      supplierTaxId: this.supplierTaxId || null,
      customerName: this.customerName,
      customerTaxId: this.customerTaxId || null,
      lines: this.lines().map((line) => ({ description: line.description, quantity: Number(line.quantity), unitPrice: Number(line.unitPrice), vatRate: Number(line.vatRate) })),
    };
    this.sending.set(true);
    this.error.set(null);
    this.queued.set(null);
    try {
      this.queued.set(await firstValueFrom(this.api.post<QueueItem>('intake-service/v1/back-office/create', model)));
      // the next invoice is another one: a new uuid
      this.invoiceId = crypto.randomUUID();
    } catch (error) {
      this.error.set(toApiError(error).text);
    } finally {
      this.sending.set(false);
    }
  }
}
