import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiClient, toApiError } from '../../core/api';
import { Paged, QueueItem } from '../../core/models';
import { SessionService } from '../../core/session.service';

const statusText: Record<string, string> = { Queued: 'Kuyrukta', DraftCreated: 'Taslak oluştu', Rejected: 'Reddedildi', Failed: 'Hatalı', Completed: 'Tamamlandı' };
const statusClass: Record<string, string> = { Queued: 'warn', DraftCreated: 'warn', Rejected: 'danger', Failed: 'danger', Completed: 'ok' };

/** The queue of the intake service: every invoice that was sent, how it was sent and what became of it (a failed or a rejected one says why). The file as it was received can be downloaded by who has the permission. */
@Component({
  selector: 'app-queue',
  imports: [FormsModule, DatePipe, DecimalPipe],
  template: `
    <div class="page-header">
      <div>
        <h1>Kuyruk</h1>
        <div class="muted">Gönderilen faturalar ve durumları.</div>
      </div>
      <button type="button" (click)="load()">Yenile</button>
    </div>

    @if (error()) {
      <div class="alert error">{{ error() }}</div>
    }

    <div class="card">
      <div class="toolbar">
        <input placeholder="Fatura no" [(ngModel)]="number" (keyup.enter)="search()" />
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
          <tr><th>Fatura no</th><th>Kaynak</th><th>Kanal</th><th>Alan servis</th><th>Dosya</th><th>Durum</th><th>Tarih</th><th></th></tr>
        </thead>
        <tbody>
          @for (item of items(); track item.id) {
            <tr>
              <td>{{ item.invoiceNumber }}</td>
              <td>{{ item.sourceType }}</td>
              <td>{{ item.channelType }}</td>
              <td>{{ item.receivedBy }}</td>
              <td>{{ item.fileName }} <span class="muted">{{ item.sizeBytes | number }} B</span></td>
              <td>
                <span class="badge" [class.ok]="cls(item.status) === 'ok'" [class.warn]="cls(item.status) === 'warn'" [class.danger]="cls(item.status) === 'danger'">{{ text(item.status) }}</span>
                @if (item.statusReason) {
                  <div class="muted reason">{{ item.statusReason }}</div>
                }
              </td>
              <td>{{ item.statusChangedAt | date: 'dd.MM.yyyy HH:mm' }}</td>
              <td class="actions">
                <button type="button" class="small" [disabled]="!session.has('endpoint.queue.download')" (click)="download(item)">İndir</button>
              </td>
            </tr>
          } @empty {
            <tr><td colspan="8" class="muted">Gösterilecek kayıt yok.</td></tr>
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
export class QueueComponent implements OnInit {
  protected readonly session = inject(SessionService);
  private readonly api = inject(ApiClient);
  private readonly size = 10;

  protected readonly statuses = Object.keys(statusText);
  protected readonly items = signal<QueueItem[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly error = signal<string | null>(null);

  protected number = '';
  protected status = '';

  protected text(status: string): string {
    return statusText[status] ?? status;
  }

  protected cls(status: string): string {
    return statusClass[status] ?? '';
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

  protected async download(item: QueueItem): Promise<void> {
    try {
      const blob = await firstValueFrom(this.api.getFile(`intake-service/v1/queue/${item.id}/download`));
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = item.fileName;
      link.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      this.error.set(toApiError(error).text);
    }
  }

  protected async load(): Promise<void> {
    this.error.set(null);
    try {
      const result = await firstValueFrom(
        this.api.post<Paged<QueueItem>>('intake-service/v1/queue/paged-list', {
          pageNumber: this.page(),
          maxResultCount: this.size,
          invoiceNumber: this.number || null,
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
}
