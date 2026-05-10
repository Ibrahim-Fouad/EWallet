import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface TokenResponse {
  access_token: string;
  token_type: string;
  expires_in: number;
  refresh_token?: string;
  id_token?: string;
  scope?: string;
}

interface JwtPayload {
  exp: number;
  sub: string;
  email?: string;
  name?: string;
  phone_number?: string;
}

const REFRESH_TOKEN_KEY = 'ewallet_rt';
const CODE_VERIFIER_KEY = 'ewallet_cv';
const STATE_KEY         = 'ewallet_st';
const RETURN_URL_KEY    = 'ewallet_ru';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private _accessToken: string | null = null;
  private _tokenExpiry = 0;
  private _refreshTimer: ReturnType<typeof setTimeout> | null = null;

  private readonly _authState = signal(false);
  readonly authenticated = computed(() => this._authState());

  // ── PKCE Utilities ──────────────────────────────────────────────────────────

  private generateCodeVerifier(): string {
    const bytes = new Uint8Array(32);
    crypto.getRandomValues(bytes);
    return this.base64urlEncode(bytes);
  }

  private async computeCodeChallenge(verifier: string): Promise<string> {
    const encoded = new TextEncoder().encode(verifier);
    const digest = await crypto.subtle.digest('SHA-256', encoded);
    return this.base64urlEncode(new Uint8Array(digest));
  }

  private base64urlEncode(bytes: Uint8Array): string {
    let str = '';
    bytes.forEach((b) => (str += String.fromCharCode(b)));
    return btoa(str).replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
  }

  private generateState(): string {
    const bytes = new Uint8Array(16);
    crypto.getRandomValues(bytes);
    return this.base64urlEncode(bytes);
  }

  // ── Login Initiation ────────────────────────────────────────────────────────

  async initiateLogin(returnUrl?: string): Promise<void> {
    const verifier  = this.generateCodeVerifier();
    const challenge = await this.computeCodeChallenge(verifier);
    const state     = this.generateState();

    sessionStorage.setItem(CODE_VERIFIER_KEY, verifier);
    sessionStorage.setItem(STATE_KEY, state);
    if (returnUrl) sessionStorage.setItem(RETURN_URL_KEY, returnUrl);

    const params = new URLSearchParams({
      response_type:         'code',
      client_id:             environment.oauth.clientId,
      redirect_uri:          environment.oauth.redirectUri,
      scope:                 environment.oauth.scopes,
      state,
      code_challenge:        challenge,
      code_challenge_method: 'S256',
    });

    window.location.href = `${environment.oauth.authorizationEndpoint}?${params}`;
  }

  // ── Callback Handling ───────────────────────────────────────────────────────

  async handleCallback(code: string, returnedState: string): Promise<string> {
    const storedState  = sessionStorage.getItem(STATE_KEY);
    const codeVerifier = sessionStorage.getItem(CODE_VERIFIER_KEY);
    const returnUrl    = sessionStorage.getItem(RETURN_URL_KEY) ?? '/dashboard';

    sessionStorage.removeItem(STATE_KEY);
    sessionStorage.removeItem(CODE_VERIFIER_KEY);
    sessionStorage.removeItem(RETURN_URL_KEY);

    if (!storedState || storedState !== returnedState) {
      throw new Error('OAuth state mismatch — possible CSRF attack');
    }
    if (!codeVerifier) {
      throw new Error('Code verifier missing from session storage');
    }

    const body = new URLSearchParams({
      grant_type:    'authorization_code',
      code,
      redirect_uri:  environment.oauth.redirectUri,
      client_id:     environment.oauth.clientId,
      code_verifier: codeVerifier,
    });

    const tokens = await firstValueFrom(
      this.http.post<TokenResponse>(
        environment.oauth.tokenEndpoint,
        body.toString(),
        { headers: new HttpHeaders({ 'Content-Type': 'application/x-www-form-urlencoded' }) },
      ),
    );

    this.storeTokens(tokens);
    return returnUrl;
  }

  // ── Token Refresh ───────────────────────────────────────────────────────────

  async refreshTokens(): Promise<void> {
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
    if (!refreshToken) throw new Error('No refresh token');

    const body = new URLSearchParams({
      grant_type:    'refresh_token',
      refresh_token: refreshToken,
      client_id:     environment.oauth.clientId,
    });

    try {
      const tokens = await firstValueFrom(
        this.http.post<TokenResponse>(
          environment.oauth.tokenEndpoint,
          body.toString(),
          { headers: new HttpHeaders({ 'Content-Type': 'application/x-www-form-urlencoded' }) },
        ),
      );
      this.storeTokens(tokens);
    } catch {
      this.clearTokens();
      throw new Error('Token refresh failed');
    }
  }

  // ── Session Restore (APP_INITIALIZER) ───────────────────────────────────────

  async restoreSession(): Promise<void> {
    if (!localStorage.getItem(REFRESH_TOKEN_KEY)) return;
    try {
      await this.refreshTokens();
    } catch {
      this.clearTokens();
    }
  }

  // ── Logout ──────────────────────────────────────────────────────────────────

  logout(): void {
    this.clearTokens();
  }

  // ── Token Access ─────────────────────────────────────────────────────────────

  getAccessToken(): string | null {
    return this._accessToken;
  }

  getClaims(): JwtPayload | null {
    if (!this._accessToken) return null;
    return this.parseJwt(this._accessToken);
  }

  isAuthenticated(): boolean {
    return !!this._accessToken && Date.now() / 1000 < this._tokenExpiry;
  }

  // ── Internal Helpers ────────────────────────────────────────────────────────

  private storeTokens(tokens: TokenResponse): void {
    this._accessToken = tokens.access_token;
    const payload = this.parseJwt(tokens.access_token);
    this._tokenExpiry = payload?.exp ?? (Date.now() / 1000 + tokens.expires_in);

    if (tokens.refresh_token) {
      localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refresh_token);
    }

    this._authState.set(true);
    this.scheduleRefresh(this._tokenExpiry);
  }

  private clearTokens(): void {
    this._accessToken = null;
    this._tokenExpiry = 0;
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    this._authState.set(false);
    if (this._refreshTimer) clearTimeout(this._refreshTimer);
  }

  private scheduleRefresh(expUnixSeconds: number): void {
    if (this._refreshTimer) clearTimeout(this._refreshTimer);
    const msUntilRefresh = (expUnixSeconds - Date.now() / 1000 - 60) * 1000;
    if (msUntilRefresh <= 0) {
      void this.refreshTokens();
      return;
    }
    this._refreshTimer = setTimeout(() => void this.refreshTokens(), msUntilRefresh);
  }

  private parseJwt(token: string): JwtPayload | null {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      const payload = atob(parts[1].replace(/-/g, '+').replace(/_/g, '/'));
      return JSON.parse(payload) as JwtPayload;
    } catch {
      return null;
    }
  }
}
