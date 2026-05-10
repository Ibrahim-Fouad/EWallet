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
  transactionId: string;
  amount: number;
  currency: string;
  senderPhoneNumber: string;
  receivedAt: string;
}

export interface TransactionCompletedPayload {
  transactionId: string;
  amount: number;
  currency: string;
  completedAt: string;
}

export interface TransactionFailedPayload {
  transactionId: string;
  failureReason: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}
