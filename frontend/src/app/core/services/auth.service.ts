import { Injectable, signal } from '@angular/core';

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly authenticated = signal(false);

  /** Mock login — mirrors the JSX prototype's 700ms timer. */
  login(_req: LoginRequest): Promise<void> {
    return new Promise((resolve) => {
      setTimeout(() => {
        this.authenticated.set(true);
        resolve();
      }, 700);
    });
  }

  /** Mock register — same 700ms timer. */
  register(_req: RegisterRequest): Promise<void> {
    return new Promise((resolve) => {
      setTimeout(() => {
        this.authenticated.set(true);
        resolve();
      }, 700);
    });
  }

  logout(): void {
    this.authenticated.set(false);
  }
}
