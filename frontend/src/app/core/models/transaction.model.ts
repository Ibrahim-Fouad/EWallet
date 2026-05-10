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
  destinationPhoneNumber: string;
  amount: number;
  currency: string;
  status: 'Pending' | 'Completed' | 'Failed';
  createdAt: string;
  completedAt: string | null;
  description: string;
  notes: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}
