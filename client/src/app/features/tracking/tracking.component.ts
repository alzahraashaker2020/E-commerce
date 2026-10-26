import { LocalDatePipe } from '../../core/i18n/format.pipe';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TrackingService } from '../../core/services/tracking.service';
import { OrderTracking } from '../../core/models/api.models';
import { StoreCurrencyPipe } from '../../core/pipes/store-currency.pipe';
import { OrderReturnsComponent } from '../returns/order-returns.component';

/**
 * Order tracking page. Looks up an order by its order number and shows the
 * status, placement date, line items, and total. Prefills the number when the
 * confirmation page links here with a query param.
 */
@Component({
  selector: 'app-tracking',
  standalone: true,
  imports: [LocalDatePipe,TranslatePipe,CommonModule, FormsModule, StoreCurrencyPipe, OrderReturnsComponent],
  templateUrl: './tracking.component.html',
  styleUrl: './tracking.component.css'
})
export class TrackingComponent implements OnInit {
  orderNumber = '';
  accessCode = '';
  /** Which lookup the form is currently using. */

  order: OrderTracking | null = null;
  searched = false;
  loading = false;
  error: string | null = null;

  constructor(private tracking: TrackingService, private route: ActivatedRoute) {}

  ngOnInit(): void {
    const prefill = this.route.snapshot.queryParamMap.get('orderNumber');
    if (prefill) {
      this.orderNumber = prefill;
      this.accessCode = localStorage.getItem('shop_tracking_access:'+window.location.hostname+':'+prefill) || '';
      if (this.accessCode) this.lookup();
    }
  }

  /** Looks up the order by number. */
  lookup(): void {
    const num = this.orderNumber.trim();
    if (!num) {
      return;
    }
    this.loading = true;
    this.order = null;
    this.error = null;
    this.searched = true;
    this.tracking.track(num, this.accessCode.trim()).subscribe({
      next: o => {
        this.order = o;
        this.loading = false;
      },
      error: err => {
        this.order = null;
        this.error = err.status === 429 ? 'Too many tracking attempts. Please wait a minute and try again.' : 'Order unavailable. Sign in to the account that placed it, or enter the private tracking code from your confirmation.';
        this.loading = false;
      }
    });
  }

  /** Maps a status to a badge style. */
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
