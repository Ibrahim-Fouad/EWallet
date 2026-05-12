import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import {
  PaymentRequestCreatedPayload,
  PaymentRequestUpdatedPayload,
  TransactionCompletedPayload,
  TransactionFailedPayload,
  TransferReceivedPayload,
} from '../models/transaction.model';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly auth = inject(AuthService);

  private connection: signalR.HubConnection | null = null;

  readonly transferReceived$ = new Subject<TransferReceivedPayload>();
  readonly transactionCompleted$ = new Subject<TransactionCompletedPayload>();
  readonly transactionFailed$ = new Subject<TransactionFailedPayload>();
  readonly paymentRequestCreated$ = new Subject<PaymentRequestCreatedPayload>();
  readonly paymentRequestUpdated$ = new Subject<PaymentRequestUpdatedPayload>();
  readonly reconnected$ = new Subject<void>();

  async connect(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) return;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.signalRUrl}/hubs/notifications`, {
        // Backend middleware maps the access_token query param to the Authorization header
        // because WebSocket connections cannot carry custom headers during the handshake.
        accessTokenFactory: () => this.auth.getAccessToken() ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('TransferReceived', (payload: TransferReceivedPayload) => {
      this.transferReceived$.next(payload);
    });

    this.connection.on('TransactionCompleted', (payload: TransactionCompletedPayload) => {
      this.transactionCompleted$.next(payload);
    });

    this.connection.on('TransactionFailed', (payload: TransactionFailedPayload) => {
      this.transactionFailed$.next(payload);
    });

    this.connection.on('PaymentRequestCreated', (payload: PaymentRequestCreatedPayload) => {
      this.paymentRequestCreated$.next(payload);
    });

    this.connection.on('PaymentRequestUpdated', (payload: PaymentRequestUpdatedPayload) => {
      this.paymentRequestUpdated$.next(payload);
    });

    this.connection.onreconnected(() => {
      this.reconnected$.next();
    });

    await this.connection.start();
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }
}
