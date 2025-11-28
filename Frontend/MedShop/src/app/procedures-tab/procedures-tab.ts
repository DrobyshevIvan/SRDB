import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

interface PurchaseResponse {
  message: string;
}

interface UserSummary {
  id: number;
  userName: string;
  fullName?: string | null;
}

interface OrderListItem {
  id: number;
  orderDate: string;
  totalAmount: number;
  status: string;
  user: UserSummary;
}

const API_BASE_URL = 'http://localhost:5244/api';

@Component({
  selector: 'app-procedures-tab',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './procedures-tab.html',
  styleUrl: '../app.css',
})
export class ProceduresTab implements OnInit {
  private readonly http = inject(HttpClient);

  procedureFormState = signal({
    productId: '',
    userId: '',
    quantity: 1,
    orderId: ''
  });
  procedurePending = signal(false);
  procedureMessage = signal<{ type: 'success' | 'error'; text: string } | null>(null);

  readonly orders = signal<OrderListItem[]>([]);
  readonly ordersLoading = signal(false);
  readonly ordersError = signal<string | null>(null);

  ngOnInit(): void {
    this.refreshOrders();
  }

  onProcedureFieldChange(field: 'productId' | 'userId' | 'quantity' | 'orderId', value: string | number): void {
    this.procedureFormState.update((current) => ({
      ...current,
      [field]: value
    }));
  }

  async executeProcedure(): Promise<void> {
    const form = this.procedureFormState();
    if (!form.productId || !form.userId || form.quantity <= 0) {
      this.procedureMessage.set({
        type: 'error',
        text: 'ProductId, UserId та Quantity повинні бути заповнені.'
      });
      return;
    }

    const payload = {
      productId: Number(form.productId),
      userId: Number(form.userId),
      quantity: Number(form.quantity),
      orderId: form.orderId ? Number(form.orderId) : null
    };

    this.procedurePending.set(true);
    this.procedureMessage.set(null);

    try {
      const response = await firstValueFrom(
        this.http.post<PurchaseResponse>(`${API_BASE_URL}/products/purchase`, payload)
      );

      this.procedureMessage.set({
        type: 'success',
        text: response?.message || 'Покупку успішно виконано.'
      });

      await this.refreshOrders();
    } catch (error) {
      this.procedureMessage.set({
        type: 'error',
        text: this.resolveError(error, 'Не вдалося виконати процедуру.')
      });
    } finally {
      this.procedurePending.set(false);
    }
  }

  async refreshOrders(): Promise<void> {
    this.ordersLoading.set(true);
    this.ordersError.set(null);

    try {
      const response = await firstValueFrom(
        this.http.get<OrderListItem[]>(`${API_BASE_URL}/orders`)
      );
      this.orders.set(response);
    } catch (error) {
      this.ordersError.set(this.resolveError(error, 'Не вдалося завантажити замовлення.'));
      this.orders.set([]);
    } finally {
      this.ordersLoading.set(false);
    }
  }

  private resolveError(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      if (typeof error.error === 'object' && error.error?.message) {
        return error.error.message;
      }
      return error.message || fallback;
    }

    if (error instanceof Error) {
      return error.message;
    }

    return fallback;
  }
}
