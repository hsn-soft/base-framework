import { Routes } from '@angular/router';
import { authGuard, menuGuard } from './core/guards';

/**
 * The routes. A page that is a menu item names its item (`data.menu`) and `menuGuard` lets the user in only when the menu the identity service gave it has that item, which is when it has the visibility permission of the page.
 * Home and the forbidden page need nothing but being signed in.
 */
export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./pages/login/login.component').then((m) => m.LoginComponent) },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell.component').then((m) => m.ShellComponent),
    children: [
      { path: '', pathMatch: 'full', loadComponent: () => import('./pages/home/home.component').then((m) => m.HomeComponent) },
      { path: 'forbidden', loadComponent: () => import('./pages/forbidden/forbidden.component').then((m) => m.ForbiddenComponent) },
      {
        path: 'invoice-create',
        canActivate: [menuGuard],
        data: { menu: 'invoice-entry.create' },
        loadComponent: () => import('./pages/invoice-create/invoice-create.component').then((m) => m.InvoiceCreateComponent),
      },
      {
        path: 'invoice-upload',
        canActivate: [menuGuard],
        data: { menu: 'invoice-entry.upload' },
        loadComponent: () => import('./pages/invoice-upload/invoice-upload.component').then((m) => m.InvoiceUploadComponent),
      },
      {
        path: 'queue',
        canActivate: [menuGuard],
        data: { menu: 'invoice-entry.queue' },
        loadComponent: () => import('./pages/queue/queue.component').then((m) => m.QueueComponent),
      },
      {
        path: 'invoice-drafts',
        canActivate: [menuGuard],
        data: { menu: 'invoice-management.drafts' },
        loadComponent: () => import('./pages/invoice-drafts/invoice-drafts.component').then((m) => m.InvoiceDraftsComponent),
      },
      {
        path: 'invoices',
        canActivate: [menuGuard],
        data: { menu: 'invoice-management.invoices' },
        loadComponent: () => import('./pages/invoices/invoices.component').then((m) => m.InvoicesComponent),
      },
      {
        path: 'purchase-invoices',
        canActivate: [menuGuard],
        data: { menu: 'invoice-management.purchase' },
        loadComponent: () => import('./pages/purchase-invoices/purchase-invoices.component').then((m) => m.PurchaseInvoicesComponent),
      },
      {
        path: 'envelopes',
        canActivate: [menuGuard],
        data: { menu: 'integration.envelopes' },
        loadComponent: () => import('./pages/envelopes/envelopes.component').then((m) => m.EnvelopesComponent),
      },
      {
        path: 'invoice-reports',
        canActivate: [menuGuard],
        data: { menu: 'invoice-management.reports' },
        loadComponent: () => import('./pages/invoice-reports/invoice-reports.component').then((m) => m.InvoiceReportsComponent),
      },
      {
        path: 'identity/users',
        canActivate: [menuGuard],
        data: { menu: 'identity-management.users' },
        loadComponent: () => import('./pages/users/users.component').then((m) => m.UsersComponent),
      },
      {
        path: 'identity/roles',
        canActivate: [menuGuard],
        data: { menu: 'identity-management.roles' },
        loadComponent: () => import('./pages/roles/roles.component').then((m) => m.RolesComponent),
      },
      { path: '**', redirectTo: 'forbidden' },
    ],
  },
  { path: '**', redirectTo: '' },
];
