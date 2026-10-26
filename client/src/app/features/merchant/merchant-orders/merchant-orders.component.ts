import { LocalDatePipe } from '../../../core/i18n/format.pipe';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MerchantService } from '../../../core/services/merchant.service';
import { ApiService } from '../../../core/services/api.service';
import { StoreCurrencyPipe } from '../../../core/pipes/store-currency.pipe';

/**
 * Merchant orders management: lists orders (most recent first) and lets the merchant
 * move an order through its fulfilment statuses.
 */
@Component({
  selector: 'app-merchant-orders',
  standalone: true,
  imports: [LocalDatePipe,TranslatePipe,CommonModule, FormsModule, StoreCurrencyPipe],
  templateUrl: './merchant-orders.component.html',
  styleUrl: './merchant-orders.component.css'
})
export class MerchantOrdersComponent implements OnInit {
  orders: any[] = [];
  loading = false;
  error: string | null = null;

  statuses: any[] = [];

  constructor(private merchant: MerchantService, private api: ApiService) {}

  ngOnInit(): void {
    this.api.get<any[]>('merchant/order-statuses').subscribe({ next: rows => this.statuses = rows, error: () => this.error = 'Could not load order statuses.' });
    this.refresh();
  }

  refresh(): void {
    this.loading = true;
    this.merchant.getOrders().subscribe({
      next: paged => {
        // The merchant orders endpoint returns a paged result { items, ... }.
        this.orders = paged?.items ?? [];
        this.loading = false;
      },
      error: () => {
        this.error = 'Could not load orders.';
        this.loading = false;
      }
    });
  }

  changeStatus(order: any, statusId: number): void {
    this.error = null;
    this.api.put(`merchant/orders/${order.id}/status`, { statusId }).subscribe({
      next: () => this.refresh(),
      error: err => { this.error = err?.error?.message || 'Status update failed.'; this.refresh(); }
    });
  }

  showTrackingCode(order: any): void {
    this.api.get<{trackingToken:string}>(`merchant/orders/${order.id}/tracking-access`).subscribe({
      next: result => order.trackingToken=result.trackingToken,
      error: () => this.error='Could not retrieve the private tracking code.'
    });
  }
}
