import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
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

  /** The status options an admin can set (mirrors the backend OrderStatus). */
  readonly statuses = [
    'Pending',
    'Confirmed',
    'Paid',
    'Processing',
    'ReadyToShip',
    'Shipped',
    'Delivered',
    'Cancelled'
  ];

  constructor(private admin: AdminService) {}

  ngOnInit(): void {
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

  /** Persists a status change for an order. */
  changeStatus(order: any, status: string): void {
    this.admin.updateOrderStatus(order.id, status).subscribe({
      next: () => (order.status = status),
      error: () => (this.error = 'Status update failed.')
    });
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Delivered':
      case 'Paid':
        return 'badge-ok';
      case 'Confirmed':
      case 'Processing':
      case 'ReadyToShip':
      case 'Shipped':
        return 'badge-info';
      case 'Cancelled':
        return 'badge-warn';
      default:
        return 'badge-muted';
    }
  }
}
