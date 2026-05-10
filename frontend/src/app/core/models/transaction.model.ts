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
