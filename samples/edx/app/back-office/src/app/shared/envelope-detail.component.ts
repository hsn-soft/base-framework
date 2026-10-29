import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, input, output, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient, toApiError } from '../core/api';
import { InboundEnvelope, OutboundEnvelope } from '../core/models';

/**
 * The envelope of an invoice, with what happened to it: asked of the GİB invoice integration by the uuid of the envelope (the list of invoices only keeps a summary). A sales invoice's envelope is one this tenant sent (outbound), a purchase invoice's one that came in (inbound);
 * the page reads with `endpoint.integration-envelope.read`.
 */
@Component({
  selector: 'app-envelope-detail',
  imports: [DatePipe],
  template: `
    <div class="backdrop">
      <div class="modal">
        <h2>Zarf <span class="muted">{{ envelopeId() }}</span></h2>
        @if (detail(); as e) {
          <table>
            <tbody>
              <tr><th>Durum</th><td>{{ e.state }} <span class="muted">{{ e.statusCode }} {{ e.statusDescription }}</span></td></tr>
              @if (outbound(); as o) {
                <tr><th>Alıcı</th><td>{{ o.receiverTitle }} <span class="muted">{{ o.receiverRegisterNumber }} · {{ o.receiverAlias }}</span></td></tr>
                <tr><th>Gönderici birim</th><td>{{ o.senderAlias }}</td></tr>
                <tr><th>Deneme</th><td>{{ o.sendCount }}</td></tr>
                @if (o.failureReason) { <tr><th>Hata</th><td>{{ o.failureStage }}: {{ o.failureReason }}</td></tr> }
              }
              @if (inbound(); as i) {
                <tr><th>Gönderen</th><td>{{ i.senderTitle ?? '' }} <span class="muted">{{ i.senderRegisterNumber ?? '' }} · {{ i.senderAlias ?? '' }}</span></td></tr>
                <tr><th>Posta kutusu</th><td>{{ i.receiverAlias ?? '' }}</td></tr>
                <tr><th>Merkezde</th><td>@if (i.centerStatusCode) { {{ i.centerStatusCode }} {{ i.centerStatusDescription }} } @else { <span class="muted">henüz sorulmadı</span> }</td></tr>
              }
            </tbody>
          </table>
          <h3>Geçmiş</h3>
          <table>
            <thead><tr><th>Zaman</th><th>Kod</th><th>Açıklama</th><th>Kaynak</th></tr></thead>
            <tbody>
              @for (event of e.history; track $index) {
                <tr>
                  <td>{{ event.at | date: 'dd.MM.yyyy HH:mm:ss' }}</td>
                  <td>{{ event.code || '' }}</td>
                  <td>{{ event.text }} @if (event.note) { <span class="muted">{{ event.note }}</span> }</td>
                  <td>{{ event.source }}</td>
                </tr>
              }
            </tbody>
          </table>
        } @else if (error()) {
          <div class="alert error">{{ error() }}</div>
        } @else {
          <div class="muted">Yükleniyor…</div>
        }
        <div class="modal-actions"><button type="button" (click)="closed.emit()">Kapat</button></div>
      </div>
    </div>
  `,
})
export class EnvelopeDetailComponent implements OnInit {
  private readonly api = inject(ApiClient);

  readonly envelopeId = input.required<string>();
  readonly direction = input.required<'outbound' | 'inbound'>();
  readonly closed = output<void>();

  protected readonly detail = signal<OutboundEnvelope | InboundEnvelope | null>(null);
  protected readonly error = signal<string | null>(null);

  protected outbound(): OutboundEnvelope | null {
    return this.direction() === 'outbound' ? (this.detail() as OutboundEnvelope | null) : null;
  }

  protected inbound(): InboundEnvelope | null {
    return this.direction() === 'inbound' ? (this.detail() as InboundEnvelope | null) : null;
  }

  async ngOnInit(): Promise<void> {
    try {
      this.detail.set(await firstValueFrom(this.api.post<OutboundEnvelope | InboundEnvelope>(`integration-service/v1/envelopes/${this.direction()}/by-envelope/${this.envelopeId()}`)));
    } catch (error) {
      this.error.set(toApiError(error).text);
    }
  }
}
