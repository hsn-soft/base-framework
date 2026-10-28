import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from './api';
import { SessionService } from './session.service';

const TOKEN_KEY = 'edx.token';
const WHO_KEY = 'edx.who';

interface Who {
  tenant: string;
  userName: string;
}

/** Signing in and out. The token of the auth server is kept for the tab only (session storage) and sent with every call. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiClient);
  private readonly router = inject(Router);
  private readonly session = inject(SessionService);

  readonly token = signal<string | null>(this.read(TOKEN_KEY));
  readonly who = signal<Who | null>(this.readWho());
  readonly isSignedIn = computed(() => this.token() !== null);

  async login(tenant: string, username: string, password: string): Promise<void> {
    const result = await firstValueFrom(this.api.post<{ accessToken: string }>('auth-server/v1/auth/login', { tenant, username, password }));
    sessionStorage.setItem(TOKEN_KEY, result.accessToken);
    sessionStorage.setItem(WHO_KEY, JSON.stringify({ tenant, userName: username }));
    this.token.set(result.accessToken);
    this.who.set({ tenant, userName: username });
  }

  logout(): void {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(WHO_KEY);
    this.token.set(null);
    this.who.set(null);
    this.session.clear();
    void this.router.navigate(['/login']);
  }

  private read(key: string): string | null {
    try {
      return sessionStorage.getItem(key);
    } catch {
      return null;
    }
  }

  private readWho(): Who | null {
    const text = this.read(WHO_KEY);
    return text ? (JSON.parse(text) as Who) : null;
  }
}
