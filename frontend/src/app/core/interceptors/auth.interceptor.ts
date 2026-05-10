import { HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { environment } from '../../../environments/environment';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  if (!req.url.startsWith(environment.backendUrl)) {
    return next(req);
  }

  const token = auth.getAccessToken();
  const authedReq = token ? addBearer(req, token) : req;

  return next(authedReq).pipe(
    catchError((err: unknown) => {
      if (!(err instanceof HttpErrorResponse) || err.status !== 401) {
        return throwError(() => err);
      }

      return from(auth.refreshTokens()).pipe(
        switchMap(() => {
          const newToken = auth.getAccessToken();
          return next(newToken ? addBearer(req, newToken) : req);
        }),
        catchError((refreshErr: unknown) => {
          auth.logout();
          return throwError(() => refreshErr);
        }),
      );
    }),
  );
};

function addBearer(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}
