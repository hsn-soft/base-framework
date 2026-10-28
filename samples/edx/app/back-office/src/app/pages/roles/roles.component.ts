import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiClient, toApiError } from '../../core/api';
import { Grant, Permission, PermissionType, Role } from '../../core/models';
import { SessionService } from '../../core/session.service';

const TYPES: { type: PermissionType; title: string; hint: string }[] = [
  { type: 'Page', title: 'Sayfa', hint: 'Menüde görünme ve sayfaya girme (görünürlük)' },
  { type: 'Endpoint', title: 'Endpoint', hint: 'Sayfanın veri çekmesi (required) ve butonları (action)' },
  { type: 'Operation', title: 'Operasyon', hint: 'Alan bazlı erişim: açık mı, maskeli mi' },
  { type: 'Constraint', title: 'Kısıt', hint: 'Çalışma zamanı limitleri; değer verilmezse yetki yok sayılır' },
];

/**
 * The roles of the tenant and what each is granted. A role is chosen and the whole catalogue is shown by type: a page, an endpoint and an operation is a checkbox; a constraint is a checkbox with the value the role is held to. Saving replaces the grants of the role as a whole.
 * Making and deleting a role are actions (`endpoint.roles.create`, `endpoint.roles.delete`), saving the grants is `endpoint.role-permissions.update`.
 */
@Component({
  selector: 'app-roles',
  imports: [FormsModule],
  template: `
    <div class="page-header"><h1>Roller</h1></div>

    @if (error()) {
      <div class="alert error">{{ error() }}</div>
    }

    <div class="layout">
      <div class="card">
        <h2>Roller</h2>
        <div class="toolbar">
          <input placeholder="Yeni rol adı" [(ngModel)]="newName" (keyup.enter)="create()" />
          <button class="primary" type="button" [disabled]="!session.has('endpoint.roles.create') || !newName.trim()" (click)="create()">Ekle</button>
        </div>
        <table>
          <tbody>
            @for (role of roles(); track role.id) {
              <tr [class.selected]="role.id === selected()?.id">
                <td><a href="#" (click)="choose(role); $event.preventDefault()">{{ role.name }}</a></td>
                <td class="actions"><button type="button" class="small danger" [disabled]="!session.has('endpoint.roles.delete')" (click)="remove(role)">Sil</button></td>
              </tr>
            } @empty {
              <tr><td class="muted">Rol yok.</td></tr>
            }
          </tbody>
        </table>
      </div>

      <div class="card">
        @if (selected(); as role) {
          <div class="page-header">
            <div>
              <h2>{{ role.name }} - yetkiler</h2>
              @if (saved()) { <span class="badge ok">Kaydedildi</span> }
            </div>
            <button class="primary" type="button" [disabled]="!session.has('endpoint.role-permissions.update') || saving()" (click)="save()">Kaydet</button>
          </div>
          @for (group of groups(); track group.type) {
            <h3>{{ group.title }} <span class="muted">{{ group.hint }}</span></h3>
            <table>
              <tbody>
                @for (permission of group.permissions; track permission.code) {
                  <tr>
                    <td style="width: 28px"><input type="checkbox" [checked]="has(permission.code)" [disabled]="!session.has('endpoint.role-permissions.update')" (change)="toggle(permission)" /></td>
                    <td>{{ permission.code }}</td>
                    <td style="width: 160px">
                      @if (permission.type === 'Constraint') {
                        <input style="width: 150px" placeholder="değer" [disabled]="!has(permission.code) || !session.has('endpoint.role-permissions.update')" [ngModel]="valueOf(permission.code)" (ngModelChange)="setValue(permission.code, $event)" />
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          }
        } @else {
          <p class="muted">Yetkilerini görmek için bir rol seçin.</p>
        }
      </div>
    </div>
  `,
  styles: `
    .layout { display: grid; grid-template-columns: 320px 1fr; gap: 16px; align-items: start; }
    h3 { margin: 18px 0 6px; font-size: 14px; }
    h3 .muted { font-weight: 400; font-size: 12px; margin-left: 6px; }
    tr.selected { background: #eff6ff; }
    a { color: var(--primary); text-decoration: none; }
  `,
})
export class RolesComponent implements OnInit {
  protected readonly session = inject(SessionService);
  private readonly api = inject(ApiClient);

  protected readonly roles = signal<Role[]>([]);
  protected readonly catalogue = signal<Permission[]>([]);
  protected readonly selected = signal<Role | null>(null);
  protected readonly granted = signal<Map<string, string | null>>(new Map());
  protected readonly error = signal<string | null>(null);
  protected readonly saving = signal(false);
  protected readonly saved = signal(false);
  protected newName = '';

  protected readonly groups = computed(() =>
    TYPES.map((entry) => ({ ...entry, permissions: this.catalogue().filter((permission) => permission.type === entry.type).sort((a, b) => a.code.localeCompare(b.code)) })).filter((group) => group.permissions.length > 0),
  );

  ngOnInit(): void {
    void this.loadRoles();
    void this.loadCatalogue();
  }

  protected has(code: string): boolean {
    return this.granted().has(code);
  }

  protected valueOf(code: string): string {
    return this.granted().get(code) ?? '';
  }

  protected toggle(permission: Permission): void {
    this.saved.set(false);
    this.granted.update((current) => {
      const next = new Map(current);
      if (!next.delete(permission.code)) {
        next.set(permission.code, permission.type === 'Constraint' ? '' : null);
      }
      return next;
    });
  }

  protected setValue(code: string, value: string): void {
    this.saved.set(false);
    this.granted.update((current) => new Map(current).set(code, value));
  }

  protected async choose(role: Role): Promise<void> {
    this.error.set(null);
    this.saved.set(false);
    this.selected.set(role);
    try {
      const grants = await firstValueFrom(this.api.post<Grant[]>(`identity-service/v1/roles/${role.id}/permissions/get`));
      this.granted.set(new Map(grants.map((grant) => [grant.code, grant.value ?? null])));
    } catch (error) {
      this.granted.set(new Map());
      this.error.set(toApiError(error).text);
    }
  }

  protected async save(): Promise<void> {
    const role = this.selected();
    if (!role) {
      return;
    }
    this.saving.set(true);
    this.error.set(null);
    try {
      const grants = [...this.granted()].map(([code, value]) => ({ code, value: value === null || value === '' ? null : value }));
      await firstValueFrom(this.api.put(`identity-service/v1/roles/${role.id}/permissions`, { grants }));
      this.saved.set(true);
    } catch (error) {
      this.error.set(toApiError(error).text);
    } finally {
      this.saving.set(false);
    }
  }

  protected async create(): Promise<void> {
    if (!this.newName.trim()) {
      return;
    }
    this.error.set(null);
    try {
      await firstValueFrom(this.api.post('identity-service/v1/roles/create', { name: this.newName.trim() }));
      this.newName = '';
      await this.loadRoles();
    } catch (error) {
      this.error.set(toApiError(error).text);
    }
  }

  protected async remove(role: Role): Promise<void> {
    if (!confirm(`${role.name} rolü silinsin mi?`)) {
      return;
    }
    this.error.set(null);
    try {
      await firstValueFrom(this.api.delete(`identity-service/v1/roles/${role.id}`));
      if (this.selected()?.id === role.id) {
        this.selected.set(null);
      }
      await this.loadRoles();
    } catch (error) {
      this.error.set(toApiError(error).text);
    }
  }

  private async loadRoles(): Promise<void> {
    try {
      this.roles.set(await firstValueFrom(this.api.post<Role[]>('identity-service/v1/roles/filter-list', { maxResultCount: 100 })));
    } catch (error) {
      this.roles.set([]);
      this.error.set(toApiError(error).text);
    }
  }

  private async loadCatalogue(): Promise<void> {
    try {
      const all = await firstValueFrom(this.api.post<Permission[]>('identity-service/v1/permissions/filter-list', { maxResultCount: 100 }));
      this.catalogue.set(all.filter((permission) => permission.isActive));
    } catch {
      this.catalogue.set([]);
    }
  }
}
