import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { WalletDto } from '../models/transaction.model';

@Injectable({ providedIn: 'root' })
export class WalletService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.backendUrl;

  getMyWallets(): Observable<WalletDto[]> {
    return this.http.get<WalletDto[]>(`${this.base}/api/v1/wallets`);
  }
}
