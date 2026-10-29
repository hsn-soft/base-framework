import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-forbidden',
  imports: [RouterLink],
  template: `
    <h1>403 - Erişim yok</h1>
    <div class="alert error">Bu sayfayı görüntüleme yetkiniz yoktur.</div>
    <a routerLink="/">Ana sayfaya dön</a>
  `,
})
export class ForbiddenComponent {}
