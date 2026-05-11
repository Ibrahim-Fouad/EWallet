import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { NotificationDto, PagedResult } from '../models/transaction.model';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.backendUrl;

  getHistory(page: number, pageSize: number): Observable<PagedResult<NotificationDto>> {
    return this.http.get<PagedResult<NotificationDto>>(`${this.base}/api/v1/notifications`, {
      params: { page, pageSize },
    });
  }

  markAsRead(id: string): Observable<void> {
    return this.http.put<void>(`${this.base}/api/v1/notifications/${id}/read`, null);
  }

  markAllAsRead(): Observable<void> {
    return this.http.put<void>(`${this.base}/api/v1/notifications/read-all`, null);
  }
}
