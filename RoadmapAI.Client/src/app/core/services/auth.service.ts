import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, map, Observable, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, RegisterPayload, LoginPayload, User } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly apiUrl = environment.apiUrl;

  // Cookie-based auth: if user exists in memory, session is active
  readonly user = signal<User | null>(null);
  readonly isLoggedIn = computed(() => !!this.user());

  register(payload: RegisterPayload) {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/auth/register`,
      payload
    );
  }

  login(payload: LoginPayload) {
    return this.http.post<AuthResponse>(
      `${this.apiUrl}/auth/login`,
      payload
    );
  }

  getCurrentSession() {
    return this.http.get<AuthResponse>(`${this.apiUrl}/auth/me`);
  }

  // Save user after successful login (JWT is now in HttpOnly cookie)
  handleLoginSuccess(response: AuthResponse): void {
    this.user.set(response.user);
    this.router.navigate(['/']);
  }

  checkSession(): Observable<boolean> {
    if (this.user()) return of(true);

    return this.getCurrentSession().pipe(
      tap((response) => this.user.set(response.user)),
      map(() => true),
      catchError(() => {
        this.user.set(null);
        return of(false);
      })
    );
  }

  logout(): void {
    this.http.post(`${this.apiUrl}/auth/logout`, {}).subscribe({
      next: () => {
        this.user.set(null);
        this.router.navigate(['/login']);
      },
      error: () => {
        this.user.set(null);
        this.router.navigate(['/login']);
      }
    });
  }
}
