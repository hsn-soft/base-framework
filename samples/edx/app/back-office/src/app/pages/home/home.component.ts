import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth.service';
import { SessionService } from '../../core/session.service';

@Component({
  selector: 'app-home',
  template: `
    <h1>Hoş geldiniz</h1>
    <p class="muted">{{ auth.who()?.userName }} ({{ auth.who()?.tenant }})</p>
    @if (!session.hasPages()) {
      <div class="alert warning">Herhangi bir menüyü kullanma yetkiniz yoktur.</div>
    }
  `,
})
export class HomeComponent {
  protected readonly auth = inject(AuthService);
  protected readonly session = inject(SessionService);
}
