import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { ApiService } from '../../../core/services/api.service';
import { StoreCurrencyPipe } from '../../../core/pipes/store-currency.pipe';

/**
 * Admin orders management: lists orders (most recent first) and lets the admin
 * move an order through its fulfilment statuses.
 */
@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [CommonModule, FormsModule, StoreCurrencyPipe],
  templateUrl: './admin-orders.component.html',
  styleUrl: './admin-orders.component.css'
})
export class AdminOrdersComponent implements OnInit {
  orders: any[] = [];
  loading = false;
  error: string | null = null;

  statuses: any[] = [];

  constructor(private admin: AdminService, private api: ApiService) {}

  ngOnInit(): void {
    this.api.get<any[]>('admin/order-statuses').subscribe({ next: rows => this.statuses = rows, error: () => this.error = 'Could not load order statuses.' });
    this.refresh();
  }

  refresh(): void {
    this.loading = true;
    this.admin.getOrders().subscribe({
      next: paged => {
        // The admin orders endpoint returns a paged result { items, ... }.
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
    this.api.put(`admin/orders/${order.id}/status`, { statusId }).subscribe({
      next: () => this.refresh(),
      error: err => { this.error = err?.error?.message || 'Status update failed.'; this.refresh(); }
    });
  }
}