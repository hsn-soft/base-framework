import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { SessionService } from './session.service';

/** Sends the token with every call, and puts the user back at the sign in when the token is not good any more (401) — but not for the sign in itself, whose 401 is a wrong password. */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const session = inject(SessionService);
  const token = auth.token();
  const signed = token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
  return next(signed).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && !request.url.includes('/auth/login')) {
        session.clear();
        auth.logout();
      }
      return throwError(() => error);
    }),
  );
};
