import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from './api';
import { Grant, MenuItem } from './models';

/**
 * What the signed-in user may do, as the identity service says it: the menu (only the pages the user may see, each with what it needs) and the permissions the user has (with the value of a constraint). The screens ask this and never decide by themselves:
 * a page opens when the menu has it, a button is enabled when its permission is granted. The services check all of it again, so this is what the user is shown and not what is enforced.
 */
@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly api = inject(ApiClient);

  readonly menus = signal<MenuItem[]>([]);
  readonly grants = signal<Grant[]>([]);
  readonly loaded = signal(false);

  readonly granted = computed(() => new Set(this.grants().map((grant) => grant.code)));

  /** Whether the menu has any page: a user who has none sees the welcome page and a warning. */
  readonly hasPages = computed(() => this.leaves().length > 0);

  async load(): Promise<void> {
    const [menus, grants] = await Promise.all([
      firstValueFrom(this.api.post<MenuItem[]>('identity-service/v1/me/menus/get')),
      firstValueFrom(this.api.post<Grant[]>('identity-service/v1/me/permissions/get')),
    ]);
    this.menus.set(menus);
    this.grants.set(grants);
    this.loaded.set(true);
  }

  clear(): void {
    this.menus.set([]);
    this.grants.set([]);
    this.loaded.set(false);
  }

  /** Whether the user has the permission (any type). */
  has(code: string): boolean {
    return this.granted().has(code);
  }

  /** The value of a constraint the user has, as the role gave it; none when it has not. */
  valueOf(code: string): string | null {
    return this.grants().find((grant) => grant.code === code)?.value ?? null;
  }

  /** The page of the menu with this code, when the user may see it (the service sends only those). */
  leaf(code: string): MenuItem | undefined {
    return this.leaves().find((item) => item.code === code);
  }

  private leaves(): MenuItem[] {
    const found: MenuItem[] = [];
    const walk = (items: MenuItem[]): void => {
      for (const item of items) {
        if (item.nodeType === 'Leaf') {
          found.push(item);
        }
        walk(item.children);
      }
    };
    walk(this.menus());
    return found;
  }
}
