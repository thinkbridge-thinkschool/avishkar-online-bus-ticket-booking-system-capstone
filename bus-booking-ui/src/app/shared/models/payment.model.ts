export type PaymentMethod = 'CreditCard' | 'DebitCard' | 'UPI' | 'NetBanking' | 'Wallet';
export type PaymentStatus = 'Pending' | 'Completed' | 'Failed' | 'Refunded';

export interface Payment {
  paymentId: string;
  bookingId: string;
  amount: number;
  method: PaymentMethod;
  status: PaymentStatus;
  transactionId?: string;
  createdAt: string;
}

export interface CreateOrderRequest {
  bookingId: string;
}

export interface CreateOrderResponse {
  orderId: string;
  amountPaise: number;
  currency: string;
  keyId: string;
}

export interface ProcessPaymentRequest {
  bookingId: string;
  razorpayOrderId: string;
  razorpayPaymentId: string;
  razorpaySignature: string;
}
