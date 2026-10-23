import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AccountService } from '../../../core/services/account.service';
import { OrderTracking } from '../../../core/models/api.models';
import { StoreCurrencyPipe } from '../../../core/pipes/store-currency.pipe';

/**
 * The signed-in customer's order history: past and current orders (newest
 * first) with line items, status, and total.
 */
@Component({
  selector: 'app-account-orders',
  standalone: true,
  imports: [CommonModule, RouterLink, StoreCurrencyPipe],
  templateUrl: './account-orders.component.html',
  styleUrl: './account-orders.component.css'
})
export class AccountOrdersComponent implements OnInit {
  orders: OrderTracking[] = [];
  loading = false;
  error: string | null = null;

  constructor(private account: AccountService) {}

  ngOnInit(): void {
    this.loading = true;
    this.account.getOrders().subscribe({
      next: list => {
        this.orders = list;
        this.loading = false;
      },
      error: () => {
        this.error = 'Could not load your orders.';
        this.loading = false;
      }
    });
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Delivered':
      case 'Paid':
        return 'badge-ok';
      case 'Cancelled':
        return 'badge-warn';
      default:
        return 'badge-muted';
    }
  }
}
