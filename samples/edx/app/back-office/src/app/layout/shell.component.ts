import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { MenuItem } from '../core/models';
import { SessionService } from '../core/session.service';

const ICONS: Record<string, string> = { invoice: '▤', report: '▦', identity: '☰', users: '☺', roles: '⚑' };

/** The frame of every page: the menu on the left (what the identity service gave this user, nothing else but Home), the user on the top, the page in the body. */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="shell">
      <aside class="sidebar">
        <div class="brand">Edx Back Office</div>
        <nav>
          <a class="item" routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }"><span class="icon">⌂</span> Ana sayfa</a>
          @for (item of session.menus(); track item.code) {
            @switch (item.nodeType) {
              @case ('Caption') {
                <div class="caption">{{ item.title }}</div>
              }
              @case ('Node') {
                <button type="button" class="item node" (click)="toggle(item.code)">
                  <span class="icon">{{ icon(item) }}</span> {{ item.title }}
                  <span class="chevron">{{ isOpen(item.code) ? '▾' : '▸' }}</span>
                </button>
                @if (isOpen(item.code)) {
                  @for (child of item.children; track child.code) {
                    <a class="item child" [routerLink]="child.route" routerLinkActive="active"><span class="icon">{{ icon(child) }}</span> {{ child.title }}</a>
                  }
                }
              }
              @default {
                <a class="item" [routerLink]="item.route" routerLinkActive="active"><span class="icon">{{ icon(item) }}</span> {{ item.title }}</a>
              }
            }
          }
        </nav>
      </aside>
      <div class="main">
        <header class="topbar">
          <span class="muted">{{ auth.who()?.tenant }}</span>
          <strong>{{ auth.who()?.userName }}</strong>
          <button type="button" class="small" (click)="auth.logout()">Çıkış</button>
        </header>
        <main class="body"><router-outlet /></main>
      </div>
    </div>
  `,
  styles: `
    .shell { display: flex; min-height: 100vh; }
    .sidebar { width: 250px; background: var(--sidebar); color: var(--sidebar-text); padding: 16px 10px; flex-shrink: 0; }
    .brand { color: #fff; font-weight: 700; font-size: 16px; padding: 4px 10px 16px; }
    .caption { text-transform: uppercase; font-size: 11px; letter-spacing: 0.08em; color: #64748b; padding: 16px 10px 6px; }
    .item { display: flex; align-items: center; gap: 10px; width: 100%; padding: 9px 10px; border: 0; border-radius: 6px; background: none; color: var(--sidebar-text); text-decoration: none; text-align: left; cursor: pointer; font: inherit; }
    .item:hover { background: var(--sidebar-active); color: #fff; }
    .item.active { background: var(--primary); color: #fff; }
    .item.child { padding-left: 34px; }
    .icon { width: 18px; text-align: center; }
    .chevron { margin-left: auto; }
    .main { flex: 1; display: flex; flex-direction: column; min-width: 0; }
    .topbar { display: flex; justify-content: flex-end; align-items: center; gap: 12px; padding: 10px 24px; background: #fff; border-bottom: 1px solid var(--border); }
    .body { padding: 24px; }
  `,
})
export class ShellComponent {
  protected readonly session = inject(SessionService);
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly open = signal<Set<string>>(new Set(['invoice-management', 'identity-management']));

  protected readonly hasMenu = computed(() => this.session.menus().length > 0);

  protected icon(item: MenuItem): string {
    return (item.icon && ICONS[item.icon]) || '•';
  }

  protected isOpen(code: string): boolean {
    return this.open().has(code);
  }

  protected toggle(code: string): void {
    this.open.update((current) => {
      const next = new Set(current);
      if (!next.delete(code)) {
        next.add(code);
      }
      return next;
    });
  }
}
