import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiClient, toApiError } from '../../core/api';
import { QueueItem } from '../../core/models';

const sourceTypes: Record<string, string> = { xml: 'Xml', txt: 'Txt', csv: 'Csv', xls: 'Xls', xlsx: 'Xls' };

/** The SHA-256 of the bytes, in lowercase hex: what the intake service checks the zip against. */
async function sha256(bytes: ArrayBuffer): Promise<string> {
  const digest = await crypto.subtle.digest('SHA-256', bytes);
  return Array.from(new Uint8Array(digest), (byte) => byte.toString(16).padStart(2, '0')).join('');
}

/**
 * The upload page: a zip with one invoice file (xml, txt, csv, xls or xlsx) is sent to the intake service (`back-office/upload`) with the uuid and the number of the invoice, the type of the file and the SHA-256 of the zip, which the page computes. Only a zip
 * is accepted, as for a customer's API. The type is guessed from the name of the zip (`invoice.csv.zip`) and can be changed.
 */
@Component({
  selector: 'app-invoice-upload',
  imports: [FormsModule],
  template: `
    <div class="page-header">
      <div>
        <h1>Fatura yükle</h1>
        <div class="muted">İçinde tek dosya olan bir zip yüklenir (xml, txt, csv, xls, xlsx); sonucu "Kuyruk" sayfasında görürsünüz.</div>
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
        <label>Zip dosyası <input name="file" type="file" accept=".zip,application/zip" (change)="pick($event)" /></label>
        <label>
          Dosya türü
          <select name="sourceType" [(ngModel)]="sourceType" required>
            <option value="">Seçin</option>
            <option value="Xml">XML</option>
            <option value="Csv">CSV</option>
            <option value="Txt">TXT</option>
            <option value="Xls">XLS / XLSX</option>
          </select>
        </label>
        <label>Fatura uuid <input name="invoiceId" [(ngModel)]="invoiceId" required /></label>
        <label>Fatura no <input name="number" placeholder="Fatura no" [(ngModel)]="number" required /></label>
      </div>
      @if (hash()) {
        <div class="muted" style="margin-top: 8px">SHA-256: {{ hash() }}</div>
      }
      <div class="modal-actions">
        <button type="submit" class="primary" [disabled]="sending() || !file">Yükle</button>
      </div>
    </form>
  `,
})
export class InvoiceUploadComponent {
  private readonly api = inject(ApiClient);
  private readonly router = inject(Router);

  protected readonly error = signal<string | null>(null);
  protected readonly queued = signal<QueueItem | null>(null);
  protected readonly sending = signal(false);
  protected readonly hash = signal('');

  protected file: File | null = null;
  protected sourceType = '';
  protected invoiceId: string = crypto.randomUUID();
  protected number = '';

  protected pick(event: Event): void {
    this.file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.hash.set('');
    if (this.file) {
      // invoice.csv.zip: the type is the extension before .zip
      const parts = this.file.name.toLowerCase().split('.');
      this.sourceType = parts.length > 2 ? (sourceTypes[parts[parts.length - 2]] ?? this.sourceType) : this.sourceType;
    }
  }

  protected toQueue(): void {
    void this.router.navigateByUrl('/queue');
  }

  protected async send(): Promise<void> {
    if (!this.file) {
      return;
    }
    this.sending.set(true);
    this.error.set(null);
    this.queued.set(null);
    try {
      const hash = await sha256(await this.file.arrayBuffer());
      this.hash.set(hash);
      const form = new FormData();
      form.append('file', this.file, this.file.name);
      form.append('invoiceId', this.invoiceId.trim());
      form.append('invoiceNumber', this.number.trim());
      form.append('fileHash', hash);
      form.append('sourceType', this.sourceType);
      this.queued.set(await firstValueFrom(this.api.postForm<QueueItem>('intake-service/v1/back-office/upload', form)));
      this.invoiceId = crypto.randomUUID();
    } catch (error) {
      this.error.set(toApiError(error).text);
    } finally {
      this.sending.set(false);
    }
  }
}
