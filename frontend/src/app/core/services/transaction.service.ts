import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { PagedResult, TransactionDto, TransferRequest, TransferResponse } from '../models/transaction.model';

@Injectable({ providedIn: 'root' })
export class TransactionService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.backendUrl;

  transfer(req: TransferRequest, idempotencyKey: string): Observable<TransferResponse> {
    return this.http.post<TransferResponse>(`${this.base}/api/v1/transactions/transfer`, req, {
      headers: new HttpHeaders({ 'Idempotency-Key': idempotencyKey }),
    });
  }

  getHistory(phoneNumber: string, page = 1, pageSize = 50): Observable<PagedResult<TransactionDto>> {
    return this.http.get<PagedResult<TransactionDto>>(
      `${this.base}/api/v1/wallets/${encodeURIComponent(phoneNumber)}/transactions`,
      { params: { page, pageSize } },
    );
  }
}
