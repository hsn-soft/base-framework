import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { urlOf } from './config';
import { ApiResponse } from './models';

/** A failed call, as the screen shows it: the status, the messages the service gave and its code. */
export class ApiError {
  constructor(
    public readonly status: number,
    public readonly messages: string[],
    public readonly errorCode?: string,
  ) {}

  get text(): string {
    return this.messages.length > 0 ? this.messages.join(' ') : `Hata (${this.status})`;
  }
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as Partial<ApiResponse<unknown>> | null;
    return new ApiError(error.status, body?.statusMessages ?? [], body?.errorCode);
  }
  return new ApiError(0, [String(error)]);
}

/** The calls to the services (through the gateway, or directly for the two integrations): the JSON of the answer is unwrapped to its payload, the body of a 204 is nothing. */
@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);

  post<T>(path: string, body: unknown = {}): Observable<T> {
    return this.http.post<ApiResponse<T>>(urlOf(path), body).pipe(map((response) => response?.payload as T));
  }

  /** A form with a file (multipart): the browser sets the content type and its boundary. */
  postForm<T>(path: string, form: FormData): Observable<T> {
    return this.http.post<ApiResponse<T>>(urlOf(path), form).pipe(map((response) => response?.payload as T));
  }

  /** A file the service sends back (the answer is the bytes, not the JSON of an answer). */
  getFile(path: string): Observable<Blob> {
    return this.http.get(urlOf(path), { responseType: 'blob' });
  }

  put(path: string, body: unknown): Observable<void> {
    return this.http.put<unknown>(urlOf(path), body).pipe(map(() => undefined));
  }

  delete(path: string): Observable<void> {
    return this.http.delete<unknown>(urlOf(path)).pipe(map(() => undefined));
  }
}
