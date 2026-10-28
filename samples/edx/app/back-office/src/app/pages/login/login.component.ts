import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { toApiError } from '../../core/api';
import { AuthService } from '../../core/auth.service';
import { SessionService } from '../../core/session.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  template: `
    <div class="wrap">
      <form class="card box" (ngSubmit)="submit()">
        <h1>Edx Back Office</h1>
        <p class="muted">Kiracı kodunuzla giriş yapın.</p>
        @if (error()) {
          <div class="alert error">{{ error() }}</div>
        }
        <label>Kiracı <input name="tenant" [(ngModel)]="tenant" placeholder="veriban" autocomplete="organization" required /></label>
        <label>Kullanıcı adı <input name="username" [(ngModel)]="username" autocomplete="username" required /></label>
        <label>Parola <input name="password" type="password" [(ngModel)]="password" autocomplete="current-password" required /></label>
        <button class="primary" type="submit" [disabled]="busy()">{{ busy() ? 'Giriş yapılıyor…' : 'Giriş yap' }}</button>
      </form>
    </div>
  `,
  styles: `
    .wrap { min-height: 100vh; display: flex; align-items: center; justify-content: center; }
    .box { width: 360px; display: flex; flex-direction: column; gap: 12px; }
    label { display: flex; flex-direction: column; gap: 4px; font-size: 12px; color: var(--muted); }
    input { font-size: 14px; color: var(--text); }
  `,
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly session = inject(SessionService);
  private readonly router = inject(Router);

  protected tenant = '';
  protected username = '';
  protected password = '';
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected async submit(): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    try {
      await this.auth.login(this.tenant.trim(), this.username.trim(), this.password);
      // the menu and the permissions of this user, before any page: the shell shows nothing else
      await this.session.load();
      await this.router.navigate(['/']);
    } catch (error) {
      const failure = toApiError(error);
      this.error.set(failure.status === 401 ? 'Kiracı, kullanıcı adı veya parola hatalı.' : failure.text);
    } finally {
      this.busy.set(false);
    }
  }
}
