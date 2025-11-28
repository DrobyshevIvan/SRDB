import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ProceduresTab } from './procedures-tab/procedures-tab';

type TabKey = 'products' | 'procedure' | 'functions' | 'triggers';

interface TabDescriptor {
  key: TabKey;
  label: string;
  subtitle: string;
}

interface CategorySummary {
  id: number;
  name: string;
  description?: string | null;
}

interface ProductListItem {
  id: number;
  name: string;
  description?: string | null;
  price: number;
  quantity: number;
  sku?: string | null;
  imageUrl?: string | null;
  category: CategorySummary;
}

interface UserSummary {
  id: number;
  userName: string;
  fullName?: string | null;
}

interface OrderSummary {
  id: number;
  orderDate: string;
  totalAmount: number;
  status: string;
  user: UserSummary;
}

interface OrderItemDetail {
  id: number;
  orderId: number;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  order: OrderSummary;
}

interface ProductDetail extends ProductListItem {
  orderItems: OrderItemDetail[];
}

interface OrderListItem {
  id: number;
  orderDate: string;
  totalAmount: number;
  status: string;
  user: UserSummary;
}

interface ExpensiveUserRow {
  userId: number;
  userName: string;
  fullName?: string | null;
}

const API_BASE_URL = 'http://localhost:5244/api';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, ProceduresTab],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  readonly title = signal('MedShop | Admin panel');

  readonly tabs: TabDescriptor[] = [
    { key: 'products', label: 'Товари', subtitle: 'Перегляд каталогу та продажів' },
    { key: 'procedure', label: 'Процедури', subtitle: 'Тестування usp_PurchaseProduct' },
    { key: 'functions', label: 'Функції', subtitle: 'Скалярні та табличні запити' },
    { key: 'triggers', label: 'Тригери', subtitle: 'Контроль виключень з БД' }
  ];

  readonly products = signal<ProductListItem[]>([]);
  readonly productsLoading = signal(false);
  readonly productsError = signal<string | null>(null);

  readonly activeTab = signal<TabKey>('products');
  readonly selectedProductId = signal<number | null>(null);
  readonly selectedProduct = signal<ProductDetail | null>(null);
  readonly selectedProductLoading = signal(false);
  readonly selectedProductError = signal<string | null>(null);

  private readonly http = inject(HttpClient);
  private readonly productDetailsCache = new Map<number, ProductDetail>();

  readonly orders = signal<OrderListItem[]>([]);
  readonly ordersLoading = signal(false);
  readonly ordersError = signal<string | null>(null);

  scalarFunctionPayload = signal({ maxAmount: 10000 });
  scalarFunctionResult = signal<number | null>(null);
  scalarFunctionLoading = signal(false);
  scalarFunctionError = signal<string | null>(null);

  tableFunctionPayload = signal({ minPrice: 5000, categoryId: 1 });
  tableFunctionResult = signal<ExpensiveUserRow[]>([]);
  tableFunctionLoading = signal(false);
  tableFunctionError = signal<string | null>(null);

  triggerTestPayload = signal({ userId: '', orderDate: '' });
  triggerWaiting = signal(false);
  triggerMessage = signal<{ type: 'success' | 'error'; text: string } | null>(null);
  triggerLog = signal<string[]>([
    
  ]);

  ngOnInit(): void {
    this.refreshProducts();
    this.refreshOrders();
    this.executeScalarFunction();
    this.executeTableFunction();
  }

  async refreshProducts(): Promise<void> {
    this.productsLoading.set(true);
    this.productsError.set(null);

    try {
      const response = await firstValueFrom(
        this.http.get<ProductListItem[]>(`${API_BASE_URL}/products`)
      );
      this.products.set(response);

      if (response.length > 0) {
        const firstProductId = response[0].id;
        if (this.selectedProductId() !== firstProductId) {
          await this.selectProduct(firstProductId);
        }
      } else {
        this.selectedProductId.set(null);
        this.selectedProduct.set(null);
      }
    } catch (error) {
      this.productsError.set(this.resolveError(error, 'Не вдалося завантажити товари.'));
      this.products.set([]);
      this.selectedProductId.set(null);
      this.selectedProduct.set(null);
    } finally {
      this.productsLoading.set(false);
    }
  }

  async refreshOrders(): Promise<void> {
    this.ordersLoading.set(true);
    this.ordersError.set(null);

    try {
      const response = await firstValueFrom(this.http.get<OrderListItem[]>(`${API_BASE_URL}/orders`));
      this.orders.set(response);
    } catch (error) {
      this.ordersError.set(this.resolveError(error, 'Не вдалося завантажити замовлення.'));
      this.orders.set([]);
    } finally {
      this.ordersLoading.set(false);
    }
  }

  selectTab(tab: TabKey): void {
    this.activeTab.set(tab);
  }

  async selectProduct(productId: number): Promise<void> {
    if (this.selectedProductId() === productId && this.selectedProduct()) {
      return;
    }

    this.selectedProductId.set(productId);

    if (this.productDetailsCache.has(productId)) {
      this.selectedProduct.set(this.productDetailsCache.get(productId)!);
      this.selectedProductError.set(null);
      return;
    }

    this.selectedProductLoading.set(true);
    this.selectedProductError.set(null);

    try {
      const detail = await firstValueFrom(
        this.http.get<ProductDetail>(`${API_BASE_URL}/products/${productId}`)
      );
      this.productDetailsCache.set(productId, detail);
      this.selectedProduct.set(detail);
    } catch (error) {
      this.selectedProduct.set(null);
      this.selectedProductError.set(
        this.resolveError(error, 'Не вдалося завантажити деталі товару.')
      );
    } finally {
      this.selectedProductLoading.set(false);
    }
  }

  logTriggerMessage(message: string): void {
    this.triggerLog.update((entries) => [message, ...entries]);
  }

  updateTriggerPayload(field: 'userId' | 'orderDate', value: string): void {
    this.triggerTestPayload.update((current) => ({
      ...current,
      [field]: value
    }));
  }

  async simulateTriggerRequest(): Promise<void> {
    const payload = this.triggerTestPayload();
    if (!payload.userId) {
      this.triggerMessage.set({ type: 'error', text: 'UserId є обов’язковим.' });
      return;
    }

    const requestBody = {
      userId: Number(payload.userId),
      orderDate: payload.orderDate ? new Date(payload.orderDate) : undefined
    };

    this.triggerWaiting.set(true);
    this.triggerMessage.set(null);

    try {
      await firstValueFrom(
        this.http.post(`${API_BASE_URL}/orders`, requestBody, { responseType: 'json' })
      );

      this.triggerMessage.set({
        type: 'success',
        text: 'Замовлення створено успішно: тригер не спрацював.'
      });
      this.logTriggerMessage(
        `OK: Замовлення користувача #${requestBody.userId} створено (дата ${
          payload.orderDate || 'сьогодні'
        }).`
      );
      await this.refreshOrders();
    } catch (error) {
      const message = this.resolveError(error, 'Сталася помилка при створенні замовлення.');
      this.triggerMessage.set({ type: 'error', text: message });
      this.logTriggerMessage(`RAISERROR: ${message}`);
    } finally {
      this.triggerWaiting.set(false);
    }
  }

  updateScalarFunctionPayload(value: number): void {
    this.scalarFunctionPayload.update(() => ({ maxAmount: value || 0 }));
  }

  updateTableFunctionPayload(field: 'minPrice' | 'categoryId', value: number): void {
    this.tableFunctionPayload.update((current) => ({
      ...current,
      [field]: value || 0
    }));
  }

  async executeScalarFunction(): Promise<void> {
    const payload = this.scalarFunctionPayload();
    this.scalarFunctionLoading.set(true);
    this.scalarFunctionError.set(null);

    try {
      const response = await firstValueFrom(
        this.http.get<{ count: number }>(`${API_BASE_URL}/products/count-orders`, {
          params: { maxAmount: payload.maxAmount }
        })
      );
      this.scalarFunctionResult.set(response.count);
    } catch (error) {
      this.scalarFunctionError.set(this.resolveError(error, 'Не вдалося отримати значення.'));
      this.scalarFunctionResult.set(null);
    } finally {
      this.scalarFunctionLoading.set(false);
    }
  }

  async executeTableFunction(): Promise<void> {
    const payload = this.tableFunctionPayload();
    this.tableFunctionLoading.set(true);
    this.tableFunctionError.set(null);

    try {
      const response = await firstValueFrom(
        this.http.get<ExpensiveUserRow[]>(`${API_BASE_URL}/products/users-with-expensive-products`, {
          params: { minPrice: payload.minPrice, categoryId: payload.categoryId }
        })
      );
      this.tableFunctionResult.set(response);
    } catch (error) {
      this.tableFunctionError.set(this.resolveError(error, 'Не вдалося отримати список користувачів.'));
      this.tableFunctionResult.set([]);
    } finally {
      this.tableFunctionLoading.set(false);
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
