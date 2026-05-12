export interface CreateWalletApiResponse {
  walletId: string;
  phoneNumber: string;
  currency: string;
  balance: number;
}

export interface TransferRequest {
  sourcePhoneNumber: string;
  destinationPhoneNumber: string;
  amount: number;
  notes?: string;
}

export interface TransferResponse {
  transactionId: string;
  status: 'Pending' | 'Completed' | 'Failed';
  amount: number;
  currency: string;
}

export interface WalletDto {
  id: string;
  ownerId: string;
  phoneNumber: string;
  balance: number;
  currency: string;
  isActive: boolean;
  createdAt: string;
}

export interface TransactionDto {
  id: string;
  sourcePhoneNumber: string;
  destinationPhoneNumber: string;
  amount: number;
  currency: string;
  status: 'Pending' | 'Completed' | 'Failed';
  createdAt: string;
  completedAt: string | null;
  description: string;
  notes: string | null;
}

export interface TransferReceivedPayload {
  notificationId: string;
  transactionId: string;
  amount: number;
  currency: string;
  senderPhoneNumber: string;
  receivedAt: string;
}

export interface TransactionCompletedPayload {
  notificationId: string;
  transactionId: string;
  amount: number;
  currency: string;
  completedAt: string;
}

export interface TransactionFailedPayload {
  notificationId: string;
  transactionId: string;
  failureReason: string;
}

export interface PaymentRequestCreatedPayload {
  notificationId: string;
  paymentRequestId: string;
  merchantName: string;
  amount: number;
  currency: string;
  expiresAt: string;
  actionStatus: string;
  createdAt: string;
}

export interface PaymentRequestUpdatedPayload {
  notificationId: string;
  paymentRequestId: string;
  actionStatus: string;
  actionTakenAt: string;
  transactionId: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export type BackendNotificationType =
  | 'TransferReceived'
  | 'TransactionCompleted'
  | 'TransactionFailed'
  | 'PaymentRequestCreated';

export type PaymentRequestStatus =
  | 'Pending'
  | 'Approved'
  | 'Rejected'
  | 'Expired'
  | 'Completed'
  | 'Failed';

export interface NotificationDto {
  id: string;
  type: BackendNotificationType;
  transactionId: string | null;
  amount: number | null;
  currency: string | null;
  senderPhoneNumber: string | null;
  failureReason: string | null;
  completedAt: string | null;
  receivedAt: string | null;
  isRead: boolean;
  createdAt: string;
  paymentRequestId: string | null;
  merchantName: string | null;
  actionStatus: PaymentRequestStatus | null;
  actionTakenAt: string | null;
  expiresAt: string | null;
}
