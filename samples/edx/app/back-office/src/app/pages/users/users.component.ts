import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiClient, toApiError } from '../../core/api';
import { Paged, Role, User } from '../../core/models';
import { SessionService } from '../../core/session.service';

interface Form {
  id: string | null;
  userName: string;
  email: string;
  displayName: string;
  password: string;
  isActive: boolean;
  roleIds: string[];
}

/**
 * The users of the tenant: list, make, change, delete. The list reads with `endpoint.users.read` (and the roles with `endpoint.roles.read`, to choose them); the buttons are the actions. The email is an operation (`operation.users.email.view`): without it the service
 * sends it empty and the page shows it masked.
 */
@Component({
  selector: 'app-users',
  imports: [FormsModule],
  template: `
    <div class="page-header">
      <h1>Kullanıcılar</h1>
      <button class="primary" type="button" [disabled]="!session.has('endpoint.users.create')" (click)="openCreate()">Yeni kullanıcı</button>
    </div>

    @if (error()) {
      <div class="alert error">{{ error() }}</div>
    }

    <div class="card">
      <div class="toolbar">
        <input placeholder="Kullanıcı adı" [(ngModel)]="userName" (keyup.enter)="search()" />
        <button type="button" (click)="search()">Ara</button>
      </div>
      <table>
        <thead><tr><th>Kullanıcı</th><th>Ad</th><th>E-posta</th><th>Roller</th><th>Durum</th><th></th></tr></thead>
        <tbody>
          @for (user of items(); track user.id) {
            <tr>
              <td>{{ user.userName }}</td>
              <td>{{ user.displayName }}</td>
              <td>@if (user.email) { {{ user.email }} } @else { <span class="masked">••••</span> }</td>
              <td>@for (role of user.roles; track role) { <span class="badge">{{ role }}</span> }</td>
              <td><span class="badge" [class.ok]="user.isActive">{{ user.isActive ? 'Aktif' : 'Pasif' }}</span></td>
              <td class="actions">
                <button type="button" class="small" [disabled]="!session.has('endpoint.users.update')" (click)="openEdit(user)">Düzenle</button>
                <button type="button" class="small danger" [disabled]="!session.has('endpoint.users.delete')" (click)="remove(user)">Sil</button>
              </td>
            </tr>
          } @empty {
            <tr><td colspan="6" class="muted">Gösterilecek kullanıcı yok.</td></tr>
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

    @if (form(); as f) {
      <div class="backdrop">
        <form class="modal" (ngSubmit)="save()">
          <h2>{{ f.id ? 'Kullanıcıyı düzenle' : 'Yeni kullanıcı' }}</h2>
          @if (formError()) {
            <div class="alert error">{{ formError() }}</div>
          }
          <div class="form-grid">
            <label>Kullanıcı adı <input name="userName" [(ngModel)]="f.userName" [disabled]="!!f.id" required /></label>
            <label>Ad <input name="displayName" [(ngModel)]="f.displayName" required /></label>
            <label>E-posta <input name="email" type="email" [(ngModel)]="f.email" required /></label>
            @if (!f.id) {
              <label>Parola <input name="password" type="password" minlength="6" [(ngModel)]="f.password" required /></label>
            }
            <label class="wide"><span><input name="isActive" type="checkbox" [(ngModel)]="f.isActive" /> Aktif</span></label>
            <div class="wide">
              <div class="muted" style="margin-bottom: 4px">Roller</div>
              @for (role of roles(); track role.id) {
                <label style="display: inline-flex; flex-direction: row; gap: 6px; margin-right: 14px"><input type="checkbox" [checked]="f.roleIds.includes(role.id)" (change)="toggleRole(role.id)" /> {{ role.name }}</label>
              } @empty {
                <span class="muted">Roller görüntülenemiyor.</span>
              }
            </div>
          </div>
          <div class="modal-actions">
            <button type="button" (click)="form.set(null)">Vazgeç</button>
            <button type="submit" class="primary" [disabled]="saving()">Kaydet</button>
          </div>
        </form>
      </div>
    }
  `,
})
export class UsersComponent implements OnInit {
  protected readonly session = inject(SessionService);
  private readonly api = inject(ApiClient);
  private readonly size = 10;

  protected readonly items = signal<User[]>([]);
  protected readonly roles = signal<Role[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly error = signal<string | null>(null);
  protected readonly form = signal<Form | null>(null);
  protected readonly formError = signal<string | null>(null);
  protected readonly saving = signal(false);
  protected userName = '';

  protected pages(): number {
    return Math.max(1, Math.ceil(this.total() / this.size));
  }

  ngOnInit(): void {
    void this.load();
    void this.loadRoles();
  }

  protected search(): void {
    this.page.set(1);
    void this.load();
  }

  protected go(page: number): void {
    this.page.set(page);
    void this.load();
  }

  protected openCreate(): void {
    this.formError.set(null);
    this.form.set({ id: null, userName: '', email: '', displayName: '', password: '', isActive: true, roleIds: [] });
  }

  protected openEdit(user: User): void {
    this.formError.set(null);
    const roleIds = this.roles().filter((role) => user.roles.includes(role.name) && role.tenantId === user.tenantId).map((role) => role.id);
    this.form.set({ id: user.id, userName: user.userName, email: user.email, displayName: user.displayName, password: '', isActive: user.isActive, roleIds });
  }

  protected toggleRole(id: string): void {
    this.form.update((f) => (f ? { ...f, roleIds: f.roleIds.includes(id) ? f.roleIds.filter((x) => x !== id) : [...f.roleIds, id] } : f));
  }

  protected async save(): Promise<void> {
    const f = this.form();
    if (!f) {
      return;
    }
    this.saving.set(true);
    try {
      if (f.id) {
        await firstValueFrom(this.api.put('identity-service/v1/users', { id: f.id, email: f.email, displayName: f.displayName, isActive: f.isActive, roleIds: f.roleIds }));
      } else {
        await firstValueFrom(this.api.post('identity-service/v1/users/create', { userName: f.userName, email: f.email, displayName: f.displayName, password: f.password, roleIds: f.roleIds }));
      }
      this.form.set(null);
      await this.load();
    } catch (error) {
      this.formError.set(toApiError(error).text);
    } finally {
      this.saving.set(false);
    }
  }

  protected async remove(user: User): Promise<void> {
    if (!confirm(`${user.userName} kullanıcısı silinsin mi?`)) {
      return;
    }
    this.error.set(null);
    try {
      await firstValueFrom(this.api.delete(`identity-service/v1/users/${user.id}`));
      await this.load();
    } catch (error) {
      this.error.set(toApiError(error).text);
    }
  }

  private async load(): Promise<void> {
    this.error.set(null);
    try {
      const result = await firstValueFrom(this.api.post<Paged<User>>('identity-service/v1/users/paged-list', { pageNumber: this.page(), maxResultCount: this.size, userName: this.userName || null }));
      this.items.set(result.items);
      this.total.set(result.dataTotalCount);
    } catch (error) {
      this.items.set([]);
      this.total.set(0);
      this.error.set(toApiError(error).text);
    }
  }

  private async loadRoles(): Promise<void> {
    try {
      this.roles.set(await firstValueFrom(this.api.post<Role[]>('identity-service/v1/roles/filter-list', { maxResultCount: 100 })));
    } catch {
      this.roles.set([]);
    }
  }
}
